public interface IStatsEvents
{
    void OnDeath();
    void OnShieldHit(float absorbedDamage);
    void OnStaminaUsed(float amount);
    void OnHealed(float amount);
    void OnMaxHealthChanged(float previousMax, float currentMax);
    void OnMaxShieldChanged(float previousMax, float currentMax);
    void OnShieldRecharged(float amount);
}