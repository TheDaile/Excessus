public class StatsOperations
{
    private readonly Health health;
    private readonly StatsRegeneration statsRegeneration;
    private readonly Shield shield;
    private readonly Stamina stamina;
    private readonly IStatsEvents events;
    public StatsOperations (
    Health health,
    Shield shield,
    Stamina stamina,
    IStatsEvents events
    )   
    {
        this.health = health;
        this.shield = shield;
        this.stamina = stamina;
        this.events = events;

        statsRegeneration = new StatsRegeneration(health, shield, events);
    }

    public void Tick(float deltaTime, float currentTime)
    {
        stamina?.Tick(deltaTime, currentTime);
        statsRegeneration.Tick(deltaTime);
    }

    public void TakeDamage(float damage)
    {
        StatValidator.RequirePositive(damage, nameof(damage), "StatsOperations.TakeDamage");

        if (!health.IsAlive)
        {
            return;
        }

        float remainingDamage = shield != null ? shield.AbsorbDamage(damage) : damage;
        float absorbedDamage = damage - remainingDamage;

        if (absorbedDamage > 0f)
        {
            events.OnShieldHit(absorbedDamage);
        }

        if (remainingDamage > 0f)
        {
            health.TakeDamage(remainingDamage);
        }

        if (!health.IsAlive)
        {
            events.OnDeath();
        }
    }

    public bool UseStamina(float amount, float currentTime)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "StatsOperations.UseStamina");

        if (stamina == null) return false;

        bool used = stamina.Use(amount, currentTime);

        if (used)
        {
            events.OnStaminaUsed(amount);
        }

        return used;
    }

    public void Heal(float amount)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "StatsOperations.Heal");

        float healthBeforeHeal = health.Current;

        health.Heal(amount);

        float healedAmount = health.Current - healthBeforeHeal;

        if (healedAmount > 0f)
        {
            events.OnHealed(healedAmount);
        }
    }

    public void HealOverTime(float amount, float duration)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "StatsOperations.HealOverTime");
        StatValidator.RequireNotNegative(duration, nameof(duration), "StatsOperations.HealOverTime");

        statsRegeneration.Heal(amount, duration);
    }


    public void IncreaseMaxHealthBy(float amount, bool increaseCurrentByAmount = true)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "StatsOperations.IncreaseMaxHealthBy");

        float maxHealthBeforeIncrease = health.Max;
        float healthBeforeIncrease = health.Current;

        health.IncreaseMaxHealthBy(amount, increaseCurrentByAmount);

        if (health.Max > maxHealthBeforeIncrease)
        {
            events.OnMaxHealthChanged(maxHealthBeforeIncrease, health.Max);
        }

        float healedAmount = health.Current - healthBeforeIncrease;

        if (healedAmount > 0f)
        {
            events.OnHealed(healedAmount);
        }
    }

    public void IncreaseMaxHealthOverTime(float amount, float duration)
    {      
        StatValidator.RequirePositive(amount, nameof(amount), "StatsOperations.IncreaseMaxHealthOverTime");
        StatValidator.RequireNotNegative(duration, nameof(duration), "StatsOperations.IncreaseMaxHealthOverTime");

        float maxHealthBeforeIncrease = health.Max;

        health.IncreaseMaxHealthBy(amount, false);

        if (health.Max > maxHealthBeforeIncrease)
        {
            events.OnMaxHealthChanged(maxHealthBeforeIncrease, health.Max);
        }

        statsRegeneration.Heal(amount, duration);
    }

    public void RechargeShield(float amount)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "StatsOperations.RechargeShield");

        if (shield == null) return;

        float shieldBeforeRecharge = shield.Current;

        shield.Recharge(amount);

        float rechargedAmount = shield.Current - shieldBeforeRecharge;

        if (rechargedAmount > 0f)
        {
            events.OnShieldRecharged(rechargedAmount);
        }
    }

    public void RechargeShieldOverTime(float amount, float duration)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "StatsOperations.RechargeShieldOverTime");
        StatValidator.RequireNotNegative(duration, nameof(duration), "StatsOperations.RechargeShieldOverTime");

        statsRegeneration.RechargeShield(amount, duration);
    }

    public void IncreaseMaxShieldBy(float amount, bool increaseCurrentByAmount = true)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "StatsOperations.IncreaseMaxShieldBy");

        if (shield == null) return;

        float maxShieldBeforeIncrease = shield.Max;
        float shieldBeforeIncrease = shield.Current;

        shield.IncreaseMaxShieldBy(amount, increaseCurrentByAmount);

        if (shield.Max > maxShieldBeforeIncrease)
        {
            events.OnMaxShieldChanged(maxShieldBeforeIncrease, shield.Max);
        }

        float rechargedAmount = shield.Current - shieldBeforeIncrease;

        if (rechargedAmount > 0f)
        {
            events.OnShieldRecharged(rechargedAmount);
        }
    }
}