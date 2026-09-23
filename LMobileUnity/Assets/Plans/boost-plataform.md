# Project Overview
- **Game Title:** LeghMobile
- **High-Level Concept:** Jogo de plataforma 2D mobile focado em agilidade, precisão e física interativa. Esta mecânica implementa uma plataforma móvel horizontal que se desloca a uma velocidade base constante e, ao receber aterrissagens do jogador, ganha um impulso de avanço rápido temporário (boost) com desaceleração suave, respeitando limites horizontais definidos e carregando o jogador com precisão.
- **Players:** Single player.
- **Inspiration / Reference Games:** Mecânicas de plataformas de propulsão e ritmo (Celeste, Super Mario Galaxy, Rayman Legends).
- **Tone / Art Direction:** 2D pixel art / sprites com Tilemaps integrados ao URP2D.
- **Target Platform:** Android (foco total em baixo consumo de CPU e zero alocação de GC no loop de física).
- **Screen Orientation / Resolution:** Landscape (padrão mobile do projeto).
- **Render Pipeline:** URP2D.

# Game Mechanics

## Core Gameplay Loop
A nova plataforma (`BoostPlataform`) opera em ciclos interativos que desafiam o timing e o posicionamento do jogador:
1. **Movimento Base:** A plataforma se desloca no eixo X com velocidade constante (`baseSpeed`). O sinal determina o sentido (+ para a direita, - para a esquerda).
2. **Gatilho de Avanço (Boost On Land):** Quando o Player pula e aterrissa sobre a plataforma (detectado via trigger/área superior no contato de entrada), um impulso de avanço rápido (`boostVelocity`) é ativado.
3. **Bloqueio Temporário:** Enquanto a velocidade de avanço estiver desacelerando e for diferente de zero, o movimento base permanece pausado.
4. **Desaceleração Suave:** A velocidade do avanço decai matematicamente a uma taxa ajustável (`boostDeceleration`) até chegar a zero. Ao zerar, a plataforma volta a se mover com a velocidade base normal descrita no passo 1.
5. **Reativação a cada Aterrissagem:** Cada vez que o jogador salta e volta a cair sobre a plataforma (desde que o impulso anterior já tenha terminado), um novo avanço é disparado.
6. **Limites de Percurso (Min X / Max X):** Dois limites no eixo horizontal impedem a plataforma de se mover indefinidamente. Ao encostar em um limite, a movimentação naquele sentido é travada; a plataforma permanece no limite até que uma velocidade/impulso no sentido oposto a leve de volta para a área válida.
7. **Carregamento do Player:** O Player em cima da plataforma é transportado fielmente via deslocamento delta horizontal no `FixedUpdate`, prevenindo deslizamentos indesejados ou conflitos com o controle do Player.

## Controls and Input Methods
O jogador não precisa de novos botões. A interação é inteiramente orientada a física e timing:
- **Movimento e Salto:** Controles existentes do jogador (toque na tela / botões virtuais de movimentação e pulo).
- **Feedback Tátil e Visual:** A aterrissagem sobre a plataforma é convertida diretamente em aceleração dinâmica da própria plataforma, proporcionando sensação imediata de peso e resposta física (Game Feel).

# UI
Nenhuma nova interface de usuário em tela (HUD) é necessária. Toda a configuração é visual e ergonômica dentro do Unity Editor:

### Layout do Inspector (`BoostPlataform`)
```
[Referências]
  Plataform (Rigidbody2D)          -> Filho com física/tilemap
  Limit Min (Transform)            -> Marcador visual do limite à esquerda (opcional/fallback: float)
  Limit Max (Transform)            -> Marcador visual do limite à direita (opcional/fallback: float)

[Movimento Base]
  Base Speed (float)               -> Velocidade base (+ direita, - esquerda)

[Avanço / Boost]
  Boost Velocity (float)           -> Velocidade inicial do avanço (+ direita, - esquerda)
  Boost Deceleration (float)       -> Taxa de frenagem/desaceleração do avanço (unidades/s²)
  Boost Cooldown (float)           -> Intervalo mínimo de segurança entre disparos de avanço (ex: 0.1s)

[Limites Horizontais (World X)]
  Use Transform Limits (bool)      -> Se true, usa as posições X dos Transforms LimitMin/LimitMax
  Min Limit X (float)              -> Limite mínimo manual caso não use Transform
  Max Limit X (float)              -> Limite máximo manual caso não use Transform

[Detecção do Player]
  Player Mask (LayerMask)          -> Layer "Player"
  Top Box Offset (Vector2)         -> Posição relativa da caixa no topo da plataforma
  Top Box Size (Vector2)           -> Dimensões da área de contato/aterrissagem

[Gizmos & Debug]
  Show Gizmos (bool)               -> Ativa visualização de trajetória, avanço e limites
```

# Key Asset & Context

### Estrutura e Convenções Técnicas
- **Física Kinematic + Delta:** O Rigidbody2D da plataforma será `Kinematic` com `Interpolate`. A movimentação é calculada em `FixedUpdate` usando `rb.MovePosition()`. O Player (`Dynamic`) é transportado somando `delta.x` diretamente a `playerRb.position`, exatamente como já testado e validado em `Assets/Scripts/Fases/MovePlataform.cs`. Isso elimina qualquer instabilidade de física mista no mobile.
- **Detecção sem Alocação (GC-Free):** Usa `Physics2D.OverlapBoxNonAlloc` com buffer cacheado para identificar quando o Player entra na área de contato superior (trigger de aterrissagem).
- **Cálculo Físico do Avanço (Gizmo Predictivo):**
  - Distância total percorrida pelo impulso de avanço até a parada:
    $$d_{boost} = \frac{v_{boost}^2}{2 \cdot a_{decel}} \cdot \operatorname{sign}(v_{boost})$$
  - Isso permite desenhar na Scene View uma seta/trajetória exata mostrando onde a plataforma terminará o avanço a partir de qualquer ponto.

### Hierarquia do Prefab `BoostPlataform.prefab`
```
BoostPlataform                   (Transform, BoostPlataform.cs)
├── Plataform                    (Grid, Tilemap, TilemapRenderer, TilemapCollider2D [Used By Composite], CompositeCollider2D, Rigidbody2D [Kinematic, Interpolate], Layer: Ground)
├── LimitMin                     (Transform vazio - marcador de limite esquerdo)
└── LimitMax                     (Transform vazio - marcador de limite direito)
```

### Arquivos Envolvidos
- **Novo Script:** `Assets/Scripts/Fases/BoostPlataform.cs`
- **Novo Prefab:** `Assets/Prefabs/GameObj/TilePrefabs/BoostPlataform.prefab`
- **Cena de Teste:** `Assets/Scenes/Fase10.unity` (cena aberta atualmente no projeto)

# Implementation Steps

### Step 1 — Criar o script `BoostPlataform.cs`
- **Description:** Criar o script em `Assets/Scripts/Fases/BoostPlataform.cs` contendo a lógica completa:
  - Movimentação base contínua em X com direção por sinal (`baseSpeed`).
  - Detecção de aterrissagem do Player por área no topo (edge-triggered: detecta transição de ar para chão na plataforma).
  - Estado de avanço rápido (`_currentBoostSpeed = boostVelocity`), bloqueando o movimento base enquanto `_currentBoostSpeed != 0`.
  - Desaceleração a cada frame de física com `Mathf.MoveTowards(_currentBoostSpeed, 0f, boostDeceleration * Time.fixedDeltaTime)`.
  - Clamping estrito entre `minLimitX` e `maxLimitX`. Se encostar em um limite, bloqueia o movimento que continuaria para além dele, mas permite sair se a velocidade apontar para a direção oposta.
  - Carregamento do jogador via `delta.x` adicionado a `playerRb.position`.
  - `OnDrawGizmos`: desenha a linha de percurso entre os limites, caixas de contorno nos limites mín/máx com o tamanho da plataforma, vetor/seta indicando a projeção do avanço e a caixa de detecção do Player.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No

### Step 2 — Montar o Prefab `BoostPlataform.prefab`
- **Description:** Criar a estrutura GameObject `BoostPlataform` com os filhos `Plataform` (Tilemap + CompositeCollider2D + Rigidbody2D Kinematic na layer `Ground`), `LimitMin` e `LimitMax`. Conectar o script `BoostPlataform.cs`, associando as referências e ajustando parâmetros iniciais de velocidade, avanço, desaceleração e tamanho da caixa de detecção. Salvar como Prefab em `Assets/Prefabs/GameObj/TilePrefabs/BoostPlataform.prefab`.
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** No

### Step 3 — Instanciar e Testar na Cena `Fase10`
- **Description:** Adicionar uma instância de `BoostPlataform` na cena ativa `Assets/Scenes/Fase10.unity` em um local acessível para teste prático com o Player já existente na cena. Posicionar os limites `LimitMin` e `LimitMax` em posições visíveis e calibrar valores iniciais equilibrados (ex: `baseSpeed = 2f`, `boostVelocity = 8f`, `boostDeceleration = 12f`).
- **Assigned role:** developer
- **Dependencies:** Step 2
- **Parallelizable:** No

# Verification & Testing

### Testes Manuais no Editor e Play Mode
1. **Movimentação Base e Sinal:**
   - Com `baseSpeed > 0`, a plataforma deve se mover constantemente para a direita.
   - Com `baseSpeed < 0`, deve se mover para a esquerda.
2. **Gatilho de Avanço (Aterrissagem):**
   - O Player pula e cai sobre a plataforma: ela deve disparar o avanço rápido na direção de `boostVelocity`.
   - Durante o avanço, a movimentação base fica suspensa.
   - Ao desacelerar até 0, o movimento base deve retomar imediatamente.
3. **Reativação Sucessiva:**
   - Saltar na plataforma repetidas vezes: a cada nova aterrissagem (após o término do impulso anterior), a plataforma deve impulsionar novamente.
4. **Respeito aos Limites:**
   - Ao alcançar `LimitMax` ou `LimitMin`, a plataforma deve travar exatamente na posição de borda e não ultrapassá-la.
   - Se estiver travada no limite direito e o Player pular com avanço apontado para a esquerda (ou velocidade base invertida), ela deve sair do limite normalmente.
5. **Carregamento do Jogador:**
   - Enquanto o jogador permanecer em cima da plataforma (parado ou andando), deve ser transportado horizontalmente sem escorregar, tremer ou ser arremessado.
   - O jogador deve conseguir pular normalmente a partir da plataforma em movimento (garantindo que o chão seja reconhecido na layer `Ground`).
6. **Gizmos na Scene View:**
   - Conferir se a linha entre os limites, os retângulos de parada e a projeção do avanço aparecem nítidos na Scene View para facilitar o level design.
7. **Performance & Console:**
   - Verificar ausência de mensagens de erro, warnings de física ou alocação desnecessária no Unity Console.
