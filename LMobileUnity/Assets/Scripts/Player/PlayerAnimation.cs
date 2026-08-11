using UnityEngine;

public class PlayerAnimation : MonoBehaviour
{
    private Animator animator;
    private Rigidbody2D rb;
    private PlayerMovements movement;
    private AnimatorOverrideController overrideController;

    void Awake()
    {
        animator = GetComponent<Animator>();

        // Cria uma cópia de override baseada no AnimatorController atual
        overrideController = new AnimatorOverrideController(animator.runtimeAnimatorController);

        // Aplica o override controller no Animator
        animator.runtimeAnimatorController = overrideController;
    }

    void Start()
    {
       
        rb = GetComponent<Rigidbody2D>();
        movement = GetComponent<PlayerMovements>();

        if (movement != null)
        {
            movement.OnJump += PlayJump;
        }
    }

    private void OnDestroy()
    {
        if (movement != null)
        {
            movement.OnJump -= PlayJump;
        }
    }

    void FixedUpdate()
    {
        if (movement == null || animator == null) return;

        // 1. Estados Básicos
        // Usamos a intenção (input) para uma resposta mais imediata na animação
        float horizontalIntent = (movement.input != null) ? movement.input.Direction : 0f;
        animator.SetFloat("speed", Mathf.Abs(horizontalIntent));
        animator.SetBool("onGround", movement.isGrounded || movement.isBelt);

        // 2. Lógica de Pulo/Queda (baseada na velocidade vertical)
        float verticalVelocity = rb.linearVelocity.y;
        animator.SetFloat("verticalVelocity", verticalVelocity);

        // 3. Estados de Habilidade
        animator.SetBool("isDash", movement.isDash);
    }

    // Para animações baseadas em eventos
    public void PlayJump() => animator.SetTrigger("Jump");

    public void TradeAnimation(AnimationClip previewAnimation, AnimationClip newAnimation)
    {
        if (overrideController != null && previewAnimation != null && newAnimation != null)
        {
            // Substitui o clipe antigo pelo novo
            overrideController[previewAnimation.name] = newAnimation;
        }
    }
}