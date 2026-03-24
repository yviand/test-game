using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class GoblinController : MonoBehaviour
{
    private enum GoblinState { Idle, Wandering, Chasing, Attacking, Dead }

    [Header("Combat")]
    [SerializeField] private int attackDamage = 1;
    [SerializeField] private float chaseBuffer = 0.2f;

    [Header("Ranges")]
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float attackRange = 1.2f;
    [SerializeField] private float attackRangeBuffer = 0.25f;
    [SerializeField] private float wanderRange = 3f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float destinationTolerance = 0.15f;
    [SerializeField] private float repathThreshold = 0.25f;

    [Header("Attack Timing")]
    [SerializeField] private float attackCooldown = 3f;
    [SerializeField] private float attackStateDuration = 0.8f;

    [Header("Wander Timing")]
    [SerializeField] private float minWanderDelay = 1.5f;
    [SerializeField] private float maxWanderDelay = 3.5f;

    [Header("Animation")]
    [SerializeField] private float movementSpeedThreshold = 0.1f;

    [Header("Death")]
    [SerializeField] private float cleanupDelay = 2f;

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

    private GoblinState currentState = GoblinState.Idle;
    private float nextAttackTime;
    private float attackStateEndTime;
    private float nextWanderTime;
    private bool isDead;
    private bool hasDestination;
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
            agent.stoppingDistance = attackRange * 1.1f;
            agent.obstacleAvoidanceType = ObstacleAvoidanceType.HighQualityObstacleAvoidance;
        }

        if (mobStats == null)
        {
            Debug.LogError($"{nameof(GoblinController)} on {name} requires a {nameof(MobStats)} component.", this);
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
        if (mobStats != null)
        {
            mobStats.OnDied += HandleMobDied;
        }
    }

    private void OnDisable()
    {
        if (mobStats != null)
        {
            mobStats.OnDied -= HandleMobDied;
        }
    }

    private void Update()
    {
        switch (currentState)
        {
            case GoblinState.Dead:
                UpdateAnimations();
                return;

            case GoblinState.Attacking:
                UpdateAttackingState();
                UpdateAnimations();
                return;

            case GoblinState.Idle:
            case GoblinState.Wandering:
            case GoblinState.Chasing:
                if (isDead)
                {
                    currentState = GoblinState.Dead;
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
        
        // MẸO: Mở rộng tầm kích hoạt đòn đánh một chút xíu khi đang có gia tốc Chasing 
        // để bù trừ cho độ trễ hãm phanh của NavMeshAgent.
        float effectiveAttackRange = (currentState == GoblinState.Chasing) 
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
            case GoblinState.Idle:
                if (playerDetected)
                {
                    // Loại bỏ vùng mù chaseBuffer. Đã thấy player và chưa thể attack -> Chase luôn.
                    SetState(GoblinState.Chasing);
                    MoveTo(playerTransform.position);
                }
                else if (Time.time >= nextWanderTime)
                {
                    BeginWander();
                }
                break;

            case GoblinState.Wandering:
                if (playerDetected)
                {
                    SetState(GoblinState.Chasing);
                    MoveTo(playerTransform.position);
                }
                else if (HasReachedDestination())
                {
                    SetState(GoblinState.Idle);
                    ScheduleNextWander();
                }
                break;

            case GoblinState.Chasing:
                if (!playerDetected)
                {
                    SetState(GoblinState.Idle);
                    StopMovement();
                    ScheduleNextWander();
                }
                else if (distanceToPlayer > attackRange)
                {
                    // Tiếp tục áp sát cho tới khi lọt hẳn vào attackRange
                    MoveTo(playerTransform.position);
                }
                else
                {
                    // Đã vào đủ gần nhưng đang đợi Cooldown đòn đánh
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
            case GoblinState.Chasing:
            case GoblinState.Attacking:
                SetState(GoblinState.Idle);
                StopMovement();
                ScheduleNextWander();
                break;

            case GoblinState.Wandering:
                if (HasReachedDestination())
                {
                    SetState(GoblinState.Idle);
                    ScheduleNextWander();
                }
                break;

            case GoblinState.Idle:
                StopMovement();
                if (Time.time >= nextWanderTime)
                {
                    BeginWander();
                }
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

        if (HasUsableAgent())
        {
            if (currentState != GoblinState.Attacking)
            {
                agent.isStopped = false;
                if (shouldRepath)
                {
                    agent.SetDestination(target);
                    lastRequestedDestination = target;
                }
                else if (agent.remainingDistance <= agent.stoppingDistance + destinationTolerance)
                {
                    agent.SetDestination(target);
                    lastRequestedDestination = target;
                }
            }
        }

        FaceTarget(target);
    }

    private void BeginWander()
    {
        Vector2 offset = Random.insideUnitCircle * wanderRange;
        Vector3 target = spawnPoint + new Vector3(offset.x, offset.y, 0f);
        SetState(GoblinState.Wandering);
        MoveTo(target);
    }

    private void StartAttack()
    {
        if (isDead || !HasLivingPlayerTarget())
        {
            return;
        }

        SetState(GoblinState.Attacking);
        // StopMovement();
        FaceTarget(playerTransform.position);

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
        if (isDead)
        {
            return;
        }

        if (!TryGetDistanceToPlayer(out float distanceToPlayer))
        {
            SetState(GoblinState.Idle);
            ScheduleNextWander();
            return;
        }

        if (distanceToPlayer <= attackRange && Time.time >= nextAttackTime)
        {
            StartAttack();
            return;
        }

        if (distanceToPlayer <= detectionRange)
        {
            // Loại bỏ chaseBuffer ở đây để tránh việc Goblin rơi vào Idle 
            // ngớ ngẩn khi player vừa lùi ra khỏi tầm đánh một chút xíu.
            if (distanceToPlayer > attackRange)
            {
                SetState(GoblinState.Chasing);
                MoveTo(playerTransform.position);
            }
            else
            {
                SetState(GoblinState.Idle);
                StopMovement();
                FaceTarget(playerTransform.position);
            }

            return;
        }

        SetState(GoblinState.Idle);
        ScheduleNextWander();
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0 || mobStats == null)
        {
            return;
        }

        ValidateAliveCollisionState();
        mobStats.TakeDamage(damage);

        if (isDead || mobStats.IsDead)
        {
            return;
        }

        if (currentState == GoblinState.Attacking)
        {
            return;
        }

        StopMovement();

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Hurt");
        }

        if (TryGetDistanceToPlayer(out float distanceToPlayer) && distanceToPlayer <= detectionRange)
        {
            SetState(GoblinState.Chasing);
            MoveTo(playerTransform.position);
            return;
        }

        SetState(GoblinState.Idle);
        ScheduleNextWander();
    }

    public void ExecuteAttackDamage()
    {
        if (isDead || !HasLivingPlayerTarget())
        {
            return;
        }

        float distanceToPlayer = Vector2.Distance(transform.position, playerTransform.position);
        Vector2 facingDirection = spriteRenderer != null && spriteRenderer.flipX ? Vector2.left : Vector2.right;
        Vector2 directionToPlayer = (playerTransform.position - transform.position).normalized;
        float dotProduct = Vector2.Dot(facingDirection, directionToPlayer);

        if (distanceToPlayer <= attackRange + attackRangeBuffer && dotProduct > 0f)
        {
            playerStats.TakeDamage(attackDamage);
        }
    }

    private void HandleMobDied()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentState = GoblinState.Dead;
        StopMovement();

        if (agent != null)
        {
            agent.enabled = false;
        }

        foreach (Collider2D col in hitColliders)
        {
            if (col != null)
            {
                col.enabled = false;
            }
        }

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.ResetTrigger("Hurt");
            animator.SetFloat("Speed", 0f);
            animator.SetBool("isDead", true);
        }

        if (enemyDrop != null)
        {
            enemyDrop.Drop();
        }

        if (cleanupCoroutine == null)
        {
            cleanupCoroutine = StartCoroutine(CleanupRoutine());
        }
    }

    private IEnumerator CleanupRoutine()
    {
        yield return new WaitForSeconds(cleanupDelay);
        Destroy(gameObject);
    }

    private void UpdateAnimations()
    {
        if (animator == null)
        {
            return;
        }

        if (currentState == GoblinState.Idle || currentState == GoblinState.Attacking || currentState == GoblinState.Dead)
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

    private void FaceTarget(Vector3 target)
    {
        if (spriteRenderer == null)
        {
            return;
        }

        spriteRenderer.flipX = target.x < transform.position.x;
    }

    private void SetState(GoblinState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        if (newState == GoblinState.Idle || newState == GoblinState.Attacking || newState == GoblinState.Dead)
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
        if (!hasDestination)
        {
            return true;
        }

        if (HasUsableAgent())
        {
            return !agent.pathPending && agent.remainingDistance <= Mathf.Max(agent.stoppingDistance, destinationTolerance);
        }

        return Vector2.Distance(transform.position, currentDestination) <= destinationTolerance;
    }

    private void CacheColliderLayerStates()
    {
        colliderLayerStates.Clear();

        foreach (Collider2D col in hitColliders)
        {
            if (col == null)
            {
                continue;
            }

            colliderLayerStates.Add(new ColliderLayerState
            {
                Collider = col,
                Layer = col.gameObject.layer
            });
        }
    }

    private bool HasUsableAgent()
    {
        return agent != null && agent.enabled && agent.isOnNavMesh;
    }

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
    }

    private void ValidateAliveCollisionState()
    {
        if (isDead)
        {
            return;
        }

        foreach (ColliderLayerState state in colliderLayerStates)
        {
            if (state.Collider == null)
            {
                continue;
            }

            if (!state.Collider.enabled)
            {
                state.Collider.enabled = true;
            }

            if (state.Collider.gameObject.layer != state.Layer)
            {
                state.Collider.gameObject.layer = state.Layer;
            }
        }
    }

    private void UpdateMovementVelocity()
    {
        if (currentState == GoblinState.Idle || currentState == GoblinState.Attacking || currentState == GoblinState.Dead)
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
            Vector3 nextPosition = Vector3.MoveTowards(
                transform.position,
                currentDestination,
                moveSpeed * Time.deltaTime
            );

            FaceTarget(currentDestination);
            transform.position = nextPosition;

            if (HasReachedDestination())
            {
                hasDestination = false;
            }
        }

        movementVelocity = (transform.position - lastFramePosition) / Mathf.Max(Time.deltaTime, 0.0001f);
        lastFramePosition = transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange + attackRangeBuffer);

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        if (hasDestination)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawLine(transform.position, currentDestination);
            Gizmos.DrawCube(currentDestination, Vector3.one * 0.2f);
        }
    }
}
