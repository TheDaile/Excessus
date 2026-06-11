using UnityEngine;

public class ShieldRegeneration
{
    private readonly Shield shield;
    private readonly IStatsEvents events;

    private bool isRecharging;
    private float startShield;
    private float targetShield;
    private float duration;
    private float elapsed;
    private float lastLerpedShield;

    public ShieldRegeneration(Shield shield, IStatsEvents events)
    {
        this.shield = shield;
        this.events = events;
    }

    public void Recharge(float amount, float duration)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "ShieldRegeneration.Recharge");
        StatValidator.RequireNotNegative(duration, nameof(duration), "ShieldRegeneration.Recharge");

        if (shield == null) return;

        if (duration == 0f)
        {
            RechargeInstantly(amount);
            return;
        }

        startShield = shield.Current;
        targetShield = Mathf.Min(startShield + amount, shield.Max);

        if (targetShield <= startShield) return;

        this.duration = duration;
        elapsed = 0f;
        lastLerpedShield = startShield;
        isRecharging = true;
    }

    public void Tick(float deltaTime)
    {
        StatValidator.RequireNotNegative(deltaTime, nameof(deltaTime), "ShieldRegeneration.Tick");

        if (!isRecharging || deltaTime == 0f) return;

        elapsed += deltaTime;

        float progress = Mathf.Clamp01(elapsed / duration);
        float nextShield = Mathf.Lerp(startShield, targetShield, progress);
        float rechargeStep = nextShield - lastLerpedShield;

        lastLerpedShield = nextShield;

        if (rechargeStep > 0f)
        {
            RechargeInstantly(rechargeStep);
        }

        if (progress >= 1f)
        {
            isRecharging = false;
        }
    }

    private void RechargeInstantly(float amount)
    {
        float shieldBeforeRecharge = shield.Current;

        shield.Recharge(amount);

        float rechargedAmount = shield.Current - shieldBeforeRecharge;

        if (rechargedAmount > 0f)
        {
            events.OnShieldRecharged(rechargedAmount);
        }
    }
}