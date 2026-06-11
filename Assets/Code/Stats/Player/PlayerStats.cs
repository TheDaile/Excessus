using UnityEngine;

public class PlayerStats: MonoBehaviour, IDamageable
{
    [SerializeField] private Health health = new Health();
    [SerializeField] private Shield shield = new Shield();
    [SerializeField] private Stamina stamina = new Stamina();

    private PlayerStatsEvents statsEvents;
    private StatsOperations statsOperations;
    public float CurrentHealth => health.Current;
    public float MaxHealth => health.Max;
    public bool IsDead => !health.IsAlive;
    public float CurrentShield => shield.Current;
    public float MaxShield => shield.Max;
    public bool HasShield => shield.HasShield;
    public float CurrentStamina => stamina.Current;
    public float MaxStamina => stamina.Max;
    public bool HasStamina => stamina.HasStamina;
    private void Awake()
    {
        health.Initialize();
        shield.Initialize();
        stamina.Initialize();

        statsEvents = new PlayerStatsEvents(this);
        statsOperations = new StatsOperations(health, shield, stamina, statsEvents);
    }

    private void Update()
    {
        statsOperations.Tick(Time.deltaTime, Time.time);
    }

    public void TakeDamage(float amount)
    {
        statsOperations.TakeDamage(amount);
    }

    public bool UseStamina(float amount)
    {
        return statsOperations.UseStamina(amount, Time.time);
    }

    public void Heal(float amount)
    {
        statsOperations.Heal(amount);
    }

    public void HealOverTime(float amount, float duration)
    {
        statsOperations.HealOverTime(amount, duration);
    }

    public void IncreaseMaxHealthBy(float amount, bool increaseCurrentByAmount = true)
    {
        statsOperations.IncreaseMaxHealthBy(amount, increaseCurrentByAmount);
    }

    public void IncreaseMaxHealthOverTime(float amount, float duration)
    {
        statsOperations.IncreaseMaxHealthOverTime(amount, duration);
    }

    public void RechargeShield(float amount)
    {
        statsOperations.RechargeShield(amount);
    }

    public void RechargeShieldOverTime(float amount, float duration)
    {
        statsOperations.RechargeShieldOverTime(amount, duration);
    }

    public void IncreaseMaxShieldBy(float amount, bool increaseCurrentByAmount = true)
    {
        statsOperations.IncreaseMaxShieldBy(amount, increaseCurrentByAmount);
    }
}