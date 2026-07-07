using UnityEngine;

public class PlayerStats: MonoBehaviour, IDamageable
{
    [SerializeField] private Health health = new Health();
    [SerializeField] private Shield shield = new Shield();
    [SerializeField] private Stamina stamina = new Stamina();
    [SerializeField] private float sprintStaminaCostPerSecond = 20f;
    [SerializeField] private float jumpStaminaCost = 20f;

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

    public bool UseSprintStamina(float deltaTime)
    {
        StatValidator.RequireNotNegative(deltaTime, nameof(deltaTime), "PlayerStats.UseSprintStamina");

        if (deltaTime == 0f || sprintStaminaCostPerSecond <= 0f)
        {
            return true;
        }

        return UseStamina(sprintStaminaCostPerSecond * deltaTime);
    }

    public bool UseJumpStamina()
    {
        if (jumpStaminaCost <= 0f)
        {
            return true;
        }

        return UseStamina(jumpStaminaCost);
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

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    internal bool SetSprintStaminaCostPerSecondForTests(float value)
    {
        if (value < 0f)
        {
            return false;
        }

        sprintStaminaCostPerSecond = value;
        return true;
    }

    internal bool SetJumpStaminaCostForTests(float value)
    {
        if (value < 0f)
        {
            return false;
        }

        jumpStaminaCost = value;
        return true;
    }
#endif
}
