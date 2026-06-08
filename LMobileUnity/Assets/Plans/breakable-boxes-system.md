# Project Overview

- **Game Title:** LeghMobile (Fase2)
- **High-Level Concept:** Plataforma/ação 2D mobile onde o jogador explora fases, derrota inimigos com a espada e agora também quebra caixas espalhadas pelo cenário.
- **Players:** Single player
- **Inspiration / Reference Games:** Plataformas de ação 2D clássicas (objetos destrutíveis estilo Castlevania / Zelda).
- **Tone / Art Direction:** Pixel/tile art 2D (TileSet `Fase 0`).
- **Target Platform:** WebGL / dispositivos mobile (otimização é prioridade).
- **Screen Orientation / Resolution:** N/A (não alterado por este plano).
- **Render Pipeline:** Built-in.

> **Objetivo deste plano:** Criar um sistema de "caixas" quebráveis. Na `Fase2`, as caixas são representadas pelo Tilemap `Grid/Box`, onde tiles agrupados que se encostam formam uma "caixa" (um *cluster* de tiles conectados). Ao acertar uma caixa com o ataque do player, a caixa inteira (todo o cluster conectado) é removida em um único golpe.

---

# Game Mechanics

## Core Gameplay Loop
1. O jogador se move pela fase.
2. Ao encontrar uma caixa (cluster de tiles do Tilemap `Box`), ela funciona como **obstáculo sólido** que bloqueia a passagem.
3. O jogador usa o ataque de espada (já existente em `PlayerAttack.Attack()`).
4. Se a área de ataque sobrepõe uma caixa, **toda a caixa (cluster conectado de tiles) é destruída em um golpe**, liberando o caminho.

Sem vida (HP), sem loot, sem respawn, sem feedback de partículas/som nesta primeira versão.

## Controls and Input Methods
Nenhuma mudança de input. Reutiliza o fluxo atual:
`InputReader.AttackTriggered` → `PlayerAttack.OnAttackInput()` → `StartCoroutine(Attack())`.

---

# UI
Nenhuma alteração de UI necessária para este sistema.

---

# Estado Atual Investigado (Contexto)

- **`Grid/Box`** (Instance em `Fase2`): possui `Tilemap` + `TilemapRenderer`. **Não possui colisor** atualmente. Está na layer `Default`. Tem **114 tiles** (tiles simples `UnityEngine.Tilemaps.Tile`, ex. `TileSetFase0_3`).
- As "caixas" são **componentes conectados** (clusters) de tiles vizinhos dentro desse Tilemap.
- **`PlayerAttack.cs`** (`Assets/Scripts/Player/PlayerAttack.cs`):
  - `IEnumerator Attack()` faz `Physics2D.OverlapBoxAll(areaAttack.position, lengthAreaAttack, 0, enemyLayer)` e chama `EnemyStats.Death()` nos inimigos.
  - Campos relevantes: `Transform areaAttack`, `Vector2 lengthAreaAttack` (atualmente `2.05 x 1.0`), `LayerMask enemyLayer` (= Enemy / layer 7).
- **Layers existentes:** Default, TransparentFX, Ignore Raycast, Ground, Water, UI, Player, Enemy, Wall, Rope, Parallax. **Há slots livres** para adicionar a layer `Box`.
- Player está na layer `Player`.

---

# Abordagem Escolhida e Justificativa (Mobile)

**Abordagem: Tilemap único + Composite Collider + flood-fill em tempo de execução.**

Mantemos o Tilemap `Box` como está e adicionamos **um único** `CompositeCollider2D` que une todos os tiles em uma geometria de colisão eficiente. Ao receber um golpe, um script faz *flood-fill* (busca em largura/profundidade) a partir das células atingidas para encontrar o cluster inteiro e remover seus tiles.

Por que esta abordagem (vs. converter cada caixa em um GameObject/prefab separado):
- **Otimizada para mobile:** 1 colisor composto em vez de dezenas de colisores/GameObjects individuais → menos overhead de física e menos draw/GC.
- **Sem instanciar objetos** em runtime (sem loot/respawn nesta versão).
- O *flood-fill* roda apenas **quando uma caixa é realmente atingida** (evento raro), não a cada frame; o grid é pequeno (114 tiles), então o custo é desprezível.
- Reaproveita a estrutura de Tilemap já existente na cena.

Alternativa considerada e descartada (por agora): converter cada cluster em um prefab `BreakableBox` com `SpriteRenderer`+`BoxCollider2D`. Mais flexível para HP/loot/animação por caixa, porém gera muitos GameObjects/colisores e reconstrução de sprites a partir de tiles — desnecessário para o requisito atual (1 golpe, sólido, sem loot) e pior para mobile.

---

# Key Asset & Context

### Novos arquivos
- **`Assets/Scripts/Fases/Breakables/BreakableBoxTilemap.cs`** — `MonoBehaviour` a ser adicionado ao GameObject `Grid/Box`.
  - Responsabilidade: detectar quais células do Tilemap são atingidas pela área de ataque, fazer flood-fill do cluster conectado e remover seus tiles, atualizando o colisor.
  - API pública (assinatura proposta):
    ```csharp
    using UnityEngine;
    using UnityEngine.Tilemaps;

    [RequireComponent(typeof(Tilemap))]
    public class BreakableBoxTilemap : MonoBehaviour
    {
        [SerializeField] private bool includeDiagonals = false; // 4-neighbor por padrão

        private Tilemap _tilemap;
        private CompositeCollider2D _composite; // opcional, para forçar regenerar geometria

        // Retorna true se ao menos uma caixa (cluster) foi destruída.
        public bool TryBreakInArea(Vector2 worldCenter, Vector2 worldSize);
    }
    ```
  - Lógica interna:
    1. Converter `worldCenter ± worldSize/2` em um intervalo de células (`_tilemap.WorldToCell`).
    2. Para cada célula nesse intervalo que tenha tile e ainda não visitada → flood-fill (BFS com `Stack`/`Queue` reutilizados) pelos vizinhos (4-dir; 8-dir se `includeDiagonals`).
    3. Coletar todas as células do cluster e fazer `_tilemap.SetTile(pos, null)`.
    4. Marcar geometria do colisor para regenerar (o `TilemapCollider2D`/`CompositeCollider2D` atualiza automaticamente; opcionalmente forçar via composite quando necessário).
    5. Retornar `true` se removeu algo.
  - Notas de otimização: usar coleções reutilizadas como campos (`Stack<Vector3Int>` + `HashSet<Vector3Int>`) e dar `Clear()` por chamada para minimizar alocações; sair cedo se nenhuma célula com tile for atingida.

### Arquivos modificados
- **`Assets/Scripts/Player/PlayerAttack.cs`** — adicionar detecção de caixas (após os scripts da caixa estarem prontos).
  - Novo campo: `[SerializeField] private LayerMask boxLayer;`
  - Dentro de `Attack()`, **após** o tratamento dos inimigos, fazer uma segunda checagem contra `boxLayer`, obter `BreakableBoxTilemap` do colisor atingido e chamar `TryBreakInArea(areaAttack.position, lengthAreaAttack)`.
  - Otimização mobile: usar consulta sem alocação (`ContactFilter2D` + buffer `Collider2D[]` reutilizado / `Physics2D.OverlapBox` com filtro) em vez de `OverlapBoxAll`, e só executar se `boxLayer` estiver configurada.

### Configuração de cena / projeto
- **Nova Layer `Box`** (em um slot livre do TagManager).
- **`Grid/Box`** atribuído à layer `Box`, e recebe os componentes:
  - `TilemapCollider2D` (com `Used By Composite` / `compositeOperation = Merge`),
  - `Rigidbody2D` (Body Type = **Static**),
  - `CompositeCollider2D` (Geometry Type = Outlines),
  - `BreakableBoxTilemap`.
- **Matriz de Colisão (Physics2D):** garantir que `Player` colide com `Box` (sólido) — e, se desejado, `Enemy` x `Box`. Ajuste em Project Settings → Physics 2D → Layer Collision Matrix.
- **`PlayerAttack` na cena/prefab:** atribuir o campo `boxLayer` = `Box`.

---

# Implementation Steps

> Ordem solicitada: **primeiro os scripts da caixa, só depois mexer no `PlayerAttack`.**

### Step 1 — Criar o script `BreakableBoxTilemap.cs`
- **Description:** Criar `Assets/Scripts/Fases/Breakables/BreakableBoxTilemap.cs` com a classe e a API `TryBreakInArea`, incluindo o flood-fill (4-vizinhos por padrão, opção de diagonais), remoção dos tiles do cluster e coleções reutilizadas para baixo GC. Cache de `Tilemap`/`CompositeCollider2D` em `Awake`.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No

### Step 2 — Adicionar a Layer `Box` e configurar o GameObject `Grid/Box`
- **Description:** Criar a layer `Box` em um slot livre. Atribuir `Grid/Box` à layer `Box`. Adicionar `TilemapCollider2D` (Used By Composite), `Rigidbody2D` (Static), `CompositeCollider2D` e o componente `BreakableBoxTilemap` ao `Grid/Box`. Configurar a Layer Collision Matrix para `Player` x `Box` (sólido).
- **Assigned role:** developer
- **Dependencies:** Depends on Step 1 (o componente precisa existir para ser adicionado)
- **Parallelizable:** No

### Step 3 — Modificar `PlayerAttack.cs` para detectar e quebrar caixas
- **Description:** Adicionar o campo `boxLayer` (LayerMask) e, dentro de `Attack()` (após o loop de inimigos), executar a checagem de sobreposição contra `boxLayer` sem alocação, obter `BreakableBoxTilemap` do colisor atingido e chamar `TryBreakInArea(areaAttack.position, lengthAreaAttack)`. Manter o comportamento atual de ataque a inimigos intacto.
- **Assigned role:** developer
- **Dependencies:** Depends on Step 1 (precisa da API) e Step 2 (layer existente)
- **Parallelizable:** No

### Step 4 — Configurar referências no Inspector
- **Description:** No `PlayerAttack` (instância na cena `Fase2` e/ou prefab do Player), atribuir `boxLayer = Box`. Confirmar que `areaAttack`/`lengthAreaAttack` cobrem a área esperada das caixas.
- **Assigned role:** developer
- **Dependencies:** Depends on Step 3
- **Parallelizable:** No

---

# Verification & Testing

### Verificações manuais (Play Mode na `Fase2`)
1. **Sólido:** Andar contra uma caixa antes de atacar — o player deve ser bloqueado (não atravessa).
2. **Quebra em 1 golpe:** Atacar uma caixa — o cluster inteiro de tiles conectados deve sumir imediatamente, e a colisão sólida daquela caixa deve desaparecer (player consegue passar).
3. **Isolamento de clusters:** Caixas separadas (não encostadas) não devem quebrar juntas; apenas a(s) que a área de ataque tocou.
4. **Inimigos intactos:** Atacar inimigos continua matando-os normalmente (regressão do comportamento original).
5. **Ataque no vazio:** Atacar sem caixa por perto não gera erros nem custo perceptível.

### Casos de borda
- Atacar na borda entre duas caixas distintas próximas → cada cluster é avaliado corretamente (apenas o(s) tocado(s) some(m)).
- Atacar a mesma área duas vezes → segunda vez não encontra tiles e retorna sem efeito/erros.
- Tilemap com tiles diagonais: validar comportamento de `includeDiagonals` (default 4-vizinhos).

### Otimização / Mobile
- Confirmar (via Profiler) que o ataque sem caixa **não gera alocações por frame** (uso de buffer/`ContactFilter2D`).
- Confirmar que existe **um único** `CompositeCollider2D` para o Tilemap `Box` (sem colisores por tile).

### Console
- Sem erros/warnings novos no Console após entrar em Play Mode e quebrar caixas.
