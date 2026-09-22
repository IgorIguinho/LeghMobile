using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class CaixaController : MonoBehaviour
{
    private Rigidbody2D rb;
    private int direction = 1;
    private float speed = 0f;
    private float boundaryMinX = -100f;
    private float boundaryMaxX = 100f;
    private bool isInitialized = false;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        ConfigureRigidbody();
    }

    private void OnValidate()
    {
        if (rb == null) rb = GetComponent<Rigidbody2D>();
        ConfigureRigidbody();
    }

    private void ConfigureRigidbody()
    {
        if (rb != null)
        {
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
            rb.constraints = RigidbodyConstraints2D.FreezePositionY | RigidbodyConstraints2D.FreezeRotation;
        }
    }

    public void Init(int direction, float speed, float boundaryMinX, float boundaryMaxX)
    {
        this.direction = direction >= 0 ? 1 : -1;
        this.speed = speed;
        this.boundaryMinX = boundaryMinX;
        this.boundaryMaxX = boundaryMaxX;
        isInitialized = true;

        if (rb == null)
        {
            rb = GetComponent<Rigidbody2D>();
            ConfigureRigidbody();
        }

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(this.direction * this.speed, 0f);
        }
    }

    private void FixedUpdate()
    {
        if (!isInitialized) return;

        if (rb != null)
        {
            rb.linearVelocity = new Vector2(direction * speed, 0f);
        }

        float currentX = transform.position.x;
        if (currentX < boundaryMinX || currentX > boundaryMaxX)
        {
            ReturnToPool();
        }
    }

    private void ReturnToPool()
    {
        isInitialized = false;
        if (rb != null)
        {
            rb.linearVelocity = Vector2.zero;
        }

        if (PoolManager.Instance != null)
        {
            PoolManager.Instance.Release(gameObject);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
