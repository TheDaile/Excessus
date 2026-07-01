using UnityEngine;

public class Target : MonoBehaviour
{
    public float health = 50f;

    public void TakeDamageOfBox(float amount)
    {
        health -= amount;
        if (health <= 0f)
        {
            Die();
        }
    }

    private void Die()
    {
        Destroy(gameObject);
    }
}
