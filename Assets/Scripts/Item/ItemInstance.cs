using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public sealed class ItemInstance
{
    [SerializeField] private ItemData data;
    [SerializeField] private float mainAttack;
    [SerializeField] private List<StatModifier> rolledSubStats;

    public ItemData Data => data;
    public float MainAttack => mainAttack;
    public IReadOnlyList<StatModifier> RolledSubStats => rolledSubStats;

    public bool IsEquippable =>
        data != null &&
        (data.itemType == ItemData.ItemType.Weapon || data.itemType == ItemData.ItemType.Armor);

    public bool HasRolledStats => IsEquippable && (mainAttack > 0f || (rolledSubStats != null && rolledSubStats.Count > 0));

    public ItemInstance(ItemData data, float mainAttack, List<StatModifier> rolledSubStats)
    {
        this.data = data;

        if (data != null && data.itemType == ItemData.ItemType.Weapon)
        {
            this.mainAttack = Mathf.Max(0f, mainAttack);
            this.rolledSubStats = rolledSubStats ?? new List<StatModifier>();

        }
        else
        {
            this.mainAttack = 0f;
            this.rolledSubStats = null;
        }
    }

    public static ItemInstance Create(ItemData data)
    {
        return new ItemInstance(data, 0f, new List<StatModifier>());
    }

    public static ItemInstance Create(ItemData data, float mainAttack, IEnumerable<StatModifier> rolledSubStats)
    {
        if (data == null)
        {
            return null;
        }

        List<StatModifier> clonedSubStats = null;
        if (rolledSubStats != null)
        {
            clonedSubStats = new List<StatModifier>();
            foreach (StatModifier modifier in rolledSubStats)
            {
                if (modifier == null)
                {
                    continue;
                }

                clonedSubStats.Add(new StatModifier(
                    modifier.StatType,
                    modifier.ModifierKind,
                    modifier.Value,
                    null));
            }
        }

        return new ItemInstance(data, mainAttack, clonedSubStats);
    }

    public static ItemInstance Create(ItemData data, Weapon weaponTemplate)
    {
        if (data == null)
        {
            return null;
        }

        if (data.itemType != ItemData.ItemType.Weapon || weaponTemplate == null)
        {
            return Create(data);
        }

        return Create(
            data,
            weaponTemplate.PrimaryAttackBonus,
            weaponTemplate.GetRolledSubStatSnapshot());
    }

    public ItemInstance Clone()
    {
        return Create(data, mainAttack, rolledSubStats);
    }

    public List<StatModifier> CreateRuntimeModifiers(object source)
    {
        List<StatModifier> modifiers = new List<StatModifier>();

        if (mainAttack > 0f)
        {
            modifiers.Add(new StatModifier(StatType.Attack, StatModifierKind.Flat, mainAttack, source));
        }

        if (rolledSubStats == null)
        {
            return modifiers;
        }

        for (int i = 0; i < rolledSubStats.Count; i++)
        {
            StatModifier rolledModifier = rolledSubStats[i];
            modifiers.Add(new StatModifier(
                rolledModifier.StatType,
                rolledModifier.ModifierKind,
                rolledModifier.Value,
                source));
        }

        return modifiers;
    }

    public IReadOnlyList<StatModifier> GetSubStats()
    {
        return rolledSubStats;
    }
}
