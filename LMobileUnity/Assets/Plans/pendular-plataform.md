# Project Overview

- **Game Title:** LeghMobile (projeto existente)
- **High-Level Concept:** Plataforma 2D. Este plano adiciona um novo tipo de plataforma móvel com movimento **pendular** (balanço), inspirado no sistema existente `MovePlataform`.
- **Players:** Single player (gameplay de plataforma 2D)
- **Inspiration / Reference Games:** Plataformas oscilantes clássicas (ex.: balanços/pêndulos em jogos como Super Mario, Celeste)
- **Tone / Art Direction:** Mantém o estilo do projeto (Tilemap 2D)
- **Target Platform:** WebGL
- **Screen Orientation / Resolution:** Conforme o projeto (2D)
- **Render Pipeline:** Built-in

---

# Game Mechanics

## Core Gameplay Loop
O `PendularPlataform` é um novo sistema, espelhado em `MovePlataform` (`Assets/Scripts/Fases/MovePlataform.cs`), com a diferença de que o filho `Plataform` se move ao longo de um **arco circular** (como um balanço preso a um pivô), em vez de uma linha reta.

- Um GameObject vazio **`PendularPlataform`** carrega apenas o script de movimentação e marca a posição no mundo.
- O filho **`Plataform`** é onde o player pisa: tem colisão + Tilemap (pintável via Tile Palette).
- Três Transforms auxiliares definem o movimento:
  - **`PosPivo`** → centro do balanço (o "prego" onde o pêndulo fica preso).
  - **`PosInicial`** → onde a `Plataform` começa.
  - **`PosFinal`** → para onde a `Plataform` vai.
- A `Plataform` desliza pelo arco entre `PosInicial` e `PosFinal`, **permanecendo sempre na horizontal** (sem inclinar).
- Ao chegar em `PosFinal`, espera `waitDelay`, inverte o sentido e volta até `PosInicial`, em loop (ping-pong).
- `StartMode` permite escolher: começar automaticamente (`Automatic`) ou só quando o player pisar em cima (`OnPlayerStep`).
- Campos ajustáveis: `speed` (velocidade ao longo do arco) e `waitDelay` (parada em cada ponta).
- O player em cima é carregado tanto em **X quanto em Y**, para acompanhar a curva do arco.

### Geometria do movimento pendular
Dado o pivô `P`, o início `S` e o fim `E`:
- `vS = S - P`, `vE = E - P`
- ângulos: `angS = Atan2(vS.y, vS.x)`, `angE = Atan2(vE.y, vE.x)` (em graus)
- raios: `radS = |vS|`, `radE = |vE|`
- Durante o trecho A→B: `eased = SmoothStep(0,1, t/duration)`
  - `ang = Mathf.LerpAngle(angA, angB, eased)` (sempre o arco mais curto)
  - `rad = Mathf.Lerp(radA, radB, eased)` (passa exatamente por ambos os pontos mesmo se os raios diferirem)
  - `pos = P + rad * (cos(ang), sin(ang))`
- `duration` baseado no comprimento do arco: `arcLen ≈ ((radA+radB)/2) * |deltaAngleRad|`, `duration = arcLen / speed`.
- Ao chegar no destino: aguarda `waitDelay`, troca (`A↔B`, ângulos e raios), reseta `t`.

## Controls and Input Methods
Nenhum input novo — o sistema é reativo (detecta o player em cima via `Physics2D.OverlapBox` na `playerMask`, igual ao `MovePlataform`).

---

# UI
Sem alterações de UI. A única interface visual é o **Gizmo de editor** (`OnDrawGizmosSelected`):
- Linha **curva** desenhada amostrando o arco entre `PosInicial` e `PosFinal` (vários segmentos seguindo o ângulo+raio interpolados).
- **WireCube** em `PosInicial` e `PosFinal` com o **tamanho da `Plataform`** (lido do `Tilemap.cellBounds` / `Collider2D.bounds`).
- Marcador no `PosPivo` (ponto/esfera) e linhas finas do pivô até cada ponta (raios do balanço).
- Caixa amarela de detecção do player no topo da `Plataform`.

---

# Key Asset & Context

### Arquivos a criar
- `Assets/Scripts/Fases/PendularPlataform.cs` — novo MonoBehaviour (espelhado em `MovePlataform`).

### Arquivo de referência (não modificar)
- `Assets/Scripts/Fases/MovePlataform.cs` — base do padrão (Rigidbody2D Kinematic, `MovePosition`, `IsPlayerOnTop`, `StartMode`, gizmos com tamanho do Tilemap).

### Hierarquia de cena alvo (criada manualmente no Editor pelo usuário/dev)
```
PendularPlataform        (GameObject vazio)  -> script PendularPlataform
├── Plataform            -> Rigidbody2D (Kinematic), Collider2D, Tilemap + TilemapRenderer
├── PosPivo              -> Transform vazio (centro do balanço)
├── PosInicial           -> Transform vazio (ponto de partida)
└── PosFinal             -> Transform vazio (ponto de chegada)
```

### Assinatura/campos públicos previstos
```csharp
public enum StartMode { Automatic, OnPlayerStep }

[Header("Referencias")]
public Rigidbody2D plataform;   // filho Plataform
public Transform posPivo;       // centro do balanco
public Transform posInicial;
public Transform posFinal;

[Header("Movimento")]
public StartMode startMode = StartMode.Automatic;
public float speed = 3f;        // unidades/segundo ao longo do arco
public float waitDelay = 1f;    // parada em cada ponta

[Header("Deteccao do Player (em cima)")]
public LayerMask playerMask;
public Vector2 topBoxOffset = new Vector2(0f, 0.6f);
public Vector2 topBoxSize   = new Vector2(2f, 0.4f);

[Header("Gizmos")]
public int arcGizmoSegments = 24; // resolucao da linha curva
```

### Trecho central de movimento (FixedUpdate)
```csharp
// _angA, _angB (graus), _radA, _radB definidos a partir de posPivo/posInicial/posFinal
float deltaAng = Mathf.DeltaAngle(_angA, _angB) * Mathf.Deg2Rad;
float arcLen   = ((_radA + _radB) * 0.5f) * Mathf.Abs(deltaAng);
float duration = (speed > 1e-4f && arcLen > 1e-4f) ? arcLen / speed : 0f;

_t += Time.fixedDeltaTime;
float eased = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(duration > 0 ? _t / duration : 1f));
float ang = Mathf.LerpAngle(_angA, _angB, eased) * Mathf.Deg2Rad;
float rad = Mathf.Lerp(_radA, _radB, eased);
Vector2 pivot = posPivo.position;
Vector2 newPos = pivot + new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * rad;

plataform.MovePosition(newPos);

// Carrega player em X E Y (escolha do usuario)
Vector2 delta = newPos - _prevPos;
if (delta.sqrMagnitude > 0f && IsPlayerOnTop(out var rb) && rb != null)
    rb.position += delta;

_prevPos = newPos;
if (_t >= duration) StartCoroutine(ArriveAndSwap());
```

---

# Implementation Steps

### Step 1 — Criar o script `PendularPlataform.cs`
- **Description:** Criar `Assets/Scripts/Fases/PendularPlataform.cs` copiando a estrutura de `MovePlataform` e adaptando:
  - Adicionar campo `posPivo`.
  - No `Awake`: configurar `plataform.bodyType = Kinematic`, `interpolation = Interpolate`, cachear `Collider2D` e o fallback de `playerMask` ("Player").
  - No `Start`: posicionar `plataform` em `posInicial`; calcular `_angInicial/_angFinal` (graus, via `Atan2`) e `_radInicial/_radFinal` a partir de `posPivo`; inicializar trecho atual `_angA=_angInicial, _angB=_angFinal` (e raios); `_moving = (startMode == Automatic)`.
  - No `FixedUpdate`: lógica do arco (trecho central acima), com detecção `OnPlayerStep` igual ao original, respeito a `_waiting`, e carga do player em X+Y.
  - `ArriveAndSwap`: `WaitForSeconds(waitDelay)`, trocar `_angA↔_angB` e `_radA↔_radB`, resetar `_t`.
  - `IsPlayerOnTop` / `GetTopBoxCenter`: idênticos ao `MovePlataform`.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No

### Step 2 — Implementar `OnDrawGizmosSelected` com arco curvo
- **Description:** No mesmo script:
  - Ler `size` da `Plataform` via `Tilemap.cellBounds.size` (com `CompressBounds()`), fallback para `Collider2D.bounds.size` ou `Vector3.one` — **com guardas de null** (corrigir o bug do `MovePlataform` que faz `col.CompressBounds()` antes do null-check).
  - Desenhar a **linha curva**: se `posPivo`, `posInicial` e `posFinal` existirem, amostrar `arcGizmoSegments` pontos interpolando ângulo (`LerpAngle`) + raio (`Lerp`) e `Gizmos.DrawLine` entre pontos consecutivos. Fallback para linha reta se faltar o pivô.
  - `DrawWireCube` em `posInicial` e `posFinal` com `size`.
  - Marcar `posPivo` (`DrawSphere` pequeno) e desenhar raios finos pivô→pontas.
  - Caixa amarela de detecção no topo (igual original).
- **Assigned role:** developer
- **Dependencies:** Depends on Step 1
- **Parallelizable:** No

### Step 3 — Montar a hierarquia de cena (manual no Editor)
- **Description:** Instruções para o usuário/dev montar na fase desejada (ex.: `Fase4.unity`):
  1. Criar GameObject vazio `PendularPlataform` e adicionar o script.
  2. Criar filho `Plataform`: adicionar `Rigidbody2D` (Body Type Kinematic), um `Collider2D` (ex.: `BoxCollider2D` ou `TilemapCollider2D`), e componentes `Tilemap` + `TilemapRenderer`. Pintar via Tile Palette.
  3. Criar filhos vazios `PosPivo`, `PosInicial`, `PosFinal` e posicioná-los (pivô acima; início e fim equidistantes do pivô para um arco limpo).
  4. Arrastar as referências no Inspector do `PendularPlataform` (`plataform`, `posPivo`, `posInicial`, `posFinal`).
  5. Ajustar `startMode`, `speed`, `waitDelay`, `playerMask`.
- **Assigned role:** developer (orientação) / usuário (montagem)
- **Dependencies:** Depends on Step 1, Step 2
- **Parallelizable:** No

---

# Verification & Testing

- **Compilação:** Sem erros no Console após criar o script.
- **Gizmo (Editor):** Ao selecionar o `PendularPlataform`, a linha curva do arco aparece entre `PosInicial` e `PosFinal`, com WireCubes do tamanho da `Plataform` nas pontas e marcador no `PosPivo`.
- **Modo Automatic:** Em Play, a `Plataform` percorre o arco de `PosInicial` a `PosFinal`, espera `waitDelay`, volta, em loop. Permanece horizontal o tempo todo.
- **Modo OnPlayerStep:** A plataforma só inicia o balanço quando o player pisa em cima.
- **Carga do player:** Com o player em cima, ele acompanha o movimento em X e Y sem cair (verificar pequenos jitters ao descer o arco; ajustar `topBoxSize`/`topBoxOffset` se necessário).
- **Parâmetros:** Alterar `speed` muda a velocidade ao longo do arco; alterar `waitDelay` muda o tempo de parada nas pontas.
- **Edge cases:** referências nulas não devem causar exceção; pivô coincidente com uma ponta (raio 0) tratado com guardas; `speed` muito baixa não trava o jogo.
