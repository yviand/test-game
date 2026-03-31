using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField] private float moveSpeed = 6f;
    [SerializeField] private float jumpVelocity = 12f;
    [SerializeField] private float maxFallSpeed = 20f;

    [Header("Jump Feel")]
    [SerializeField] private float coyoteTime = 0.1f;
    [SerializeField] private float jumpBufferTime = 0.1f;

    [Header("Ground Detection")]
    [SerializeField] private float groundNormalThreshold = 0.7f;

    [Header("Input")]
    [SerializeField] private string horizontalAxis = "Horizontal";
    [SerializeField] private string jumpButton = "Jump";

    [Header("Visuals")]
    [SerializeField] private float flipThreshold = 0.01f;
    [SerializeField] private Transform attackPoint;

    private readonly HashSet<int> groundedColliderIds = new();
    private readonly ContactPoint2D[] contactBuffer = new ContactPoint2D[8];

    private Rigidbody2D rb;
    private Animator animator;
    private SpriteRenderer spriteRenderer;
    private Vector3 originalLocalScale;
    private Vector3 originalAttackPointLocalPosition;
    private float horizontalInput;
    private float coyoteTimer;
    private float jumpBufferTimer;
    private bool facingRight = true;

    public bool IsGrounded => groundedColliderIds.Count > 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
        originalLocalScale = transform.localScale;

        if (attackPoint == null)
        {
            Transform foundAttackPoint = transform.Find("AttackPoint");
            if (foundAttackPoint != null)
            {
                attackPoint = foundAttackPoint;
            }
        }

        if (attackPoint != null)
        {
            originalAttackPointLocalPosition = attackPoint.localPosition;
        }
    }

    private void OnEnable()
    {
        jumpBufferTimer = 0f;
        coyoteTimer = 0f;
    }

    private void Update()
    {
        horizontalInput = Input.GetAxisRaw(horizontalAxis);

        if (Input.GetButtonDown(jumpButton))
        {
            jumpBufferTimer = jumpBufferTime;
        }
        else
        {
            jumpBufferTimer -= Time.deltaTime;
        }

        if (IsGrounded)
        {
            coyoteTimer = coyoteTime;
        }
        else
        {
            coyoteTimer -= Time.deltaTime;
        }

        HandleFlip();
        UpdateAnimation();
    }

    private void FixedUpdate()
    {
        Vector2 velocity = rb.linearVelocity;
        velocity.x = horizontalInput * moveSpeed;

        if (jumpBufferTimer > 0f && coyoteTimer > 0f)
        {
            velocity.y = jumpVelocity;
            jumpBufferTimer = 0f;
            coyoteTimer = 0f;
            groundedColliderIds.Clear();
        }

        if (maxFallSpeed > 0f)
        {
            velocity.y = Mathf.Max(velocity.y, -maxFallSpeed);
        }

        rb.linearVelocity = velocity;
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

    public void StopImmediately()
    {
        horizontalInput = 0f;
        coyoteTimer = 0f;
        jumpBufferTimer = 0f;
        groundedColliderIds.Clear();

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        UpdateAnimation();
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

    private void HandleFlip()
    {
        if (Mathf.Abs(horizontalInput) <= flipThreshold)
        {
            return;
        }

        bool shouldFaceRight = horizontalInput > 0f;
        if (shouldFaceRight == facingRight)
        {
            return;
        }

        facingRight = shouldFaceRight;

        if (spriteRenderer != null)
        {
            spriteRenderer.flipX = !facingRight;
        }
        else
        {
            Vector3 localScale = transform.localScale;
            localScale.x = facingRight
                ? Mathf.Abs(originalLocalScale.x)
                : -Mathf.Abs(originalLocalScale.x);
            transform.localScale = localScale;
        }

        if (attackPoint != null)
        {
            Vector3 localPosition = attackPoint.localPosition;
            localPosition.x = facingRight
                ? Mathf.Abs(originalAttackPointLocalPosition.x)
                : -Mathf.Abs(originalAttackPointLocalPosition.x);
            localPosition.y = originalAttackPointLocalPosition.y;
            localPosition.z = originalAttackPointLocalPosition.z;
            attackPoint.localPosition = localPosition;
        }
    }

    private void UpdateAnimation()
    {
        if (animator == null)
        {
            return;
        }

        bool isRunning = Mathf.Abs(horizontalInput) > flipThreshold;
        animator.SetBool("isRunning", isRunning);
    }
}
