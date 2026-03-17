using UnityEngine;
using System.Collections.Generic;

public class PlayerAttack : MonoBehaviour
{
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

        if (Input.GetKeyDown(KeyCode.Space))
        {
            animator.SetTrigger("attack");
        }
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

        HashSet<GoblinController> damagedGoblins = new HashSet<GoblinController>();

        foreach (Collider2D enemy in hits)
        {
            Debug.Log("Hit: " + enemy.name);

            GoblinController goblinController = enemy.GetComponentInParent<GoblinController>();
            if (goblinController != null && damagedGoblins.Add(goblinController))
            {
                goblinController.TakeDamage(damage);
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        if (attackPoint == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackSize);
    }
}
