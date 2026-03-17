using System;
using System.Collections.Generic;
using UnityEngine;

public enum WeaponRarity
{
    Common,
    Uncommon,
    Rare,
    Epic,
    Legendary,
    Unique
}

public class Weapon : MonoBehaviour
{
    private enum WeaponSubStatTemplate
    {
        AttackPercent,
        HealthPercent,
        FlatHealth,
        CooldownPercent
    }

    [Serializable]
    public sealed class WeaponSubStatRoll
    {
        [field: SerializeField] public StatType StatType { get; private set; }
        [field: SerializeField] public StatModifierKind ModifierKind { get; private set; }
        [field: SerializeField] public float Value { get; private set; }

        public WeaponSubStatRoll(StatType statType, StatModifierKind modifierKind, float value)
        {
            StatType = statType;
            ModifierKind = modifierKind;
            Value = value;
        }

        public StatModifier ToModifier(object source)
        {
            return new StatModifier(StatType, ModifierKind, Value, source);
        }
    }

    [field: Header("Optional Data Link")]
    [field: SerializeField] public ItemData ItemData { get; private set; }
    [field: SerializeField] public bool IsWorldPickup { get; private set; }

    [field: Header("Instance Weapon Stats")]
    [field: SerializeField] public WeaponRarity Rarity { get; private set; } = WeaponRarity.Common;
    [field: SerializeField] public float PrimaryAttackBonus { get; private set; } = 5f;

    [field: Header("Instance Rolls")]
    [field: SerializeField] public List<WeaponSubStatRoll> SubStats { get; private set; } = new List<WeaponSubStatRoll>();
    [field: SerializeField] public string SpecialSubStat { get; private set; } = string.Empty;

    [field: Header("Roll Behavior")]
    [field: SerializeField] public bool RollStatsOnAwake { get; private set; } = true;
    [SerializeField] private bool hasRolledStats;

    [field: Header("Roll Ranges")]
    [field: SerializeField] public Vector2 AttackPercentRange { get; private set; } = new Vector2(0.05f, 0.15f);
    [field: SerializeField] public Vector2 HealthPercentRange { get; private set; } = new Vector2(0.05f, 0.20f);
    [field: SerializeField] public Vector2 FlatHealthRange { get; private set; } = new Vector2(10f, 50f);
    [field: SerializeField] public Vector2 CooldownPercentRange { get; private set; } = new Vector2(-0.20f, -0.05f);

    [field: Header("Unique Placeholder")]
    [field: SerializeField] public string UniqueSpecialSubStatTemplate { get; private set; } = "Special effect placeholder";

    private void Awake()
    {
        ResolveItemData();

        if (Application.isPlaying && RollStatsOnAwake && !hasRolledStats)
        {
            RollInstanceStats();
        }
    }

    [ContextMenu("Roll Instance Stats")]
    public void RollInstanceStats()
    {
        SubStats.Clear();
        SpecialSubStat = string.Empty;

        List<WeaponSubStatTemplate> pool = new List<WeaponSubStatTemplate>
        {
            WeaponSubStatTemplate.AttackPercent,
            WeaponSubStatTemplate.HealthPercent,
            WeaponSubStatTemplate.FlatHealth,
            WeaponSubStatTemplate.CooldownPercent
        };

        int subStatCount = GetStandardSubStatCount(Rarity);

        for (int i = 0; i < subStatCount && pool.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            WeaponSubStatTemplate template = pool[randomIndex];
            pool.RemoveAt(randomIndex);

            SubStats.Add(CreateRoll(template));
        }

        if (Rarity == WeaponRarity.Unique)
        {
            SpecialSubStat = UniqueSpecialSubStatTemplate;
        }

        hasRolledStats = true;
    }

    public List<StatModifier> RollSubStatModifiers()
    {
        List<StatModifier> rolledModifiers = new List<StatModifier>();
        List<WeaponSubStatTemplate> pool = new List<WeaponSubStatTemplate>
        {
            WeaponSubStatTemplate.AttackPercent,
            WeaponSubStatTemplate.HealthPercent,
            WeaponSubStatTemplate.FlatHealth,
            WeaponSubStatTemplate.CooldownPercent
        };

        int subStatCount = GetStandardSubStatCount(Rarity);
        for (int i = 0; i < subStatCount && pool.Count > 0; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, pool.Count);
            WeaponSubStatTemplate template = pool[randomIndex];
            pool.RemoveAt(randomIndex);

            WeaponSubStatRoll roll = CreateRoll(template);
            rolledModifiers.Add(new StatModifier(roll.StatType, roll.ModifierKind, roll.Value, null));
        }

        return rolledModifiers;
    }

    public List<StatModifier> GetAllModifiers(object sourceOverride = null)
    {
        object modifierSource = sourceOverride ?? this;
        List<StatModifier> modifiers = new List<StatModifier>
        {
            new StatModifier(StatType.Attack, StatModifierKind.Flat, PrimaryAttackBonus, modifierSource)
        };

        for (int i = 0; i < SubStats.Count; i++)
        {
            modifiers.Add(SubStats[i].ToModifier(modifierSource));
        }

        return modifiers;
    }

    public List<StatModifier> GetRolledSubStatSnapshot()
    {
        List<StatModifier> snapshot = new List<StatModifier>();
        for (int i = 0; i < SubStats.Count; i++)
        {
            WeaponSubStatRoll subStat = SubStats[i];
            if (subStat == null)
            {
                continue;
            }

            snapshot.Add(new StatModifier(
                subStat.StatType,
                subStat.ModifierKind,
                subStat.Value,
                null));
        }

        return snapshot;
    }

    public void SetInstanceStats(float primaryAttackBonus, IEnumerable<WeaponSubStatRoll> subStats, string specialSubStat = "")
    {
        PrimaryAttackBonus = Mathf.Max(0f, primaryAttackBonus);

        SubStats.Clear();
        if (subStats != null)
        {
            SubStats.AddRange(subStats);
        }

        SpecialSubStat = specialSubStat ?? string.Empty;
        hasRolledStats = true;
    }

    public void ApplyItemInstance(ItemInstance itemInstance)
    {
        if (itemInstance == null)
        {
            return;
        }

        SetWorldPickupState(false);
        SetItemData(itemInstance.Data);
        PrimaryAttackBonus = Mathf.Max(0f, itemInstance.MainAttack);
        SubStats.Clear();

        if (itemInstance.RolledSubStats == null)
        {
            return;
        }

        for (int i = 0; i < itemInstance.RolledSubStats.Count; i++)
        {
            StatModifier modifier = itemInstance.RolledSubStats[i];
            SubStats.Add(new WeaponSubStatRoll(modifier.StatType, modifier.ModifierKind, modifier.Value));
        }

        hasRolledStats = true;
    }

    public void SetWorldPickupState(bool isWorldPickup)
    {
        IsWorldPickup = isWorldPickup;
    }

    private int GetStandardSubStatCount(WeaponRarity rarity)
    {
        switch (rarity)
        {
            case WeaponRarity.Uncommon:
                return 1;
            case WeaponRarity.Rare:
                return 2;
            case WeaponRarity.Epic:
                return 3;
            case WeaponRarity.Legendary:
                return 4;
            case WeaponRarity.Unique:
                return 3;
            case WeaponRarity.Common:
            default:
                return 0;
        }
    }

    private WeaponSubStatRoll CreateRoll(WeaponSubStatTemplate template)
    {
        switch (template)
        {
            case WeaponSubStatTemplate.AttackPercent:
                return new WeaponSubStatRoll(
                    StatType.Attack,
                    StatModifierKind.Percent,
                    UnityEngine.Random.Range(AttackPercentRange.x, AttackPercentRange.y));

            case WeaponSubStatTemplate.HealthPercent:
                return new WeaponSubStatRoll(
                    StatType.Health,
                    StatModifierKind.Percent,
                    UnityEngine.Random.Range(HealthPercentRange.x, HealthPercentRange.y));

            case WeaponSubStatTemplate.FlatHealth:
                return new WeaponSubStatRoll(
                    StatType.Health,
                    StatModifierKind.Flat,
                    UnityEngine.Random.Range(FlatHealthRange.x, FlatHealthRange.y));

            case WeaponSubStatTemplate.CooldownPercent:
                return new WeaponSubStatRoll(
                    StatType.Cooldown,
                    StatModifierKind.Percent,
                    UnityEngine.Random.Range(CooldownPercentRange.x, CooldownPercentRange.y));

            default:
                return new WeaponSubStatRoll(StatType.Attack, StatModifierKind.Flat, 0f);
        }
    }

    private void OnValidate()
    {
        PrimaryAttackBonus = Mathf.Max(0f, PrimaryAttackBonus);
        ResolveItemData();

        if (ItemData != null)
        {
            ItemData.itemType = ItemData.ItemType.Weapon;
        }
    }

    public void SetItemData(ItemData itemData)
    {
        ItemData = itemData;
        ResolveItemData();
    }

    private void ResolveItemData()
    {
        if (ItemData != null)
        {
            return;
        }

        ItemDrop itemDrop = GetComponent<ItemDrop>();
        if (itemDrop != null && itemDrop.ItemData != null)
        {
            ItemData = itemDrop.ItemData;
        }
    }
}
