using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Move o filho "Plataform" entre dois pontos (PosInicial &lt;-&gt; PosFinal) em loop,
/// com suavizacao nas pontas e delay em cada parada. Carrega o player que estiver em cima.
/// </summary>
public class MovePlataform : MonoBehaviour
{
    public enum StartMode { Automatic, OnPlayerStep }

    [Header("Referencias")]
    [Tooltip("Rigidbody2D do filho Plataform que sera movido.")]
    public Rigidbody2D plataform;
    public Transform posInicial;
    public Transform posFinal;

    [Header("Movimento")]
    public StartMode startMode = StartMode.Automatic;
    [Tooltip("Velocidade media em unidades/segundo.")]
    public float speed = 3f;
    [Tooltip("Tempo parado em cada ponta (segundos).")]
    public float waitDelay = 1f;

    [Header("Deteccao do Player (em cima)")]
    public LayerMask playerMask;                 // fallback "Player"
    public Vector2 topBoxOffset = new Vector2(0f, 0.6f); // relativo ao topo do collider
    public Vector2 topBoxSize   = new Vector2(2f, 0.4f);

    // Estado interno
    private Vector2 _a, _b;          // origem e destino do trecho atual
    private float _t;                // tempo decorrido no trecho
    private bool _moving;            // esta se movendo?
    private bool _activated;         // ja foi ativado (modo OnPlayerStep)?
    private bool _waiting;           // esta no delay de parada?
    private Vector2 _prevPos;        // posicao anterior (para delta)

    private Collider2D _plataformCollider;

    private void Awake()
    {
        if (playerMask.value == 0)
        {
            int pl = LayerMask.NameToLayer("Player");
            if (pl >= 0) playerMask = 1 << pl;
        }

        if (plataform != null)
        {
            plataform.bodyType = RigidbodyType2D.Kinematic;
            plataform.interpolation = RigidbodyInterpolation2D.Interpolate;
            _plataformCollider = plataform.GetComponent<Collider2D>();
        }
    }

    private void Start()
    {
        // Comeca na PosInicial
        if (plataform != null && posInicial != null)
            plataform.position = posInicial.position;

        _a = posInicial != null ? (Vector2)posInicial.position : (plataform != null ? plataform.position : (Vector2)transform.position);
        _b = posFinal   != null ? (Vector2)posFinal.position   : (plataform != null ? plataform.position : (Vector2)transform.position);
        _t = 0f;
        _prevPos = plataform != null ? plataform.position : (Vector2)transform.position;

        _moving = (startMode == StartMode.Automatic);
        _activated = _moving;
    }

    private void FixedUpdate()
    {
        if (plataform == null) return;

        // Modo "OnPlayerStep": ativa no primeiro contato e nunca mais para (loop eterno).
        if (!_activated && IsPlayerOnTop(out _))
        {
            _activated = true;
            _moving = true;
        }

        if (!_moving || _waiting) { _prevPos = plataform.position; return; }

        float dist = Vector2.Distance(_a, _b);
        float duration = (speed > 0.0001f && dist > 0.0001f) ? dist / speed : 0f;

        Vector2 newPos;
        if (duration <= 0f)
        {
            newPos = _b;
            _t = duration;
        }
        else
        {
            _t += Time.fixedDeltaTime;
            float p = Mathf.Clamp01(_t / duration);
            float eased = Mathf.SmoothStep(0f, 1f, p); // ease-in/ease-out
            newPos = Vector2.Lerp(_a, _b, eased);
        }

        // Move a plataforma
        plataform.MovePosition(newPos);

        // Carrega o player (somente delta horizontal; vertical resolve pela colisao)
        Vector2 delta = newPos - _prevPos;
       if (Mathf.Abs(delta.x) > 0f && IsPlayerOnTop(out Rigidbody2D playerRb) && playerRb != null)
           playerRb.position = new Vector2(playerRb.position.x + delta.x, playerRb.position.y);

        _prevPos = newPos;

        // Chegou no destino?
        if (_t >= duration)
            StartCoroutine(ArriveAndSwap());
    }

    private IEnumerator ArriveAndSwap()
    {
        _waiting = true;
        yield return new WaitForSeconds(waitDelay);
        // Inverte sentido (ping-pong)
        Vector2 tmp = _a; _a = _b; _b = tmp;
        _t = 0f;
        _waiting = false;
    }

    private bool IsPlayerOnTop(out Rigidbody2D playerRb)
    {
        playerRb = null;
        Vector2 center = GetTopBoxCenter();

        Collider2D hit = Physics2D.OverlapBox(center, topBoxSize, 0f, playerMask);
        if (hit == null) return false;

        playerRb = hit.GetComponentInParent<Rigidbody2D>();
        return true;
    }

    private Vector2 GetTopBoxCenter()
    {
        if (_plataformCollider != null)
        {
            Bounds bnd = _plataformCollider.bounds;
            return new Vector2(bnd.center.x, bnd.max.y) + topBoxOffset;
        }
        if (plataform != null)
            return (Vector2)plataform.position + topBoxOffset;
        return (Vector2)transform.position + topBoxOffset;
    }

    private void OnDrawGizmosSelected()
    {
        // Tamanho da plataforma (a partir do collider, com fallback)
        Vector3 size = Vector3.one;
        var col = plataform != null ? plataform.GetComponent<Tilemap>() : null;
        col.CompressBounds();
        if (col != null) size = col.cellBounds.size;

        // Caminho (linha) entre as pontas
        if (posInicial != null && posFinal != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(posInicial.position, posFinal.position);
        }

        // PosInicial (verde) com o tamanho da plataforma
        if (posInicial != null)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube((new Vector2 (posInicial.transform.position.x,posInicial.transform.position.y - 0.5f)), size);
        }

        // PosFinal (vermelho) com o tamanho da plataforma
        if (posFinal != null)
        {
            Gizmos.color = Color.black;
            Gizmos.DrawWireCube((new Vector2(posFinal.transform.position.x, posFinal.transform.position.y - 0.5f)), size);
        }

        // Caixa de deteccao do player (amarelo)
        if (col != null)
        {
            Bounds b = plataform.GetComponent<Collider2D>().bounds;
            Vector2 c = new Vector2(b.center.x, b.max.y) + topBoxOffset;
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(c, topBoxSize);
        }
    }
}
