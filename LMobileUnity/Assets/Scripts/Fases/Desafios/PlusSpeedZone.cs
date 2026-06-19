using UnityEngine;

public class PlusSpeedZone : MonoBehaviour
{
    [Header("PlusSpeed (Impulso)")]
    [Tooltip("Velocidade horizontal do impulso aplicada na direcao em que o player caminha.")]
    [SerializeField] private float boostSpeed = 20f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var pm = collision.GetComponent<PlayerMovements>();
        var rb = collision.attachedRigidbody;
        if (pm == null || rb == null) return;

        // Direcao do caminhar (1 = direita, -1 = esquerda)
        rb.linearVelocity = new Vector2(boostSpeed * pm.direction, rb.linearVelocity.y);

        // Suspende o controle de X ate o boost decair (ver modificacao no PlayerMovements)
        pm.isPlusSpeedBoost = true;
    }
}
