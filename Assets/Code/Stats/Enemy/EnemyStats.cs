using UnityEngine;

public class EnemyStats : MonoBehaviour, IDamageable
{
    [SerializeField] private Health health = new Health();

    public float CurrentHealth => health.Current;
    public float MaxHealth => health.Max;
    public bool IsDead => !health.IsAlive;

    private void Awake()
    {
        health.Initialize();

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void TakeDamage(float amount)
    {
        Debug.Log($"Enemy took {CurrentHealth} health");
        health.TakeDamage(amount);
        if (!health.IsAlive)
        {
            Destroy(gameObject);
        }
    }

}
