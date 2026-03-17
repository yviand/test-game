using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStats : MonoBehaviour
{
    public static PlayerStats Instance { get; private set; }
    public static event Action<PlayerStats> OnPlayerSpawned;

    [field: Header("Base Stats")]
    [field: SerializeField] public float BaseAttack { get; private set; } = 10f;
    [field: SerializeField] public float BaseHealth { get; private set; } = 100f;
    [field: SerializeField] public float BaseCooldown { get; private set; } = 1f;

    [field: Header("Final Stats")]
    [field: SerializeField] public float FinalAttack { get; private set; }
    [field: SerializeField] public float FinalMaxHealth { get; private set; }
    [field: SerializeField] public float FinalCooldown { get; private set; }

    [field: Header("Health State")]
    [field: SerializeField] public float CurrentHealth { get; private set; }
    [field: SerializeField] public float InvincibleDuration { get; private set; } = 1f;

    [Header("Death")]
    [SerializeField] private float deathCleanupDelay = 0.5f;
    [SerializeField] private string deadLayerName = "Ignore Raycast";

    private readonly List<StatModifier> activeModifiers = new();
    private Animator animator;
    private Rigidbody2D rb;
    private PlayerMovement playerMovement;
    private PlayerAttack playerAttack;
    private Collider2D[] colliders;
    private DeathScreenController deathScreenController;
    private bool isInvincible;
    private float invincibleTimer;
    private Coroutine deathSequenceCoroutine;

    public IReadOnlyList<StatModifier> ActiveModifiers => activeModifiers;
    public bool IsDead { get; private set; }

    public event Action<PlayerStats> StatsChanged;
    public event Action<PlayerStats> HealthChanged;
    public event Action<PlayerStats> Died;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody2D>();
        playerMovement = GetComponent<PlayerMovement>();
        playerAttack = GetComponent<PlayerAttack>();
        colliders = GetComponentsInChildren<Collider2D>(true);
        deathScreenController = FindObjectOfType<DeathScreenController>();

        RecalculateAllFinalStats();
        CurrentHealth = FinalMaxHealth;
        IsDead = false;
    }

    private void Start()
    {
        OnPlayerSpawned?.Invoke(this);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public static void ReleaseInstance(PlayerStats playerStats)
    {
        if (Instance == playerStats)
        {
            Instance = null;
        }
    }

    private void Update()
    {
        if (!isInvincible)
        {
            return;
        }

        invincibleTimer -= Time.deltaTime;
        if (invincibleTimer <= 0f)
        {
            isInvincible = false;
        }
    }

    public void SetBaseStats(float attack, float health, float cooldown)
    {
        BaseAttack = attack;
        BaseHealth = health;
        BaseCooldown = cooldown;
        RecalculateAllFinalStats();
    }

    public void SetBaseAttack(float attack)
    {
        BaseAttack = attack;
        RecalculateAllFinalStats();
    }

    public void SetBaseHealth(float health)
    {
        BaseHealth = health;
        RecalculateAllFinalStats();
    }

    public void SetBaseCooldown(float cooldown)
    {
        BaseCooldown = cooldown;
        RecalculateAllFinalStats();
    }

    public void ApplyModifiers(IEnumerable<StatModifier> modifiers)
    {
        if (modifiers == null)
        {
            return;
        }

        activeModifiers.AddRange(modifiers);
        RecalculateAllFinalStats();
    }

    public void ApplyItemInstance(ItemInstance itemInstance, object source)
    {
        if (itemInstance == null || !itemInstance.IsEquippable)
        {
            return;
        }

        AddItemInstanceModifiers(itemInstance, source);
        RecalculateAllFinalStats();
    }

    public void RemoveModifiersFromSource(object source)
    {
        if (source == null)
        {
            return;
        }

        activeModifiers.RemoveAll(modifier => modifier.Source == source);
        RecalculateAllFinalStats();
    }

    public void ClearAllModifiers()
    {
        activeModifiers.Clear();
        RecalculateAllFinalStats();
    }

    public void RecalculateAllFinalStats()
    {
        float previousMaxHealth = FinalMaxHealth;
        FinalAttack = CalculateFinalStat(StatType.Attack, BaseAttack);
        FinalMaxHealth = Mathf.Max(1f, CalculateFinalStat(StatType.Health, BaseHealth));
        FinalCooldown = CalculateFinalStat(StatType.Cooldown, BaseCooldown);
        SyncCurrentHealth(previousMaxHealth);
        StatsChanged?.Invoke(this);
        HealthChanged?.Invoke(this);
    }

    public void TakeDamage(int damage)
    {
        if (IsDead || isInvincible || damage <= 0)
        {
            return;
        }

        CurrentHealth = Mathf.Max(0f, CurrentHealth - damage);
        isInvincible = true;
        invincibleTimer = InvincibleDuration;

        Debug.Log("Player HP: " + CurrentHealth, this);
        HealthChanged?.Invoke(this);

        if (CurrentHealth <= 0f)
        {
            Die();
        }
    }

    public void Heal(float amount)
    {
        if (IsDead || amount <= 0f)
        {
            return;
        }

        CurrentHealth = Mathf.Min(FinalMaxHealth, CurrentHealth + amount);
        HealthChanged?.Invoke(this);
    }

    private void Die()
    {
        if (IsDead)
        {
            return;
        }

        IsDead = true;
        isInvincible = false;
        invincibleTimer = 0f;

        DisablePlayerInteractions();
        TriggerDeathAnimation();
        Died?.Invoke(this);

        if (deathSequenceCoroutine != null)
        {
            StopCoroutine(deathSequenceCoroutine);
        }

        deathSequenceCoroutine = StartCoroutine(HandleDeathSequence());
        Debug.Log("Player died", this);
    }

    private void DisablePlayerInteractions()
    {
        if (playerMovement != null)
        {
            playerMovement.enabled = false;
        }

        if (playerAttack != null)
        {
            playerAttack.enabled = false;
        }

        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (colliders != null)
        {
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                {
                    colliders[i].enabled = false;
                }
            }
        }

        int deadLayer = LayerMask.NameToLayer(deadLayerName);
        if (deadLayer >= 0)
        {
            SetLayerRecursively(gameObject, deadLayer);
        }
        else
        {
            SetLayerRecursively(gameObject, LayerMask.NameToLayer("Ignore Raycast"));
        }
    }

    private IEnumerator HandleDeathSequence()
    {
        if (deathScreenController == null)
        {
            deathScreenController = FindObjectOfType<DeathScreenController>();
        }

        if (deathScreenController != null)
        {
            deathScreenController.ShowDeathScreen();
        }

        if (deathCleanupDelay > 0f)
        {
            yield return new WaitForSeconds(deathCleanupDelay);
        }

        PlayerStats.ReleaseInstance(this);
        Destroy(gameObject);
    }

    private void TriggerDeathAnimation()
    {
        if (animator == null)
        {
            return;
        }

        if (HasAnimatorParameter("isDead", AnimatorControllerParameterType.Bool))
        {
            animator.SetBool("isDead", true);
        }

        if (HasAnimatorParameter("Die", AnimatorControllerParameterType.Trigger))
        {
            animator.SetTrigger("Die");
        }
    }

    public void FreezeAnimation() {
        GetComponent<Animator>().speed = 0;
    }
    private bool HasAnimatorParameter(string parameterName, AnimatorControllerParameterType parameterType)
    {
        if (animator == null)
        {
            return false;
        }

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            if (parameter.type == parameterType && parameter.name == parameterName)
            {
                return true;
            }
        }

        return false;
    }

    private void SetLayerRecursively(GameObject target, int layer)
    {
        if (target == null || layer < 0)
        {
            return;
        }

        target.layer = layer;

        for (int i = 0; i < target.transform.childCount; i++)
        {
            SetLayerRecursively(target.transform.GetChild(i).gameObject, layer);
        }
    }

    private float CalculateFinalStat(StatType statType, float baseStat)
    {
        float totalFlat = 0f;
        float totalPercent = 0f;

        for (int i = 0; i < activeModifiers.Count; i++)
        {
            StatModifier modifier = activeModifiers[i];
            if (modifier == null)
            {
                continue;
            }

            if (modifier.StatType != statType)
            {
                continue;
            }

            if (modifier.ModifierKind == StatModifierKind.Flat)
            {
                totalFlat += modifier.Value;
            }
            else
            {
                totalPercent += modifier.Value;
            }
        }

        return (baseStat * (1f + totalPercent)) + totalFlat;
    }

    private void SyncCurrentHealth(float previousMaxHealth)
    {
        if (previousMaxHealth <= 0f)
        {
            CurrentHealth = FinalMaxHealth;
            return;
        }

        float healthRatio = Mathf.Clamp01(CurrentHealth / previousMaxHealth);
        CurrentHealth = Mathf.Clamp(FinalMaxHealth * healthRatio, 0f, FinalMaxHealth);
    }

    private void AddItemInstanceModifiers(ItemInstance itemInstance, object source)
    {
        if (itemInstance.MainAttack > 0f)
        {
            activeModifiers.Add(new StatModifier(
                StatType.Attack,
                StatModifierKind.Flat,
                itemInstance.MainAttack,
                source));
        }

        IReadOnlyList<StatModifier> rolledSubStats = itemInstance.GetSubStats();
        if (rolledSubStats == null)
        {
            return;
        }

        for (int i = 0; i < rolledSubStats.Count; i++)
        {
            StatModifier modifier = rolledSubStats[i];
            if (modifier == null)
            {
                continue;
            }

            activeModifiers.Add(new StatModifier(
                modifier.StatType,
                modifier.ModifierKind,
                modifier.Value,
                source));
        }
    }
}
