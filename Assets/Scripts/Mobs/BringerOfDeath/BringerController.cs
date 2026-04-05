using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class BringerController : MonoBehaviour
{
    private enum BossState { Idle, Patrolling, Chasing, Attacking, Dead }

    [Header("Combat & Hitbox")]
    [SerializeField] private int attackDamage = 15;
    [SerializeField] private Transform attackPoint;
    [SerializeField] private float attackHitboxRadius = 1.5f;
    [SerializeField] private LayerMask playerLayer;

    [Header("Visual Orientation")]
    [SerializeField] private bool initialFacingRight = false;

    [Header("Ranges")]
    [SerializeField] private float detectionRange = 7f;
    [SerializeField] private float detectionHeightTolerance = 2.5f;
    [SerializeField] private float attackRange = 2f;
    [SerializeField] private float attackRangeBuffer = 0.5f;
    [SerializeField] private float attackHeightTolerance = 1.5f;
    [SerializeField] private float moveSpeed = 3f;

    [Header("Attack Timing")]
    [SerializeField] private float attackCooldown = 2.5f;
    [SerializeField] private float attackStateDuration = 1.2f;

    [Header("Patrol Timing")]
    [SerializeField] private float minPatrolDelay = 2f;
    [SerializeField] private float maxPatrolDelay = 4f;

    [Header("Environment Detection")]
    [SerializeField] private LayerMask environmentLayer = 1;
    [SerializeField] private Transform wallCheckPoint;
    [SerializeField] private Vector2 wallCheckOffset = new(0.6f, 0.1f);
    [SerializeField] private float wallCheckDistance = 0.25f;
    [SerializeField] private Transform ledgeCheckPoint;
    [SerializeField] private Vector2 ledgeCheckOffset = new(0.55f, -0.15f);
    [SerializeField] private float ledgeCheckDistance = 0.95f;
    [SerializeField] private float ledgeForwardWeight = 0.25f;
    [SerializeField] private float environmentCheckInterval = 0.08f;
    [SerializeField] private bool allowCliffDrop = false;
    [SerializeField] private float groundNormalThreshold = 0.7f;

    [Header("Animation")]
    [SerializeField] private float movementSpeedThreshold = 0.1f;

    [Header("Death")]
    [SerializeField] private float cleanupDelay = 3f;

    private readonly HashSet<int> groundedColliderIds = new();
    private readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[8];
    private readonly List<ColliderLayerState> colliderLayerStates = new();

    private Animator animator;
    private Rigidbody2D rb;
    private SpriteRenderer spriteRenderer;
    private EnemyDrop enemyDrop;
    private MobStats mobStats;
    private Transform playerTransform;
    private PlayerStats playerStats;
    private Collider2D[] hitColliders;
    private Coroutine cleanupCoroutine;
    private Vector3 originalAttackPointLocalPosition;

    private BossState currentState = BossState.Idle;
    private float nextAttackTime;
    private float attackStateEndTime;
    private float nextPatrolTime;
    private float nextEnvironmentCheckTime;
    private float horizontalIntent;
    private int facingDirection;
    private bool cachedWallAhead;
    private bool cachedGroundAhead = true;
    private bool hasAppliedDamageThisAttack;
    private bool isDead;

    private struct ColliderLayerState
    {
        public Collider2D Collider;
        public int Layer;
    }

    public bool IsGrounded => groundedColliderIds.Count > 0;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        enemyDrop = GetComponent<EnemyDrop>();
        mobStats = GetComponent<MobStats>();
        hitColliders = GetComponentsInChildren<Collider2D>(true);

        if (attackPoint != null)
        {
            originalAttackPointLocalPosition = attackPoint.localPosition;
        }

        CacheColliderLayerStates();
        SetFacingDirection(initialFacingRight ? 1 : -1, true);

        if (mobStats == null)
        {
            Debug.LogError($"{nameof(BringerController)} on {name} requires a {nameof(MobStats)} component.", this);
        }

        RefreshPlayerTargetReference();
        ScheduleNextPatrol();
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
        if (currentState == BossState.Dead)
        {
            UpdateAnimations();
            return;
        }

        ValidateAliveCollisionState();
        RefreshPlayerTargetReference();
        RefreshEnvironmentCacheIfNeeded();

        if (currentState == BossState.Attacking)
        {
            UpdateAttackingState();
        }
        else
        {
            HandleStateLogic();
        }

        UpdateAnimations();
    }

    private void FixedUpdate()
    {
        if (rb == null)
        {
            return;
        }

        Vector2 velocity = rb.linearVelocity;

        if (currentState == BossState.Dead || currentState == BossState.Idle || currentState == BossState.Attacking)
        {
            velocity.x = 0f;
        }
        else
        {
            velocity.x = horizontalIntent * moveSpeed;
        }

        rb.linearVelocity = velocity;
    }

    private void HandleStateLogic()
    {
        horizontalIntent = 0f;
        bool hasPlayerDelta = TryGetPlayerDelta(out Vector2 playerDelta);
        bool playerDetected = hasPlayerDelta && CanDetectPlayer(playerDelta);
        bool playerInAttackRange = hasPlayerDelta && CanAttackPlayer(playerDelta);

        if (playerInAttackRange && Time.time >= nextAttackTime)
        {
            StartAttack(playerDelta);
            return;
        }

        switch (currentState)
        {
            case BossState.Idle:
                if (hasPlayerDelta && Mathf.Abs(playerDelta.x) > 0.01f)
                {
                    SetFacingDirection(playerDelta.x >= 0f ? 1 : -1);
                }

                if (playerDetected)
                {
                    SetState(BossState.Chasing);
                }
                else if (Time.time >= nextPatrolTime)
                {
                    SetState(BossState.Patrolling);
                }
                break;

            case BossState.Patrolling:
                if (playerDetected)
                {
                    SetState(BossState.Chasing);
                    break;
                }

                if (!TryMoveInDirection(facingDirection))
                {
                    FlipDirection();
                }
                break;

            case BossState.Chasing:
                if (!playerDetected)
                {
                    SetState(BossState.Idle);
                    ScheduleNextPatrol();
                    break;
                }

                if (Mathf.Abs(playerDelta.x) > 0.01f)
                {
                    int targetDirection = playerDelta.x > 0f ? 1 : -1;
                    SetFacingDirection(targetDirection);
                }

                if (!playerInAttackRange)
                {
                    TryMoveInDirection(facingDirection);
                }
                break;
        }
    }

    private void UpdateAttackingState()
    {
        horizontalIntent = 0f;

        if (TryGetPlayerDelta(out Vector2 playerDelta) && Mathf.Abs(playerDelta.x) > 0.01f)
        {
            SetFacingDirection(playerDelta.x > 0f ? 1 : -1);
        }

        if (Time.time >= attackStateEndTime)
        {
            FinishAttack();
        }
    }

    private bool TryMoveInDirection(int direction)
    {
        if (!CanMoveInDirection(direction))
        {
            horizontalIntent = 0f;
            return false;
        }

        horizontalIntent = direction;
        return true;
    }

    private bool CanMoveInDirection(int direction)
    {
        bool wallAhead = direction == facingDirection ? cachedWallAhead : CheckWall(direction);
        bool groundAhead = direction == facingDirection ? cachedGroundAhead : CheckGroundAhead(direction);
        return !wallAhead && (allowCliffDrop || groundAhead);
    }

    private void RefreshEnvironmentCacheIfNeeded()
    {
        if (Time.time < nextEnvironmentCheckTime)
        {
            return;
        }

        nextEnvironmentCheckTime = Time.time + environmentCheckInterval;

        if (!IsGrounded || currentState == BossState.Idle || currentState == BossState.Attacking || currentState == BossState.Dead)
        {
            cachedWallAhead = false;
            cachedGroundAhead = true;
            return;
        }

        cachedWallAhead = CheckWall(facingDirection);
        cachedGroundAhead = CheckGroundAhead(facingDirection);
    }

    private bool CheckWall(int direction)
    {
        Vector2 origin = GetSensorOrigin(wallCheckPoint, wallCheckOffset, direction);
        RaycastHit2D hit = Physics2D.Raycast(origin, Vector2.right * direction, wallCheckDistance, environmentLayer);
        return hit.collider != null;
    }

    private bool CheckGroundAhead(int direction)
    {
        Vector2 origin = GetSensorOrigin(ledgeCheckPoint, ledgeCheckOffset, direction);
        Vector2 rayDirection = new Vector2(direction * ledgeForwardWeight, -1f).normalized;
        RaycastHit2D hit = Physics2D.Raycast(origin, rayDirection, ledgeCheckDistance, environmentLayer);
        return hit.collider != null;
    }

    private Vector2 GetSensorOrigin(Transform point, Vector2 fallbackOffset, int direction)
    {
        if (point != null)
        {
            return point.position;
        }

        return (Vector2)transform.position + new Vector2(Mathf.Abs(fallbackOffset.x) * direction, fallbackOffset.y);
    }

    private void StartAttack(Vector2 playerDelta)
    {
        if (isDead || !HasLivingPlayerTarget())
        {
            return;
        }

        SetState(BossState.Attacking);
        horizontalIntent = 0f;
        hasAppliedDamageThisAttack = false;

        if (Mathf.Abs(playerDelta.x) > 0.01f)
        {
            SetFacingDirection(playerDelta.x > 0f ? 1 : -1);
        }

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

        if (!TryGetPlayerDelta(out Vector2 playerDelta) || !CanDetectPlayer(playerDelta))
        {
            SetState(BossState.Idle);
            ScheduleNextPatrol();
            return;
        }

        if (CanAttackPlayer(playerDelta) && Time.time >= nextAttackTime)
        {
            StartAttack(playerDelta);
            return;
        }

        SetState(BossState.Chasing);
    }

    public void TakeDamage(int damage)
    {
        if (isDead || damage <= 0 || mobStats == null)
        {
            return;
        }

        ValidateAliveCollisionState();
        mobStats.TakeDamage(damage);

        if (isDead || mobStats.IsDead || currentState == BossState.Attacking)
        {
            return;
        }

        horizontalIntent = 0f;

        if (animator != null)
        {
            animator.ResetTrigger("Attack");
            animator.SetTrigger("Hurt");
        }

        if (TryGetPlayerDelta(out Vector2 playerDelta) && CanDetectPlayer(playerDelta))
        {
            SetState(BossState.Chasing);
            if (Mathf.Abs(playerDelta.x) > 0.01f)
            {
                SetFacingDirection(playerDelta.x > 0f ? 1 : -1);
            }

            return;
        }

        SetState(BossState.Idle);
        ScheduleNextPatrol();
    }

    public void ExecuteAttackDamage()
    {
        if (isDead || currentState != BossState.Attacking || attackPoint == null || hasAppliedDamageThisAttack)
        {
            return;
        }

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPoint.position, attackHitboxRadius, playerLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            PlayerStats targetPlayerStats = hits[i].GetComponentInParent<PlayerStats>();
            if (targetPlayerStats != null)
            {
                targetPlayerStats.TakeDamage(attackDamage);
                hasAppliedDamageThisAttack = true;
                break;
            }
        }
    }

    private void HandleMobDied()
    {
        if (isDead)
        {
            return;
        }

        isDead = true;
        currentState = BossState.Dead;
        horizontalIntent = 0f;
        groundedColliderIds.Clear();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.bodyType = RigidbodyType2D.Kinematic;
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

        enemyDrop?.Drop();

        if (cleanupCoroutine == null)
        {
            cleanupCoroutine = StartCoroutine(CleanupRoutine());
        }
    }

    private IEnumerator CleanupRoutine()
    {
        yield return new WaitForSeconds(cleanupDelay);
        GetComponent<PersistentObject>()?.MarkAsDestroyed();
        Destroy(gameObject);
    }

    private void UpdateAnimations()
    {
        if (animator == null)
        {
            return;
        }

        if (currentState == BossState.Idle || currentState == BossState.Attacking || currentState == BossState.Dead)
        {
            animator.SetFloat("Speed", 0f);
            return;
        }

        float speed = rb != null ? Mathf.Abs(rb.linearVelocity.x) : 0f;
        if (speed <= movementSpeedThreshold)
        {
            speed = 0f;
        }

        animator.SetFloat("Speed", speed);
    }

    private void SetState(BossState newState)
    {
        if (currentState == newState)
        {
            return;
        }

        currentState = newState;

        if (newState == BossState.Idle || newState == BossState.Attacking || newState == BossState.Dead)
        {
            horizontalIntent = 0f;
        }
    }

    private void ScheduleNextPatrol()
    {
        nextPatrolTime = Time.time + Random.Range(minPatrolDelay, maxPatrolDelay);
    }

    private void FlipDirection()
    {
        SetFacingDirection(-facingDirection);
    }

    private void SetFacingDirection(int direction, bool force = false)
    {
        if (direction == 0)
        {
            return;
        }

        if (!force && facingDirection == direction)
        {
            return;
        }

        facingDirection = direction;
        nextEnvironmentCheckTime = 0f;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = (direction > 0) != initialFacingRight;
        }

        if (attackPoint != null)
        {
            Vector3 localPosition = originalAttackPointLocalPosition;
            localPosition.x = Mathf.Abs(originalAttackPointLocalPosition.x) * direction;
            attackPoint.localPosition = localPosition;
        }
    }

    private bool CanDetectPlayer(Vector2 playerDelta)
    {
        return Mathf.Abs(playerDelta.x) <= detectionRange && Mathf.Abs(playerDelta.y) <= detectionHeightTolerance;
    }

    private bool CanAttackPlayer(Vector2 playerDelta)
    {
        return Mathf.Abs(playerDelta.x) <= attackRange + attackRangeBuffer && Mathf.Abs(playerDelta.y) <= attackHeightTolerance;
    }

    private bool HasLivingPlayerTarget()
    {
        RefreshPlayerTargetReference();
        return playerTransform != null && playerStats != null && !playerStats.IsDead;
    }

    private bool TryGetPlayerDelta(out Vector2 playerDelta)
    {
        if (!HasLivingPlayerTarget())
        {
            playerDelta = Vector2.zero;
            return false;
        }

        playerDelta = playerTransform.position - transform.position;
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

    private void OnCollisionEnter2D(Collision2D collision)
    {
        RefreshGroundState(collision);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        RefreshGroundState(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        groundedColliderIds.Remove(collision.collider.GetInstanceID());
    }

    private void RefreshGroundState(Collision2D collision)
    {
        int colliderId = collision.collider.GetInstanceID();
        int contactCount = collision.GetContacts(contactBuffer);
        bool hasWalkableContact = false;

        for (int i = 0; i < contactCount; i++)
        {
            if (contactBuffer[i].normal.y > groundNormalThreshold)
            {
                hasWalkableContact = true;
                break;
            }
        }

        if (hasWalkableContact)
        {
            groundedColliderIds.Add(colliderId);
        }
        else
        {
            groundedColliderIds.Remove(colliderId);
        }
    }

    private void OnDrawGizmosSelected()
    {
        int drawDirection = facingDirection == 0 ? (initialFacingRight ? 1 : -1) : facingDirection;

        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        if (attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, attackHitboxRadius);
        }

        Gizmos.color = Color.yellow;
        Vector2 wallOrigin = GetSensorOrigin(wallCheckPoint, wallCheckOffset, drawDirection);
        Gizmos.DrawLine(wallOrigin, wallOrigin + (Vector2.right * drawDirection * wallCheckDistance));

        Gizmos.color = Color.green;
        Vector2 ledgeOrigin = GetSensorOrigin(ledgeCheckPoint, ledgeCheckOffset, drawDirection);
        Vector2 ledgeDirection = new Vector2(drawDirection * ledgeForwardWeight, -1f).normalized * ledgeCheckDistance;
        Gizmos.DrawLine(ledgeOrigin, ledgeOrigin + ledgeDirection);
    }
}
