using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Data;

public class PlayerMovements : MonoBehaviour
{


    InputReader input;

    Rigidbody2D rb;
    Animator animator;
    RewindObj rewindObj;

    [Header("Movimento on ground")]
    public float speed;
    public float speedOnAir;
    public int direction;
    public bool isFaceRight = true;
    public bool canMove = true;
    

    [Header("Swtich speed")]
    public bool switchSpeedSlow;
    bool isSwtichSpeed = false;
    public float speedSwitch;
    public LayerMask swtichSpeedMask;

    [Header("PlusSpeed Boost")]
    public bool isPlusSpeedBoost = false;

    [Header("DontJump")]
    public bool canJump = true;

    [Header("Invert Gravity")]
    public bool isGravityInverted = false;

    [Header("Jump")]
    public float jumpForce;
    public float airJumpForce;
    public int numberJump;
    public bool isGrounded;
    public Vector2 lengthGroundedCheck;
    public Transform groundChecker;
    public LayerMask groundMask;

    [Header("WallJump")]
    public float wallJumpForce;
    public float wallHorizontalJumpForce;
    public float wallFallForce;
    [Tooltip("Tempo para conseguir se mover após realizar o pulo")] public float timeWallJump;
    public bool isWall;
    public Vector2 lengthWallCheck;
    public Transform wallChecker;
    public LayerMask wallMask;

    [Header("Dash")]
    public float dashForce;
    public float timeDash;
    public float dashCooldown;
    public GameObject trailObject;
    public bool canDash = true;
    private bool isDash;
    public GameObject buttonDash;
    public Color canDashColor;
    public Color notCanDashColor;

    [Header("Spear Dash Upgrade")]
    public GameObject spearVisual;
    public Vector2 spearArea;
    public Vector2 spearOffset;
    public int spearDamage = 1;
    public float enemyKnockbackForce = 10f;
    public float playerKnockbackForce = 8f;
    public float hitStopDuration = 0.1f;
    public float shakeMagnitude = 0.15f;
    public float shakeDuration = 0.15f;
    public LayerMask enemyLayer;

    [Header("Prototipo da corda")]
    public bool isRope;
    public float ropeJumpForce;
    public float ropeHorizontalJumpForce;
    public float ropeFall;
    public LayerMask layerRope;

    private void OnEnable()
    {
        if (input != null)
        {
            input.JumpTriggered += OnJumpInput;
            input.DashTriggered += OnDashInput;
        }
    }

    private void OnDisable()
    {
        if (input != null)
        {
            input.JumpTriggered -= OnJumpInput;
            input.DashTriggered -= OnDashInput;
        }
    }

    private void Awake()
    {
        input = GetComponent<InputReader>();
        input.TradeActionMap(input.controls.Land, input.controls.Dialogue);
    }

    void OnJumpInput() => Jump();
    void OnDashInput()
    {
        if (PlayerSkillsManager.Instance != null && !PlayerSkillsManager.Instance.IsSkillUnlocked(SkillType.Dash) && !PlayerSkillsManager.Instance.IsSkillUnlocked(SkillType.Spear))
        {
            return;
        }
        if (canDash) { StartCoroutine(Dash()); }
    }

    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        animator = gameObject.GetComponent<Animator>();
        rewindObj = gameObject.GetComponent<RewindObj>();
        
    }

    void FixedUpdate()
    {
        if (!rewindObj.isRewind)
        {
            if (canMove) { Moviment(); }
            CheckGround();
            WallFall();
            InRope();
        }
    }

    void Moviment()
    {
        if (isDash) return; 

        // --- PlusSpeed boost ---
        if (isPlusSpeedBoost)
        {
            if (Mathf.Abs(rb.linearVelocity.x) <= speed)
            {
                isPlusSpeedBoost = false; // boost decaiu -> controle normal volta
            }
            else
            {
                animator.SetFloat("speed", Mathf.Abs((input != null) ? input.Direction : 0f));
                return; // ignora a reescrita normal de X enquanto boostando
            }
        }
        // --- fim PlusSpeed ---

        float currentDirection = (input != null) ? input.Direction : 0f;
        float switchSpeed = switchSpeedSlow ? (speed / speedSwitch) : (speed * speedSwitch);

        if (isGrounded  && !isSwtichSpeed) // movimento normal no chão
        { rb.linearVelocity = new Vector2(speed * currentDirection , rb.linearVelocity.y); }

        else if (isSwtichSpeed) //movimento modificado pelo terreno de switch speed
        { rb.linearVelocity = new Vector2(switchSpeed * currentDirection, rb.linearVelocity.y);  }

        else { rb.linearVelocity = new Vector2(speedOnAir * currentDirection , rb.linearVelocity.y); } //Movimento norma no ar
        animator.SetFloat("speed", Mathf.Abs(currentDirection));
        
        if (rb.linearVelocity.x * direction < 0f)
        {
            Flip();
        }
    }

    public void ToggleGravity()
    {
        isGravityInverted = !isGravityInverted;

        // Troca o SINAL da gravidade preservando a magnitude (3 -> -3 -> 3)
        rb.gravityScale = Mathf.Abs(rb.gravityScale) * (isGravityInverted ? -1f : 1f);

        // Zera o Y para o flip ficar limpo
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        // Rotaciona 180 no eixo X (de cabeca para baixo / volta ao normal)
        transform.Rotate(180f, 0f, 0f);
    }

    void Jump()
    {
        if (!canJump) return;   // DontJump: bloqueia o pulo
        if (isDash)  return; 
        float g = isGravityInverted ? -1f : 1f;
        if (isGrounded) 
        {
            numberJump = 0;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(0f, jumpForce * g), ForceMode2D.Impulse);
            numberJump++;
        }
        else if (isWall)
        {
            numberJump = 0;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(wallHorizontalJumpForce * -direction , wallJumpForce * g), ForceMode2D.Impulse);
            Flip();
            numberJump++;
            StartCoroutine(StopMove());
        }
        else if (isRope)
        {
            numberJump = 0;
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(ropeHorizontalJumpForce * direction, ropeJumpForce * g), ForceMode2D.Impulse);
            numberJump++;
        }
        else if (numberJump < 1)
        {
            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(0f, airJumpForce * g), ForceMode2D.Impulse);
            numberJump++;
        }
    
}

    IEnumerator Dash()
    { 
        isDash = true;
        canDash = false;
        float gravityScale = rb.gravityScale;
        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;

        //Dash, se estiver na parede ele vai para o outro lado
        if (isWall) 
        {
            rb.linearVelocity = new Vector2(dashForce * -direction , 0); 
            Flip(); 
        }
        else { rb.linearVelocity = new Vector2(dashForce * direction , 0); }

        animator.SetFloat("speed", Mathf.Abs((input != null) ? input.Direction : 0f)); 
        trailObject.SetActive(true); //Efeito de dash, um trail configurado no editor
        buttonDash.gameObject.GetComponent<Image>().color = notCanDashColor; //Modifica a cor do botão de dash

        bool hasSpear = PlayerSkillsManager.Instance != null && PlayerSkillsManager.Instance.IsSkillUnlocked(SkillType.Spear);
        if (hasSpear && spearVisual != null)
        {
            spearVisual.SetActive(true);
        }

        float elapsed = 0f;
        bool hitEnemy = false;

        while (elapsed < timeDash)
        {
            if (hasSpear)
            {
                Vector2 boxCenter = (Vector2)transform.TransformPoint(spearOffset);
                Collider2D[] hits = Physics2D.OverlapBoxAll(boxCenter, spearArea, 0f, enemyLayer);
                
                foreach (Collider2D hit in hits)
                {
                    if (hit.CompareTag("Enemy"))
                    {
                        // 1. Dano ao inimigo
                        EnemyStats enemyStats = hit.GetComponent<EnemyStats>();
                        if (enemyStats != null)
                        {
                            enemyStats.TakeDamage(spearDamage);
                        }

                        // 2. Impulso ao inimigo (knockback)
                        Rigidbody2D enemyRb = hit.GetComponent<Rigidbody2D>();
                        if (enemyRb != null)
                        {
                            enemyRb.linearVelocity = Vector2.zero;
                            enemyRb.AddForce(new Vector2(direction * enemyKnockbackForce , enemyKnockbackForce/6f), ForceMode2D.Force);
                        }

                        //3. Caso seja um obstaculo da fase6
                        ObstaculoFase6 obstaculo = hit.GetComponent<ObstaculoFase6>();
                        if (obstaculo != null)
                        {
                            obstaculo.TakeHit();
                        }

                        rb.linearVelocity = Vector2.zero;
                        rb.AddForce(new Vector2(-direction * playerKnockbackForce * 2, playerKnockbackForce/4f ), ForceMode2D.Force);
                        isPlusSpeedBoost = true;
                        numberJump = 0; // Reset jump count after hitting an enemy
                        hitEnemy = true;
                   
                        break;
                    }
                }

                if (hitEnemy) break;
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (!hitEnemy)
        {
            isDash = false;
            if (spearVisual != null) spearVisual.SetActive(false);
            trailObject.SetActive(false);
            rb.gravityScale = gravityScale;
        }
        else
        {
            isDash = false;
            if (spearVisual != null) spearVisual.SetActive(false);
            trailObject.SetActive(false);
            rb.gravityScale = gravityScale;
        }

        yield return new WaitForSeconds(dashCooldown);

        canDash = true;
        animator.SetFloat("speed", Mathf.Abs((input != null) ? input.Direction : 0f));
        buttonDash.gameObject.GetComponent<Image>().color = canDashColor;
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        float originalTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = originalTimeScale;
    }

    void CheckGround()
    {
        isGrounded = Physics2D.OverlapBox(groundChecker.position, lengthGroundedCheck, 0, groundMask);
        isSwtichSpeed = Physics2D.OverlapBox(groundChecker.position, lengthGroundedCheck, 0, swtichSpeedMask); 
        
        isWall = Physics2D.OverlapBox(wallChecker.position, lengthWallCheck, 0, wallMask);
        isRope = Physics2D.OverlapBox(wallChecker.position, lengthWallCheck, 0, layerRope);
    }

    void WallFall()
    {
        float g = isGravityInverted ? -1f : 1f;
        if (isWall && rb.linearVelocity.y * g < wallFallForce)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallFallForce * g);
        }
    }

    void InRope()
    {
        float g = isGravityInverted ? -1f : 1f;
        if (isRope && rb.linearVelocity.y * g < ropeFall)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -ropeFall * g);
        }
    }

    void Flip()
    {
        direction *= -1;
        isFaceRight = !isFaceRight;
        transform.Rotate(0, 180f, 0);
    }

    IEnumerator StopMove()
    {
        canMove = false;
        yield return new WaitForSeconds(timeWallJump);
        canMove = true;
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireCube(groundChecker.position, lengthGroundedCheck);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(wallChecker.position, lengthWallCheck);

        // Draw Spear Area
        bool hasSpear = PlayerSkillsManager.Instance != null && PlayerSkillsManager.Instance.IsSkillUnlocked(SkillType.Spear);
        if (hasSpear)
        {
            Gizmos.color = Color.cyan;
            Vector2 boxCenter = (Vector2)transform.TransformPoint(spearOffset);
            Gizmos.DrawWireCube(boxCenter, spearArea);
        }
    }
}