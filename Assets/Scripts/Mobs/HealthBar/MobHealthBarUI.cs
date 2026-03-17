using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MobHealthBarUI : MonoBehaviour
{
    [SerializeField] private Image healthFill;
    [SerializeField] private TextMeshProUGUI healthText;
    [SerializeField] private bool faceMainCamera = true;

    private MobStats mobStats;
    private Camera cachedCamera;
    private float targetCurrentHealth;
    private float targetMaxHealth = 1f;
    private float targetFillAmount = 1f;
    private string targetHealthText = "1 / 1";
    private bool hasSyncedState;

    private void Awake()
    {
        mobStats = GetComponentInParent<MobStats>();
        CacheMainCamera();

        if (healthText == null)
        {
            healthText = GetComponentInChildren<TextMeshProUGUI>(true);
        }

        if (healthFill == null)
        {
            Debug.LogWarning($"{nameof(MobHealthBarUI)} on {name} is missing a health fill Image reference.", this);
        }
    }

    private void OnEnable()
    {
        mobStats = GetComponentInParent<MobStats>();

        if (mobStats == null)
        {
            Debug.LogWarning($"{nameof(MobHealthBarUI)} on {name} could not find a parent {nameof(MobStats)}.", this);
            return;
        }

        mobStats.OnHealthChanged -= HandleHealthChanged;
        mobStats.OnHealthChanged += HandleHealthChanged;
        SyncFromStats();
    }

    private void OnDisable()
    {
        if (mobStats != null)
        {
            mobStats.OnHealthChanged -= HandleHealthChanged;
        }
    }

    private void LateUpdate()
    {
        EnforceVisualState();

        if (!faceMainCamera)
        {
            return;
        }

        if (cachedCamera == null)
        {
            CacheMainCamera();
            if (cachedCamera == null)
            {
                return;
            }
        }

        transform.rotation = cachedCamera.transform.rotation;
    }

    private void HandleHealthChanged(float currentHealth, float maxHealth)
    {
        bool unexpectedFullHealthEvent =
            hasSyncedState &&
            targetCurrentHealth < targetMaxHealth &&
            Mathf.Approximately(currentHealth, maxHealth);

        SetTargetState(currentHealth, maxHealth);
    }

    private void SyncFromStats()
    {
        if (mobStats == null)
        {
            return;
        }

        SetTargetState(mobStats.CurrentHealth, mobStats.MaxHealth);
    }

    private void SetTargetState(float currentHealth, float maxHealth)
    {
        targetMaxHealth = Mathf.Max(1f, maxHealth);
        targetCurrentHealth = Mathf.Clamp(currentHealth, 0f, targetMaxHealth);
        targetFillAmount = targetCurrentHealth / targetMaxHealth;
        targetHealthText = $"{Mathf.CeilToInt(targetCurrentHealth)} / {Mathf.CeilToInt(targetMaxHealth)}";
        hasSyncedState = true;
    }

    private void EnforceVisualState()
    {
        if (!hasSyncedState)
        {
            return;
        }

        if (healthFill != null)
        {
            healthFill.fillAmount = targetFillAmount;
        }

        if (healthText != null)
        {
            healthText.text = targetHealthText;
        }
    }

    private void CacheMainCamera()
    {
        cachedCamera = Camera.main;
    }
}
