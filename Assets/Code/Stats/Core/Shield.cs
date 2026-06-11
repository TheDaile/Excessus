using UnityEngine;

[System.Serializable]
public class Shield : IStat
{
    [SerializeField] private float maxShield = 50f;

    public string Name => "Shield";
    public float Current => CurrentShield;
    public float Max => maxShield;
    
    public float CurrentShield { get; private set; }
    public float MaxShield => maxShield;
    public bool HasShield => CurrentShield > 0f;

    public void Initialize()
    {
        CurrentShield = maxShield;
    }

    public float AbsorbDamage(float damage)
    {
        StatValidator.RequirePositive(damage, nameof(damage), "Shield.AbsorbDamage");

        if (!HasShield) return damage;

        float absorbedDamage = Mathf.Min(CurrentShield, damage);
        CurrentShield -= absorbedDamage;
        return damage - absorbedDamage;
    }

    public void IncreaseMaxShieldBy(float amount, bool increaseCurrentByAmount = true)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "Shield.IncreaseMaxShieldBy");

        maxShield += amount;

        if (increaseCurrentByAmount)
        {
            CurrentShield = Mathf.Min(CurrentShield + amount, maxShield);
            return;
        }

        CurrentShield = Mathf.Min(CurrentShield, maxShield);
    }

    public void Recharge(float amount)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "Shield.Recharge");

        CurrentShield = Mathf.Min(CurrentShield + amount, maxShield);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    internal bool SetCurrentForTests(float value)
    {
        if (value < 0f || value > maxShield)
        {
            return false;
        }

        CurrentShield = value;
        return true;
    }

    internal bool SetMaxForTests(float value)
    {
        if (value <= 0f)
        {
            return false;
        }

        maxShield = value;
        CurrentShield = Mathf.Min(CurrentShield, maxShield);
        return true;
    }
#endif
}