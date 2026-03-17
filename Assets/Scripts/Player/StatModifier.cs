using System;
using UnityEngine;

public enum StatType
{
    Attack,
    Health,
    Cooldown
}

public enum StatModifierKind
{
    Flat,
    Percent
}

[Serializable]
public sealed class StatModifier
{
    [SerializeField] private StatType statType;
    [SerializeField] private StatModifierKind modifierKind;
    [SerializeField] private float value;
    [NonSerialized] private object source;

    public StatType StatType => statType;
    public StatModifierKind ModifierKind => modifierKind;
    public float Value => value;
    public object Source => source;

    public StatModifier(float value, StatModifierKind modifierKind, StatType statType, object source)
    {
        this.value = value;
        this.modifierKind = modifierKind;
        this.statType = statType;
        this.source = source;
    }

    public StatModifier(StatType statType, StatModifierKind modifierKind, float value, object source)
        : this(value, modifierKind, statType, source)
    {
    }

    public string GetDisplayText()
    {
        string statLabel = GetStatLabel();

        if (modifierKind == StatModifierKind.Percent)
        {
            float percentValue = value * 100f;
            return $"{percentValue:+0.##;-0.##;0}% {statLabel}";
        }

        return $"{value:+0.##;-0.##;0} {statLabel}";
    }

    private string GetStatLabel()
    {
        switch (StatType)
        {
            case StatType.Attack:
                return "ATK";
            case StatType.Health:
                return "HP";
            case StatType.Cooldown:
                return "CD";
            default:
                return StatType.ToString();
        }
    }
}
