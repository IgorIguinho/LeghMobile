using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStats : MonoBehaviour ,  IDamageable
{
    [Header("Health")]
    [SerializeField] private int maxHealth = 1;
    private int currentHealth;

    public GameObject dropItem;
    public int dropChance = 50; // Chance de drop em porcentagem (0 a 100)

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

        // Drop item logic
        if (dropItem != null)
        {
            int randomChance = Random.Range(0, 101);
            if (randomChance <= dropChance)
            {
                GameObject recoveryItem = null;
                if (PoolManager.Instance != null)
                {
                    recoveryItem = PoolManager.Instance.Get(dropItem, transform.position, Quaternion.identity);
                }
                else
                {
                    recoveryItem = Instantiate(dropItem, transform.position, Quaternion.identity);
                }
            }
        }
    }
}
