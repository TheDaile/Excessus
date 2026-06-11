public class StatsRegeneration
{
    private readonly HealthRegeneration healthRegeneration;
    private readonly ShieldRegeneration shieldRegeneration;

    public StatsRegeneration(
        Health health,
        Shield shield,
        IStatsEvents events)
    {
        healthRegeneration = new HealthRegeneration(health, events);
        shieldRegeneration = new ShieldRegeneration(shield, events);
    }

    public void Tick(float deltaTime)
    {
        healthRegeneration.Tick(deltaTime);
        shieldRegeneration.Tick(deltaTime);
    }

    public void Heal(float amount, float duration)
    {
        healthRegeneration.Heal(amount, duration);
    }

    public void RechargeShield(float amount, float duration)
    {
        shieldRegeneration.Recharge(amount, duration);
    }
}