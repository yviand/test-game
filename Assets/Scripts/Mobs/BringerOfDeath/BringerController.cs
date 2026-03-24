using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class BringerController : MonoBehaviour
{
    private enum BossState { Idle, Wandering, Chasing, Attacking, Dead }

    [Header("Combat & Hitbox")]
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private float chaseBuffer = 0.2f;
    [SerializeField] private Transform attackPoint; // Gắn 1 object con (Empty) vào đây
    [SerializeField] private float attackHitboxRadius = 1.5f;
    [SerializeField] private LayerMask playerLayer; // Đặt layer của Player

    [Header("Visual Orientation")]
    [Tooltip("Tick nếu frame gốc của Boss đang quay mặt sang phải, bỏ tick nếu quay sang trái")]
    [SerializeField] private bool initialFacingRight = false; 

    [Header("Ranges")]
    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackRangeBuffer = 0.5f;
    [SerializeField] private float wanderRange = 3f;
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float destinationTolerance = 0.15f;
    [SerializeField] private float repathThreshold = 0.25f;

    [Header("Attack Timing")]
    [SerializeField] private float attackCooldown = 2.5f;
    [SerializeField] private float attackStateDuration = 1.2f;

    [Header("Wander Timing")]
    [SerializeField] private float minWanderDelay = 2f;
    [SerializeField] private float maxWanderDelay = 4f;

    [Header("Animation")]
    [SerializeField] private float movementSpeedThreshold = 0.1f;

    [Header("Death")]
    [SerializeField] private float cleanupDelay = 3f;

    private Animator animator;
    private NavMeshAgent agent;
    private SpriteRenderer spriteRenderer;
    private EnemyDrop enemyDrop;
    private MobStats mobStats;
    private Transform playerTransform;
    private PlayerStats playerStats;
    private Collider2D[] hitColliders;
    private readonly List<ColliderLayerState> colliderLayerStates = new();
    
    private Vector3 spawnPoint;
    private Vector3 currentDestination;
    private Vector3 lastRequestedDestination;
    private Vector3 lastFramePosition;
    private Vector3 movementVelocity;
    private Coroutine cleanupCoroutine;

    private BossState currentState = BossState.Idle;
    private float nextAttackTime;
    private float attackStateEndTime;
    private float nextWanderTime;
    private bool isDead;
    private bool hasDestination;
    private bool hasAppliedDamageThisAttack;
    private float repathThresholdSqr;

    private struct ColliderLayerState
    {
        public Collider2D Collider;
        public int Layer;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        agent = GetComponent<NavMeshAgent>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyDrop = GetComponent<EnemyDrop>();
        mobStats = GetComponent<MobStats>();
        hitColliders = GetComponentsInChildren<Collider2D>(true);
        spawnPoint = transform.position;
        currentDestination = transform.position;
        lastRequestedDestination = transform.position;
        lastFramePosition = transform.position;
        repathThresholdSqr = repathThreshold * repathThreshold;
        
        CacheColliderLayerStates();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.speed = moveSpeed;
            // Boss to nên dừng đúng mốc attackRange để tránh húc đẩy Player
            agent.stoppingDistance = attackRange * 1.1f; 
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        if (mobStats == null)
        {
            Debug.LogError($"{nameof(BringerController)} on {name} requires a {nameof(MobStats)} component.", this);
        }

        GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
        if (playerObject != null)
        {
            playerTransform = playerObject.transform;
            playerStats = playerObject.GetComponent<PlayerStats>();
        }

        ScheduleNextWander();
    }

    private void OnEnable()
    {
        if (mobStats != null) mobStats.OnDied += HandleMobDied;
    }

    private void OnDisable()
    {
        if (mobStats != null) mobStats.OnDied -= HandleMobDied;
    }

    private void Update()
    {
        switch (currentState)
        {
            case BossState.Dead:
                UpdateAnimations();
                return;

            case BossState.Attacking:
                UpdateAttackingState();
                UpdateAnimations();
                return;

            case BossState.Idle:
            case BossState.Wandering:
            case BossState.Chasing:
                if (isDead)
                {
                    currentState = BossState.Dead;
                    UpdateAnimations();
                    return;
                }

                ValidateAliveCollisionState();
                HandleLogic();
                UpdateMovementVelocity();
                UpdateAnimations();
                return;
        }
    }

    private void HandleLogic()
    {
        if (!TryGetDistanceToPlayer(out float distanceToPlayer))
        {
            HandleNoPlayerTarget();
            return;
        }

        bool playerDetected = distanceToPlayer <= detectionRange;
        
        float effectiveAttackRange = (currentState == BossState.Chasing) 
            ? attackRange + (attackRangeBuffer * 0.5f) 
            : attackRange;
            
        bool canAttack = distanceToPlayer <= effectiveAttackRange && Time.time >= nextAttackTime;

        if (canAttack)
        {
            StartAttack();
            return;
        }

        switch (currentState)
        {
            case BossState.Idle:
                if (playerDetected)
                {
                    SetState(BossState.Chasing);
                    MoveTo(playerTransform.position);
                }
                else if (Time.time >= nextWanderTime)
                {
                    BeginWander();
                }
                break;

            case BossState.Wandering:
                if (playerDetected)
                {
                    SetState(BossState.Chasing);
                    MoveTo(playerTransform.position);
                }
                else if (HasReachedDestination())
                {
                    SetState(BossState.Idle);
                    ScheduleNextWander();
                }
                break;

            case BossState.Chasing:
                if (!playerDetected)
                {
                    SetState(BossState.Idle);
                    StopMovement();
                    ScheduleNextWander();
                }
                else if (distanceToPlayer > attackRange)
                {
                    MoveTo(playerTransform.position);
                }
                else
                {
                    StopMovement();
                    FaceTarget(playerTransform.position);
                }
                break;
        }
    }

    private void HandleNoPlayerTarget()
    {
        switch (currentState)
        {
            case BossState.Chasing:
            case BossState.Attacking:
                SetState(BossState.Idle);
                StopMovement();
                ScheduleNextWander();
                break;

            case BossState.Wandering:
                if (HasReachedDestination())
                {
                    SetState(BossState.Idle);
                    ScheduleNextWander();
                }
                break;

            case BossState.Idle:
                StopMovement();
                if (Time.time >= nextWanderTime) BeginWander();
                break;
        }
    }

    private void UpdateAttackingState()
    {
        StopMovement();

        if (HasLivingPlayerTarget())
        {
            FaceTarget(playerTransform.position);
        }

        if (Time.time >= attackStateEndTime)
        {
            FinishAttack();
        }
    }

    private void MoveTo(Vector3 target)
    {
        target.z = transform.position.z;
        bool shouldRepath = !hasDestination || (target - lastRequestedDestination).sqrMagnitude >= repathThresholdSqr;

        currentDestination = target;
        hasDestination = true;

        if (HasUsableAgent() && currentState != BossState.Attacking)
        {
            agent.isStopped = false;
            if (shouldRepath || agent.remainingDistance <= agent.stoppingDistance + destinationTolerance)
            {
                agent.SetDestination(target);
                lastRequestedDestination = target;
            }
        }

        FaceTarget(target);
    }

    private void BeginWander()
    {
        Vector2 offset = Random.insideUnitCircle * wanderRange;
        Vector3 target = spawnPoint + new Vector3(offset.x, offset.y, 0f);
        SetState(BossState.Wandering);
        MoveTo(target);
    }

    private void StartAttack()
    {
        if (isDead || !HasLivingPlayerTarget()) return;

        SetState(BossState.Attacking);
        StopMovement();
        FaceTarget(playerTransform.position);
        hasAppliedDamageThisAttack = false;

        nextAttackTime = Time.time + attackCooldown;
        attackStateEndTime = Time.time + attackStateDuration;

        if (animator != null)
        {
            animator.SetFloat("Speed", 0f);
            animator.ResetTrigger("Hurt");
            animator.SetTrigger("Attack");
        }
    }

    public void FinishAttack()
    {
        if (isDead) return;

        if (!TryGetDistanceToPlayer(out float distanceToPlayer))
        {
            SetState(BossState.Idle);
            ScheduleNextWander();
            return;
        }

        if (distanceToPlayer <= detectionRange)
        {
            if (distanceToPlayer > attackRange)
            {
                SetState(BossState.Chasing);
                MoveTo(playerTransform.position);
            }
            else
            {
                SetState(BossState.Idle);
                StopMovement();
                FaceTarget(playerTransform.position);
            }
            return;
        }

        SetState(BossState.Idle);
        ScheduleNextWander();
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0 || mobStats == null) return;

        ValidateAliveCollisionState();
        mobStats.TakeDamage(damage);

        if (isDead || mobStats.IsDead || currentState == BossState.Attacking) return;

        StopMovement();

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Hurt");
        }

        if (TryGetDistanceToPlayer(out float distanceToPlayer) && distanceToPlayer <= detectionRange)
        {
            SetState(BossState.Chasing);
            MoveTo(playerTransform.position);
            return;
        }

        SetState(BossState.Idle);
        ScheduleNextWander();
    }

    // ==== HITBOX LOGIC MỚI ====
    // Gọi hàm này bằng Animation Event tại đúng frame vung vũ khí trúng mục tiêu
    public void ExecuteAttackDamage()
    {
        if (isDead || currentState != BossState.Attacking || attackPoint == null || hasAppliedDamageThisAttack) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackHitboxRadius, playerLayer);
        
        for (int i = 0; i < hits.Length; i++)
        {
            PlayerStats pStats = hits[i].GetComponentInParent<PlayerStats>();
            if (pStats != null)
            {
                pStats.TakeDamage(attackDamage);
                hasAppliedDamageThisAttack = true;
                break;
            }
        }
    }

    private void HandleMobDied()
    {
        if (isDead) return;

        isDead = true;
        currentState = BossState.Dead;
        StopMovement();

        if (agent != null) agent.enabled = false;

        foreach (Collider2D col in hitColliders)
        {
            if (col != null) col.enabled = false;
        }

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Hurt");
            animator.SetFloat("Speed", 0f);
            animator.SetBool("isDead", true);
        }

        if (enemyDrop != null) enemyDrop.Drop();
        if (cleanupCoroutine == null) cleanupCoroutine = StartCoroutine(CleanupRoutine());
    }

    private IEnumerator CleanupRoutine()
    {
        yield return new WaitForSeconds(cleanupDelay);
        Destroy(gameObject);
    }

    private void UpdateAnimations()
    {
        if (animator == null) return;

        if (currentState == BossState.Idle || currentState == BossState.Attacking || currentState == BossState.Dead)
        {
            animator.SetFloat("Speed", 0f);
            return;
        }

        float speed = 0f;
        if (HasUsableAgent() && !agent.isStopped && agent.velocity.sqrMagnitude > movementSpeedThreshold)
        {
            speed = agent.velocity.magnitude;
        }
        else if (!HasUsableAgent() && movementVelocity.sqrMagnitude > movementSpeedThreshold)
        {
            speed = movementVelocity.magnitude;
        }

        animator.SetFloat("Speed", speed);
    }

    // ==== FLIP LOGIC MỚI ====
    private void FaceTarget(Vector3 target)
    {
        if (spriteRenderer == null) return;

        bool targetIsLeft = target.x < transform.position.x;

        // Đảo ngược logic phụ thuộc vào việc Boss ban đầu vẽ quay mặt đi đâu
        if (initialFacingRight)
        {
            spriteRenderer.flipX = targetIsLeft;
            
            // Xoay Hitbox theo hướng mặt (Local Position của Hitbox)
            if (attackPoint != null) 
                attackPoint.localPosition = new Vector3(targetIsLeft ? -Mathf.Abs(attackPoint.localPosition.x) : Mathf.Abs(attackPoint.localPosition.x), attackPoint.localPosition.y, 0f);
        }
        else
        {
            spriteRenderer.flipX = !targetIsLeft;
            
            // Xoay Hitbox theo hướng mặt
            if (attackPoint != null) 
                attackPoint.localPosition = new Vector3(targetIsLeft ? -Mathf.Abs(attackPoint.localPosition.x) : Mathf.Abs(attackPoint.localPosition.x), attackPoint.localPosition.y, 0f);
        }
    }

    private void SetState(BossState newState)
    {
        if (currentState == newState) return;

        currentState = newState;
        if (newState == BossState.Idle || newState == BossState.Attacking || newState == BossState.Dead)
        {
            StopMovement();
        }
    }

    private void ScheduleNextWander()
    {
        nextWanderTime = Time.time + Random.Range(minWanderDelay, maxWanderDelay);
    }

    private void StopMovement()
    {
        hasDestination = false;
        movementVelocity = Vector3.zero;
        currentDestination = transform.position;
        lastRequestedDestination = transform.position;

        if (HasUsableAgent())
        {
            agent.isStopped = true;
            agent.ResetPath();
            agent.velocity = Vector3.zero;
        }
    }

    private bool HasReachedDestination()
    {
        if (!hasDestination) return true;
        if (HasUsableAgent()) return !agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, destinationTolerance);
        return Vector2.Distance(transform.position, currentDestination) <= destinationTolerance;
    }

    private void CacheColliderLayerStates()
    {
        colliderLayerStates.Clear();
        foreach (Collider2D col in hitColliders)
        {
            if (col == null) continue;
            colliderLayerStates.Add(new ColliderLayerState { Collider = col, Layer = col.gameObject.layer });
        }
    }

    private bool HasUsableAgent() => agent != null && agent.enabled && agent.isOnNavMesh;

    private bool HasLivingPlayerTarget()
    {
        RefreshPlayerTargetReference();
        return playerTransform != null && playerStats != null && !playerStats.IsDead;
    }

    private bool TryGetDistanceToPlayer(out float distanceToPlayer)
    {
        if (!HasLivingPlayerTarget())
        {
            distanceToPlayer = float.MaxValue;
            return false;
        }

        distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        return true;
    }

    private void RefreshPlayerTargetReference()
    {
        if (playerStats != null && playerTransform != null && !playerStats.IsDead)
        {
            return;
        }

        playerStats = PlayerStats.Instance;
        playerTransform = playerStats != null ? playerStats.transform : null;

        if (playerTransform == null)
        {
            GameObject playerObject = GameObject.FindGameObjectWithTag("Player");
            if (playerObject != null)
            {
                playerTransform = playerObject.transform;
                playerStats = playerObject.GetComponent<PlayerStats>();
            }
        }
    }

    private void ValidateAliveCollisionState()
    {
        if (isDead) return;
        foreach (ColliderLayerState state in colliderLayerStates)
        {
            if (state.Collider == null) continue;
            if (!state.Collider.enabled) state.Collider.enabled = true;
            if (state.Collider.gameObject.layer != state.Layer) state.Collider.gameObject.layer = state.Layer;
        }
    }

    private void UpdateMovementVelocity()
    {
        if (currentState == BossState.Idle || currentState == BossState.Attacking || currentState == BossState.Dead)
        {
            movementVelocity = Vector3.zero;
            lastFramePosition = transform.position;
            return;
        }

        if (HasUsableAgent())
        {
            movementVelocity = agent.velocity;
            lastFramePosition = transform.position;
            return;
        }

        if (hasDestination)
        {
            Vector3 nextPosition = Vector3.MoveTowards(transform.position, currentDestination, moveSpeed * Time.deltaTime);
            FaceTarget(currentDestination);
            transform.position = nextPosition;

            if (HasReachedDestination()) hasDestination = false;
        }

        movementVelocity = (transform.position - lastFramePosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastFramePosition = transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Hiển thị phạm vi Hitbox mới
        if (attackPoint != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(attackPoint.position, attackHitboxRadius);
            Gizmos.DrawLine(transform.position, attackPoint.position);
            Gizmos.DrawSphere(attackPoint.position, 0.08f);
        }
    }
}
