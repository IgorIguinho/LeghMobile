using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStats : MonoBehaviour ,  IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 1;
    private int currentHealth;

    public System.Action<EnemyStats> OnEnemyDeath;

    private void Awake()
    {
        currentHealth = maxHealth;
    }

    private void OnEnable()
    {
        currentHealth = maxHealth;
    }

    public void TakeDamage(int damage)
    {
        currentHealth -= damage;
        if (currentHealth <= 0)
        {
            Death();
        }
    }

    public void Death()
    {
        if (OnEnemyDeath != null)
        {
            OnEnemyDeath.Invoke(this);
        }
        else if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(gameObject);
        }
        else
        {
            Destroy(this.gameObject);
        }
    }
}
