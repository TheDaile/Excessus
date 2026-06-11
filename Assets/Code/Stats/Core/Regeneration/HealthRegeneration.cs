using UnityEngine;
public class HealthRegeneration
{
    private readonly Health health;
    private readonly IStatsEvents events;

    private bool isHealing;
    private float startHealth;
    private float targetHealth;
    private float duration;
    private float elapsed;
    private float lastLerpedHealth;

    public HealthRegeneration(Health health, IStatsEvents events)
    {
        this.health = health;
        this.events = events;
    }

    public void Heal(float amount, float duration)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "HealthRegeneration.Heal");
        StatValidator.RequireNotNegative(duration, nameof(duration), "HealthRegeneration.Heal");

        if (health == null || !health.IsAlive) return;

        if (duration == 0f)
        {
            HealInstantly(amount);
            return;
        }

        startHealth = health.Current;
        targetHealth = UnityEngine.Mathf.Min(health.Current + amount, health.Max);

        if (targetHealth <= startHealth) return;

        this.duration = duration;
        elapsed = 0f;
        lastLerpedHealth = startHealth;
        isHealing = true;
    }

    public void Tick(float deltaTime)
    {
        StatValidator.RequireNotNegative(deltaTime, nameof(deltaTime), "HealthRegeneration.Tick");

        if (!isHealing || deltaTime == 0f) return;

        if (!health.IsAlive)
        {
            isHealing = false;
            return;
        }

        elapsed += deltaTime;

        float progress = Mathf.Clamp01(elapsed / duration);
        float nextHealth = Mathf.Lerp(startHealth, targetHealth, progress);
        float healStep = nextHealth - lastLerpedHealth;

        lastLerpedHealth = nextHealth;

        if (healStep > 0f)
        {
            HealInstantly(healStep);
        }

        if (progress >= 1f)
        {
            isHealing = false;
        }
    }

    private void HealInstantly(float amount)
    {
        float healthBeforeHeal = health.Current;

        health.Heal(amount);

        float healedAmount = health.Current - healthBeforeHeal;

        if (healedAmount > 0f)
        {
            events.OnHealed(healedAmount);
        }
    }
}