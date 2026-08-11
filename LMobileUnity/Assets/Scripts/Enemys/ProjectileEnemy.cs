using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectileEnemy : MonoBehaviour
{
    public int projectileSpeed;
    public int projectileDmg;
    public int direction;

    private Rigidbody2D rb;
    private Coroutine deactivateCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    private void OnEnable()
    {
        if (deactivateCoroutine != null) StopCoroutine(deactivateCoroutine);
        deactivateCoroutine = StartCoroutine(DeactivateAfterDelay(10f));
    }

    private void OnDisable()
    {
        if (deactivateCoroutine != null)
        {
            StopCoroutine(deactivateCoroutine);
            deactivateCoroutine = null;
        }
    }

    private IEnumerator DeactivateAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        DeactivateProjectile();
    }

    // Update is called once per frame
    void Update()
    {
        Moviment(); 
    }

    void Moviment()
    {
        if (rb != null)
        {
            rb.linearVelocity = new Vector2(projectileSpeed * direction, 0);
        }
    }

    private void DeactivateProjectile()
    {
        if (Fase7PoolManager.Instance != null)
        {
            Fase7PoolManager.Instance.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player")) 
        {
            PlayerStats stats = collision.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDmg(projectileDmg);
            }
            DeactivateProjectile();
        }
        else if (collision.gameObject.CompareTag("NPC"))
        {
            Fase7NPC npc = collision.GetComponent<Fase7NPC>();
            if (npc != null)
            {
                npc.TakeDamage(projectileDmg);
            }
            DeactivateProjectile();
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            DeactivateProjectile();
        }
    }
}
