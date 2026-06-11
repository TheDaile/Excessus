using UnityEngine;

[System.Serializable]
public class Stamina : IStat
{
    [SerializeField] private float maxStamina = 100f;
    [SerializeField] private float regenerationPerSecond = 15f;
    [SerializeField] private float regenerationDelay = 1f;

    private float lastUseTime;

    public string Name => "Stamina";
    public float Current => CurrentStamina;
    public float Max => maxStamina;

    public float CurrentStamina { get; private set; }
    public float MaxStamina => maxStamina;
    public bool HasStamina => CurrentStamina > 0f;

    public void Initialize()
    {
        CurrentStamina = maxStamina;
    }

    public void Tick(float deltaTime, float currentTime)
    {
        StatValidator.RequireNotNegative(deltaTime, nameof(deltaTime), "Stamina.Tick");

        if (deltaTime == 0f) return;
        
        if (currentTime < lastUseTime + regenerationDelay) return;

        Regenerate(regenerationPerSecond * deltaTime);
    }

    public bool Use(float amount, float currentTime)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "Stamina.Use");

        if (CurrentStamina < amount) return false;

        CurrentStamina -= amount;
        lastUseTime = currentTime;
        return true;
    }

    public void Regenerate(float amount)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "Stamina.Regenerate");

        CurrentStamina = Mathf.Min(CurrentStamina + amount, maxStamina);
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    internal bool SetCurrentForTests(float value)
    {
        if (value < 0f || value > maxStamina)
        {
            return false;
        }

        CurrentStamina = value;
        return true;
    }

    internal bool SetMaxForTests(float value)
    {
        if (value <= 0f)
        {
            return false;
        }

        maxStamina = value;
        CurrentStamina = Mathf.Min(CurrentStamina, maxStamina);
        return true;
    }

    internal bool SetRegenerationPerSecondForTests(float value)
    {
        if (value <= 0f)
        {
            return false;
        }

        regenerationPerSecond = value;
        return true;
    }

    internal bool SetRegenerationDelayForTests(float value)
    {
        if (value < 0f)
        {
            return false;
        }

        regenerationDelay = value;
        return true;
    }
#endif
}
