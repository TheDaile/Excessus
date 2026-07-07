using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class PlayerStats: NetworkBehaviour, IDamageable
{
    private NetworkVariable<float> networkCurrentHealth = new NetworkVariable<float>();
    private NetworkVariable<float> networkMaxHealth = new NetworkVariable<float>();
    private NetworkVariable<float> networkCurrentShield = new NetworkVariable<float>();
    private NetworkVariable<float> networkMaxShield = new NetworkVariable<float>();
    private NetworkVariable<float> networkCurrentStamina = new NetworkVariable<float>();
    private NetworkVariable<float> networkMaxStamina = new NetworkVariable<float>();

    [SerializeField] private Health health = new Health();
    [SerializeField] private Shield shield = new Shield();
    [SerializeField] private Stamina stamina = new Stamina();
    [SerializeField] private float sprintStaminaCostPerSecond = 20f;
    [SerializeField] private float jumpStaminaCost = 20f;

    private PlayerStatsEvents statsEvents;
    private StatsOperations statsOperations;
    public float CurrentHealth => IsSpawned ? networkCurrentHealth.Value : health.Current;
    public float MaxHealth => IsSpawned ? networkMaxHealth.Value : health.Max;
    public bool IsDead => CurrentHealth <= 0f;
    public float CurrentShield => IsSpawned ? networkCurrentShield.Value : shield.Current;
    public float MaxShield => IsSpawned ? networkMaxShield.Value : shield.Max;
    public bool HasShield => CurrentShield > 0f;
    public float CurrentStamina => IsSpawned ? networkCurrentStamina.Value : stamina.Current;
    public float MaxStamina => IsSpawned ? networkMaxStamina.Value : stamina.Max;
    public bool HasStamina => CurrentStamina > 0f;

    private void Awake()
    {
        InitializeStats();
        SyncNetworkStatsFromLocal();
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            SyncNetworkStatsFromLocal();
        }
    }

    private void Update()
    {
        if (statsOperations == null)
        {
            return;
        }

        if (IsSpawned && !IsServer && !IsOwner)
        {
            return;
        }

        statsOperations.Tick(Time.deltaTime, Time.time);

        if (!IsSpawned || IsServer)
        {
            SyncNetworkStatsFromLocal();
        }
    }

    public void TakeDamage(float amount)
    {
        if (IsSpawned && !IsServer)
        {
            TakeDamageRpc(amount);
            return;
        }

        ApplyTakeDamage(amount);
    }

    public bool UseStamina(float amount)
    {
        if (IsSpawned && !IsServer)
        {
            bool used = ApplyUseStamina(amount, false);

            if (used)
            {
                UseStaminaRpc(amount);
            }

            return used;
        }

        return ApplyUseStamina(amount);
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
        if (IsSpawned && !IsServer)
        {
            HealRpc(amount);
            return;
        }

        ApplyHeal(amount);
    }

    public void HealOverTime(float amount, float duration)
    {
        if (IsSpawned && !IsServer)
        {
            HealOverTimeRpc(amount, duration);
            return;
        }

        statsOperations.HealOverTime(amount, duration);
        SyncNetworkStatsFromLocal();
    }

    public void IncreaseMaxHealthBy(float amount, bool increaseCurrentByAmount = true)
    {
        if (IsSpawned && !IsServer)
        {
            IncreaseMaxHealthByRpc(amount, increaseCurrentByAmount);
            return;
        }

        statsOperations.IncreaseMaxHealthBy(amount, increaseCurrentByAmount);
        SyncNetworkStatsFromLocal();
    }

    public void IncreaseMaxHealthOverTime(float amount, float duration)
    {
        if (IsSpawned && !IsServer)
        {
            IncreaseMaxHealthOverTimeRpc(amount, duration);
            return;
        }

        statsOperations.IncreaseMaxHealthOverTime(amount, duration);
        SyncNetworkStatsFromLocal();
    }

    public void RechargeShield(float amount)
    {
        if (IsSpawned && !IsServer)
        {
            RechargeShieldRpc(amount);
            return;
        }

        ApplyRechargeShield(amount);
    }

    public void RechargeShieldOverTime(float amount, float duration)
    {
        if (IsSpawned && !IsServer)
        {
            RechargeShieldOverTimeRpc(amount, duration);
            return;
        }

        statsOperations.RechargeShieldOverTime(amount, duration);
        SyncNetworkStatsFromLocal();
    }

    public void IncreaseMaxShieldBy(float amount, bool increaseCurrentByAmount = true)
    {
        if (IsSpawned && !IsServer)
        {
            IncreaseMaxShieldByRpc(amount, increaseCurrentByAmount);
            return;
        }

        statsOperations.IncreaseMaxShieldBy(amount, increaseCurrentByAmount);
        SyncNetworkStatsFromLocal();
    }

    private void InitializeStats()
    {
        health.Initialize();
        shield.Initialize();
        stamina.Initialize();

        statsEvents = new PlayerStatsEvents(this);
        statsOperations = new StatsOperations(health, shield, stamina, statsEvents);
    }

    private void ApplyTakeDamage(float amount)
    {
        statsOperations.TakeDamage(amount);
        SyncNetworkStatsFromLocal();
    }

    private bool ApplyUseStamina(float amount, bool syncNetworkStats = true)
    {
        bool used = statsOperations.UseStamina(amount, Time.time);

        if (used && syncNetworkStats)
        {
            SyncNetworkStatsFromLocal();
        }

        return used;
    }

    private void ApplyHeal(float amount)
    {
        statsOperations.Heal(amount);
        SyncNetworkStatsFromLocal();
    }

    private void ApplyRechargeShield(float amount)
    {
        statsOperations.RechargeShield(amount);
        SyncNetworkStatsFromLocal();
    }

    private void SyncNetworkStatsFromLocal()
    {
        if (IsSpawned && !IsServer)
        {
            return;
        }

        networkCurrentHealth.Value = health.Current;
        networkMaxHealth.Value = health.Max;
        networkCurrentShield.Value = shield.Current;
        networkMaxShield.Value = shield.Max;
        networkCurrentStamina.Value = stamina.Current;
        networkMaxStamina.Value = stamina.Max;
    }

    [Rpc(SendTo.Server)]
    private void TakeDamageRpc(float amount)
    {
        ApplyTakeDamage(amount);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Owner)]
    private void UseStaminaRpc(float amount)
    {
        ApplyUseStamina(amount);
    }

    [Rpc(SendTo.Server)]
    private void HealRpc(float amount)
    {
        ApplyHeal(amount);
    }

    [Rpc(SendTo.Server)]
    private void HealOverTimeRpc(float amount, float duration)
    {
        statsOperations.HealOverTime(amount, duration);
        SyncNetworkStatsFromLocal();
    }

    [Rpc(SendTo.Server)]
    private void IncreaseMaxHealthByRpc(float amount, bool increaseCurrentByAmount)
    {
        statsOperations.IncreaseMaxHealthBy(amount, increaseCurrentByAmount);
        SyncNetworkStatsFromLocal();
    }

    [Rpc(SendTo.Server)]
    private void IncreaseMaxHealthOverTimeRpc(float amount, float duration)
    {
        statsOperations.IncreaseMaxHealthOverTime(amount, duration);
        SyncNetworkStatsFromLocal();
    }

    [Rpc(SendTo.Server)]
    private void RechargeShieldRpc(float amount)
    {
        ApplyRechargeShield(amount);
    }

    [Rpc(SendTo.Server)]
    private void RechargeShieldOverTimeRpc(float amount, float duration)
    {
        statsOperations.RechargeShieldOverTime(amount, duration);
        SyncNetworkStatsFromLocal();
    }

    [Rpc(SendTo.Server)]
    private void IncreaseMaxShieldByRpc(float amount, bool increaseCurrentByAmount)
    {
        statsOperations.IncreaseMaxShieldBy(amount, increaseCurrentByAmount);
        SyncNetworkStatsFromLocal();
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
