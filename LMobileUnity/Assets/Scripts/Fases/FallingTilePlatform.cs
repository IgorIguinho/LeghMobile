using System;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;


[RequireComponent(typeof(Rigidbody2D))]
public class FallingTilePlatform : MonoBehaviour
{
    public enum FallMotion { ConstantSpeed, Gravity }
    public enum TriggerType { PassUnder, StepOn }   

    [Header("Movimento de Queda")]
    public FallMotion motion = FallMotion.ConstantSpeed;
    public float fallSpeed = 6f;        // velocidade constante OU terminal (clamp) no modo Gravity
    public float gravityScale = 3f;     // usado só no modo Gravity (aceleração)
   
   

    [Header("Gatilho: Passar por baixo (área de detecção)")]
    public bool triggerOnPassUnder = true;
    public float fallDelayUnder = 0.1f;    // atraso após o gatilho
    private WaitForSeconds delayUnderWTFS;
    public Vector2 underBoxOffset = new Vector2(-2f, -1f); // relativo ao centro da plataforma (default: ponta esquerda, abaixo)
    public Vector2 underBoxSize = new Vector2(2f, 1.5f);   // dimensões da caixa de detecção

    [Header("Gatilho: Pisar em cima (área de detecção)")]
    public bool triggerOnStep = true;
    public float fallDelayOnStep = 0.5f;    // atraso após o gatilho
    private WaitForSeconds delayOnStepWTFS;
    public Vector2 stepBoxOffset = new Vector2(0f, 0.75f); // acima do topo da plataforma
    public Vector2 stepBoxSize = new Vector2(3f, 0.5f);

    [Header("Detecção (sem GC)")]
    public LayerMask playerMask;        // layer Player (para OverlapBox dos gatilhos e do esmagamento)
    public LayerMask groundLayer;       // para detectar pouso (default: Ground)

    [Header("Dano / Esmagamento")]
    public int crushDamage = 100;
    public Vector2 crushBoxOffsetExtra = Vector2.zero; // ajuste fino da caixa de esmagamento no fundo

    [Header("Esmagamento sem destruição (isDestroy = false)")]
    public float pushForceX = 10f;                     // força de empurrão horizontal
    public float pushForceY = 5f;                      // força de empurrão vertical (para descolar o chão)
    public float disableMovementDuration = 0.5f;       // tempo de movimentação desabilitada para o player

    [Header("Pós-queda")]
    public bool isDestroy = true;                      // se verdadeiro, destrói a plataforma; se falso, desabilita o componente
    public float destroyDelayAfterLand = 0.1f;

    // Buffer non-alloc compartilhado (evita GC a cada sondagem).
    private readonly Collider2D[] _overlapResults = new Collider2D[4];

    // Estado interno.
    private bool _triggered;
    private bool _falling;
    private bool _hasCrushed;

    // Refs cacheadas.
    private Rigidbody2D _rb;
    private Collider2D _ownCollider;
    private Transform _transform;

    private void Awake()
    {
        _transform = transform;
        _rb = GetComponent<Rigidbody2D>();
        _ownCollider = GetComponent<Collider2D>();

        delayOnStepWTFS = new WaitForSeconds(fallDelayOnStep);
        delayUnderWTFS = new WaitForSeconds(fallDelayUnder);

        // Corpo só cai na vertical (mantém X e rotação travados).
        _rb.bodyType = RigidbodyType2D.Kinematic;
        _rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        // Fallback por nome caso as masks não estejam configuradas no Inspector.
        if (playerMask.value == 0)
        {
            int playerLayer = LayerMask.NameToLayer("Player");
            if (playerLayer >= 0) playerMask = 1 << playerLayer;
        }
        if (groundLayer.value == 0)
        {
            int gLayer = LayerMask.NameToLayer("Ground");
            if (gLayer >= 0) groundLayer = 1 << gLayer;
        }
    }

    private void FixedUpdate()
    {
        if (!_triggered)
        {
            CheckTriggers();
            return;
        }

        if (_falling)
        {
            DoFall();
            CheckCrush();
            CheckLanding();
        }

        if (transform.position.y < -35f)
        {
            Destroy(gameObject);
        }
    }

    // ---- Fase de gatilho ----------------------------------------------------

    private void CheckTriggers()
    {
        if (triggerOnPassUnder && OverlapBox(underBoxOffset, underBoxSize, playerMask) != null)
        {
            Trigger(TriggerType.PassUnder);
            return;
        }

        if (triggerOnStep && OverlapBox(stepBoxOffset, stepBoxSize, playerMask) != null)
        {
            Trigger(TriggerType.StepOn);
        }
    }

    private void Trigger(TriggerType trigerType)
    {
        if (_triggered) return;
        _triggered = true;
        StartCoroutine(FallRoutine(trigerType));
    }

    private IEnumerator FallRoutine(TriggerType trigerType)
    {   
        if (trigerType == TriggerType.PassUnder)
            yield return delayUnderWTFS;
        else if (trigerType == TriggerType.StepOn)
            yield return delayOnStepWTFS;
    
      

        _falling = true;

        if (motion == FallMotion.Gravity)
        {
            // Corpo dinâmico acelera por gravidade; X e rotação continuam travados.
            _rb.bodyType = RigidbodyType2D.Dynamic;
            _rb.gravityScale = gravityScale;
            _rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;
        }
    }

    // ---- Fase de queda ------------------------------------------------------

    private void DoFall()
    {
        if (motion == FallMotion.ConstantSpeed)
        {
            // Kinematic empurra o player dinâmico ao tocar.
            _rb.MovePosition(_rb.position + Vector2.down * fallSpeed * Time.fixedDeltaTime);
        }
        else // Gravity
        {
            // Clamp na velocidade terminal.
            Vector2 v = _rb.linearVelocity;
            if (v.y < -fallSpeed)
                _rb.linearVelocity = new Vector2(v.x, -fallSpeed);
        }
    }

    private void CheckCrush()
    {
        if (_hasCrushed) return;

        // Caixa fina no fundo da plataforma.
        Bounds b = _ownCollider != null ? _ownCollider.bounds : new Bounds(_transform.position, Vector3.one);
        Vector2 worldCenter = new Vector2(b.center.x, b.min.y) + crushBoxOffsetExtra;
        Vector2 localOffset = worldCenter - (Vector2)_transform.position;
        Vector2 size = new Vector2(b.size.x * 0.95f, 0.3f);

        Collider2D hit = OverlapBox(localOffset, size, playerMask);
        if (hit == null) return;

        EnemyStats enemyStats = hit.GetComponentInParent<EnemyStats>();
        if (enemyStats != null)
        {
            enemyStats.Death();
            return;
        }

        PlayerMovements playerMovScript = hit.GetComponentInParent<PlayerMovements>();
        if (playerMovScript == null || !playerMovScript.isGrounded) return; // player precisa estar prensado contra o Ground

        PlayerStats stats = hit.GetComponentInParent<PlayerStats>();
        if (stats == null) return;

       

        stats.TakeDmg(crushDamage);
        _hasCrushed = true;

        if (isDestroy)
        {
            Destroy(gameObject);
        }
  
    }

    private IEnumerator DisablePlayerMovementRoutine(PlayerMovements pm)
    {
        float elapsed = 0f;
        while (elapsed < disableMovementDuration)
        {
            if (pm != null)
            {
                pm.canMove = false;
                pm.canDash = false;
                pm.isGrounded = false;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        if (pm != null)
        {
            pm.canMove = true;
            pm.canDash = true;
        }
    }

    private void CheckLanding()
    {
        if (_ownCollider == null) return;

        Bounds b = _ownCollider.bounds;
        Vector2 origin = new Vector2(b.center.x, b.min.y);
        Vector2 castSize = new Vector2(b.size.x * 0.95f, 0.05f);
        const float castDistance = 0.01f;

        RaycastHit2D hit = Physics2D.BoxCast(origin, castSize, 0f, Vector2.down, castDistance, groundLayer);
        if (hit.collider != null && hit.collider != _ownCollider)
        {
            Debug.Log("Plataforma caiu e pousou no chão: " + hit.collider.name);
            Land();
        }
    }

    private void Land()
    {
        _falling = false;

        if (_rb.bodyType == RigidbodyType2D.Dynamic)
        {
            _rb.linearVelocity = Vector2.zero;
            _rb.bodyType = RigidbodyType2D.Kinematic;
        }

        if (isDestroy)
        {
            Destroy(gameObject, destroyDelayAfterLand);
        }
        else
        {
            enabled = false;
        }
    }

    // ---- Helpers ------------------------------------------------------------

    /// <summary>
    /// Sonda uma caixa em world-space e retorna o primeiro collider que casa com a mask. Sem GC.
    /// </summary>
    private Collider2D OverlapBox(Vector2 localOffset, Vector2 size, LayerMask mask)
    {
        Vector2 center = (Vector2)_transform.position + localOffset;
        int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, _overlapResults, mask);
        for (int i = 0; i < count; i++)
            if (_overlapResults[i] != null) return _overlapResults[i];
        return null;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 pos = transform.position;

        // Passar por baixo (vermelho).
        if (triggerOnPassUnder)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.6f);
            Gizmos.DrawWireCube(pos + underBoxOffset, underBoxSize);
        }

        // Pisar em cima (verde).
        if (triggerOnStep)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.6f);
            Gizmos.DrawWireCube(pos + stepBoxOffset, stepBoxSize);
        }

        // Esmagamento (amarelo) — baseado no collider quando disponível.
        Collider2D col = GetComponent<Collider2D>();
        if (col != null)
        {
            Bounds b = col.bounds;
            Vector2 crushCenter = new Vector2(b.center.x, b.min.y) + crushBoxOffsetExtra;
            Vector2 crushSize = new Vector2(b.size.x * 0.95f, 0.3f);
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(crushCenter, crushSize);
        }
    }
}
