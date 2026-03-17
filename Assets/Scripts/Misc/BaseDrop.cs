using UnityEngine;

public abstract class BaseDrop : MonoBehaviour
{
    [Header("Pickup")]
    [SerializeField] private float pickupRange = 1f;
    [SerializeField] private float magnetSpeed = 6f;
    [SerializeField] private float collectDistance = 0.2f;

    private Transform playerTransform;
    private PlayerItem cachedPlayerItem;
    private bool isMagnetActive;
    private bool isCollected;

    protected virtual void Start()
    {
        TryResolvePlayerTarget();
        LaunchOnSpawn();
    }

    protected virtual void Update()
    {
        if (playerTransform == null || cachedPlayerItem == null)
        {
            TryResolvePlayerTarget();
        }

        if (playerTransform == null)
        {
            return;
        }

        PlayerItem playerItem = GetPlayerItem();
        float distance = Vector2.Distance(transform.position, playerTransform.position);

        if (playerItem == null || !CanMagnetize(playerItem))
        {
            SetMagnetActive(false);
            return;
        }

        if (!isMagnetActive && distance <= pickupRange)
        {
            SetMagnetActive(true);
        }

        if (!isMagnetActive)
        {
            return;
        }

        Vector2 direction = (playerTransform.position - transform.position).normalized;
        transform.position += (Vector3)(direction * magnetSpeed * Time.deltaTime);

        if (Vector2.Distance(transform.position, playerTransform.position) <= collectDistance)
        {
            TryCollect(playerItem);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player"))
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

        // The drop only knows how to hand itself to the player-facing API.
        // Each subclass decides whether it updates balance or inventory.
        if (Collect(playerItem))
        {
            isCollected = true;
            Destroy(gameObject);
        }
    }

    protected virtual void LaunchOnSpawn()
    {
        GetComponent<Rigidbody2D>()?.AddForce(Random.insideUnitCircle * 2f, ForceMode2D.Impulse);
    }

    protected virtual bool CanMagnetize(PlayerItem playerItem)
    {
        return playerItem != null;
    }

    private PlayerItem GetPlayerItem()
    {
        if (cachedPlayerItem == null && playerTransform != null)
        {
            cachedPlayerItem = playerTransform.GetComponent<PlayerItem>();
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
        if (isMagnetActive)
        {
            return;
        }

        Rigidbody2D rigidbody2D = GetComponent<Rigidbody2D>();
        if (rigidbody2D == null)
        {
            return;
        }

        rigidbody2D.linearVelocity = Vector2.zero;
        rigidbody2D.angularVelocity = 0f;
    }

    private void TryResolvePlayerTarget()
    {
        PlayerStats playerStats = PlayerStats.Instance;
        if (playerStats != null)
        {
            playerTransform = playerStats.transform;
            cachedPlayerItem = playerStats.GetComponent<PlayerItem>();
            return;
        }

        GameObject playerObject = GameObject.FindWithTag("Player");
        if (playerObject == null)
        {
            playerTransform = null;
            cachedPlayerItem = null;
            return;
        }

        playerTransform = playerObject.transform;
        cachedPlayerItem = playerObject.GetComponent<PlayerItem>();
    }

    protected abstract bool Collect(PlayerItem playerItem);
}
