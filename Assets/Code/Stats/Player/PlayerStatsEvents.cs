using UnityEngine;

public class PlayerStatsEvents : IStatsEvents
{
    private readonly PlayerStats stats;

    public PlayerStatsEvents(PlayerStats stats)
    {
        this.stats = stats;
    }

    public void OnDeath()
    {
        Debug.Log("Player died.", stats);
    }

    public void OnShieldHit(float absorbedDamage)
    {
        Debug.Log("Player shield absorbed " + absorbedDamage + " damage.", stats);
    }

    public void OnStaminaUsed(float amount)
    {
        Debug.Log("Player used " + amount + " stamina.", stats);
    }

    public void OnHealed(float amount)
    {
        Debug.Log("Player healed " + amount + " health.", stats);
    }

    public void OnMaxHealthChanged(float previousMax, float currentMax)
    {
        Debug.Log($"Player max health changed from {previousMax} to {currentMax}.", stats);
    }

    public void OnMaxShieldChanged(float previousMax, float currentMax)
    {
        Debug.Log($"Player max shield changed from {previousMax} to {currentMax}.", stats);
    }

    public void OnShieldRecharged(float amount)
    {
        Debug.Log("Player shield recharged by " + amount + ".", stats);
    }
}