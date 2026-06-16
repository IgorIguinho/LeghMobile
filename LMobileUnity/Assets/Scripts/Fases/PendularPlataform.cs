using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Move o filho "Plataform" ao longo de um arco circular (balanco/pendulo) entre
/// PosInicial &lt;-&gt; PosFinal, girando em torno de PosPivo. A plataforma permanece
/// sempre na horizontal. Faz loop ping-pong com delay em cada ponta e carrega o
/// player que estiver em cima (em X e Y, acompanhando a curva).
/// Espelhado em <see cref="MovePlataform"/>, mas com trajetoria curva em vez de reta.
/// </summary>
public class PendularPlataform : MonoBehaviour
{
    public enum StartMode { Automatic, OnPlayerStep }

    [Header("Referencias")]
    [Tooltip("Rigidbody2D do filho Plataform que sera movido.")]
    public Rigidbody2D plataform;
    [Tooltip("Centro do balanco (o 'prego' onde o pendulo fica preso).")]
    public Transform posPivo;
    public Transform posInicial;
    public Transform posFinal;

    [Header("Movimento")]
    public StartMode startMode = StartMode.Automatic;
    [Tooltip("Velocidade media ao longo do arco em unidades/segundo.")]
    public float speed = 3f;
    [Tooltip("Tempo parado em cada ponta (segundos).")]
    public float waitDelay = 1f;

    [Header("Deteccao do Player (em cima)")]
    public LayerMask playerMask;                 // fallback "Player"
    public Vector2 topBoxOffset = new Vector2(0f, 0.6f); // relativo ao topo do collider
    public Vector2 topBoxSize   = new Vector2(2f, 0.4f);

    [Header("Gizmos")]
    [Tooltip("Quantidade de segmentos usados para desenhar a linha curva do arco.")]
    public int arcGizmoSegments = 24;

    // Estado interno (graus para os angulos)
    private float _angInicial, _angFinal;        // angulos das pontas relativos ao pivo
    private float _radInicial, _radFinal;        // raios das pontas relativos ao pivo
    private float _angA, _angB;                  // angulo origem/destino do trecho atual
    private float _radA, _radB;                  // raio origem/destino do trecho atual
    private float _t;                            // tempo decorrido no trecho
    private bool _moving;                        // esta se movendo?
    private bool _activated;                     // ja foi ativado (modo OnPlayerStep)?
    private bool _waiting;                       // esta no delay de parada?
    private Vector2 _prevPos;                    // posicao anterior (para delta)

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

        ComputeArcEnds();

        // Trecho atual: PosInicial -> PosFinal
        _angA = _angInicial; _angB = _angFinal;
        _radA = _radInicial; _radB = _radFinal;
        _t = 0f;
        _prevPos = plataform != null ? plataform.position : (Vector2)transform.position;

        _moving = (startMode == StartMode.Automatic);
        _activated = _moving;
    }

    /// <summary>Calcula angulos (graus) e raios das pontas em relacao ao pivo.</summary>
    private void ComputeArcEnds()
    {
        Vector2 pivot = posPivo != null ? (Vector2)posPivo.position : (Vector2)transform.position;

        Vector2 s = posInicial != null ? (Vector2)posInicial.position
                  : (plataform != null ? plataform.position : (Vector2)transform.position);
        Vector2 e = posFinal != null ? (Vector2)posFinal.position
                  : (plataform != null ? plataform.position : (Vector2)transform.position);

        Vector2 vS = s - pivot;
        Vector2 vE = e - pivot;

        _angInicial = Mathf.Atan2(vS.y, vS.x) * Mathf.Rad2Deg;
        _angFinal   = Mathf.Atan2(vE.y, vE.x) * Mathf.Rad2Deg;
        _radInicial = vS.magnitude;
        _radFinal   = vE.magnitude;
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

        // Comprimento aproximado do arco do trecho atual.
        float deltaAngRad = Mathf.DeltaAngle(_angA, _angB) * Mathf.Deg2Rad;
        float arcLen = ((_radA + _radB) * 0.5f) * Mathf.Abs(deltaAngRad);
        float duration = (speed > 0.0001f && arcLen > 0.0001f) ? arcLen / speed : 0f;

        float eased;
        if (duration <= 0f)
        {
            eased = 1f;
            _t = 0f;
        }
        else
        {
            _t += Time.fixedDeltaTime;
            float p = Mathf.Clamp01(_t / duration);
            eased = Mathf.SmoothStep(0f, 1f, p); // ease-in/ease-out
        }

        float ang = Mathf.LerpAngle(_angA, _angB, eased) * Mathf.Deg2Rad;
        float rad = Mathf.Lerp(_radA, _radB, eased);
        Vector2 pivot = posPivo != null ? (Vector2)posPivo.position : (Vector2)transform.position;
        Vector2 newPos = pivot + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;

        // Move a plataforma (permanece horizontal: apenas posicao, sem rotacao)
        plataform.MovePosition(newPos);

        // Carrega o player acompanhando a curva (X e Y)
        Vector2 delta = newPos - _prevPos;
        if (delta.sqrMagnitude > 0f && IsPlayerOnTop(out Rigidbody2D playerRb) && playerRb != null)
            playerRb.position = new Vector2(playerRb.position.x + delta.x, playerRb.position.y);

        _prevPos = newPos;

        // Chegou no destino?
        if (duration <= 0f || _t >= duration)
            StartCoroutine(ArriveAndSwap());
    }

    private IEnumerator ArriveAndSwap()
    {
        _waiting = true;
        yield return new WaitForSeconds(waitDelay);
        // Inverte sentido (ping-pong): troca angulos e raios
        float tmpAng = _angA; _angA = _angB; _angB = tmpAng;
        float tmpRad = _radA; _radA = _radB; _radB = tmpRad;
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

    /// <summary>Tamanho da plataforma a partir do Tilemap (fallback: Collider2D, depois 1x1).</summary>
    private Vector3 GetPlataformSize()
    {
        if (plataform != null)
        {
            Tilemap tm = plataform.GetComponent<Tilemap>();
            if (tm != null)
            {
                tm.CompressBounds();
                return tm.cellBounds.size;
            }

            Collider2D col = plataform.GetComponent<Collider2D>();
            if (col != null)
                return col.bounds.size;
        }
        return Vector3.one;
    }

    /// <summary>Posicao no arco para um parametro normalizado p (0 = inicial, 1 = final).</summary>
    private Vector2 ArcPoint(Vector2 pivot, float p)
    {
        float ang = Mathf.LerpAngle(_angInicial, _angFinal, p) * Mathf.Deg2Rad;
        float rad = Mathf.Lerp(_radInicial, _radFinal, p);
        return pivot + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;
    }

    private void OnDrawGizmos()
    {
        // Recalcula angulos/raios no editor para o gizmo refletir as posicoes atuais.
        ComputeArcEnds();

        Vector3 size = GetPlataformSize();

        Vector2 pivot = posPivo != null ? (Vector2)posPivo.position
                      : (plataform != null ? plataform.position : (Vector2)transform.position);

        // Linha curva do arco (amostrando angulo + raio interpolados)
        if (posInicial != null && posFinal != null)
        {
            Gizmos.color = Color.cyan;
            if (posPivo != null)
            {
                int segs = Mathf.Max(2, arcGizmoSegments);
                Vector2 prev = ArcPoint(pivot, 0f);
                for (int i = 1; i <= segs; i++)
                {
                    Vector2 cur = ArcPoint(pivot, i / (float)segs);
                    Gizmos.DrawLine(prev, cur);
                    prev = cur;
                }
            }
            else
            {
                // Sem pivo: fallback para linha reta
                Gizmos.DrawLine(posInicial.position, posFinal.position);
            }
        }

        // Pivo (marcador) e raios ate as pontas
        if (posPivo != null)
        {
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(pivot, 0.12f);
            Gizmos.color = new Color(1f, 1f, 1f, 0.35f);
            if (posInicial != null) Gizmos.DrawLine(pivot, posInicial.position);
            if (posFinal != null)   Gizmos.DrawLine(pivot, posFinal.position);
        }

        // PosInicial com o tamanho da plataforma
        if (posInicial != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube((new Vector2(posInicial.transform.position.x, posInicial.transform.position.y - 0.5f)), size);
        }

        // PosFinal com o tamanho da plataforma
        if (posFinal != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube((new Vector2(posFinal.transform.position.x, posFinal.transform.position.y - 0.5f)) , size);
        }

        // Caixa de deteccao do player (amarelo)
        if (plataform != null)
        {
            Vector2 c = GetTopBoxCenter();
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(c, topBoxSize);
        }
    }
}
