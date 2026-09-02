using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ColliderDmgEnemy : MonoBehaviour
{
    public Collider2D dmgCollider;
    public int dmg;
    public int direction;
    public int forceImpulseDmg;
    Rigidbody2D rb;

    private void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.IsTouching(dmgCollider))
        {
            if (collision.gameObject.CompareTag("Player"))
            {
                PlayerStats stats = collision.GetComponent<PlayerStats>();
                if (stats != null)
                {
                    stats.TakeDmg(dmg);
                }
                Despawn();
            }
            else if (collision.gameObject.CompareTag("NPC"))
            {
                Fase7NPC npc = collision.GetComponent<Fase7NPC>();
                if (npc != null)
                {
                    npc.TakeDamage(dmg);
                }
                Despawn();
            }
        }
    }

    private void Despawn()
    {
        // Try releasing through EnemyStats or PoolManager
        EnemyStats enemyStats = GetComponent<EnemyStats>();
        if (enemyStats != null)
        {
            enemyStats.Death();
        }
        else if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void ImpulseDmg(GameObject obj)
    {
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(forceImpulseDmg * direction, forceImpulseDmg), ForceMode2D.Impulse);
        }
    }
}
