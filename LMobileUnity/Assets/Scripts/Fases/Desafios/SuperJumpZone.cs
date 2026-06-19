using UnityEngine;

public class SuperJumpZone : MonoBehaviour
{
    [Header("SuperJump (Trampolim)")]
    [Tooltip("Altura aproximada (em unidades) que o player atinge ao tocar o trampolim.")]
    [SerializeField] private float jumpHeight = 5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        Rigidbody2D rb = collision.attachedRigidbody;          // rb do 
        if (rb == null) return;

        PlayerMovements pm = rb.GetComponent<PlayerMovements>();
        if (pm == null) return;

        // g efetivo do player (gravidade global * gravityScale do rb)
        float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        if (g == 0f) return;                                   // evita divisão por zero / dash em andamento

        float gravityDirection = pm.isGravityInverted ? -1 : 1 ;

     

        // v = sqrt(2 * g * h)  -> velocidade para atingir a altura desejada
        float launchVelocity = Mathf.Sqrt(Mathf.Abs(2f * g * jumpHeight ));

        // Define o Y diretamente (sobrescreve a queda) e PRESERVA o X.
        // Não usamos Vector2.zero em nenhum momento -> o impulso não é zerado aqui.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, launchVelocity * gravityDirection);


      
    }
}
