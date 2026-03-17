using UnityEngine;

[DisallowMultipleComponent]
[RequireComponent(typeof(HealthSystem))]
public class PlayerHealthHUD : MonoBehaviour
{
    public static PlayerHealthHUD Instance { get; private set; }

    [SerializeField] private HealthSystem healthSystem;
    [SerializeField] private bool disableInternalSimulationOnAwake = true;

    public HealthSystem HealthSystem => healthSystem;

    private void Awake()
    {
        ResolveHealthSystem();

        if (!CanRegisterAsInstance())
        {
            return;
        }

        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerHealthHUD] Multiple HUD instances found. Keeping the first registered instance.", this);
            return;
        }

        Instance = this;

        if (disableInternalSimulationOnAwake)
        {
            DisableInternalSimulation();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public void SetHealth(float current, float max, bool triggerHurtEffect = false)
    {
        if (healthSystem == null)
        {
            return;
        }

        healthSystem.SetHealth(current, max, triggerHurtEffect);
    }

    public void ForceSync(float current, float max)
    {
        if (healthSystem == null)
        {
            return;
        }

        healthSystem.ForceSync(current, max);
    }

    public void TakeDamage(float damage)
    {
        if (healthSystem == null)
        {
            return;
        }

        healthSystem.TakeDamage(damage);
    }

    public void HealDamage(float heal)
    {
        if (healthSystem == null)
        {
            return;
        }

        healthSystem.HealDamage(heal);
    }

    public void SetMaxHealth(float percent)
    {
        if (healthSystem == null)
        {
            return;
        }

        healthSystem.SetMaxHealth(percent);
    }

    public void DisableInternalSimulation()
    {
        if (healthSystem == null)
        {
            return;
        }

        healthSystem.Regenerate = false;
        healthSystem.GodMode = false;
    }

    private void ResolveHealthSystem()
    {
        if (healthSystem == null)
        {
            healthSystem = GetComponent<HealthSystem>();
        }
    }

    private bool CanRegisterAsInstance()
    {
        if (GetComponentInParent<Canvas>() != null)
        {
            return true;
        }

        Debug.LogWarning("[PlayerHealthHUD] Instance registration skipped because this object is not inside a Canvas.", this);
        return false;
    }
}
