using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using System.Data;

public class PlayerMovements : MonoBehaviour
{


    public InputReader input;

    Rigidbody2D rb;
    RewindObj rewindObj;
    PlayerAnimation playerAnimation;

    public event System.Action OnJump;

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

    [Header("Belt system")]
    public bool isBelt;
    public LayerMask beltMask;
    private Collider2D beltCollider;

    [Header("Caixa Push System")]
    public LayerMask caixaMask;
    public Transform bodyChecker;
    public Vector2 lengthBodyCheck;
    private Collider2D caixaCollider;

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
    public AnimationClip dashAnimation;
    public float dashForce;
    public float timeDash;
    public float dashCooldown;
    public GameObject trailObject;
    public bool canDash = true;
    public bool isDash { get; private set; }
    public GameObject buttonDash;
    public Color canDashColor;
    public Color notCanDashColor;
    float gravityScaleOriginal;

    [Header("Spear Dash Upgrade")]
    public AnimationClip spearDashAnimation;
    public Vector2 spearArea;
    public Vector2 spearOffset;
    public int spearDamage = 1;
    public float enemyKnockbackForce = 10f;
    public float playerKnockbackForce = 8f;
    public float hitStopDuration = 0.1f;
    public float shakeMagnitude = 0.15f;
    public float shakeDuration = 0.15f;
    public LayerMask enemyLayer;

    public enum RopeSide { Left, Right }

    [Header("Sistema de corda")]
    public bool isRope;
    public float ropeJumpForce = 12f;
    public float ropeHorizontalJumpForce = 20f;
    public float ropeSlideSpeed = 2.5f;
    public float ropeHorizontalOffset = 0.35f;
    public float ropeBottomOffsetY = 0.5f;
    public LayerMask layerRope;
    public RopeSide currentRopeSide = RopeSide.Right;
    [HideInInspector] public float ropeFall;
    private bool ropeRequiresInputNeutral;

    private float defaultGravityScale = 3f;
    private RopeTilemap.RopeColumnData currentRopeColumn;
    private float ropeJumpCooldownTimer = 0f;

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
        playerAnimation = GetComponent<PlayerAnimation>();
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
        rewindObj = gameObject.GetComponent<RewindObj>();
        gravityScaleOriginal = rb.gravityScale;
        if (rb != null)
        {
            defaultGravityScale = Mathf.Abs(rb.gravityScale);
            if (defaultGravityScale <= 0f) defaultGravityScale = 3f;
        }
        if (ropeSlideSpeed <= 0f) ropeSlideSpeed = 2.5f;
        if (ropeHorizontalOffset <= 0f) ropeHorizontalOffset = 0.35f;
        if (ropeBottomOffsetY <= 0f) ropeBottomOffsetY = 0.5f;
    }

    void FixedUpdate()
    {
        if (!rewindObj.isRewind)
        {
            if (ropeJumpCooldownTimer > 0f)
            {
                ropeJumpCooldownTimer -= Time.fixedDeltaTime;
            }

            CheckGround();

            if (isRope)
            {
                ProcessRopeMovement();
            }
            else
            {
                if (canMove) { Moviment(); }
                if (!isDash) { WallFall(); } // <- Não aplicar WallFall durante o Dash
            }
        }

    }

    public void UnlockDashButton()
    {
        if (PlayerSkillsManager.Instance != null && PlayerSkillsManager.Instance.IsSkillUnlocked(SkillType.Dash))
        {
            buttonDash.gameObject.SetActive(true);
        }
        else
        {
            buttonDash.gameObject.SetActive(false);
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
                return; // ignora a reescrita normal de X enquanto boostando
            }
        }
        // --- fim PlusSpeed ---

        // --- Belt Speed ---
        float beltSpeed = 0f;
        if (isBelt && beltCollider != null)
        {
            SurfaceEffector2D beltSpeedZone = beltCollider.GetComponent<SurfaceEffector2D>();
            if (beltSpeedZone != null)
            {
                beltSpeed = beltSpeedZone.speed;
            }
        }

        // --- Caixa Speed ---
        float caixaSpeed = 0f;
        if (caixaCollider != null)
        {
            Rigidbody2D caixaRb = caixaCollider.attachedRigidbody;
            if (caixaRb != null) caixaSpeed = caixaRb.linearVelocity.x;
        }

        float currentDirection = (input != null) ? input.Direction : 0f;
        float switchSpeed = switchSpeedSlow ? (speed / speedSwitch) : (speed * speedSwitch);

        if (isGrounded  && !isSwtichSpeed) // movimento normal no chão
        { rb.linearVelocity = new Vector2((speed * currentDirection) + beltSpeed + caixaSpeed, rb.linearVelocity.y); }

        else if (isSwtichSpeed) //movimento modificado pelo terreno de switch speed
        { rb.linearVelocity = new Vector2((switchSpeed * currentDirection) + beltSpeed + caixaSpeed, rb.linearVelocity.y);  }

        else if (isBelt)
            rb.linearVelocity = new Vector2(((speed/ 2) * currentDirection) + beltSpeed + caixaSpeed, rb.linearVelocity.y);
        
        else { rb.linearVelocity = new Vector2((speedOnAir * currentDirection) + caixaSpeed, rb.linearVelocity.y); } //Movimento norma no ar
        
        if (rb.linearVelocity.x * direction < 0f)
        {
            Flip();
        }
    }

    public void ToggleGravity()
    {
        isGravityInverted = !isGravityInverted;

        // Troca o SINAL da gravidade preservando a magnitude (3 -> -3 -> 3)
        if (!isRope)
        {
            rb.gravityScale = Mathf.Abs(rb.gravityScale) * (isGravityInverted ? -1f : 1f);
        }

        // Zera o Y para o flip ficar limpo
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

        // Rotaciona 180 no eixo X (de cabeca para baixo / volta ao normal)
        transform.Rotate(180f, 0f, 0f);
    }

    void Jump()
    {
        if (!canJump) return;   // DontJump: bloqueia o pulo
        if (isDash)  return; 
        OnJump?.Invoke();
        float g = isGravityInverted ? -1f : 1f;

        if (isRope)
        {
            numberJump = 0;
            // Salto Direcional: direção estritamente baseada no lado da corda em que o jogador está pendurado
            // Esquerda: vetor [-vx, +vy]
            // Direita: vetor [+vx, +vy]
            float vx = (currentRopeSide == RopeSide.Left) ? -ropeHorizontalJumpForce : ropeHorizontalJumpForce;
            float vy = ropeJumpForce * g;

            ExitRope();

            rb.linearVelocity = Vector2.zero;
            rb.AddForce(new Vector2(vx, vy), ForceMode2D.Impulse);
            numberJump++;
        }
        else if (isGrounded || isBelt) 
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
        if (isRope)
        {
            ExitRope();
        }

        rb.gravityScale = 0;
        rb.linearVelocity = Vector2.zero;

        //Dash, se estiver na parede ele vai para o outro lado
        if (isWall) 
        {
            rb.linearVelocity = new Vector2(dashForce * -direction , 0); 
            Flip(); 
        }
        else { rb.linearVelocity = new Vector2(dashForce * direction , 0); }

        trailObject.SetActive(true); //Efeito de dash, um trail configurado no editor
        buttonDash.gameObject.GetComponent<Image>().color = notCanDashColor; //Modifica a cor do botão de dash

        bool hasSpear = PlayerSkillsManager.Instance != null && PlayerSkillsManager.Instance.IsSkillUnlocked(SkillType.Spear);
        if (hasSpear && spearDashAnimation != null)
        {
          playerAnimation.TradeAnimation(dashAnimation, spearDashAnimation);
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
          
            trailObject.SetActive(false);
            rb.gravityScale = gravityScaleOriginal;
        }
        else
        {
            isDash = false;
           
            trailObject.SetActive(false);
            rb.gravityScale = gravityScaleOriginal;
        }

        yield return new WaitForSeconds(dashCooldown);

        canDash = true;
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
        beltCollider = Physics2D.OverlapBox(groundChecker.position, lengthGroundedCheck, 0, beltMask);
        isBelt = beltCollider != null;

        if (bodyChecker != null)
        {
            caixaCollider = Physics2D.OverlapBox(bodyChecker.position, lengthBodyCheck, 0, caixaMask);
        }
        else if (groundChecker != null)
        {
            caixaCollider = Physics2D.OverlapBox(groundChecker.position, lengthGroundedCheck, 0, caixaMask);
        }

        isWall = Physics2D.OverlapBox(wallChecker.position, lengthWallCheck, 0, wallMask);

        // Detecção da corda
        if (!isRope && ropeJumpCooldownTimer <= 0f)
        {
            if (!isDash)
            {
                //Stop dash
                isDash = false;
                trailObject.SetActive(false);
                rb.gravityScale = gravityScaleOriginal;
            }

            Collider2D ropeHit = Physics2D.OverlapBox(wallChecker != null ? wallChecker.position : transform.position, lengthWallCheck, 0, layerRope);
            if (ropeHit != null)
            {
                EnterRope(ropeHit);
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (isDash || ropeJumpCooldownTimer > 0f || isRope) return;
        if (((1 << collision.gameObject.layer) & layerRope) != 0)
        {
            EnterRope(collision);
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (isDash || ropeJumpCooldownTimer > 0f || isRope) return;
        if (((1 << collision.gameObject.layer) & layerRope) != 0)
        {
            EnterRope(collision);
        }
    }

    void WallFall()
    {
        float g = isGravityInverted ? -1f : 1f;
        if (isWall && rb.linearVelocity.y * g < wallFallForce)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallFallForce * g);
        }
    }

    public void SetFaceDirection(int targetDir)
    {
        if (targetDir == 0) return;
        if (direction != targetDir)
        {
            Flip();
        }
    }

    public void EnterRope(Collider2D ropeHit)
    {
        if (isRope) return;

        RopeTilemap ropeTm = RopeTilemap.Instance;
        if (ropeTm == null && ropeHit != null)
        {
            ropeTm = ropeHit.GetComponent<RopeTilemap>() ?? ropeHit.GetComponentInParent<RopeTilemap>();
        }

        Vector2 playerPos = transform.position;
        Vector2 searchPoint = (ropeHit != null) ? ropeHit.ClosestPoint(playerPos) : playerPos;

        if (ropeTm != null && (ropeTm.TryGetRopeAt(searchPoint, out currentRopeColumn) || ropeTm.TryGetRopeAt(playerPos, out currentRopeColumn)))
        {
            // Coluna encontrada com precisão
        }
        else
        {
            float localCenterX = Mathf.Floor(searchPoint.x) + 0.5f;
            currentRopeColumn = new RopeTilemap.RopeColumnData
            {
                cellX = Mathf.FloorToInt(searchPoint.x),
                centerX = localCenterX,
                minY = playerPos.y - 2f,
                maxY = playerPos.y + 2f
            };
        }
        // ... restante do método ...

        isRope = true;
        numberJump = 0;

        if (rb.gravityScale != 0f)
        {
            defaultGravityScale = Mathf.Abs(rb.gravityScale);
        }
        rb.gravityScale = 0f;

        // Determina lado inicial pela posição de entrada:
        // Se entrou pela esquerda do centro -> Esquerda; se pela direita -> Direita
        if (playerPos.x < currentRopeColumn.centerX)
        {
            currentRopeSide = RopeSide.Left;
        }
        else
        {
            currentRopeSide = RopeSide.Right;
        }
        ropeRequiresInputNeutral = (input != null && Mathf.Abs(input.Direction) > 0.1f);

        ApplyRopeSide(currentRopeSide);
    }

    public void ApplyRopeSide(RopeSide side)
    {
        currentRopeSide = side;

        float targetX = (side == RopeSide.Left)
            ? (currentRopeColumn.centerX - ropeHorizontalOffset)
            : (currentRopeColumn.centerX + ropeHorizontalOffset);

        transform.position = new Vector3(targetX, transform.position.y, transform.position.z);

        // Orientação: Olhando para fora (Salto)
        // Lado Esquerdo -> olha para a esquerda (-1)
        // Lado Direito -> olha para a direita (1)
        int targetDir = (side == RopeSide.Left) ? -1 : 1;
        SetFaceDirection(targetDir);
    }

    public void ExitRope()
    {
        if (!isRope) return;
        isRope = false;

        float gSign = isGravityInverted ? -1f : 1f;
        float restoredGravity = (defaultGravityScale > 0f) ? defaultGravityScale : 3f;
        rb.gravityScale = restoredGravity * gSign;

        ropeJumpCooldownTimer = 0.15f;
        ropeRequiresInputNeutral = false;
    }

    void ProcessRopeMovement()
    {
        float currentDirection = (input != null) ? input.Direction : 0f;

        // Verifica se o jogador soltou o direcional para liberar a troca
        if (ropeRequiresInputNeutral)
        {
            if (Mathf.Abs(currentDirection) < 0.1f)
            {
                ropeRequiresInputNeutral = false;
            }
        }
        else
        {
            // Alternância de Lado permitida apenas após soltar e pressionar novamente
            if (currentDirection < -0.1f && currentRopeSide != RopeSide.Left)
            {
                ApplyRopeSide(RopeSide.Left);
            }
            else if (currentDirection > 0.1f && currentRopeSide != RopeSide.Right)
            {
                ApplyRopeSide(RopeSide.Right);
            }
        }

        // Posição no eixo X travada no offset do lado atual
        float targetX = (currentRopeSide == RopeSide.Left)
            ? (currentRopeColumn.centerX - ropeHorizontalOffset)
            : (currentRopeColumn.centerX + ropeHorizontalOffset);

        // Movimentação vertical (Eixo Y)
        float currentY = rb.position.y;
        float newY;
        float newVy;

        if (!isGravityInverted)
        {
            float minYLimit = currentRopeColumn.minY + ropeBottomOffsetY;
            if (currentY <= minYLimit)
            {
                // Trava na extremidade inferior
                newY = minYLimit;
                newVy = 0f;
            }
            else
            {
                // Descida passiva contínua
                newVy = -ropeSlideSpeed;
                newY = currentY + (newVy * Time.fixedDeltaTime);
                if (newY < minYLimit)
                {
                    newY = minYLimit;
                    newVy = 0f;
                }
            }
        }
        else
        {
            float maxYLimit = currentRopeColumn.maxY - ropeBottomOffsetY;
            if (currentY >= maxYLimit)
            {
                newY = maxYLimit;
                newVy = 0f;
            }
            else
            {
                newVy = ropeSlideSpeed;
                newY = currentY + (newVy * Time.fixedDeltaTime);
                if (newY > maxYLimit)
                {
                    newY = maxYLimit;
                    newVy = 0f;
                }
            }
        }

        rb.MovePosition(new Vector2(targetX, newY));
        rb.linearVelocity = new Vector2(0f, newVy);
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

        if (bodyChecker != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireCube(bodyChecker.position, lengthBodyCheck);
        }

        // Draw Spear Area
        bool hasSpear = PlayerSkillsManager.Instance != null && PlayerSkillsManager.Instance.IsSkillUnlocked(SkillType.Spear);
        if (hasSpear)
        {
            Gizmos.color = Color.cyan;
            Vector2 boxCenter = (Vector2)transform.TransformPoint(spearOffset);
            Gizmos.DrawWireCube(boxCenter, spearArea);
        }

        if (isRope)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(new Vector3(currentRopeColumn.centerX, rb != null ? rb.position.y : transform.position.y, 0f), 0.25f);
        }
    }
}