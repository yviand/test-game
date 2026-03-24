using UnityEngine;

public class PlayerStatsHealthSystemBinder : MonoBehaviour
{
    [SerializeField] private PlayerStats playerStats;
    [SerializeField] private PlayerHealthHUD playerHealthHUD;
    [SerializeField] private bool disableInternalSimulation = true;
    [SerializeField] private bool syncOnStart = true;
    [SerializeField] private bool triggerHurtEffectOnDamage = true;

    private Coroutine bindCoroutine;

    private void Awake()
    {
        EnsurePlayerHealthHudReference();
        DisableInternalSimulation();
    }

    private void OnEnable()
    {
        PlayerStats.OnPlayerSpawned += BindToPlayer;

        if (PlayerStats.Instance != null)
        {
            BindToPlayer(PlayerStats.Instance);
        }
        else
        {
            ClearHealthUI();
        }
    }

    private void OnDisable()
    {
        PlayerStats.OnPlayerSpawned -= BindToPlayer;
        StopPendingBind();
        UnsubscribeFromPlayer(playerStats);
    }

    private void HandleHealthChanged(PlayerStats stats)
    {
        SyncHealth(stats, triggerHurtEffectOnDamage);
    }

    private void SyncHealth(PlayerStats stats, bool triggerHurtEffect)
    {
        if (stats == null || playerHealthHUD == null)
        {
            return;
        }

        if (triggerHurtEffect)
        {
            playerHealthHUD.SetHealth(stats.CurrentHealth, stats.FinalMaxHealth, true);
            return;
        }

        playerHealthHUD.ForceSync(stats.CurrentHealth, stats.FinalMaxHealth);
    }

    private void EnsurePlayerHealthHudReference()
    {
        if (PlayerHealthHUD.Instance != null)
        {
            playerHealthHUD = PlayerHealthHUD.Instance;
        }

        if (playerHealthHUD == null)
        {
            playerHealthHUD = GetComponent<PlayerHealthHUD>();
        }

        if (playerHealthHUD == null)
        {
            playerHealthHUD = FindFirstObjectByType<PlayerHealthHUD>();
        }

        if (playerHealthHUD == null)
        {
            Debug.LogWarning("[UI Binder] Missing PlayerHealthHUD reference.", this);
        }
    }

    private void DisableInternalSimulation()
    {
        if (!disableInternalSimulation || playerHealthHUD == null)
        {
            return;
        }

        playerHealthHUD.DisableInternalSimulation();
    }

    private void SubscribeToPlayer(PlayerStats player)
    {
        if (player == null)
        {
            return;
        }

        player.HealthChanged -= HandleHealthChanged;
        player.HealthChanged += HandleHealthChanged;
    }

    private void UnsubscribeFromPlayer(PlayerStats player)
    {
        if (player == null)
        {
            return;
        }

        player.HealthChanged -= HandleHealthChanged;
    }

    private void ClearHealthUI()
    {
        EnsurePlayerHealthHudReference();

        if (playerHealthHUD == null)
        {
            return;
        }

        playerHealthHUD.ForceSync(0f, 1f);
    }

    private void BindToPlayer(PlayerStats targetStats)
    {
        StopPendingBind();
        bindCoroutine = StartCoroutine(WaitAndBind(targetStats));
    }

    private System.Collections.IEnumerator WaitAndBind(PlayerStats targetStats)
    {
        yield return null;

        EnsurePlayerHealthHudReference();

        if (playerStats == targetStats)
        {
            if (playerStats != null && syncOnStart)
            {
                DisableInternalSimulation();
                SyncHealth(playerStats, false);
            }
            bindCoroutine = null;
            yield break;
        }

        UnsubscribeFromPlayer(playerStats);
        this.playerStats = targetStats;

        if (playerStats == null)
        {
            ClearHealthUI();
            bindCoroutine = null;
            yield break;
        }

        DisableInternalSimulation();
        SubscribeToPlayer(playerStats);
        Debug.Log($"[UI Binder] Assigned {playerStats.name} to {name}", this);
        Debug.Log("[UI Binder] Successfully bound to player: " + playerStats.name, this);
        SyncHealth(playerStats, false);
        bindCoroutine = null;
    }

    private void StopPendingBind()
    {
        if (bindCoroutine == null)
        {
            return;
        }

        StopCoroutine(bindCoroutine);
        bindCoroutine = null;
    }
}
