using UnityEngine;

public class Target : MonoBehaviour, IDamageable
{
    [SerializeField] private Health health = new Health();
    public float CurrentHealth => health.Current;
    public float MaxHealth => health.Max;
    public bool IsDead => !health.IsAlive;

    private void Awake()
    {
        health.Initialize();
    }

    public void TakeDamage(float amount)
    {
        health.TakeDamage(amount);
        if (!health.IsAlive)
        {
            Die();
        }
    }

    public void Die()
    {
        Destroy(gameObject);
    }
}
