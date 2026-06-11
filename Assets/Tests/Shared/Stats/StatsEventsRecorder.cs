public class StatsEventsRecorder : IStatsEvents
{
    public int DeathCallCount;
    public int ShieldHitCallCount;
    public int StaminaUsedCallCount;
    public int HealedCallCount;
    public int MaxHealthChangedCallCount;
    public int MaxShieldChangedCallCount;
    public int ShieldRechargedCallCount;

    public float LastShieldHitAmount;
    public float LastStaminaUsedAmount;
    public float LastHealedAmount;
    public float LastShieldRechargedAmount;
    public float LastPreviousMaxHealth;
    public float LastCurrentMaxHealth;
    public float LastPreviousMaxShield;
    public float LastCurrentMaxShield;

    public void OnDeath() => DeathCallCount++;

    public void OnShieldHit(float absorbedDamage)
    {
        ShieldHitCallCount++;
        LastShieldHitAmount = absorbedDamage;
    }

    public void OnStaminaUsed(float amount)
    {
        StaminaUsedCallCount++;
        LastStaminaUsedAmount = amount;
    }

    public void OnHealed(float amount)
    {
        HealedCallCount++;
        LastHealedAmount = amount;
    }

    public void OnMaxHealthChanged(float previousMax, float currentMax)
    {
        MaxHealthChangedCallCount++;
        LastPreviousMaxHealth = previousMax;
        LastCurrentMaxHealth = currentMax;
    }

    public void OnMaxShieldChanged(float previousMax, float currentMax)
    {
        MaxShieldChangedCallCount++;
        LastPreviousMaxShield = previousMax;
        LastCurrentMaxShield = currentMax;
    }

    public void OnShieldRecharged(float amount)
    {
        ShieldRechargedCallCount++;
        LastShieldRechargedAmount = amount;
    }
}
