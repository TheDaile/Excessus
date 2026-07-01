using UnityEngine;
using System;

public class EnemyStatsOperations
{
    private  readonly Health health;

    public EnemyStatsOperations(Health health)
    {
        this.health = health;
    }


    public void TakeDamage(float damage, Action onDeath)
    {

        health.TakeDamage(damage);
        if (!health.IsAlive)
        {
            onDeath?.Invoke();
        }
    }
}
