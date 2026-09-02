using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ProjectilePlayer : MonoBehaviour
{
    public int projectileSpeed;
    public int projectileDmg;
    public int direction = 1;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Coroutine deactivateCoroutine;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
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
        if (sr != null)
        {
            sr.flipX = direction < 0;
        }
    }

    private void DeactivateProjectile()
    {
        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        // Verifica se o objeto atingido possui IDamageable
        IDamageable damageable = collision.GetComponent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(projectileDmg);
            DeactivateProjectile();
        }
        else if (collision.gameObject.CompareTag("Ground"))
        {
            DeactivateProjectile();
        }
    }
}
