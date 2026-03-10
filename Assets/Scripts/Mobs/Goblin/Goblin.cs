using UnityEngine;

public class Goblin : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float detectionRange = 5f;
    [SerializeField] private float wanderRadius = 4f;
    [SerializeField] private float wanderChangeTime = 2f;

    [SerializeField] private float minPauseTime = 0.5f;
    [SerializeField] private float maxPauseTime = 2f;

    private Transform player;
    private SpriteRenderer sr;
    private Rigidbody2D rb;

    private Vector2 spawnPosition;
    private Vector2 wanderDirection;
    private Vector2 moveDirection;

    private float wanderTimer;

    private bool isPaused;
    private float pauseTimer;

    private bool returningToSpawn;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();

        spawnPosition = transform.position;

        PickNewDirection();
    }

    void Update()
    {
        float playerDistance = Vector2.Distance(transform.position, player.position);
        float distanceFromSpawn = Vector2.Distance(transform.position, spawnPosition);

        if (returningToSpawn)
        {
            ReturnToSpawn();
            return;
        }

        if (distanceFromSpawn > wanderRadius)
        {
            returningToSpawn = true;
            return;
        }

        if (playerDistance <= detectionRange)
        {
            ChasePlayer();
        }
        else
        {
            Wander();
        }
    }

    void Wander()
    {
        if (isPaused)
        {
            moveDirection = Vector2.zero;   // ⬅️ dừng hẳn

            pauseTimer -= Time.deltaTime;

            if (pauseTimer <= 0)
            {
                isPaused = false;
                PickNewDirection();
            }

            return;
        }

        wanderTimer -= Time.deltaTime;

        if (wanderTimer <= 0)
        {
            StartPause();
        }

        Move(wanderDirection);
    }

    void PickNewDirection()
    {
        wanderDirection = Random.insideUnitCircle.normalized;
        wanderTimer = wanderChangeTime;
    }

    void StartPause()
    {
        isPaused = true;
        pauseTimer = Random.Range(minPauseTime, maxPauseTime);
    }

    void ChasePlayer()
    {
        Vector2 direction = (player.position - transform.position).normalized;
        Move(direction);
    }

    void ReturnToSpawn()
    {
        Vector2 direction = spawnPosition - rb.position;

        if (direction.magnitude < 0.2f)
        {
            returningToSpawn = false;
            PickNewDirection();
            return;
        }

        direction.Normalize();

        Move(direction);
    }

    void Move(Vector2 direction)
    {
        moveDirection = direction;
        Flip(direction);
    }

    void Flip(Vector2 direction)
    {
        if (direction.x > 0)
            sr.flipX = false;
        else if (direction.x < 0)
            sr.flipX = true;
    }

    void FixedUpdate()
    {
        rb.MovePosition(rb.position + moveDirection * moveSpeed * Time.fixedDeltaTime);
    }

    void OnCollisionStay2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerHealth playerHealth = collision.gameObject.GetComponent<PlayerHealth>();

            if (playerHealth != null)
            {
                playerHealth.TakeDamage(1);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(Application.isPlaying ? spawnPosition : transform.position, wanderRadius);
    }
}