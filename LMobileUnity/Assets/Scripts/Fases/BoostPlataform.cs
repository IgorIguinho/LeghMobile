using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Plataforma horizontal para mobile que se move com velocidade base contínua em X e,
/// ao receber aterrissagens do jogador, ganha um avanço rápido (boost) com desaceleração suave.
/// Respeita limites horizontais configuráveis e transporta o jogador em cima sem alocações de GC.
/// </summary>
public class BoostPlataform : MonoBehaviour
{
    [Header("Referências")]
    [Tooltip("Rigidbody2D do filho Plataform que será movido.")]
    public Rigidbody2D plataform;
    [Tooltip("Marcador do limite esquerdo (opcional se definir minLimitX manualmente).")]
    public Transform limitMin;
    [Tooltip("Marcador do limite direito (opcional se definir maxLimitX manualmente).")]
    public Transform limitMax;

    [Header("Movimento Base")]
    [Tooltip("Velocidade base contínua no eixo X (+ direita, - esquerda).")]
    public float baseSpeed = 2f;

    [Header("Avanço / Boost (ao aterrissar)")]
    [Tooltip("Velocidade inicial do avanço rápido no eixo X (+ direita, - esquerda).")]
    public float boostVelocity = 8f;
    [Tooltip("Taxa de desaceleração do avanço em unidades/segundo².")]
    public float boostDeceleration = 12f;
    [Tooltip("Cooldown de segurança entre impulsos (segundos).")]
    public float boostCooldown = 0.2f;

    [Header("Limites Horizontais (Eixo X)")]
    [Tooltip("Se marcado, usa a posição X de limitMin e limitMax. Se desmarcado, usa os campos float abaixo.")]
    public bool useTransformLimits = true;
    [Tooltip("Posição X mínima no mundo (usada quando useTransformLimits for falso ou sem Transform).")]
    public float minLimitX = -10f;
    [Tooltip("Posição X máxima no mundo (usada quando useTransformLimits for falso ou sem Transform).")]
    public float maxLimitX = 10f;

    [Header("Detecção do Player")]
    [Tooltip("LayerMask para identificar o jogador.")]
    public LayerMask playerMask;
    [Tooltip("Offset da caixa de detecção no topo da plataforma.")]
    public Vector2 topBoxOffset = new Vector2(0f, 0.35f);
    [Tooltip("Dimensões da caixa de detecção no topo.")]
    public Vector2 topBoxSize = new Vector2(2f, 0.5f);

    [Header("Gizmos")]
    public bool showGizmos = true;

    // Estado interno
    private float _currentBoostVelocity;
    private bool _isBoosting;
    private bool _playerWasOnTop;
    private float _lastBoostTime = -10f;
    private Vector2 _currentPosition;
    private Vector2 _prevPos;
    private Collider2D _plataformCollider;

    // Propriedades públicas para telemetria, feedback (VFX/SFX) e validação
    public bool IsBoosting => _isBoosting;
    public float CurrentBoostVelocity => _currentBoostVelocity;
    public Vector2 CurrentPosition => _currentPosition;
    public bool PlayerWasOnTop => _playerWasOnTop;

    /// <summary>
    /// Dispara manualmente o avanço rápido (útil para testes ou gatilhos externos).
    /// </summary>
    public void TriggerBoost()
    {
        TryTriggerBoost();
    }

    /// <summary>
    /// Executa um passo de física simulado (útil para testes determinísticos).
    /// </summary>
    public void StepPhysics(float deltaTime)
    {
        GetLimits(out float minX, out float maxX);
        bool playerIsOnTop = DetectPlayerOnTop(out Rigidbody2D playerRb);

        if (playerIsOnTop && !_playerWasOnTop)
        {
            TryTriggerBoost();
        }
        _playerWasOnTop = playerIsOnTop;

        float currentSpeedX = 0f;

        if (_isBoosting)
        {
            currentSpeedX = _currentBoostVelocity;
            float decelStep = boostDeceleration * deltaTime;
            _currentBoostVelocity = Mathf.MoveTowards(_currentBoostVelocity, 0f, decelStep);

            if (Mathf.Abs(_currentBoostVelocity) <= 0.001f)
            {
                _currentBoostVelocity = 0f;
                _isBoosting = false;
            }
        }
        else
        {
            currentSpeedX = baseSpeed;
        }

        float targetX = _currentPosition.x + (currentSpeedX * deltaTime);

        if (targetX >= maxX)
        {
            targetX = maxX;
            if (_isBoosting && _currentBoostVelocity > 0f)
            {
                _currentBoostVelocity = 0f;
                _isBoosting = false;
            }
        }
        else if (targetX <= minX)
        {
            targetX = minX;
            if (_isBoosting && _currentBoostVelocity < 0f)
            {
                _currentBoostVelocity = 0f;
                _isBoosting = false;
            }
        }

        _currentPosition = new Vector2(targetX, _currentPosition.y);

        if (plataform != null)
        {
            plataform.MovePosition(_currentPosition);
        }

        Vector2 delta = _currentPosition - _prevPos;
        if (Mathf.Abs(delta.x) > 0.00001f && playerIsOnTop && playerRb != null)
        {
            playerRb.position = new Vector2(playerRb.position.x + delta.x, playerRb.position.y);
        }

        _prevPos = _currentPosition;
    }

    public void SetCurrentPositionForTest(Vector2 pos)
    {
        _currentPosition = pos;
        _prevPos = pos;
        if (plataform != null) plataform.position = pos;
    }

    // Buffer estático reutilizável para OverlapBox sem GC
    private static readonly Collider2D[] _overlapBuffer = new Collider2D[4];
    private ContactFilter2D _contactFilter;

    private void Awake()
    {
        if (playerMask.value == 0)
        {
            int pl = LayerMask.NameToLayer("Player");
            if (pl >= 0) playerMask = 1 << pl;
        }

        _contactFilter = new ContactFilter2D();
        _contactFilter.SetLayerMask(playerMask);
        _contactFilter.useLayerMask = true;
        _contactFilter.useTriggers = true;

        if (plataform != null)
        {
            plataform.bodyType = RigidbodyType2D.Kinematic;
            plataform.interpolation = RigidbodyInterpolation2D.Interpolate;
            _plataformCollider = plataform.GetComponent<Collider2D>();
        }
    }

    private Collider2D GetPlataformCollider()
    {
        if (_plataformCollider == null && plataform != null)
        {
            _plataformCollider = plataform.GetComponent<Collider2D>();
        }
        return _plataformCollider;
    }

    private void Start()
    {
        if (plataform != null)
        {
            _currentPosition = plataform.position;
            _prevPos = _currentPosition;

            // Clampa a posição inicial dentro dos limites
            GetLimits(out float minX, out float maxX);
            float clampedX = Mathf.Clamp(_currentPosition.x, minX, maxX);
            if (!Mathf.Approximately(clampedX, _currentPosition.x))
            {
                _currentPosition.x = clampedX;
                plataform.position = _currentPosition;
                _prevPos = _currentPosition;
            }
        }
    }

    private void FixedUpdate()
    {
        if (plataform == null) return;
        StepPhysics(Time.fixedDeltaTime);
    }

    private void TryTriggerBoost()
    {
        if (Time.time < _lastBoostTime + boostCooldown) return;
        if (_isBoosting) return;

        _currentBoostVelocity = boostVelocity;
        _isBoosting = true;
        _lastBoostTime = Time.time;
    }

    private bool DetectPlayerOnTop(out Rigidbody2D playerRb)
    {
        playerRb = null;
        Vector2 center = GetTopBoxCenter();

        int count = Physics2D.OverlapBox(center, topBoxSize, 0f, _contactFilter, _overlapBuffer);
        for (int i = 0; i < count; i++)
        {
            Collider2D col = _overlapBuffer[i];
            if (col == null) continue;

            playerRb = col.GetComponentInParent<Rigidbody2D>();
            if (playerRb != null)
            {
                return true;
            }
        }
        return false;
    }

    private Vector2 GetTopBoxCenter()
    {
        var col = GetPlataformCollider();
        if (col != null)
        {
            Bounds bnd = col.bounds;
            return new Vector2(bnd.center.x, bnd.max.y) + topBoxOffset;
        }
        if (plataform != null)
        {
            return (Vector2)plataform.position + topBoxOffset;
        }
        return (Vector2)transform.position + topBoxOffset;
    }

    public void GetLimits(out float minX, out float maxX)
    {
        float min = minLimitX;
        float max = maxLimitX;

        if (useTransformLimits)
        {
            if (limitMin != null) min = limitMin.position.x;
            if (limitMax != null) max = limitMax.position.x;
        }

        if (min > max)
        {
            float tmp = min;
            min = max;
            max = tmp;
        }

        minX = min;
        maxX = max;
    }

    /// <summary>
    /// Calcula a distância teórica que o impulso percorrerá até parar: d = (v² / 2a) * sign(v)
    /// </summary>
    public float CalculateBoostDistance()
    {
        if (boostDeceleration <= 0.0001f) return 0f;
        float dist = (boostVelocity * boostVelocity) / (2f * boostDeceleration);
        return Mathf.Sign(boostVelocity) * dist;
    }

    private Vector3 GetPlataformDimensions()
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
            {
                return col.bounds.size;
            }
        }
        return Vector3.one;
    }

    private void OnDrawGizmos()
    {
        if (!showGizmos) return;

        GetLimits(out float minX, out float maxX);
        Vector3 size = GetPlataformDimensions();

        float yPos = plataform != null ? plataform.position.y : transform.position.y;
        Vector3 leftLimitPos = new Vector3(minX, yPos, 0f);
        Vector3 rightLimitPos = new Vector3(maxX, yPos, 0f);

        // 1. Linha do percurso entre os limites
        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(leftLimitPos, rightLimitPos);

        // 2. Caixas de limite nos extremos
        Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.6f); // Vermelho limite esquerdo
        Gizmos.DrawWireCube(leftLimitPos, size);

        Gizmos.color = new Color(0.2f, 1f, 0.2f, 0.6f); // Verde limite direito
        Gizmos.DrawWireCube(rightLimitPos, size);

        // 3. Projeção do avanço (Boost) a partir da posição atual da plataforma
        Vector3 currentPlatPos = plataform != null ? (Vector3)plataform.position : transform.position;
        float boostDist = CalculateBoostDistance();
        Vector3 boostTargetPos = currentPlatPos + new Vector3(boostDist, 0f, 0f);

        // Clampa a projeção no limite caso ultrapasse
        boostTargetPos.x = Mathf.Clamp(boostTargetPos.x, minX, maxX);

        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(currentPlatPos, boostTargetPos);
        Gizmos.DrawWireSphere(boostTargetPos, 0.25f);

        // Seta ou ponta no destino do boost
        Vector3 arrowHeadOffset = new Vector3(Mathf.Sign(boostDist) * -0.2f, 0.2f, 0f);
        Gizmos.DrawLine(boostTargetPos, boostTargetPos + arrowHeadOffset);
        Gizmos.DrawLine(boostTargetPos, boostTargetPos + new Vector3(arrowHeadOffset.x, -arrowHeadOffset.y, 0f));

        // 4. Caixa de detecção do Player no topo
        if (plataform != null)
        {
            Vector2 c = GetTopBoxCenter();
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.7f);
            Gizmos.DrawWireCube(c, topBoxSize);
        }
    }
}
