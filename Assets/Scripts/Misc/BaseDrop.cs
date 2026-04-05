using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class BaseDrop : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private float pickupRange = 1f;
    [SerializeField] private float magnetSpeed = 6f;
    [SerializeField] private float collectDistance = 0.2f;

    [Header("Spawn Physics")]
    [SerializeField] private float gravityScale = 2f;
    [SerializeField] private Vector2 horizontalTossRange = new Vector2(0.6f, 1.25f);
    [SerializeField] private Vector2 upwardTossRange = new Vector2(1.75f, 2.75f);
    [SerializeField] private LayerMask groundLayers = ~0;
    [SerializeField] private float groundNormalThreshold = 0.7f;
    [SerializeField] private bool freezeHorizontalOnGround = true;
    [SerializeField] private float playerCollisionIgnoreDuration = 0.35f;

    private readonly HashSet<int> groundedColliderIds = new HashSet<int>();

    private Rigidbody2D rigidbody2D;
    private Collider2D dropCollider;
    private Collider2D[] playerColliders;
    private Transform playerTransform;
    private PlayerItem cachedPlayerItem;
    private bool isMagnetActive;
    private bool isCollected;
    private bool isIgnoringPlayerCollision;
    private float ignorePlayerCollisionTimer;

    protected bool IsGrounded => groundedColliderIds.Count > 0;

    protected virtual void Awake()
    {
        rigidbody2D = GetComponent<Rigidbody2D>();
        dropCollider = GetComponent<Collider2D>();
        ApplyPhysicsDefaults();
    }

    protected virtual void Start()
    {
        TryResolvePlayerTarget();
        BeginIgnoringPlayerCollisions();
        LaunchOnSpawn();
    }

    protected virtual void Update()
    {
        if (playerTransform == null || cachedPlayerItem == null)
        {
            TryResolvePlayerTarget();
        }

        UpdatePlayerCollisionIgnoreTimer();

        if (playerTransform == null)
        {
            return;
        }

        PlayerItem playerItem = GetPlayerItem();
        Vector2 currentPosition = rigidbody2D != null ? rigidbody2D.position : (Vector2)transform.position;
        float distance = Vector2.Distance(currentPosition, playerTransform.position);

        if (playerItem == null || !CanMagnetize(playerItem))
        {
            SetMagnetActive(false);
            return;
        }

        if (distance <= collectDistance)
        {
            TryCollect(playerItem);
            return;
        }

        if (!isMagnetActive && distance <= pickupRange)
        {
            SetMagnetActive(true);
        }
    }

    protected virtual void FixedUpdate()
    {
        if (rigidbody2D == null || isCollected)
        {
            return;
        }

        if (isMagnetActive && playerTransform != null)
        {
            UnlockHorizontalMovement();
            rigidbody2D.gravityScale = 0f;

            Vector2 direction = (Vector2)playerTransform.position - rigidbody2D.position;
            if (direction.sqrMagnitude <= collectDistance * collectDistance)
            {
                rigidbody2D.linearVelocity = Vector2.zero;
                return;
            }

            rigidbody2D.linearVelocity = direction.normalized * magnetSpeed;
            rigidbody2D.angularVelocity = 0f;
            return;
        }

        rigidbody2D.gravityScale = gravityScale;

        if (!IsGrounded)
        {
            UnlockHorizontalMovement();
            return;
        }

        rigidbody2D.angularVelocity = 0f;
        if (freezeHorizontalOnGround)
        {
            LockHorizontalMovement();
            return;
        }

        rigidbody2D.linearVelocity = new Vector2(0f, rigidbody2D.linearVelocity.y);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        TryCollectFromCollider(other);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        RefreshGroundState(collision);
        TryCollectFromCollider(collision.collider);
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        RefreshGroundState(collision);
    }

    private void OnCollisionExit2D(Collision2D collision)
    {
        if (collision.collider != null)
        {
            groundedColliderIds.Remove(collision.collider.GetInstanceID());
        }

        if (!IsGrounded)
        {
            UnlockHorizontalMovement();
        }
    }

    private void OnDisable()
    {
        ApplyPlayerCollisionIgnore(false);
    }

    private void TryCollectFromCollider(Collider2D other)
    {
        if (other == null || !other.CompareTag("Player"))
        {
            return;
        }

        PlayerItem playerItem = other.GetComponent<PlayerItem>();
        if (playerItem == null)
        {
            playerItem = other.GetComponentInParent<PlayerItem>();
        }

        if (playerItem == null)
        {
            playerItem = other.GetComponentInChildren<PlayerItem>();
        }

        if (playerItem == null || !CanMagnetize(playerItem))
        {
            SetMagnetActive(false);
            return;
        }

        TryCollect(playerItem);
    }

    private void TryCollect(PlayerItem playerItem)
    {
        if (isCollected || playerItem == null)
        {
            return;
        }

        if (Collect(playerItem))
        {
            isCollected = true;
            GetComponent<PersistentObject>()?.MarkAsDestroyed();
            Destroy(gameObject);
        }
    }

    protected virtual void LaunchOnSpawn()
    {
        if (rigidbody2D == null)
        {
            return;
        }

        UnlockHorizontalMovement();
        rigidbody2D.gravityScale = gravityScale;
        rigidbody2D.linearVelocity = Vector2.zero;
        rigidbody2D.angularVelocity = 0f;

        float horizontalDirection = Random.value < 0.5f ? -1f : 1f;
        float horizontalImpulse = Random.Range(horizontalTossRange.x, horizontalTossRange.y) * horizontalDirection;
        float upwardImpulse = Random.Range(upwardTossRange.x, upwardTossRange.y);
        rigidbody2D.AddForce(new Vector2(horizontalImpulse, upwardImpulse), ForceMode2D.Impulse);
    }

    protected virtual bool CanMagnetize(PlayerItem playerItem)
    {
        return playerItem != null;
    }

    private PlayerItem GetPlayerItem()
    {
        if (cachedPlayerItem == null && playerTransform != null)
        {
            cachedPlayerItem = ResolvePlayerItem(playerTransform);
        }

        return cachedPlayerItem;
    }

    private void SetMagnetActive(bool active)
    {
        if (isMagnetActive == active)
        {
            return;
        }

        isMagnetActive = active;
        if (rigidbody2D == null)
        {
            return;
        }

        if (isMagnetActive)
        {
            UnlockHorizontalMovement();
            ApplyPlayerCollisionIgnore(false);
            return;
        }

        rigidbody2D.gravityScale = gravityScale;
        rigidbody2D.angularVelocity = 0f;
        if (IsGrounded)
        {
            rigidbody2D.linearVelocity = new Vector2(0f, rigidbody2D.linearVelocity.y);
            LockHorizontalMovement();
        }
    }

    private void TryResolvePlayerTarget()
    {
        PlayerStats playerStats = PlayerStats.Instance;
        if (playerStats != null)
        {
            CachePlayerTarget(playerStats.transform);
            return;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject == null)
        {
            playerTransform = null;
            cachedPlayerItem = null;
            playerColliders = null;
            return;
        }

        CachePlayerTarget(playerObject.transform);
    }

    private void CachePlayerTarget(Transform target)
    {
        playerTransform = target;
        cachedPlayerItem = ResolvePlayerItem(target);
        playerColliders = target.GetComponentsInChildren<Collider2D>(true);

        if (ignorePlayerCollisionTimer > 0f)
        {
            ApplyPlayerCollisionIgnore(true);
        }
    }

    private PlayerItem ResolvePlayerItem(Transform target)
    {
        if (target == null)
        {
            return null;
        }

        PlayerItem playerItem = target.GetComponent<PlayerItem>();
        if (playerItem == null)
        {
            playerItem = target.GetComponentInParent<PlayerItem>();
        }

        if (playerItem == null)
        {
            playerItem = target.GetComponentInChildren<PlayerItem>();
        }

        return playerItem;
    }

    private void ApplyPhysicsDefaults()
    {
        if (rigidbody2D == null)
        {
            return;
        }

        rigidbody2D.bodyType = RigidbodyType2D.Dynamic;
        rigidbody2D.gravityScale = gravityScale;
        rigidbody2D.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        rigidbody2D.interpolation = RigidbodyInterpolation2D.Interpolate;
        rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private void RefreshGroundState(Collision2D collision)
    {
        if (collision == null || collision.collider == null)
        {
            return;
        }

        int colliderId = collision.collider.GetInstanceID();
        if (!IsLayerInMask(collision.collider.gameObject.layer, groundLayers) || !HasGroundContact(collision))
        {
            groundedColliderIds.Remove(colliderId);
            return;
        }

        groundedColliderIds.Add(colliderId);
    }

    private bool HasGroundContact(Collision2D collision)
    {
        foreach (ContactPoint2D contact in collision.contacts)
        {
            if (contact.normal.y > groundNormalThreshold)
            {
                return true;
            }
        }

        return false;
    }

    private void BeginIgnoringPlayerCollisions()
    {
        if (playerCollisionIgnoreDuration <= 0f)
        {
            return;
        }

        ignorePlayerCollisionTimer = playerCollisionIgnoreDuration;
        ApplyPlayerCollisionIgnore(true);
    }

    private void UpdatePlayerCollisionIgnoreTimer()
    {
        if (!isIgnoringPlayerCollision)
        {
            return;
        }

        if (isMagnetActive)
        {
            ApplyPlayerCollisionIgnore(false);
            return;
        }

        ignorePlayerCollisionTimer -= Time.deltaTime;
        if (ignorePlayerCollisionTimer > 0f)
        {
            return;
        }

        ApplyPlayerCollisionIgnore(false);
    }

    private void ApplyPlayerCollisionIgnore(bool shouldIgnore)
    {
        if (dropCollider == null || playerColliders == null)
        {
            isIgnoringPlayerCollision = shouldIgnore && playerColliders != null;
            return;
        }

        foreach (Collider2D playerCollider in playerColliders)
        {
            if (playerCollider == null)
            {
                continue;
            }

            Physics2D.IgnoreCollision(dropCollider, playerCollider, shouldIgnore);
        }

        isIgnoringPlayerCollision = shouldIgnore;
    }

    private void LockHorizontalMovement()
    {
        if (rigidbody2D == null)
        {
            return;
        }

        rigidbody2D.linearVelocity = new Vector2(0f, rigidbody2D.linearVelocity.y);
        rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation | RigidbodyConstraints2D.FreezePositionX;
    }

    private void UnlockHorizontalMovement()
    {
        if (rigidbody2D == null)
        {
            return;
        }

        rigidbody2D.constraints = RigidbodyConstraints2D.FreezeRotation;
    }

    private static bool IsLayerInMask(int layer, LayerMask layerMask)
    {
        return (layerMask.value & (1 << layer)) != 0;
    }

    protected abstract bool Collect(PlayerItem playerItem);
}
