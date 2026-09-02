using System.Collections;

using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayerStats : MonoBehaviour
{
    public int maxHp;
    public int actualHp;

    public float timeAnimationDmg;

    [Header("Invulnerabilidade (i-frames)")]
    [Tooltip("Tempo TOTAL (segundos) que o player fica invulneravel apos tomar dano. Inclui o tempo do flash vermelho.")]
    [SerializeField] float invulnerabilityDuration = 1.5f;

    [Tooltip("Layer(s) de inimigos, para desligar a colisao fisica durante a invulnerabilidade. Pode marcar mais de uma layer se necessario. No prefab atual, PlayerAttack.enemyLayer usa apenas a layer index 7 (bit 128).")]
    [SerializeField] LayerMask enemyLayer;

    // Intervalo do piscar fixo no codigo, conforme pedido (nao exposto no Inspector).
    const float BLINK_INTERVAL = 0.1f;
    const float BLINK_ALPHA = 0.3f;

    string actualScene;
    SpriteRenderer sr;

    bool isInvulnerable;
    int playerLayerIndex;

    // Suporta enemyLayer com MAIS de uma layer marcada no Inspector.
    // Preenchido uma unica vez em Start() (nao aloca em runtime durante o dano).
    int[] enemyLayerIndices;
    int enemyLayerCount;

    Coroutine invulnerabilityRoutine;

    // Instancias de WaitForSeconds cacheadas para evitar GC (mobile).
    WaitForSeconds waitDmgFlash;
    WaitForSeconds waitBlinkInterval;

    // Start is called before the first frame update
    void Start()
    {
        actualHp = maxHp;
        sr = GetComponent<SpriteRenderer>();
        actualScene = SceneManager.GetActiveScene().name;

        playerLayerIndex = gameObject.layer;
        CacheEnemyLayerIndices();

        waitDmgFlash = new WaitForSeconds(timeAnimationDmg);
        waitBlinkInterval = new WaitForSeconds(BLINK_INTERVAL);
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void TakeDmg(int dmg)
    {
        // Durante a invulnerabilidade, ignora completamente novo dano (sem re-trigger do efeito).
        if (isInvulnerable) return;

        actualHp -= dmg;
        HudManagerOnFase.Instance.hpCount.text = "HP " + actualHp.ToString();

        if (actualHp <= 0)
        {
            SceneManager.LoadScene(actualScene);
            return; // cena vai recarregar, nao ha motivo para iniciar i-frames
        }

        // Defensivo: evita duas coroutines de invulnerabilidade rodando ao mesmo tempo
        // caso algo dispare TakeDmg de forma inesperada durante a invulnerabilidade.
        if (invulnerabilityRoutine != null)
        {
            StopCoroutine(invulnerabilityRoutine);
        }

        invulnerabilityRoutine = StartCoroutine(InvulnerabilityRoutine());
    }

    IEnumerator InvulnerabilityRoutine()
    {
        isInvulnerable = true;
        SetCollisionWithEnemies(false);

        // 1) Flash vermelho original (comportamento inalterado)
        sr.color = Color.red;
        yield return waitDmgFlash;
        sr.color = Color.white;

        // 2) Piscar via Alpha pelo tempo restante de invulnerabilidade
        float elapsed = timeAnimationDmg;
        Color blinkColor = sr.color;
        bool visible = true;

        while (elapsed < invulnerabilityDuration)
        {
            visible = !visible;
            blinkColor.a = visible ? 1f : BLINK_ALPHA;
            sr.color = blinkColor;

            yield return waitBlinkInterval;
            elapsed += BLINK_INTERVAL;
        }

        // Garante que o alpha volta para 1 no final, mesmo que o loop nao feche perfeitamente
        blinkColor.a = 1f;
        sr.color = blinkColor;

        SetCollisionWithEnemies(true);
        isInvulnerable = false;
        invulnerabilityRoutine = null;
    }

    void SetCollisionWithEnemies(bool collide)
    {
        for (int i = 0; i < enemyLayerCount; i++)
        {
            Physics2D.IgnoreLayerCollision(playerLayerIndex, enemyLayerIndices[i], !collide);
        }
    }

    // Converte a LayerMask (que pode ter 1 ou varias layers marcadas) num array
    // de indices de layer. Roda uma unica vez em Start(), entao o alloc aqui nao afeta
    // performance em runtime (TakeDmg/InvulnerabilityRoutine nao alocam nada).
    void CacheEnemyLayerIndices()
    {
        int mask = enemyLayer.value;
        enemyLayerIndices = new int[12];
        enemyLayerCount = 0;

        for (int i = 0; i < 32; i++)
        {
            if ((mask & (1 << i)) != 0)
            {
                enemyLayerIndices[enemyLayerCount] = i;
                enemyLayerCount++;
            }
        }
    }
}