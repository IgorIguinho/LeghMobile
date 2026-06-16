# Project Overview
- **Game Title:** LeghMobile (projeto 2D plataforma)
- **High-Level Concept:** Jogo de plataforma 2D com mecânicas de movimento (pulo, dash, wall jump, corda) e desafios de cenário. Esta feature adiciona uma **plataforma móvel** que viaja entre dois pontos e carrega o player.
- **Players:** Single player.
- **Inspiration / Reference Games:** Plataformas móveis clássicas (Celeste, Super Mario, Hollow Knight).
- **Tone / Art Direction:** 2D (sprites + Tilemap).
- **Target Platform:** WebGL.
- **Screen Orientation / Resolution:** Landscape (padrão do projeto).
- **Render Pipeline:** Built-in.
- **Input System:** New Input System (não impacta esta feature — a plataforma é dirigida por física/transform).

# Game Mechanics

## Core Gameplay Loop
A **MovePlataform** desloca seu filho **Plataform** entre dois pontos do mundo (`PosInicial` ↔ `PosFinal`), em loop, com **suavização (ease-in/ease-out)** nas pontas e um **delay** configurável a cada parada. O player pode pisar em cima e ser carregado junto. A plataforma pode:
- começar a se mover automaticamente, **ou**
- começar parada e só iniciar quando o player pisar nela (e, uma vez iniciada, **continua em loop para sempre** — decisão confirmada).

### Decisões de design confirmadas com o usuário
1. **Carregar o player:** transferência de *delta* — a plataforma soma seu deslocamento horizontal à posição do `Rigidbody2D` do player; o eixo vertical é resolvido naturalmente pela colisão do corpo cinemático (evita "double push" / jitter).
2. **Modo "só move quando o player está em cima":** ao ser ativada pela primeira vez, **mantém o loop para sempre**.
3. **Tilemap:** criar um **Tilemap novo e vazio** (o usuário pinta depois no Tile Palette).
4. **Perfil de movimento:** **suavizado** (ease-in/ease-out) via `Mathf.SmoothStep`.

## Controls and Input Methods
Nenhum input novo. A interação é apenas "pisar em cima" (detecção por `OverlapBox` na layer `Player`).

# UI
Nenhuma UI nova. A configuração é feita 100% no Inspector do componente `MovePlataform` + ferramentas visuais de Gizmos na Scene View.

Layout do Inspector (campos serializados em `MovePlataform`):
```
[Referências]
  Plataform (Rigidbody2D)   -> filho que se move
  Pos Inicial (Transform)
  Pos Final   (Transform)

[Movimento]
  Start Mode  (Automatic | OnPlayerStep)
  Speed       (float, unidades/seg)
  Wait Delay  (float, seg de pausa em cada ponta)

[Detecção do Player]
  Player Mask (LayerMask, fallback "Player")
  Top Box Offset / Top Box Size (área em cima da plataforma)
```

# Key Asset & Context

## Contexto do projeto (já verificado)
- Player: layer **`Player`**, tag **`Player`**, `Rigidbody2D` **Dynamic**.
- `PlayerMovements.groundMask` = layer **`Ground`** (layer 3). **A `Plataform` precisa estar na layer `Ground`** para o player detectar como chão e poder pisar/pular nela.
- `PlayerMovements.Moviment()` sobrescreve `rb.linearVelocity.x` a cada `FixedUpdate` (vira 0 quando sem input) → por isso o carregamento horizontal **precisa** ser por transferência de delta, não por atrito do corpo cinemático.
- Convenções do projeto (ver `Assets/Scripts/Fases/FallingTilePlatform.cs`):
  - Plataformas usam `Rigidbody2D` **Kinematic** + `rb.MovePosition(...)` em `FixedUpdate`.
  - Detecção de player com `Physics2D.OverlapBoxNonAlloc` + buffer compartilhado (sem GC).
  - `LayerMask` com *fallback* por nome (`LayerMask.NameToLayer("Player")`).
  - Visualização com `OnDrawGizmosSelected`.
  - Comentários em **português**.

## Hierarquia de GameObjects a criar (na cena Fase4)
```
MovePlataform            (vazio) -> APENAS o script MovePlataform.cs + posição no mundo
├── Plataform            -> Grid + Tilemap + TilemapRenderer
│                           + TilemapCollider2D (Used By Composite = true)
│                           + CompositeCollider2D (Geometry: Outlines)
│                           + Rigidbody2D (Kinematic, Interpolate)
│                           layer = "Ground"
├── PosInicial           (vazio) -> só Transform (referência no script)
└── PosFinal             (vazio) -> só Transform (referência no script)
```
> Observação técnica: o componente `Tilemap` exige um `Grid` em si mesmo ou num pai. Vamos colocar `Grid` **no próprio `Plataform`** (Grid + Tilemap juntos no mesmo GameObject) para manter exatamente a estrutura pedida (MovePlataform → Plataform). O `Rigidbody2D` é obrigatório pelo `CompositeCollider2D` e fica em `Plataform` (faz parte da "colisão").
> `MovePlataform` permanece **estático** no mundo; quem se move é o `Rigidbody2D` do filho `Plataform` via `MovePosition` (em world-space). `PosInicial`/`PosFinal` ficam como filhos de `MovePlataform` apenas para organização (não se movem, pois o pai não se move).

## Novo script: `Assets/Scripts/Fases/MovePlataform.cs` (implementação de referência)
```csharp
using System.Collections;
using UnityEngine;

/// <summary>
/// Move o filho "Plataform" entre dois pontos (PosInicial <-> PosFinal) em loop,
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
    public Vector2 topBoxOffset = new Vector2(0f, 0.6f); // relativo ao centro do collider
    public Vector2 topBoxSize   = new Vector2(2f, 0.4f);

    // Estado interno
    private Vector2 _a, _b;          // origem e destino do trecho atual
    private float _t;                // tempo decorrido no trecho
    private bool _moving;            // esta se movendo?
    private bool _activated;         // ja foi ativado (modo OnPlayerStep)?
    private bool _waiting;           // esta no delay de parada?
    private Vector2 _prevPos;        // posicao anterior (para delta)

    private Collider2D _plataformCollider;
    private readonly Collider2D[] _overlap = new Collider2D[4];

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

        _a = posInicial != null ? (Vector2)posInicial.position : plataform.position;
        _b = posFinal   != null ? (Vector2)posFinal.position   : plataform.position;
        _t = 0f;
        _prevPos = plataform != null ? plataform.position : Vector2.zero;

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
        Vector2 center;
        if (_plataformCollider != null)
        {
            Bounds bnd = _plataformCollider.bounds;
            center = new Vector2(bnd.center.x, bnd.max.y) + new Vector2(topBoxOffset.x, topBoxOffset.y - 0.6f);
        }
        else
        {
            center = (Vector2)plataform.position + topBoxOffset;
        }

        int n = Physics2D.OverlapBoxNonAlloc(center, topBoxSize, 0f, _overlap, playerMask);
        for (int i = 0; i < n; i++)
        {
            if (_overlap[i] == null) continue;
            playerRb = _overlap[i].GetComponentInParent<Rigidbody2D>();
            return true;
        }
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        // Tamanho da plataforma (a partir do collider, com fallback)
        Vector2 size = Vector2.one;
        var col = plataform != null ? plataform.GetComponent<Collider2D>() : null;
        if (col != null) size = col.bounds.size;

        // Caminho (linha) entre as pontas
        if (posInicial != null && posFinal != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(posInicial.position, posFinal.position);
        }

        // PosInicial (verde) com o tamanho da plataforma
        if (posInicial != null)
        {
            Gizmos.color = new Color(0.3f, 1f, 0.3f, 0.5f);
            Gizmos.DrawWireCube(posInicial.position, size);
        }

        // PosFinal (vermelho) com o tamanho da plataforma
        if (posFinal != null)
        {
            Gizmos.color = new Color(1f, 0.3f, 0.3f, 0.5f);
            Gizmos.DrawWireCube(posFinal.position, size);
        }

        // Caixa de deteccao do player (amarelo)
        if (col != null)
        {
            Bounds b = col.bounds;
            Vector2 c = new Vector2(b.center.x, b.max.y) + new Vector2(topBoxOffset.x, topBoxOffset.y - 0.6f);
            Gizmos.color = new Color(1f, 1f, 0.2f, 0.6f);
            Gizmos.DrawWireCube(c, topBoxSize);
        }
    }
}
```
> Notas:
> - `waitDelay` é aplicado em **cada** ponta (ida e volta) para um loop consistente. Se o usuário quiser delay só no `PosFinal`, é uma alteração trivial (separar em dois campos).
> - O carregamento usa **apenas delta.x**; o vertical é resolvido pela colisão do corpo cinemático (evita arremessar o player). Confirmado como abordagem "transferência de delta".

# Implementation Steps

### Step 1 — Criar o script `MovePlataform.cs`
- **Description:** Criar `Assets/Scripts/Fases/MovePlataform.cs` com o conteúdo da seção "Key Asset & Context" (movimento ease-in/out com `SmoothStep`, ping-pong com `waitDelay`, modos `Automatic`/`OnPlayerStep`, carregamento por delta.x, gizmos).
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No (base das próximas etapas)

### Step 2 — Montar a hierarquia na cena Fase4
- **Description:** Criar GameObject vazio `MovePlataform`. Como filho, criar `Plataform` e adicionar `Grid` + `Tilemap` + `TilemapRenderer`. Criar dois GameObjects vazios `PosInicial` e `PosFinal` como filhos de `MovePlataform`, posicionados nos pontos desejados.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** Yes (independente do Step 1)

### Step 3 — Configurar colisão e física da `Plataform`
- **Description:** Em `Plataform`: setar layer = **`Ground`**; adicionar `TilemapCollider2D` (marcar **Used By Composite**), `CompositeCollider2D` (Geometry Type = Outlines), e `Rigidbody2D` (Body Type = **Kinematic**, Interpolate = **Interpolate**). O `CompositeCollider2D` adiciona o `Rigidbody2D` automaticamente — só ajustar para Kinematic.
- **Assigned role:** developer
- **Dependencies:** Step 2
- **Parallelizable:** No

### Step 4 — Ligar o script e referências
- **Description:** Adicionar `MovePlataform` ao GameObject `MovePlataform`. Atribuir: `Plataform` (Rigidbody2D) → campo `plataform`; `PosInicial` → `posInicial`; `PosFinal` → `posFinal`. Configurar `Start Mode`, `Speed`, `Wait Delay`. Verificar `Player Mask` (deve apontar para layer `Player`; há fallback automático). Ajustar `Top Box Offset/Size` para cobrir o topo da plataforma.
- **Assigned role:** developer
- **Dependencies:** Step 1, Step 3
- **Parallelizable:** No

### Step 5 — Pintar o Tilemap (manual, usuário)
- **Description:** O usuário abre Window → 2D → Tile Palette, seleciona o `Tilemap` da `Plataform` e pinta os tiles. Como `TilemapCollider2D` usa Composite, a colisão acompanha o desenho automaticamente.
- **Assigned role:** developer (orientação) / usuário (execução)
- **Dependencies:** Step 3
- **Parallelizable:** Yes

# Verification & Testing

## Checagens manuais (Play Mode)
1. **Modo Automatic:** a `Plataform` parte de `PosInicial`, vai até `PosFinal` com aceleração/desaceleração suave, espera o `waitDelay`, volta — em loop.
2. **Modo OnPlayerStep:** a `Plataform` fica parada; ao o player pisar, ela inicia e **continua em loop para sempre** mesmo depois do player sair.
3. **Carregar player:** com a plataforma se movendo na horizontal, o player parado em cima é levado junto (sem escorregar e sem ser arremessado). O player ainda pode andar/pular normalmente.
4. **Stand & jump:** o player consegue ficar em cima e pular (confirma que `Plataform` está na layer `Ground`).
5. **Velocidade/Delay:** alterar `Speed` e `Wait Delay` no Inspector reflete no movimento.

## Gizmos (Scene View, com `MovePlataform` selecionado)
6. Linha (ciano) ligando `PosInicial`→`PosFinal`.
7. Wireframe verde em `PosInicial` e vermelho em `PosFinal`, ambos com o **tamanho da Plataform** (lido do collider).
8. Caixa amarela de detecção sobre o topo da plataforma.

## Edge cases
- `PosInicial` == `PosFinal` (distância 0): não deve travar nem dividir por zero (tratado: `duration<=0` vai direto ao destino).
- `Speed = 0`: plataforma fica parada sem erro (tratado).
- Player sai no meio do trajeto (modo OnPlayerStep): movimento continua (loop eterno).
- Referências não atribuídas (`plataform` nulo): `FixedUpdate` retorna sem erro.

## Console
- Sem novos warnings/erros ao entrar em Play Mode. (Verificar especialmente warnings de física por reparenting — não devem ocorrer, pois NÃO reparentamos o player.)
