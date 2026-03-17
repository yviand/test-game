//==============================================================
// HealthSystem
// Generic health bar controller for a single UI widget.
//==============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HealthSystem : MonoBehaviour
{
	public Image currentHealthBar;
	public Image currentHealthGlobe;
	public TextMeshProUGUI healthText;
	public float hitPoint = 100f;
	public float maxHitPoint = 100f;

	// public Image currentManaBar;
	// public Image currentManaGlobe;
	// public TextMeshProUGUI manaText;
	// public float manaPoint = 100f;
	// public float maxManaPoint = 100f;

	//==============================================================
	// Regenerate Health & Mana
	//==============================================================
	public bool Regenerate = true;
	public float regen = 0.1f;
	private float timeleft = 0.0f;	// Left time for current interval
	public float regenUpdateInterval = 1f;

	public bool GodMode;
	private Coroutine hurtCoroutine;
	
	//==============================================================
	// Awake
	//==============================================================
  	void Start()
	{
		UpdateGraphics();
		timeleft = regenUpdateInterval; 
	}

	//==============================================================
	// Update
	//==============================================================
	void Update ()
	{
		if (Regenerate)
			Regen();
	}

	//==============================================================
	// Regenerate Health & Mana
	//==============================================================
	private void Regen()
	{
		timeleft -= Time.deltaTime;

		if (timeleft <= 0.0) // Interval ended - update health & mana and start new interval
		{
			// Debug mode
			if (GodMode)
			{
				HealDamage(maxHitPoint);
				// RestoreMana(maxManaPoint);
			}
			else
			{
				HealDamage(regen);
				// RestoreMana(regen);				
			}

			UpdateGraphics();

			timeleft = regenUpdateInterval;
		}
	}

	//==============================================================
	// Health Logic
	//==============================================================
	private void UpdateHealthBar()
	{
		if (currentHealthBar == null) return;

		float ratio = hitPoint / maxHitPoint;
		// Dùng dòng này để KHÔNG bị nhảy vị trí nữa
		currentHealthBar.fillAmount = ratio; 

		if (healthText != null)
			healthText.text = hitPoint.ToString("0") + "/" + maxHitPoint.ToString("0");
	}

	// private void UpdateHealthGlobe()
	// {
	// 	float ratio = hitPoint / maxHitPoint;
	// 	currentHealthGlobe.rectTransform.localPosition = new Vector3(0, currentHealthGlobe.rectTransform.rect.height * ratio - currentHealthGlobe.rectTransform.rect.height, 0);
	// 	healthText.text = hitPoint.ToString("0") + "/" + maxHitPoint.ToString("0");
	// }

	public void TakeDamage(float Damage)
	{
		SetHealth(hitPoint - Damage, maxHitPoint, true);
	}

	public void HealDamage(float Heal)
	{
		SetHealth(hitPoint + Heal, maxHitPoint);
	}
	public void SetMaxHealth(float max)
	{
		maxHitPoint += (int)(maxHitPoint * max / 100);

		SetHealth(hitPoint, maxHitPoint);
	}

	//==============================================================
	// Mana Logic
	//==============================================================
	// private void UpdateManaBar()
	// {
	// 	float ratio = manaPoint / maxManaPoint;
	// 	currentManaBar.rectTransform.localPosition = new Vector3(currentManaBar.rectTransform.rect.width * ratio - currentManaBar.rectTransform.rect.width, 0, 0);
	// 	manaText.text = manaPoint.ToString ("0") + "/" + maxManaPoint.ToString ("0");
	// }

	// private void UpdateManaGlobe()
	// {
	// 	float ratio = manaPoint / maxManaPoint;
	// 	currentManaGlobe.rectTransform.localPosition = new Vector3(0, currentManaGlobe.rectTransform.rect.height * ratio - currentManaGlobe.rectTransform.rect.height, 0);
	// 	manaText.text = manaPoint.ToString("0") + "/" + maxManaPoint.ToString("0");
	// }

	// public void UseMana(float Mana)
	// {
	// 	SetMana(manaPoint - Mana, maxManaPoint);
	// }

	// public void RestoreMana(float Mana)
	// {
	// 	SetMana(manaPoint + Mana, maxManaPoint);
	// }
	// public void SetMaxMana(float max)
	// {
	// 	maxManaPoint += (int)(maxManaPoint * max / 100);
		
	// 	SetMana(manaPoint, maxManaPoint);
	// }

	public void SetHealth(float current, float max, bool triggerHurtEffect = false)
	{
		float previousHealth = hitPoint;
		maxHitPoint = Mathf.Max(1f, max);
		hitPoint = Mathf.Clamp(current, 0f, maxHitPoint);

		UpdateHealthBar();
		// UpdateHealthGlobe();

		if (triggerHurtEffect && hitPoint < previousHealth)
		{
			TriggerHurtEffect();
		}
	}

	public void ForceSync(float current, float max)
	{
		SetHealth(current, max);
		UpdateGraphics();
	}

	// public void SetMana(float current, float max)
	// {
	// 	maxManaPoint = Mathf.Max(1f, max);
	// 	manaPoint = Mathf.Clamp(current, 0f, maxManaPoint);

	// 	UpdateManaBar();
	// 	UpdateManaGlobe();
	// }

	//==============================================================
	// Update all Bars & Globes UI graphics
	//==============================================================
	private void UpdateGraphics()
	{
		UpdateHealthBar();
		// UpdateHealthGlobe();
		// UpdateManaBar();
		// UpdateManaGlobe();
	}

	//==============================================================
	// Coroutine Player Hurts
	//==============================================================
	IEnumerator PlayerHurts()
	{
		// Player gets hurt. Do stuff.. play anim, sound..

		if (PopupText.Instance != null)
			PopupText.Instance.Popup("Ouch!", 1f, 1f); // Demo stuff!

		if (hitPoint < 1) // Health is Zero!!
		{
			yield return StartCoroutine(PlayerDied()); // Hero is Dead
		}

		else
			yield return null;
	}

	//==============================================================
	// Hero is dead
	//==============================================================
	IEnumerator PlayerDied()
	{
		// Player is dead. Do stuff.. play anim, sound..
		if (PopupText.Instance != null)
			PopupText.Instance.Popup("You have died!", 1f, 1f); // Demo stuff!

		yield return null;
	}

	private void TriggerHurtEffect()
	{
		if (hurtCoroutine != null)
		{
			StopCoroutine(hurtCoroutine);
		}

		hurtCoroutine = StartCoroutine(PlayerHurts());
	}
}
