using UnityEngine;

/// <summary>
/// Controla o auto-scrolling horizontal da câmera (eixo X).
/// Mantém uma barreira cinemática na borda traseira que empurra o jogador suavemente caso fique para trás.
/// Caso o jogador seja prensado entre a barreira da câmera e uma parede/obstáculo, aplica morte instantânea por esmagamento (Crush).
/// Totalmente otimizado para mobile (zero GC alloc em runtime, buffers estáticos e FixedUpdate).
/// </summary>
[RequireComponent(typeof(Camera))]
public class AutoScrolling : MonoBehaviour
{
    public enum ScrollDirection
    {
        LeftToRight = 1, // Avança para a direita (+X), barreira fica na borda esquerda
        RightToLeft = -1 // Avança para a esquerda (-X), barreira fica na borda direita
    }

    [Header("Configuracao de Movimento")]
    [Tooltip("Direcao do deslocamento horizontal.")]
    public ScrollDirection direction = ScrollDirection.LeftToRight;

    [Tooltip("Velocidade do deslocamento em unidades por segundo.")]
    public float scrollSpeed = 2.5f;

    [Tooltip("Se falso, o auto-scroller permanece pausado.")]
    public bool isScrolling = true;

    [Header("Inicio por Dialogo")]
    [Tooltip("Se verdadeiro, o movimento da camera aguarda o dialogo da fase terminar.")]
    public bool startAfterDialogueFinish = true;

    [Tooltip("Tempo em segundos de atraso apos o dialogo terminar para a camera iniciar o movimento.")]
    public float delayAfterDialogueFinish = 2f;

    [Header("Limites em X")]
    [Tooltip("Se verdadeiro, limita o deslocamento da camera entre MinX e MaxX.")]
    public bool useLimitX = false;
    public float minX;
    public float maxX;

    [Header("Comportamento no Eixo Y (Hibrido)")]
    [Tooltip("Se verdadeiro, a camera acompanha suavemente a posicao Y do jogador.")]
    public bool followPlayerY = false;

    [Tooltip("Suavidade (lerp) do acompanhamento vertical.")]
    public float lerpSpeedY = 5f;

    [Tooltip("Limites verticais da camera quando followPlayerY estiver ativado.")]
    public float minY = -5f;
    public float maxY = 5f;

    [Header("Barreira de Empurrao (Push Barrier)")]
    [Tooltip("Espessura da barreira cinemática da borda em unidades.")]
    public float barrierThickness = 1.5f;

    [Tooltip("Altura extra adicionada acima e abaixo da tela para impedir que o player pule por cima da barreira.")]
    public float extraHeight = 6f;

    [Tooltip("Offset horizontal adicional na borda da tela.")]
    public float edgeOffset = 0f;

    [Tooltip("LayerMask do Player.")]
    public LayerMask playerMask;

    [Header("Deteccao de Esmagamento (Crush)")]
    [Tooltip("LayerMask dos obstaculos que podem esmagar o player (Ground, Wall, caixas).")]
    public LayerMask obstacleMask;

    [Tooltip("Distancia minima para a parede a frente do player para disparar esmagamento.")]
    public float crushCheckDistance = 0.1f;

    [Tooltip("Penetracao maxima permitida da barreira sobre o jogador antes de disparar esmagamento forcado.")]
    public float penetrationThreshold = 0.15f;

    [Header("Debug")]
    public bool showGizmos = true;

    // Referencias em cache
    private Camera _cam;
    private Transform _playerTransform;
    private Rigidbody2D _playerRb;
    private Collider2D _playerCollider;
    private PlayerStats _playerStats;

    // Barreira cinematica
    private GameObject _barrierObj;
    private Rigidbody2D _barrierRb;
    private BoxCollider2D _barrierCollider;

    // Buffers estáticos para Zero GC Alloc em dispositivos mobile
    private static readonly RaycastHit2D[] s_HitBuffer = new RaycastHit2D[4];
    private static readonly Collider2D[] s_OverlapBuffer = new Collider2D[4];

    private ContactFilter2D _obstacleFilter;
    private float _fixedY;
    private bool _isPlayerDead;
    private bool _hasDialogueTriggered;
    private Coroutine _startDelayCoroutine;

    private void Awake()
    {
        Initialize();

        // Desativa FollowCam se estiver presente no mesmo GameObject para evitar conflito de movimento
        FollowCam followCam = GetComponent<FollowCam>();
        if (followCam != null && followCam.enabled)
        {
            followCam.enabled = false;
            Debug.Log("[AutoScrolling] FollowCam detectado na camera e desativado para evitar conflito de movimentacao.");
        }
    }

    private void OnEnable()
    {
        DialogueSystem.OnDialogueFinished += HandleDialogueFinished;
    }

    private void OnDisable()
    {
        DialogueSystem.OnDialogueFinished -= HandleDialogueFinished;
        if (_startDelayCoroutine != null)
        {
            StopCoroutine(_startDelayCoroutine);
            _startDelayCoroutine = null;
        }
    }

    /// <summary>
    /// Callback disparado quando qualquer dialogo termina (naturalmente ou pulado).
    /// Caso a opcao esteja ativa, inicia contagem do delay para comecar o auto-scrolling.
    /// </summary>
    private void HandleDialogueFinished(DialogueData data)
    {
        if (!startAfterDialogueFinish || _hasDialogueTriggered) return;
        _hasDialogueTriggered = true;

        if (delayAfterDialogueFinish > 0f)
        {
            if (_startDelayCoroutine != null)
            {
                StopCoroutine(_startDelayCoroutine);
            }
            _startDelayCoroutine = StartCoroutine(StartScrollingAfterDelayRoutine(delayAfterDialogueFinish));
        }
        else
        {
            isScrolling = true;
        }
    }

    private System.Collections.IEnumerator StartScrollingAfterDelayRoutine(float delay)
    {
        yield return new WaitForSeconds(delay);
        isScrolling = true;
        _startDelayCoroutine = null;
    }

    /// <summary>
    /// Inicializa referencias da camera e cria a barreira se ainda nao existir.
    /// </summary>
    public void Initialize()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        _fixedY = transform.position.y;

        if (Application.isPlaying && startAfterDialogueFinish)
        {
            isScrolling = false;
        }

        SetupLayerMaskFallbacks();
        SetupPushBarrier();
    }

    private void Reset()
    {
        Initialize();
    }

    private void OnValidate()
    {
        if (_cam == null) _cam = GetComponent<Camera>();
        SetupLayerMaskFallbacks();
        if (_barrierObj != null && _barrierCollider != null)
        {
            UpdateBarrierPositionAndSize();
        }
    }

    private void Start()
    {
        LocateAndCachePlayer();
    }

    private void FixedUpdate()
    {
        if (!isScrolling || _isPlayerDead) return;

        if (_playerTransform == null || _playerRb == null)
        {
            LocateAndCachePlayer();
            if (_playerTransform == null) return;
        }

        UpdateCameraPosition();
        UpdateBarrierPositionAndSize();
        CheckPlayerPushAndCrush();
    }

    /// <summary>
    /// Configura fallbacks automaticos para LayerMasks caso nao preenchidos no Inspector.
    /// </summary>
    private void SetupLayerMaskFallbacks()
    {
        if (playerMask.value == 0)
        {
            int pl = LayerMask.NameToLayer("Player");
            if (pl >= 0) playerMask = 1 << pl;
        }

        if (obstacleMask.value == 0)
        {
            string[] defaultObstacleLayers = { "Ground", "Wall", "Box", "SingleBox", "Caixa" };
            int mask = 0;
            foreach (var lName in defaultObstacleLayers)
            {
                int l = LayerMask.NameToLayer(lName);
                if (l >= 0) mask |= (1 << l);
            }
            obstacleMask = mask != 0 ? mask : (LayerMask)(1 << 3); // Fallback layer 3 (Ground)
        }

        _obstacleFilter = new ContactFilter2D();
        _obstacleFilter.SetLayerMask(obstacleMask);
        _obstacleFilter.useLayerMask = true;
        _obstacleFilter.useTriggers = false;
    }

    /// <summary>
    /// Cria e configura o GameObject da barreira cinematica acoplada a camera.
    /// </summary>
    private void SetupPushBarrier()
    {
        Transform existing = transform.Find("AutoScrollPushBarrier");
        if (existing != null)
        {
            _barrierObj = existing.gameObject;
        }
        else
        {
            _barrierObj = new GameObject("AutoScrollPushBarrier");
            _barrierObj.transform.SetParent(transform, false);
        }

        // Camada da barreira: usa Wall se existir, senao Default
        int wallLayer = LayerMask.NameToLayer("Wall");
        _barrierObj.layer = wallLayer >= 0 ? wallLayer : 0;

        _barrierRb = _barrierObj.GetComponent<Rigidbody2D>();
        if (_barrierRb == null)
        {
            _barrierRb = _barrierObj.AddComponent<Rigidbody2D>();
        }
        _barrierRb.bodyType = RigidbodyType2D.Kinematic;
        _barrierRb.interpolation = RigidbodyInterpolation2D.Interpolate;
        _barrierRb.simulated = true;

        _barrierCollider = _barrierObj.GetComponent<BoxCollider2D>();
        if (_barrierCollider == null)
        {
            _barrierCollider = _barrierObj.AddComponent<BoxCollider2D>();
        }
        _barrierCollider.isTrigger = false;

        UpdateBarrierPositionAndSize();
    }

    /// <summary>
    /// Encontra e armazena em cache todos os componentes necessarios do Player.
    /// </summary>
    private void LocateAndCachePlayer()
    {
        GameObject playerGo = GameObject.FindGameObjectWithTag("Player");
        if (playerGo != null)
        {
            _playerTransform = playerGo.transform;
            _playerRb = playerGo.GetComponent<Rigidbody2D>();
            _playerCollider = playerGo.GetComponent<Collider2D>();
            _playerStats = playerGo.GetComponent<PlayerStats>();
        }
    }

    /// <summary>
    /// Atualiza a posicao da camera respeitando a velocidade, direcao e limites.
    /// </summary>
    private void UpdateCameraPosition()
    {
        Vector3 pos = transform.position;
        float dirSign = (direction == ScrollDirection.LeftToRight) ? 1f : -1f;

        float newX = pos.x + (dirSign * scrollSpeed * Time.fixedDeltaTime);
        if (useLimitX)
        {
            if (dirSign > 0) newX = Mathf.Min(newX, maxX);
            else newX = Mathf.Max(newX, minX);
        }

        float newY = _fixedY;
        if (followPlayerY && _playerTransform != null)
        {
            float targetY = Mathf.Clamp(_playerTransform.position.y, minY, maxY);
            newY = Mathf.Lerp(pos.y, targetY, lerpSpeedY * Time.fixedDeltaTime);
        }

        transform.position = new Vector3(newX, newY, pos.z);
    }

    /// <summary>
    /// Mantem o tamanho e a posicao da barreira exatamente alinhados com a borda da camera.
    /// </summary>
    private void UpdateBarrierPositionAndSize()
    {
        if (_cam == null || _barrierObj == null) return;

        float camHeight = _cam.orthographicSize * 2f;
        float camWidth = camHeight * _cam.aspect;
        float totalHeight = camHeight + extraHeight;

        _barrierCollider.size = new Vector2(barrierThickness, totalHeight);

        // Posiciona a face interna da barreira na borda traseira da tela
        float localX;
        if (direction == ScrollDirection.LeftToRight)
        {
            // Borda esquerda
            localX = -(camWidth * 0.5f) - (barrierThickness * 0.5f) + edgeOffset;
        }
        else
        {
            // Borda direita
            localX = (camWidth * 0.5f) + (barrierThickness * 0.5f) - edgeOffset;
        }

        Vector3 targetWorldPos = transform.position + new Vector3(localX, 0f, 0f);
        targetWorldPos.z = 0f;

        if (_barrierRb != null)
        {
            _barrierRb.MovePosition(targetWorldPos);
        }
        else
        {
            _barrierObj.transform.position = targetWorldPos;
        }
    }

    /// <summary>
    /// Verifica se a borda alcancou o jogador para empurra-lo e se ha obstaculo que cause esmagamento.
    /// </summary>
    private void CheckPlayerPushAndCrush()
    {
        if (_playerCollider == null || _playerRb == null || _isPlayerDead) return;

        Bounds playerBounds = _playerCollider.bounds;
        float dirSign = (direction == ScrollDirection.LeftToRight) ? 1f : -1f;

        // Calcula a coordenada X da face interna da barreira no espaco de mundo
        float barrierFaceX;
        if (direction == ScrollDirection.LeftToRight)
        {
            barrierFaceX = _barrierRb.position.x + (barrierThickness * 0.5f);
            // Jogador esta sendo empurrado se sua extremidade esquerda estiver atras ou na face da barreira
            bool isPushing = playerBounds.min.x <= barrierFaceX + 0.05f;

            if (isPushing)
            {
                // Empurra a posicao do player para a face da barreira
                float overlap = barrierFaceX - playerBounds.min.x;
                if (overlap > 0f)
                {
                    _playerRb.position = new Vector2(_playerRb.position.x + overlap, _playerRb.position.y);
                }

                // Checagem de esmagamento a frente (+X)
                if (IsTrappedAgainstObstacle(Vector2.right, playerBounds, overlap))
                {
                    TriggerCrushDeath();
                }
            }
        }
        else
        {
            barrierFaceX = _barrierRb.position.x - (barrierThickness * 0.5f);
            // Jogador esta sendo empurrado se sua extremidade direita estiver a frente ou na face da barreira
            bool isPushing = playerBounds.max.x >= barrierFaceX - 0.05f;

            if (isPushing)
            {
                // Empurra a posicao do player para a face da barreira
                float overlap = playerBounds.max.x - barrierFaceX;
                if (overlap > 0f)
                {
                    _playerRb.position = new Vector2(_playerRb.position.x - overlap, _playerRb.position.y);
                }

                // Checagem de esmagamento a frente (-X)
                if (IsTrappedAgainstObstacle(Vector2.left, playerBounds, overlap))
                {
                    TriggerCrushDeath();
                }
            }
        }
    }

    /// <summary>
    /// Verifica se ha uma parede ou obstaculo intransponivel imediatamente a frente do jogador.
    /// Utiliza Physics2D.BoxCastNonAlloc sem alocacao de heap (Zero GC).
    /// </summary>
    private bool IsTrappedAgainstObstacle(Vector2 pushDir, Bounds playerBounds, float barrierOverlap)
    {
        // Se a barreira penetrou alem do limite aceitavel, o jogador ja esta fisicamente impedido de avancar
        if (barrierOverlap > penetrationThreshold)
        {
            return true;
        }

        // Lança um BoxCast do tamanho do colisor do player ligeiramente a frente na direcao de empurrao
        Vector2 origin = playerBounds.center;
        Vector2 size = new Vector2(playerBounds.size.x * 0.85f, playerBounds.size.y * 0.85f);

        int hitCount = Physics2D.BoxCast(
            origin,
            size,
            0f,
            pushDir,
            _obstacleFilter,
            s_HitBuffer,
            crushCheckDistance
        );

        for (int i = 0; i < hitCount; i++)
        {
            Collider2D col = s_HitBuffer[i].collider;
            if (col != null && col != _barrierCollider && col != _playerCollider && !col.isTrigger)
            {
                return true;
            }
        }

        // Checagem suplementar: o player esta sobreposto com obstaculo?
        int overlapCount = Physics2D.OverlapBox(origin, size, 0f, _obstacleFilter, s_OverlapBuffer);
        for (int i = 0; i < overlapCount; i++)
        {
            Collider2D col = s_OverlapBuffer[i];
            if (col != null && col != _barrierCollider && col != _playerCollider && !col.isTrigger)
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Executa a morte por esmagamento (Crush Death), ignorando invulnerabilidade/i-frames.
    /// </summary>
    private void TriggerCrushDeath()
    {
        if (_isPlayerDead) return;
        _isPlayerDead = true;

        if (_playerStats != null)
        {
            _playerStats.Kill();
        }
        else
        {
            // Fallback caso PlayerStats nao seja encontrado
            UnityEngine.SceneManagement.SceneManager.LoadScene(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene().name
            );
        }
    }

    #region Metodos Publicos de Controle

    public void SetSpeed(float newSpeed)
    {
        scrollSpeed = Mathf.Max(0f, newSpeed);
    }

    public void SetDirection(ScrollDirection newDirection)
    {
        direction = newDirection;
        if (_barrierObj != null && _barrierCollider != null)
        {
            UpdateBarrierPositionAndSize();
        }
    }

    public void PauseScrolling()
    {
        isScrolling = false;
    }

    public void ResumeScrolling()
    {
        isScrolling = true;
    }

    /// <summary>
    /// Inicia o auto-scrolling apos uma quantidade de segundos especificada.
    /// </summary>
    public void StartScrollingAfterDelay(float delay)
    {
        if (_startDelayCoroutine != null)
        {
            StopCoroutine(_startDelayCoroutine);
        }
        _startDelayCoroutine = StartCoroutine(StartScrollingAfterDelayRoutine(delay));
    }

    #endregion

    private void OnDrawGizmosSelected()
    {
        if (!showGizmos) return;

        Camera c = GetComponent<Camera>();
        if (c == null) c = Camera.main;
        if (c == null) return;

        float h = c.orthographicSize * 2f;
        float w = h * c.aspect;
        Vector3 camPos = transform.position;

        // Moldura da tela (Ciano)
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(camPos, new Vector3(w, h, 0f));

        // Barreira cinemática (Vermelho)
        float localX = (direction == ScrollDirection.LeftToRight)
            ? -(w * 0.5f) - (barrierThickness * 0.5f) + edgeOffset
            : (w * 0.5f) + (barrierThickness * 0.5f) - edgeOffset;

        Vector3 barrierPos = camPos + new Vector3(localX, 0f, 0f);
        Gizmos.color = new Color(1f, 0f, 0f, 0.4f);
        Gizmos.DrawCube(barrierPos, new Vector3(barrierThickness, h + extraHeight, 0f));
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(barrierPos, new Vector3(barrierThickness, h + extraHeight, 0f));

        // Seta de direcao (Amarelo)
        Gizmos.color = Color.yellow;
        Vector3 arrowStart = camPos;
        Vector3 arrowDir = (direction == ScrollDirection.LeftToRight) ? Vector3.right : Vector3.left;
        Gizmos.DrawRay(arrowStart, arrowDir * 2f);
    }
}
