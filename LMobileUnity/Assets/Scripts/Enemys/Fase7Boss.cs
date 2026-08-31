
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(SpriteRenderer))]
public class Fase7Boss : MonoBehaviour , IDamageable
{
    public enum BossState { Spawning, Idle, MovingMelee, AttackingMelee, AttackingProjectile, AttackingCharge, AttackingSequence, WalkingToShoot, JumpingToPlatform, Dead }

    [Header("Configuration")]
    public Fase7BossConfig config;
    public GameObject projectileObj;
    public Transform firePoint;
    public Slider bossHealthSlider;
    public GameObject meleeVisualIndicator;

    [Header("Platforms")]
    public Transform[] platformReferences = new Transform[4];

    [Header("Current State")]
    [SerializeField] private BossState state = BossState.Spawning;
    [SerializeField] private int actualHealth;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private Transform playerTransform;
    private Collider2D collider;
    private int facingDirection = -1;
    private float cooldownTimer = 0f;
    private float chaseJumpTimer = 0f;
    private bool hasTriggeredFury = false;

    public float limitArenaX;
    public float limitArenaY;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        collider = GetComponent<Collider2D>();
    }

    private void Start()
    {
        if (meleeVisualIndicator != null) meleeVisualIndicator.SetActive(false);
        InitializeBoss();
    }

    public void InitializeBoss()
    {
        if (config == null) return;

        actualHealth = config.maxHealth;
        hasTriggeredFury = false;
        state = BossState.Idle;
        cooldownTimer = 0f;

        if (bossHealthSlider != null)
        {
            bossHealthSlider.gameObject.SetActive(true);
            bossHealthSlider.maxValue = config.maxHealth;
            bossHealthSlider.value = actualHealth;
        }

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null)
        {
            playerTransform = player.transform;
        }
    }

    private void FixedUpdate()
    {
        if (state == BossState.Dead || config == null) return;

        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null) playerTransform = player.transform;
            return;
        }
        ReturnArena();

        if (state == BossState.Idle)
        {
            cooldownTimer += Time.deltaTime;
            if (cooldownTimer >= config.actionCooldown)
            {
                int playerPlat = GetPlatformIndex(playerTransform);
                int bossPlat = GetPlatformIndex(transform);

                if (playerPlat != bossPlat && playerPlat != -1)
                {
                    if (playerPlat == 3) // Plataforma 4: sempre usa Charge, nunca pula
                    {
                        StartCoroutine(ChargeAttackRoutine(config.platform4ChargeSection));
                        return;
                    }

                    // Distância horizontal até o player
                    float horizontalDist = Mathf.Abs(playerTransform.position.x - transform.position.x);

                    // Perto -> pula para a plataforma do player e faz Melee.
                    // Longe -> anda e atira.
                    if (horizontalDist <= config.jumpChaseDistance)
                    {
                        StartCoroutine(JumpToPlatformRoutine(playerPlat, bossPlat));
                    }
                    else
                    {
                        StartCoroutine(WalkAndShootRoutine());
                    }
                    return;
                }
                
                DecideNextAction();
            }
        }
        else if (state == BossState.MovingMelee)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= config.meleeReach)
            {
                if (rb != null) rb.linearVelocity = new Vector2(0f, rb.linearVelocity.y);
                StartCoroutine(MeleeAttackRoutine());
            }
            else
            {
                int dir = (int)Mathf.Sign(playerTransform.position.x - transform.position.x);
                if (dir != facingDirection && dir != 0)
                {
                    Flip(dir);
                }
                if (rb != null)
                {
                    rb.linearVelocity = new Vector2(config.speed * facingDirection, rb.linearVelocity.y);

                    // Lógica de pulo na perseguição
                    chaseJumpTimer += Time.deltaTime;
                    if (chaseJumpTimer >= 1.5f) // Tenta pular a cada 1.5s se necessário
                    {
                        bool shouldJump = playerTransform.position.y > transform.position.y + 0.5f;
                        
                        // Também pula se estiver bloqueado horizontalmente (velocidade real baixa comparada à desejada)
                        if (!shouldJump && Mathf.Abs(rb.linearVelocity.x) < config.speed * 0.5f)
                        {
                            shouldJump = true;
                        }

                        if (shouldJump && Mathf.Abs(rb.linearVelocity.y) < 0.1f) // Se estiver no chão/estável
                        {
                            // Aplica a velocidade vertical diretamente para o pulo de perseguição
                            rb.linearVelocity = new Vector2(rb.linearVelocity.x, config.chaseJumpForce);
                            chaseJumpTimer = 0f;
                        }
                    }
                }
            }
        }
    }

    private void DecideNextAction()
    {
        if (state == BossState.Dead || playerTransform == null) return;

        float hpPercent = (float)actualHealth / config.maxHealth;
        int meleeWeight, projectileWeight, chargeWeight, sequenceWeight;

        if (hpPercent > 0.5f)
        {
            meleeWeight = config.phase1MeleeWeight;
            projectileWeight = config.phase1ProjectileWeight;
            chargeWeight = config.phase1ChargeWeight;
            sequenceWeight = 0;
        }
        else
        {
            meleeWeight = config.phase2MeleeWeight;
            projectileWeight = config.phase2ProjectileWeight;
            chargeWeight = config.phase2ChargeWeight;
            sequenceWeight = config.phase2SequenceWeight;
        }

        int totalWeight = meleeWeight + projectileWeight + chargeWeight + sequenceWeight;
        int roll = Random.Range(0, totalWeight);

        if (roll < meleeWeight)
        {
            // Neste ponto o player está na mesma plataforma do Boss
            // (a decisão de pular entre plataformas é tratada no FixedUpdate).
            state = BossState.MovingMelee;
        }
        else if (roll < meleeWeight + projectileWeight)
        {
            StartCoroutine(ProjectileAttackRoutine());
        }
        else if (roll < meleeWeight + projectileWeight + chargeWeight)
        {
            StartCoroutine(ChargeAttackRoutine());
        }
        else
        {
            StartCoroutine(SequenceAttackRoutine());
        }
    }

    private IEnumerator JumpToPlatformRoutine(int targetPlatformIndex, int actualPlataformIndex)
    {
        state = BossState.JumpingToPlatform;
        
        if (rb != null)
        {
            // Calcula direção horizontal para o player
            int dir = (int)Mathf.Sign(playerTransform.position.x - transform.position.x);
            if (dir != facingDirection && dir != 0) Flip(dir);

            // Zera a velocidade antes do pulo para ser consistente
            rb.linearVelocity = Vector2.zero;

            // Aplica força de pulo (Y para subir, X para avançar)
            // Nota: Se a plataforma for abaixo, a física da gravidade cuidará disso, 
            // mas ainda damos um pequeno pulo para parecer natural.
            float jumpForceY = config.jumpForceY;
            if (targetPlatformIndex < actualPlataformIndex) jumpForceY /= 2;
            if (targetPlatformIndex == actualPlataformIndex) jumpForceY = 0f; // Se estiver na mesma plataforma, não pula verticalmente
       

            // Define a velocidade diretamente, ignorando a massa de 1000
            rb.linearVelocity = new Vector2(config.jumpForceX * facingDirection, jumpForceY);
            collider.isTrigger = true; // Para não colidir com a plataforma durante o pulo
        }

        // Aguarda um pouco para sair da plataforma atual
        yield return new WaitForSeconds(1f);

        // Espera o boss chegar perto da altura alvo ou começar a cair/estabilizar
        float timeout = 1.0f;
        float elapsed = 0f;
        while (elapsed < timeout)
        {
            int currentPlat = GetPlatformIndex(transform);
            if (currentPlat == targetPlatformIndex) { collider.isTrigger = false; break; }

            // Se o boss estiver caindo e já passou da altura da plataforma (caso de descer)
            if (rb.linearVelocity.y < 0.1f && Mathf.Abs(transform.position.y - platformReferences[targetPlatformIndex].position.y) < config.platformThresholdY)
            { 
                
                break; 
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        // Uma vez na plataforma ou perto do player, inicia o ataque melee
        StartCoroutine(MeleeAttackRoutine());
    }

    private IEnumerator MeleeAttackRoutine()
    {
        state = BossState.AttackingMelee;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        int dirToPlayer = (int)Mathf.Sign(playerTransform.position.x - transform.position.x);
        if (dirToPlayer != facingDirection && dirToPlayer != 0)
        {
            Flip(dirToPlayer);
        }

        if (meleeVisualIndicator != null)
        {
            meleeVisualIndicator.transform.position = transform.position + new Vector3(facingDirection * (config.meleeReach + 0.3f), 0, 0);
            meleeVisualIndicator.SetActive(true);
        }

        Color originalColor = sr != null ? sr.color : Color.white;
        if (sr != null) sr.color = Color.yellow;
        yield return new WaitForSeconds(0.6f); // telegraph
        if (sr != null) sr.color = originalColor;

        if (meleeVisualIndicator != null) meleeVisualIndicator.SetActive(false);

        if (playerTransform != null)
        {
            float dist = Vector3.Distance(transform.position, playerTransform.position);
            if (dist <= config.meleeReach + 0.6f)
            {
                PlayerStats stats = playerTransform.GetComponent<PlayerStats>();
                if (stats != null) stats.TakeDmg(config.meleeDamage);
            }
        }

        yield return new WaitForSeconds(0.4f); // recovery
        state = BossState.Idle;
        cooldownTimer = 0f;
    }

    private IEnumerator ProjectileAttackRoutine()
    {
        state = BossState.AttackingProjectile;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Color originalColor = sr != null ? sr.color : Color.white;
        if (sr != null) sr.color = Color.cyan;
        yield return new WaitForSeconds(0.5f); // telegraph
        if (sr != null) sr.color = originalColor;

        ShootProjectile();

        yield return new WaitForSeconds(0.4f); // recovery
        state = BossState.Idle;
        cooldownTimer = 0f;
    }

    private IEnumerator WalkAndShootRoutine()
    {
        state = BossState.WalkingToShoot;
        float timer = 0f;
        float walkTime = config.walkDurationDifferentPlatform; // 2 segundos (conforme solicitado, alterável no config)

        while (timer < walkTime)
        {
            if (rb != null)
            {
                rb.linearVelocity = new Vector2(config.speed * facingDirection, rb.linearVelocity.y);
            }

            // Simples lógica de bater e voltar ou oscilar
            // Aqui podemos adicionar verificação de borda se necessário, 
            // mas por agora ele apenas anda na direção que está virado.
            
            timer += Time.deltaTime;
            yield return null;
        }

        if (rb != null) rb.linearVelocity = Vector2.zero;
        
        // Atira em direção ao player
        ShootProjectile();

        yield return new WaitForSeconds(0.5f); // Pequena pausa após o tiro
        state = BossState.Idle;
        cooldownTimer = 0f;
    }

    private void ShootProjectile()
    {
        if (projectileObj == null || playerTransform == null) return;

        Vector2 spawnPos = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        int dirToPlayer = (int)Mathf.Sign(playerTransform.position.x - transform.position.x);
        if (dirToPlayer != facingDirection && dirToPlayer != 0)
        {
            Flip(dirToPlayer);
        }

        GameObject proj;
        if (Fase7PoolManager.Instance != null)
        {
            proj = Fase7PoolManager.Instance.Get(projectileObj, spawnPos, Quaternion.identity);
        }
        else
        {
            proj = Instantiate(projectileObj, spawnPos, Quaternion.identity);
        }

        ProjectileEnemyComplex projEnemy = proj.GetComponent<ProjectileEnemyComplex>();
        if (projEnemy != null)
        {
            projEnemy.projectileSpeed = (int)config.projectileSpeed;
            projEnemy.projectileDmg = config.projectileDamage;
            Vector2 directionVector = ((Vector2)playerTransform.position - spawnPos).normalized;
            projEnemy.directionPlayer = directionVector;
        }

        SpriteRenderer projSr = proj.GetComponent<SpriteRenderer>();
        if (projSr != null)
        {
            projSr.flipX = facingDirection > 0;
        }
    }

    private IEnumerator ChargeAttackRoutine(int? sectionOverride = null)
    {
        state = BossState.AttackingCharge;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        int sec = sectionOverride ?? Random.Range(0, 3);
        if (Fase7BeamSystem.Instance != null)
        {
            yield return Fase7BeamSystem.Instance.TriggerSectionRoutine(sec);
        }

        state = BossState.Idle;
        cooldownTimer = 0f;
    }

    private IEnumerator SequenceAttackRoutine()
    {
        state = BossState.AttackingSequence;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        for (int i = 0; i < 3; i++)
        {
            if (Fase7BeamSystem.Instance != null)
            {
                yield return Fase7BeamSystem.Instance.TriggerSectionRoutine(i);
            }
            yield return new WaitForSeconds(0.35f);
        }

        state = BossState.Idle;
        cooldownTimer = 0f;
    }

    private IEnumerator FurySequenceAttackRoutine()
    {
        state = BossState.AttackingSequence;
        if (rb != null) rb.linearVelocity = Vector2.zero;

        Color originalColor = sr != null ? sr.color : Color.white;
        if (sr != null) sr.color = Color.magenta; // Fury color!
        yield return new WaitForSeconds(1.0f);
        if (sr != null) sr.color = originalColor;

        for (int i = 0; i < 3; i++)
        {
            if (Fase7BeamSystem.Instance != null)
            {
                yield return Fase7BeamSystem.Instance.TriggerSectionRoutine(i);
            }
            yield return new WaitForSeconds(0.3f);
        }

        state = BossState.Idle;
        cooldownTimer = 0f;
    }

    public void TakeDamage(int damage)
    {
        if (state == BossState.Dead) return;

        actualHealth -= damage;
        if (bossHealthSlider != null)
        {
            bossHealthSlider.value = actualHealth;
        }

        StartCoroutine(DamageFlashRoutine());

        float hpPercent = (float)actualHealth / config.maxHealth;
        if (hpPercent <= 0.5f && !hasTriggeredFury)
        {
            hasTriggeredFury = true;
            StopAllCoroutines();
            if (sr != null) sr.color = Color.white;
            StartCoroutine(FurySequenceAttackRoutine());
            return;
        }

        if (actualHealth <= 0)
        {
            Die();
        }
    }

    private IEnumerator DamageFlashRoutine()
    {
        if (sr != null)
        {
            Color current = sr.color;
            sr.color = Color.red;
            yield return new WaitForSeconds(0.15f);
            sr.color = current;
        }
    }

    private void Flip(int dir)
    {
        facingDirection = dir;
        transform.rotation = Quaternion.Euler(0, facingDirection > 0 ? 180f : 0f, 0);
    }

    private void Die()
    {
        playerTransform.GetComponent<PlayerInteract>().CanOpenDialogue(true, config.dialogue);
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (state == BossState.Dead) return;

        if (collision.gameObject.CompareTag("Player"))
        {
            PlayerStats stats = collision.gameObject.GetComponent<PlayerStats>();
            if (stats != null)
            {
                stats.TakeDmg(config.contactDamage);
            }
            rb.linearVelocity = Vector2.zero;
        }
    }

    private int GetPlatformIndex(Transform target)
    {
        if (target == null || platformReferences == null) return -1;
        
        int closestIndex = -1;
        float minDistanceY = float.MaxValue;

        for (int i = 0; i < platformReferences.Length; i++)
        {
            if (platformReferences[i] == null) continue;

            float distY = Mathf.Abs(target.position.y - platformReferences[i].position.y);
            if (distY < minDistanceY && distY < config.platformThresholdY)
            {
                minDistanceY = distY;
                closestIndex = i;
            }
        }

        // Se não estiver "em cima" de uma plataforma, tenta pegar a que está logo abaixo
        if (closestIndex == -1)
        {
            for (int i = 0; i < platformReferences.Length; i++)
            {
                if (platformReferences[i] == null) continue;
                if (target.position.y > platformReferences[i].position.y)
                {
                    float distY = target.position.y - platformReferences[i].position.y;
                    if (distY < minDistanceY)
                    {
                        minDistanceY = distY;
                        closestIndex = i;
                    }
                }
            }
        }

        return closestIndex;
    }

    public void ReturnArena()
    {
        Vector3 pos = transform.position;
        if (System.MathF.Abs(pos.x) > limitArenaX || System.MathF.Abs(pos.y) > limitArenaY)
        {
            transform.position = Vector3.zero;
            rb.angularVelocity = 0f;
            rb.linearVelocity = Vector2.zero;
        }
    }

}