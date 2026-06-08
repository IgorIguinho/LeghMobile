# Project Overview

- **Game Title:** LeghMobile (Lmobile)
- **High-Level Concept:** Plataforma 2D mobile com mecânicas de movimento (corrida, pulo, wall-jump, dash) e rewind. Esta feature adiciona **plataformas que caem** na Fase 2 como elemento de perigo/puzzle.
- **Players:** Single player.
- **Inspiration / Reference Games:** Plataformers de hazard/timing (Celeste, Super Meat Boy — armadilhas de plataforma que desabam).
- **Tone / Art Direction:** Pixel/2D existente do projeto (tiles via Tilemap).
- **Target Platform:** WebGL / Mobile (otimização para mobile é prioridade).
- **Screen Orientation / Resolution:** Conforme o projeto atual (não alterado por esta feature).
- **Render Pipeline:** Built-in.

# Game Mechanics

## Core Gameplay Loop
Na Fase 2 existe um Tilemap `Grid/FallPlataform` com vários **clusters** (agrupamentos de tiles conectados); cada cluster é uma plataforma. Cada plataforma pode desabar quando o jogador a ativa por um de dois gatilhos:

1. **Passar por baixo (gatilho principal):** o jogador, estando **abaixo** da plataforma (por padrão na **ponta esquerda**, prestes a passar por baixo), aciona a queda. Cria tensão: andar embaixo é arriscado. A **área de detecção** desse gatilho é totalmente **configurável no Inspector** (offset + tamanho da caixa, relativos à plataforma) e a sondagem é feita por **`Physics2D.OverlapBoxNonAlloc`** (sem alocação/GC).
2. **Pisar em cima:** o jogador pousa/anda sobre a plataforma e ela começa a desabar após um *delay*. Também detectado por uma **área de detecção configurável** (caixa acima do topo) via OverlapBox non-alloc.

Após o gatilho, espera-se `fallDelay` segundos e a plataforma cai. Consequências de colisão durante a queda:
- Se a plataforma cair **sobre** o jogador → empurra o jogador para baixo (resolução física).
- Se o jogador for **esmagado** entre a plataforma e o `Ground` → recebe **dano único configurável** via `PlayerStats.TakeDmg(int)`.
- Ao atingir o `Ground` (ou sair de cena) a plataforma é **destruída**.

### Decisões confirmadas com o usuário
- **Rewind:** NÃO integrar (plataforma independente do `RewindObj`/`RewindManager`).
- **Dano de esmagamento:** valor único configurável aplicado uma vez.
- **Movimento de queda:** dois modos selecionáveis por enum — `ConstantSpeed` (velocidade fixa) e `Gravity` (acelera por gravidade), **ambos usando a mesma variável de velocidade** (`fallSpeed`) do Inspector. Em `Gravity`, `fallSpeed` atua como velocidade terminal (clamp).
- **Pós-queda:** destruir.
- **Conversão tilemap → plataformas:** **bake no Editor** (ferramenta de menu), zero custo em runtime.

## Controls and Input Methods
Sem novos inputs. Reaproveita o controlador existente (New Input System / `PlayerMovements`). Os gatilhos são detectados por sondagem física **OverlapBox non-alloc** (sem GC), não por colisão/trigger nem por botão.

# UI
Sem nova UI. O HUD de HP existente (`HudManagerOnFase.Instance.hpCount`) já é atualizado dentro de `PlayerStats.TakeDmg`, então o dano de esmagamento aparece automaticamente.

# Estado atual investigado (contexto factual)

- `Grid/FallPlataform` (Tilemap, InstanceID 83998): **apenas** `Tilemap` + `TilemapRenderer`, **sem collider/rigidbody**. 103 tiles ocupados, layer `Default`. cellSize (1,1). É a fonte dos clusters.
- `Grid/Ground` (InstanceID 84024): `TilemapCollider2D` + `CompositeCollider2D` + `Rigidbody2D` estático, **layer `Ground` (3)**.
- **Player** (tag `Player`, layer `Player` 6): `Rigidbody2D`, `BoxCollider2D`, `PlayerMovements`, `PlayerStats`, `PlayerAttack`, `RewindObj`, `InputReader`, `Animator`, `SpriteRenderer`.
  - `PlayerMovements.isGrounded` é **public** (leitura sem modificar player).
  - `PlayerMovements.groundMask` = somente layer `Ground` (3). **Por isso a plataforma desabável deve ficar na layer `Ground`** para que o jogador possa pisar nela (isGrounded) e ser empurrado.
  - `PlayerStats.TakeDmg(int dmg)` aplica dano, atualiza HUD e recarrega cena se HP<=0. `maxHp=1000`.
- **Não tocar** em `Assets/Scripts/Fases/FallPlataform.cs` (sistema da Fase 1, totalmente diferente).
- Não há `.asmdef` no projeto (tudo em `Assembly-CSharp`); uma pasta `Editor` é suficiente para o assembly de editor.
- Layers relevantes: 3 `Ground`, 6 `Player`. Player↔Ground colidem (player já anda no chão).

# Key Asset & Context

## Novos scripts
1. `Assets/Scripts/Fases/FallingTilePlatform.cs` — componente runtime em cada plataforma baked.
   - Nome **distinto** de `FallPlataform` (Fase 1) para evitar conflito.
   - Campos configuráveis no Inspector:
     ```csharp
     public enum FallMotion { ConstantSpeed, Gravity }

     [Header("Movimento de Queda")]
     public FallMotion motion = FallMotion.ConstantSpeed;
     public float fallSpeed = 6f;        // velocidade constante OU terminal (clamp) no modo Gravity
     public float gravityScale = 3f;     // usado só no modo Gravity (aceleração)
     public float fallDelay = 0.4f;      // atraso após o gatilho

     [Header("Gatilho: Passar por baixo (área de detecção)")]
     public bool triggerOnPassUnder = true;
     public Vector2 underBoxOffset = new Vector2(-2f, -1f); // relativo ao centro da plataforma (default: ponta esquerda, abaixo)
     public Vector2 underBoxSize   = new Vector2(2f, 1.5f); // dimensões da caixa de detecção

     [Header("Gatilho: Pisar em cima (área de detecção)")]
     public bool triggerOnStep = true;
     public Vector2 stepBoxOffset = new Vector2(0f, 0.75f); // acima do topo da plataforma
     public Vector2 stepBoxSize   = new Vector2(3f, 0.5f);

     [Header("Detecção (sem GC)")]
     public LayerMask playerMask;        // layer Player (para OverlapBox dos gatilhos e do esmagamento)
     public LayerMask groundLayer;       // para detectar pouso (default: Ground)

     [Header("Dano / Esmagamento")]
     public int crushDamage = 100;
     public Vector2 crushBoxOffsetExtra = Vector2.zero; // ajuste fino da caixa de esmagamento no fundo

     [Header("Pós-queda")]
     public float destroyDelayAfterLand = 0.1f;
     ```
   - Buffer non-alloc compartilhado (evita GC a cada sondagem):
     ```csharp
     private readonly Collider2D[] _overlapResults = new Collider2D[4];
     ```
   - Estado interno: `bool _triggered`, `bool _falling`, `bool _hasCrushed`, refs cacheadas (`Rigidbody2D`, `CompositeCollider2D` próprio para o `BoxCast` de pouso, `Transform`). As refs do player (`PlayerMovements`, `PlayerStats`) são obtidas do `Collider2D` detectado pelo OverlapBox (não precisa cachear antes).
   - Helper de detecção sem alocação:
     ```csharp
     // Sonda uma caixa em world-space e retorna o primeiro collider que casa com a mask. Sem GC.
     private Collider2D OverlapBox(Vector2 localOffset, Vector2 size, LayerMask mask)
     {
         Vector2 center = (Vector2)transform.position + localOffset;
         int count = Physics2D.OverlapBoxNonAlloc(center, size, 0f, _overlapResults, mask);
         for (int i = 0; i < count; i++)
             if (_overlapResults[i] != null) return _overlapResults[i];
         return null;
     }
     ```
   - Lógica:
     - `Awake/Start`: cachear refs; `rb.bodyType = Kinematic`; `rb.constraints = FreezePositionX | FreezeRotation` (só cai na vertical). Se `playerMask`/`groundLayer` não estiverem setados, resolver por nome (`Player`/`Ground`) como fallback.
     - **`FixedUpdate` — fase de gatilho (enquanto `!_triggered`):** chamar `CheckTriggers()`:
       - Se `triggerOnPassUnder` e `OverlapBox(underBoxOffset, underBoxSize, playerMask) != null` → `Trigger()`.
       - Se `triggerOnStep` e `OverlapBox(stepBoxOffset, stepBoxSize, playerMask) != null` → `Trigger()`.
       - (Ambos os gatilhos usam o **OverlapBox non-alloc** acima; nenhuma `OnCollision*`/`OnTrigger*` é usada.)
     - `Trigger()`: se já `_triggered` retorna; senão `_triggered=true` e inicia coroutine `FallRoutine()` (espera `fallDelay`, depois `_falling=true` e configura o corpo conforme `motion`).
     - **`FixedUpdate` — fase de queda (quando `_falling`):**
       - `ConstantSpeed`: `rb.MovePosition(rb.position + Vector2.down * fallSpeed * Time.fixedDeltaTime)` (kinematic empurra o player dinâmico ao tocar).
       - `Gravity`: `rb.bodyType=Dynamic; rb.gravityScale=gravityScale;` e clamp `if (rb.linearVelocity.y < -fallSpeed) rb.linearVelocity = new Vector2(rb.linearVelocity.x, -fallSpeed);`
       - **Esmagamento (dano) — também por OverlapBox non-alloc:** sonda uma caixa fina no fundo da plataforma (`playerMask`); se achar o player **e** o `PlayerMovements.isGrounded == true` (player prensado contra o Ground) e `!_hasCrushed` → obter `PlayerStats` do collider e `TakeDmg(crushDamage); _hasCrushed = true;`
       - **Detecção de pouso:** `Physics2D.BoxCast` para baixo a partir do fundo do collider, `groundLayer`, ignorando o próprio collider; se atingir o Ground dentro de um threshold → `Land()`.
     - **Empurrar o player:** resolvido pela física (kinematic via MovePosition empurra dinâmico; no modo Gravity o corpo dinâmico empurra naturalmente). Sem alterar o player.
     - `Land()`: para o movimento, `_falling=false`, `Destroy(gameObject, destroyDelayAfterLand)`.
   - `OnDrawGizmosSelected`: desenha as três caixas (passar-por-baixo, pisar-em-cima, esmagamento) em world-space para ajuste visual rápido das áreas de detecção no Inspector.

2. `Assets/Scripts/Fases/Editor/FallingPlatformBaker.cs` — ferramenta de Editor (não vai para o build).
   - `MenuItem("Tools/Fase2/Bake Falling Platforms")` (e opção "Clear Baked Platforms").
   - Passos:
     1. Localiza `Grid/FallPlataform` (Tilemap fonte) na cena ativa.
     2. **Flood fill (4-conectividade)** sobre `cellBounds` para achar clusters de tiles ocupados.
     3. Remove plataformas baked anteriores (por marcador/nome `FallingPlatform_*`) para re-bake idempotente.
     4. Para cada cluster cria `FallingPlatform_{i}` como filho do `Grid`:
        - `Tilemap` + `TilemapRenderer` (copia material, sortingLayer/order do fonte).
        - Copia, célula a célula, `GetTile`, `GetTransformMatrix`, `GetColor` do fonte para o novo Tilemap.
        - `TilemapCollider2D` (`usedByComposite=true`), `Rigidbody2D` (Kinematic, `simulated=true`), `CompositeCollider2D` (sólido → colisão com player; também usado pelo `BoxCast` de pouso).
        - `layer = Ground` (para `groundMask`/isGrounded do player funcionar).
        - Adiciona `FallingTilePlatform` e pré-preenche, a partir dos bounds do cluster: `underBoxOffset`/`underBoxSize` (caixa abaixo da ponta esquerda), `stepBoxOffset`/`stepBoxSize` (caixa acima do topo) e `crushBoxOffsetExtra`. Define `playerMask = Player` e `groundLayer = Ground`. **Nenhum** `BoxCollider2D` trigger é criado — a detecção é via OverlapBox non-alloc em runtime, configurável depois no Inspector.
     5. **Desabilita o `TilemapRenderer` do fonte** `Grid/FallPlataform` (mantém os dados de tiles para re-bake) para não duplicar o visual. (Alternativa documentada: limpar tiles do fonte.)
     6. Usa `Undo.RegisterCreatedObjectUndo` / `Undo.RecordObject` e marca a cena dirty (`EditorSceneManager.MarkSceneDirty`).
   - Observação mobile: como o bake é em editor, em runtime só existem colliders compostos por cluster (mínimo) e nenhum gerador roda no Start.

## Assets/objetos modificados
- Cena `Assets/Scenes/Fase2.unity`: novos GameObjects `FallingPlatform_*` sob `Grid`; `TilemapRenderer` do `FallPlataform` desativado. (Modificação feita pela ferramenta de bake — não manual.)
- **Player:** sem alterações de script (apenas leitura de `isGrounded` e chamada de `TakeDmg`). Caso o esmagamento exija um hook melhor, será discutido antes de tocar no player.

## Não alterar
- `Assets/Scripts/Fases/FallPlataform.cs` (Fase 1).
- `RewindObj.cs` / `RewindManager` (sem integração).

# Implementation Steps

### Step 1 — Criar `FallingTilePlatform.cs` (runtime)
- **Descrição:** Implementar o componente em `Assets/Scripts/Fases/FallingTilePlatform.cs` com os campos e a lógica de gatilhos via **OverlapBox non-alloc** (área de detecção configurável no Inspector), queda (2 modos), empurrão (física), dano de esmagamento (leitura de `PlayerMovements.isGrounded` + `PlayerStats.TakeDmg`), detecção de pouso e destruição.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** Sim (independente do Step 2, mas o Step 2 referencia o tipo)

### Step 2 — Criar a ferramenta de bake `FallingPlatformBaker.cs` (Editor)
- **Descrição:** Implementar em `Assets/Scripts/Fases/Editor/FallingPlatformBaker.cs` o flood-fill de clusters, criação dos GameObjects por cluster (Tilemap copiado + Composite collider na layer Ground), atribuição do `FallingTilePlatform` com a área de detecção pré-calculada a partir dos bounds do cluster, desativação do renderer fonte, suporte a re-bake/clear e Undo.
- **Assigned role:** developer
- **Dependencies:** Step 1 (usa o tipo `FallingTilePlatform`)
- **Parallelizable:** Não

### Step 3 — Executar o bake na Fase 2 e verificar a geração
- **Descrição:** Rodar `Tools/Fase2/Bake Falling Platforms` na cena `Fase2`. Conferir que cada cluster virou `FallingPlatform_*` com CompositeCollider na layer Ground, que as áreas de detecção (gizmos) ficaram na ponta esquerda inferior / acima do topo, e que o visual não está duplicado (renderer fonte desativado). Ajustar defaults/áreas se necessário.
- **Assigned role:** developer
- **Dependencies:** Steps 1, 2
- **Parallelizable:** Não

### Step 4 — Ajuste fino de parâmetros por plataforma
- **Descrição:** Configurar `motion`, `fallSpeed`, `gravityScale`, `fallDelay`, `crushDamage` e, principalmente, as **áreas de detecção** (`underBoxOffset`/`underBoxSize` e `stepBoxOffset`/`stepBoxSize`) no Inspector — usando os gizmos como referência — conforme o design de cada plataforma na Fase 2.
- **Assigned role:** developer
- **Dependencies:** Step 3
- **Parallelizable:** Sim (por plataforma)

# Verification & Testing

**Verificações de configuração (antes de jogar):**
- Cada `FallingPlatform_*` está na layer `Ground`, tem `Rigidbody2D` Kinematic e `CompositeCollider2D` sólido (sem `BoxCollider2D` trigger).
- `TilemapRenderer` do `Grid/FallPlataform` original está desativado (sem visual duplicado).
- `playerMask` = `Player` e `groundLayer` = `Ground` no componente.
- As caixas de detecção (gizmos) aparecem na posição correta: caixa de "passar por baixo" na ponta esquerda inferior e caixa de "pisar" acima do topo. Ajustáveis no Inspector.

**Testes manuais (Play Mode):**
1. **Gatilho B (pisar):** pular em cima de uma plataforma → após `fallDelay` ela cai. O jogador, em cima, desce junto.
2. **Gatilho A (passar por baixo na ponta esquerda):** andar por baixo, entrando pela ponta esquerda → após `fallDelay` ela cai.
3. **Empurrão:** ficar parado embaixo (sem chão imediato) e deixar a plataforma cair → o jogador é empurrado para baixo, **sem** dano.
4. **Esmagamento:** ficar entre a plataforma caindo e o `Ground` → `TakeDmg(crushDamage)` é chamado uma única vez (HP no HUD diminui exatamente `crushDamage`); confirmar que não repete.
5. **Modos de queda:** alternar `motion` entre `ConstantSpeed` (desce a `fallSpeed` constante) e `Gravity` (acelera até o clamp `fallSpeed`).
6. **Pós-queda:** plataforma é destruída ao atingir o `Ground` (ou sair de cena).
7. **Não-regressão Fase 1:** confirmar que a `FallPlataform` da Fase 1 continua intacta (script não alterado).
8. **Rewind:** ativar rewind não deve quebrar (plataforma é independente; aceitável que não retorne).

**Casos de borda:**
- Cluster de 1 tile (plataforma mínima): bake e queda funcionam.
- Dois gatilhos quase simultâneos: só dispara uma vez (`_triggered`).
- Re-bake: rodar a ferramenta novamente não duplica plataformas (idempotente).
- Player nunca recebe dano só por ser empurrado em queda livre (sem Ground por baixo).

**Mobile/performance:**
- Conferir no Profiler que a detecção dos gatilhos **não gera GC** (uso de `Physics2D.OverlapBoxNonAlloc` com `private readonly Collider2D[] _overlapResults` reutilizado).
- `FixedUpdate` faz apenas sondagens leves de OverlapBox enquanto `!_triggered`; após disparar, alterna para a lógica de queda. Nenhum trabalho em `Update`.
- Contagem de colliders: apenas 1 composite por plataforma (sem trigger extra).
- Manter o buffer `_overlapResults` pequeno (ex.: 4) — suficiente, pois só interessa achar o player.
