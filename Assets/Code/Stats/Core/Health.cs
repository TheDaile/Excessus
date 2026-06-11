using UnityEngine;

[System.Serializable]
public class Health : IStat
{
    [SerializeField] private float maxHealth = 100f;

    public string Name => "Health";
    public float Current { get; private set; }
    public float Max => maxHealth;
    
    public float CurrentHealth => Current;
    public float MaxHealth => maxHealth;
    public bool IsAlive => Current > 0f;

    public void Initialize()
    {
        Current = maxHealth;
    }

    public void TakeDamage(float amount)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "Health.TakeDamage");

        if (!IsAlive) return;

        Current = Mathf.Max(Current - amount, 0f);
    }

    public void Heal(float amount)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "Health.Heal");

        if (!IsAlive) return;

        Current = Mathf.Min(Current + amount, maxHealth);
    }

    public void IncreaseMaxHealthBy(float amount, bool increaseCurrentByAmount = true)
    {
        StatValidator.RequirePositive(amount, nameof(amount), "Health.IncreaseMaxHealthBy");

        maxHealth += amount;

        if (!IsAlive)
        {
            Current = 0f;
            return;
        }

        if (increaseCurrentByAmount)
        {
            Current = Mathf.Min(Current + amount, maxHealth);
        }
        else
        {
            Current = Mathf.Min(Current, maxHealth);
        }
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    internal bool SetCurrentForTests(float value)
    {
        if (value < 0f || value > maxHealth)
        {
            return false;
        }
        Current = value;
        return true;
    }

    internal bool SetMaxForTests(float value)
    {
        if (value <= 0f)
        {
            return false;
        }

        maxHealth = value;
        Current = Mathf.Min(Current, maxHealth);
        return true;
    }
#endif
}