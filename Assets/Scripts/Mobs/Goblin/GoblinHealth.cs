using UnityEngine;

public class GoblinHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 3;

    private int currentHealth;
    private EnemyDrop drop;

    void Start()
    {
        currentHealth = maxHealth;
        drop = GetComponent<EnemyDrop>();
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;

        Debug.Log("Goblin HP: " + currentHealth);

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        Debug.Log("Goblin died");

        if (drop != null)
        {
            drop.DropItems();
        }

        Destroy(gameObject);
    }
}