using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    [SerializeField] private float flipThreshold = 0.01f;

    private Rigidbody2D rb;
    private Vector2 movement;
    private Animator animator;
    private Vector3 originalLocalScale;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        originalLocalScale = transform.localScale;
    }

    void Update()
    {
        movement.x = Input.GetAxisRaw("Horizontal");
        movement.y = Input.GetAxisRaw("Vertical");

        movement = movement.normalized;

        HandleFlip();
        UpdateAnimation();
    }

    void FixedUpdate()
    {
        rb.linearVelocity = movement * moveSpeed;
    }

    void HandleFlip()
    {
        if (Mathf.Abs(movement.x) <= flipThreshold)
        {
            return;
        }

        Vector3 localScale = transform.localScale;
        localScale.x = movement.x > 0f
            ? Mathf.Abs(originalLocalScale.x)
            : -Mathf.Abs(originalLocalScale.x);
        transform.localScale = localScale;
    }

    void UpdateAnimation()
    {
        bool isRunning = movement != Vector2.zero;
        animator.SetBool("isRunning", isRunning);
    }
} 