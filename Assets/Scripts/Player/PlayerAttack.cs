using UnityEngine;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private string attackButton = "Fire1";
    [SerializeField] private KeyCode attackKey = KeyCode.Mouse0;

    private Animator animator;
    private PlayerStats playerStats;

    [SerializeField] private Transform attackPoint;
    [SerializeField] private Vector2 attackSize = new Vector2(1.5f, 0.8f);
    [SerializeField] private LayerMask enemyLayer;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        playerStats = GetComponent<PlayerStats>();
    }

    void Update()
    {
        if (animator == null || attackPoint == null)
        {
            return;
        }

        if (IsAttackPressed())
        {
            Debug.Log("ATTACKED");
            animator.SetTrigger("Attack");
        }
    }

    private bool IsAttackPressed()
    {
        bool attackButtonPressed = !string.IsNullOrWhiteSpace(attackButton) && Input.GetButtonDown(attackButton);
        bool attackKeyPressed = Input.GetKeyDown(attackKey);
        return attackButtonPressed || attackKeyPressed;
    }

    // gọi bằng Animation Event
    public void PerformAttack()
    {
        if (attackPoint == null)
        {
            Debug.LogWarning($"{nameof(PlayerAttack)} is missing an attack point reference.", this);
            return;
        }

        int damage = 1;
        if (playerStats != null)
        {
            damage = Mathf.Max(1, Mathf.RoundToInt(playerStats.FinalAttack));
        }

        Collider2D[] hits = Physics2D.OverlapBoxAll(
            attackPoint.position,
            attackSize,
            0f,
            enemyLayer
        );

        HashSet<MobStats> damagedMobs = new HashSet<MobStats>();

        foreach (Collider2D enemy in hits)
        {
            Debug.Log("Hit: " + enemy.name);

            MobStats mobStats = enemy.GetComponentInParent<MobStats>();
            if (mobStats == null || !damagedMobs.Add(mobStats))
            {
                continue;
            }

            GoblinController goblinController = enemy.GetComponentInParent<GoblinController>();
            if (goblinController != null)
            {
                goblinController.TakeDamage(damage);
                continue;
            }

            BringerController bringerController = enemy.GetComponentInParent<BringerController>();
            if (bringerController != null)
            {
                bringerController.TakeDamage(damage);
                continue;
            }

            mobStats.TakeDamage(damage);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackSize);
    }
}
