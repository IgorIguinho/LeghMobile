# Project Overview
- **Game Title:** LeghMobile (LMobileUnity) — *(confirmar título oficial)*
- **High-Level Concept:** Plataforma 2D de ação com mecânicas de movimento avançado (dash, wall jump, corda) e inimigos. Este plano adiciona uma **IA de estados (FSM) para o inimigo atirador (RangedEnemy)**: ele fica parado, detecta o player que se aproxima, carrega a munição, dispara e vira quando o player passa para as suas costas — cada ação com sua própria animação.
- **Players:** Single player.
- **Inspiration / Reference Games:** Plataformas 2D de ação (estilo Celeste/Hollow Knight para movimento; inimigos atiradores estáticos como torres/sentinelas).
- **Tone / Art Direction:** 2D sprite-based (built-in pipeline). *(Sprites do inimigo serão gerados como placeholder por IA.)*
- **Target Platform:** WebGL.
- **Screen Orientation / Resolution:** Landscape *(confirmar resolução alvo)*.
- **Render Pipeline:** Built-in.
- **Unity Version:** 6000.4.7f1 — Input System: New Input System (`PlayerControl` asset).

> Observação: os campos marcados com *(confirmar)* não afetam a implementação da IA; estão aqui apenas para completude.

# Game Mechanics

## Core Gameplay Loop (foco: inimigo atirador)
O RangedEnemy é uma sentinela estática que cria pressão de área. Loop de comportamento (FSM por enum):

```
Idle ──(player dentro do raio)──► [precisa virar?] ──sim──► Flipping ──► (reavalia)
  ▲                                      │ não
  │                                      ▼
  └──(cooldown)── Shooting ◄── Charging (carrega munição)
```

1. **Idle** — parado, animação de repouso, varrendo o ambiente com detecção 360°.
2. **Detecção (alcance único)** — `Physics2D.OverlapCircle` em torno do inimigo na `layerPlayer`. Mesmo raio para detectar e disparar.
3. **Flipping** — se o player está do lado oposto ao que o inimigo encara, toca a animação de virar e inverte o `direction` (continua atacando depois — escolha do usuário).
4. **Charging** — "carregar munição": toca animação de carga e aguarda `chargeTime`.
5. **Shooting** — "disparar": toca animação de tiro, instancia o `ProjectileEnemy` no `firePoint` com a direção correta, aguarda `shootRecover`.
6. **Cooldown** — aguarda `attackCooldown` e volta para Idle/Detecção.

## Controls and Input Methods
N/A para o inimigo (IA autônoma). O player usa o New Input System (`PlayerControl`). A detecção depende de o player estar na `LayerMask layerPlayer` (validar que o GameObject do player está na layer correta e tem o `Collider2D`).

## Detecção sem Garbage Collector
A varredura roda todo frame (`Update`), então não pode alocar memória. Em vez de `Physics2D.OverlapCircle(...)` (que retorna um `Collider2D` novo) ou da sobrecarga que retorna `Collider2D[]`, usamos a versão **non-alloc** com objetos pré-alocados:
- `ContactFilter2D playerFilter` — criado **uma vez** no `Start()` (`useLayerMask = true`, `SetLayerMask(layerPlayer)`).
- `Collider2D[] detectResults = new Collider2D[1]` — buffer reutilizado (basta 1 resultado para single player).
- `Physics2D.OverlapCircle(center, radius, playerFilter, detectResults)` retorna a contagem de hits, preenchendo o buffer — **zero alocação por frame**.
- Os `WaitForSeconds` das corrotinas também são cacheados no `Start()` para evitar GC repetido.

# UI
Sem UI nova. Auxílio de debug:
- **Gizmo** de `OnDrawGizmosSelected` desenhando o raio de detecção (`DrawWireSphere(transform.position, detectRadius)`).
- Campo `state` (enum) visível no Inspector via `[SerializeField]` para inspeção em runtime.

# Key Asset & Context

## Assets existentes (a modificar)
- `Assets/Prefab/Enemy/RangedEnemy.prefab` — Raiz: `Transform, SpriteRenderer, RangedEnemy, EnemyStats, ColliderDmgEnemy, BoxCollider2D, Rigidbody2D`. Filhos: `DmgCollider` (BoxCollider2D), `CheckTransform` (Transform). **Não possui Animator.**
- `Assets/Prefab/Enemy/ProjectileEnemy.prefab` — `SpriteRenderer, ProjectileEnemy, Rigidbody2D, CapsuleCollider2D`.
- `Assets/Scripts/Enemys/RangedEnemy.cs` — lógica atual (detecção em 1 direção, corrotina de ataque, `Flip()` nunca chamado).
- `Assets/Scripts/Enemys/ProjectileEnemy.cs` — **bug:** `rb.linearVelocity = new Vector2(projectileSpeed, 0)` ignora `direction`.

## Assets a criar
- **Sprites placeholder (IA):** sprite sheets para 4 ações:
  - `RangedEnemy_Idle` (loop, ~2-4 frames)
  - `RangedEnemy_Charge` (carregar munição, ~3-4 frames, não-loop)
  - `RangedEnemy_Shoot` (disparar, ~2-3 frames, não-loop)
  - `RangedEnemy_Flip` (virar, ~2-3 frames, não-loop)
  - Pasta sugerida: `Assets/Arts/Inimigos/RangedEnemy/Sprites/`
- **AnimationClips (.anim):** `RangedEnemy_Idle.anim`, `RangedEnemy_Charge.anim`, `RangedEnemy_Shoot.anim`, `RangedEnemy_Flip.anim`
  - Pasta: `Assets/Arts/Inimigos/RangedEnemy/Animation/`
- **AnimatorController:** `RangedEnemyControle.controller` (mesmo padrão de nome dos controllers existentes em `Assets/Arts/Personagens/Protagonista/Animation/`).

## Parâmetros do Animator (contrato código ↔ animator)
- `PlayerDetected` (Bool) — true enquanto há player no raio.
- `Charge` (Trigger) — inicia animação de carregar.
- `Shoot` (Trigger) — inicia animação de disparo.
- `Flip` (Trigger) — inicia animação de virar.

Estados/transições (loop de combate contínuo enquanto o player é visto):
- `Idle` (default) → `Charge` (cond: trigger `Charge`)
- `Charge` → `Shoot` (cond: trigger `Shoot`)
- **`Shoot` → `Charge`** (cond: `PlayerDetected == true`, após Exit Time) — depois de atirar, se o player **ainda está no raio**, o inimigo **recarrega** e dispara de novo.
- **`Shoot` → `Idle`** (cond: `PlayerDetected == false`, após Exit Time) — só volta a Idle quando o player **sai** do raio.
- `Idle/Charge/Shoot` → `Flip` (cond: trigger `Flip`) → `Idle` (Exit Time)

Resumo do fluxo: `Idle → Charge → Shoot → (player visível? → Charge → Shoot → …) | (player fora? → Idle)`.

## Snippet de referência — novo `RangedEnemy.cs` (FSM por enum)
```csharp
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Animator))]
public class RangedEnemy : MonoBehaviour
{
    enum EnemyState { Idle, Charging, Shooting, Flipping }
    [Header("Estado (debug)")]
    [SerializeField] EnemyState state = EnemyState.Idle;

    [Header("Ataque")]
    public GameObject projectileObj;
    public Transform firePoint;          // origem do projétil (reaproveitar CheckTransform)
    public float chargeTime = 0.6f;      // carregar munição
    public float shootRecover = 0.3f;    // recuperação pós-disparo
    public float attackCooldown = 1f;    // espera entre ataques
    public float flipTime = 0.3f;        // duração da animação de virar
    int direction = 1;

    [Header("Detecção (alcance único)")]
    public float detectRadius = 6f;
    public LayerMask layerPlayer;
    Transform player;

    // --- Detecção sem alocação (zero GC) ---
    ContactFilter2D playerFilter;
    readonly Collider2D[] detectResults = new Collider2D[1];

    Animator animator;
    bool busy; // ocupado em charge/shoot/flip

    // cache de WaitForSeconds para não alocar a cada uso
    WaitForSeconds waitCharge, waitRecover, waitCooldown, waitFlip;

    void Start()
    {
        animator = GetComponent<Animator>();

        // Configura o filtro uma única vez (reutilizado em todo frame)
        playerFilter = new ContactFilter2D();
        playerFilter.useLayerMask = true;
        playerFilter.SetLayerMask(layerPlayer);
        playerFilter.useTriggers = true;

        waitCharge   = new WaitForSeconds(chargeTime);
        waitRecover  = new WaitForSeconds(shootRecover);
        waitCooldown = new WaitForSeconds(attackCooldown);
        waitFlip     = new WaitForSeconds(flipTime);
    }

    void Update()
    {
        DetectPlayer();
        if (busy) return;
        if (player == null) { state = EnemyState.Idle; return; }
        if (NeedFlip()) { StartCoroutine(FlipRoutine()); return; }
        StartCoroutine(AttackRoutine());
    }

    // Sem GC: OverlapCircle non-alloc reutilizando filtro e buffer pré-alocados
    void DetectPlayer()
    {
        int count = Physics2D.OverlapCircle(transform.position, detectRadius, playerFilter, detectResults);
        player = count > 0 ? detectResults[0].transform : null;
        animator.SetBool("PlayerDetected", player != null);
    }

    bool NeedFlip()
    {
        float dx = player.position.x - transform.position.x;
        return (dx > 0 && direction < 0) || (dx < 0 && direction > 0);
    }

    // Loop de combate contínuo: enquanto o player for visto, recarrega e atira de novo.
    IEnumerator AttackRoutine()
    {
        busy = true;
        // Continua o ciclo Charge -> Shoot enquanto o player estiver no raio e alinhado.
        while (player != null && !NeedFlip())
        {
            state = EnemyState.Charging;
            animator.SetTrigger("Charge");
            yield return waitCharge;

            // Reavalia após carregar; se o player saiu/cruzou, sai do loop.
            if (player == null || NeedFlip()) break;

            state = EnemyState.Shooting;
            animator.SetTrigger("Shoot");
            Shoot();                      // (ou via Animation Event — ver passo 8)
            yield return waitRecover;

            yield return waitCooldown;    // espera entre disparos; depois volta ao topo do while (recarrega)
        }
        state = EnemyState.Idle;          // só sai do combate quando o player não é mais visto (ou precisa virar)
        busy = false;
    }

    void Shoot()
    {
        Vector3 spawn = firePoint ? firePoint.position : transform.position;
        GameObject p = Instantiate(projectileObj, spawn, Quaternion.identity);
        p.GetComponent<ProjectileEnemy>().direction = direction;
    }

    IEnumerator FlipRoutine()
    {
        busy = true;
        state = EnemyState.Flipping;
        animator.SetTrigger("Flip");
        yield return new WaitForSeconds(flipTime);
        Flip();
        busy = false;
    }

    void Flip()
    {
        direction *= -1;
        transform.Rotate(0, 180f, 0);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, detectRadius);
    }
}
```

## Correção em `ProjectileEnemy.cs`
```csharp
void Moviment()
{
    rb.linearVelocity = new Vector2(projectileSpeed * direction, 0); // antes: sem * direction
}
```

# Implementation Steps

### Step 1 — Gerar sprites placeholder (IA) para as 4 ações
- **Descrição:** Gerar sprite sheets placeholder para `Idle`, `Charge` (carregar munição), `Shoot` (disparar) e `Flip` (virar), salvando em `Assets/Arts/Inimigos/RangedEnemy/Sprites/`. Importar como `Sprite (2D and UI)`, ajustar `Pixels Per Unit` coerente com os sprites do projeto e fatiar (Sprite Editor) quando for sheet.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** Yes (independe do código)

### Step 2 — Criar os AnimationClips
- **Descrição:** Criar `RangedEnemy_Idle.anim` (loop), `RangedEnemy_Charge.anim`, `RangedEnemy_Shoot.anim`, `RangedEnemy_Flip.anim` (não-loop) a partir dos sprites do Step 1, em `Assets/Arts/Inimigos/RangedEnemy/Animation/`.
- **Assigned role:** developer
- **Dependencies:** Depends on Step 1
- **Parallelizable:** No

### Step 3 — Criar o AnimatorController
- **Descrição:** Criar `RangedEnemyControle.controller`. Adicionar parâmetros `PlayerDetected (Bool)`, `Charge/Shoot/Flip (Triggers)`. Montar estados e transições conforme a seção "Parâmetros do Animator". Idle como default.
- **Assigned role:** developer
- **Dependencies:** Depends on Step 2
- **Parallelizable:** No

### Step 4 — Reescrever `RangedEnemy.cs` como FSM por enum
- **Descrição:** Substituir o conteúdo de `Assets/Scripts/Enemys/RangedEnemy.cs` pelo snippet de referência (FSM com `Idle/Charging/Shooting/Flipping`). Pontos-chave: detecção 360° **sem GC** via `OverlapCircle` non-alloc com `ContactFilter2D` + buffer pré-alocado; `WaitForSeconds` cacheados; **loop de combate contínuo** (Charge→Shoot→Charge enquanto o player for visto, voltando a Idle só quando ele sai do raio); chamada real de `Flip()`; gizmo de raio.
- **Assigned role:** developer
- **Dependencies:** None (independe da arte)
- **Parallelizable:** Yes (concorrente com Steps 1-3)

### Step 5 — Corrigir bug de direção do projétil
- **Descrição:** Em `Assets/Scripts/Enemys/ProjectileEnemy.cs`, multiplicar a velocidade por `direction` para o tiro respeitar o lado que o inimigo encara.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** Yes

### Step 6 — Configurar o prefab RangedEnemy
- **Descrição:** No `Assets/Prefab/Enemy/RangedEnemy.prefab`: adicionar componente `Animator` na raiz e atribuir `RangedEnemyControle.controller`; reaproveitar o filho `CheckTransform` como `firePoint` (ou criar um `FirePoint` posicionado na "boca" do canhão).
- **Assigned role:** developer
- **Dependencies:** Depends on Step 3, Step 4
- **Parallelizable:** No

### Step 7 — Wire dos campos no Inspector do prefab
- **Descrição:** Atribuir `projectileObj` (= `ProjectileEnemy.prefab`), `firePoint`, `layerPlayer` (layer do player), e ajustar `detectRadius`, `chargeTime`, `shootRecover`, `attackCooldown`, `flipTime`. Verificar que o player está na layer correta e tem `Collider2D` + tag "Player".
- **Assigned role:** developer
- **Dependencies:** Depends on Step 6, Step 5
- **Parallelizable:** No

### Step 8 — (Opcional) Animation Event para o disparo
- **Descrição:** Em vez de chamar `Shoot()` por tempo, adicionar um Animation Event no frame de tiro do clip `RangedEnemy_Shoot` chamando um método público `ShootEvent()`. Garante o spawn do projétil no frame visual exato. Manter timing por corrotina como fallback.
- **Assigned role:** developer
- **Dependencies:** Depends on Step 3, Step 4
- **Parallelizable:** No

# Verification & Testing

## Verificações manuais (Edit/Play Mode)
1. **Idle:** sem player no raio → inimigo parado tocando `RangedEnemy_Idle`, `state == Idle`, `PlayerDetected == false`.
2. **Detecção + Ataque contínuo:** player entra no `detectRadius` pela frente → `Charging` (anim carregar) → `Shooting` (anim disparar) → projétil nasce no `firePoint` na direção correta → **enquanto o player continuar no raio, repete `Charge → Shoot`** sem voltar a Idle. Ao **sair** do raio, transita `Shoot → Idle`.
3. **Flip (player nas costas):** mover o player para o lado oposto → toca `RangedEnemy_Flip`, `transform` inverte (Rotate 180), `direction` troca de sinal, e o inimigo **volta a atacar** o player do novo lado.
4. **Direção do projétil:** confirmar que o tiro vai para a esquerda quando o inimigo encara a esquerda (valida a correção do Step 5).
5. **Gizmo:** selecionar o prefab na cena e confirmar o `WireSphere` do `detectRadius`.

## Casos de borda
- Player saindo do raio durante o `chargeTime` → não dispara (re-checagem após a carga, dentro do `while`) e o loop encerra voltando a Idle.
- Player cruzando para as costas **durante** a carga → após a carga, `NeedFlip()` quebra o loop; no próximo frame o inimigo vira e retoma o combate.
- Sem `firePoint` atribuído → fallback para `transform.position` (não deve quebrar).
- Múltiplos players/colliders na layer → buffer `detectResults[1]` retorna o primeiro; suficiente para single player.
- **Zero GC:** com o Profiler aberto (Memory/GC Alloc), confirmar que `Update`/`DetectPlayer` e as corrotinas **não geram alocações por frame** (filtro, buffer e `WaitForSeconds` reutilizados).

## Teste de Play Mode (automatizável)
- Spawnar player dentro do raio e verificar que `ProjectileEnemy` é instanciado após `chargeTime` e que sua `direction` corresponde ao `direction` do inimigo.
- Mover o player para o lado oposto e verificar a inversão de `transform.localScale`/rotação e do sinal de `direction`.
