using UnityEngine;
using System.Collections;

public class ProjectileEnemyComplex : MonoBehaviour
{
    public int projectileSpeed;
    public int projectileDmg;
    public int direction;

    private Rigidbody2D rb;
    private Coroutine deactivateCoroutine;
    public Vector2 directionPlayer;
    

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
    void FixedUpdate()
    {
        Moviment(directionPlayer);
    }

    void Moviment(Vector2 direction)
    {
        if (rb != null)
        {
            rb.MovePosition(rb.position + direction * (projectileSpeed * Time.deltaTime));
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
