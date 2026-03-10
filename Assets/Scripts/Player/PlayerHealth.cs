using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    [SerializeField] private int maxHealth = 10;
    [SerializeField] private float invincibleTime = 1f;

    private int currentHealth;
    private bool isInvincible;
    private float invincibleTimer;

    void Start()
    {
        currentHealth = maxHealth;
    }

    void Update()
    {
        if (isInvincible)
        {
            invincibleTimer -= Time.deltaTime;

            if (invincibleTimer <= 0)
            {
                isInvincible = false;
            }
        }
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        currentHealth -= damage;

        Debug.Log("Player HP: " + currentHealth);

        isInvincible = true;
        invincibleTimer = invincibleTime;

        if (currentHealth <= 0)
        {
            Debug.Log("Player died");
        }
    }
}