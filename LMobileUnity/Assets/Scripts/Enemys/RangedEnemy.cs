using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RangedEnemy : MonoBehaviour
{
    enum EnemyState { Idle, Charging, Shooting, Flipping }

    [Header("Estado (debug)")]
    [SerializeField] private EnemyState state = EnemyState.Idle;

    [Header("Ataque")]
    public GameObject projectileObj;
    [Tooltip("Origem do projétil. Pode reaproveitar o filho CheckTransform.")]
    public Transform firePoint;
    [Tooltip("Tempo carregando a munição antes de disparar.")]
    public float chargeTime = 0.6f;
    [Tooltip("Tempo de recuperação logo após o disparo.")]
    public float shootRecover = 0.3f;
    [Tooltip("Espera entre disparos antes de recarregar novamente.")]
    public float attackCooldown = 1f;
    [Tooltip("Duração da animação de virar.")]
    public float flipTime = 0.3f;
    private int direction = -1;

    [Header("Detecção (alcance único)")]
    public Vector2 detectRadius ;
    public LayerMask layerPlayer;
    private Transform player;

    // --- Detecção sem alocação (zero GC) ---
    private ContactFilter2D playerFilter;
    private readonly Collider2D[] detectResults = new Collider2D[1];

    private Animator animator;
    private bool busy; // ocupado em charge/shoot/flip

    // Cache de WaitForSeconds para não alocar a cada uso
    private WaitForSeconds waitCharge, waitRecover, waitCooldown, waitFlip;

    // Hash dos parâmetros do Animator
    private static readonly int HashPlayerDetected = Animator.StringToHash("PlayerDetected");
    private static readonly int HashCharge = Animator.StringToHash("Charge");
    private static readonly int HashShoot = Animator.StringToHash("Shoot");
    private static readonly int HashFlip = Animator.StringToHash("Flip");

    void Start()
    {
        animator = GetComponent<Animator>();

        // Configura o filtro uma única vez (reutilizado a cada frame)
        playerFilter = new ContactFilter2D();
        playerFilter.useLayerMask = true;
        playerFilter.SetLayerMask(layerPlayer);
        playerFilter.useTriggers = true;

        waitCharge = new WaitForSeconds(chargeTime);
        waitRecover = new WaitForSeconds(shootRecover);
        waitCooldown = new WaitForSeconds(attackCooldown);
        waitFlip = new WaitForSeconds(flipTime);
    }

    void Update()
    {
        DetectPlayer();
        if (busy) { return; }
        if (player == null) { state = EnemyState.Idle; return; }
        if (NeedFlip()) { StartCoroutine(FlipRoutine()); return; }
        StartCoroutine(AttackRoutine());
    }

    // Sem GC: OverlapCircle non-alloc reutilizando filtro e buffer pré-alocados
    void DetectPlayer()
    {
        int count = Physics2D.OverlapBox(transform.position, detectRadius,0f, playerFilter, detectResults);
        player = count > 0 ? detectResults[0].transform : null;
        animator.SetBool(HashPlayerDetected, player != null);
    }

    bool NeedFlip()
    {
        if (player == null) { return false; }
        float dx = player.position.x - transform.position.x;
        return (dx > 0f && direction < 0) || (dx < 0f && direction > 0);
    }

    // Loop de combate contínuo: enquanto o player for visto e estiver alinhado,
    // recarrega e atira de novo. Só volta para Idle quando o player sai do raio.
    IEnumerator AttackRoutine()
    {
        busy = true;
        while (player != null && !NeedFlip())
        {
            state = EnemyState.Charging;
            animator.SetTrigger(HashCharge);
            yield return waitCharge;

            // Reavalia após carregar; se o player saiu/cruzou, encerra o loop.
            if (player == null || NeedFlip()) { break; }

            state = EnemyState.Shooting;
            animator.SetTrigger(HashShoot);
            // O projétil é instanciado pelo Animation Event "ShootEvent" no frame de tiro do clip.
            yield return waitRecover;

            yield return waitCooldown;
        }
        state = EnemyState.Idle;
        busy = false;
    }

    void Shoot()
    {
        Vector2 spawn = firePoint != null ? firePoint.position : transform.position;
        GameObject projectile = Instantiate(projectileObj, spawn, Quaternion.identity);
        projectile.GetComponent<ProjectileEnemy>().direction = -direction;
        projectile.GetComponent<SpriteRenderer>().flipX = direction > 0; // Vira o sprite se estiver mirando para a direita 
    }

    // Pode ser chamado por um Animation Event no frame de tiro do clip Shoot.
    public void ShootEvent()
    {
        Shoot();
    }

    IEnumerator FlipRoutine()
    {
        busy = true;
        state = EnemyState.Flipping;
        animator.SetTrigger(HashFlip);
        yield return waitFlip;
        Flip();
        busy = false;
    }

    void Flip()
    {
        direction *= -1;
        transform.Rotate(0f, 180f, 0f);
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(transform.position, detectRadius);
    }
}
