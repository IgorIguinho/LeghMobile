# Project Overview
- **Game Title:** LeghMobile
- **High-Level Concept:** Jogo de plataforma 2D mobile com desafios de precisão e fases dinâmicas. Esta feature implementa um sistema de **Auto-Scrolling** horizontal para a câmera, onde a borda da tela empurra o jogador que ficar para trás e causa **morte por esmagamento** se o jogador for prensado contra paredes ou obstáculos do cenário.
- **Players:** Single player.
- **Inspiration / Reference Games:** Fases clássicas de auto-scrolling em Super Mario Bros 3 / World, Celeste, Rayman Origins.
- **Tone / Art Direction:** 2D (Pixel art / Tilemaps do projeto LeghMobile).
- **Target Platform:** Android (Mobile).
- **Screen Orientation / Resolution:** Landscape (adaptativo para resoluções mobile 16:9 a 20:9).
- **Render Pipeline:** URP2D (Universal Render Pipeline 2D).
- **Input System:** New Input System (o jogador usa os controles existentes; o auto-scrolling opera de forma autônoma na câmera).

# Game Mechanics

## Core Gameplay Loop
O jogador precisa navegar pela fase mantendo o ritmo de avanço imposto pela câmera:
1. **Deslocamento Contínuo:** A câmera se desloca ao longo do eixo horizontal (X) com velocidade constante e direção configurável (Direita ou Esquerda).
2. **Empurrão na Borda (Push):** Quando o jogador fica parado ou se atrasa, a borda traseira da tela o alcança. Uma barreira física cinemática ajustada à viewport empurra o jogador suavemente na direção do movimento da câmera, evitando trepidação (jitter) e impedindo que o player saia da tela.
3. **Esmagamento Fatal (Crush Death):** Se o jogador estiver sendo empurrado pela borda da câmera e colidir contra um obstáculo intransponível (Tilemap de `Ground`, `Wall`, caixas sólidas), o espaço físico se esgota. O sistema detecta a compressão do colisor do jogador e aciona a morte instantânea por esmagamento (`Kill()`), recarregando a fase.
4. **Comportamento Vertical Híbrido:** O designer pode optar por travar a câmera no eixo Y ou permitir que ela acompanhe o jogador suavemente (`followPlayerY`) respeitando limites `MinY`/`MaxY`.

## Controls and Input Methods
- Nenhum novo input é adicionado para o jogador.
- O jogador utiliza os controles habituais (Move, Jump, Dash) processados por `PlayerMovements` e `InputReader`.
- A pressão temporal e espacial exercida pelo auto-scroller intensifica o desafio de movimentação e reflexos sem adicionar sobrecarga cognitiva aos controles de toque mobile.

# UI
Não há elementos visuais de UI em jogo dedicados a esta mecânica. Toda a configuração é feita no Inspector do componente `AutoScrolling` e visualizada na Scene View via Gizmos.

### Layout do Inspector (`AutoScrolling.cs`):
```
[Header("Configuração de Movimento")]
  Direction          (Enum: RightToLeft | LeftToRight)
  Scroll Speed       (float: velocidade em unidades/s)
  Is Scrolling       (bool: permite pausar/iniciar via eventos ou gatilhos)
  Use Limit X        (bool: limitar avanço horizontal)
  Min X / Max X      (float: limites mínimo e máximo em X)

[Header("Comportamento no Eixo Y (Híbrido)")]
  Follow Player Y    (bool: seguir jogador verticalmente)
  Lerp Speed Y       (float: suavidade do acompanhamento em Y)
  Min Y / Max Y      (float: limites verticais da câmera)

[Header("Barreira de Empurrão (Push Barrier)")]
  Barrier Thickness  (float: espessura do colisor da barreira)
  Extra Height       (float: altura extra para evitar que o player pule por cima)
  Player Mask        (LayerMask: padrão layer "Player")

[Header("Deteccao de Esmagamento (Crush)")]
  Obstacle Mask      (LayerMask: Ground, Wall, Caixa, SingleBox)
  Crush Check Distance (float: distância mínima para detectar prensagem)
  Penetration Threshold (float: tolerância de penetração antes do crush fatal)

[Header("Debug & Gizmos")]
  Show Gizmos        (bool: desenha a barreira e área de detecção no Scene View)
```

# Key Asset & Context

## Contexto do Projeto (Verificado)
- **Câmera:** Câmera ortográfica 2D principal (`Camera.main`, `orthographicSize = 5`). O script atual `FollowCam.cs` é usado em fases convencionais e opera em `FixedUpdate`.
- **Player:** Tag `"Player"`, Layer `"Player"` (layer 6), `Rigidbody2D` Dynamic com colisão `Discrete`.
- **Movimentação do Player (`PlayerMovements.cs`):**
  - Em `FixedUpdate`, `PlayerMovements.Moviment()` atribui explicitamente `rb.linearVelocity.x`. Logo, a barreira física precisa atuar via colisor cinemático (`Rigidbody2D.MovePosition`) para que a resolução de contato de Box2D mova o player sem conflitos de velocidade.
- **Layers de Obstáculos:**
  - `Ground` (layer 3)
  - `Wall` (layer 8)
  - `Box` (layer 11)
  - `SingleBox` (layer 14)
  - `Caixa` (layer 17)
- **Vida e Morte (`PlayerStats.cs`):**
  - Atualmente possui `TakeDmg(int dmg)`, mas `TakeDmg` ignora dano quando `isInvulnerable == true` (i-frames). Para esmagamento, o jogador deve morrer imediatamente mesmo se estiver sob efeito de i-frames. Será adicionado o método público `Kill()` em `PlayerStats.cs`.
- **Script Antigo:** `Assets/Scripts/Fases/AutoScrollingCam.cs` é um protótipo inicial sem referências em cenas, que será preservado ou substituído pelo novo `AutoScrolling.cs`.

## Otimizações para Mobile (Android)
1. **Zero Garbage Collection (GC) no loop de física:**
   - Uso de `Physics2D.BoxCastNonAlloc` com buffers pré-alocados de tamanho fixo (`RaycastHit2D[4]`).
   - Sem alocações de `new Vector3`, arrays ou strings dentro de `Update` ou `FixedUpdate`.
2. **Sincronização Física no `FixedUpdate`:**
   - Movimentação da câmera e da barreira executada estritamente em `FixedUpdate` usando `rb.MovePosition`, mantendo coerência absoluta com a simulação do `Rigidbody2D` do Player.
3. **Adaptação Dinâmica de Viewport:**
   - O cálculo da borda da tela utiliza `cam.orthographicSize` e `cam.aspect`, garantindo posicionamento milimétrico em qualquer formato de tela (16:9, 18:9, 19.5:9, 20:9).
4. **Cache Completo de Referências:**
   - Acesso a `Camera`, `Transform`, `PlayerStats`, `Collider2D` e `Rigidbody2D` cacheados em `Awake`/`Start`, sem chamadas a `Camera.main` ou `FindWithTag` por frame.

# Implementation Steps

- **Step 1: Adicionar suporte a morte instantânea em `PlayerStats.cs`**
  - **Description:** Adicionar método público `public void Kill()` em `Assets/Scripts/Player/PlayerStats.cs` para garantir que mortes catastróficas (como esmagamento e quedas fatais) eliminem o jogador instantaneamente, cancelando qualquer corrotina de invulnerabilidade ativa e recarregando a cena imediatamente.
  - **Assigned role:** developer
  - **Dependencies:** None
  - **Parallelizable:** Yes

- **Step 2: Criar o script principal `AutoScrolling.cs`**
  - **Description:** Criar `Assets/Scripts/Fases/AutoScrolling.cs` herdando de `MonoBehaviour`. Implementar:
    - Enums de direção (`ScrollDirection { LeftToRight, RightToLeft }`).
    - Parâmetros de velocidade, limites X e lógica de acompanhamento Y híbrido.
    - Criação ou vinculação automática do GameObject filho `PushBarrier` com `Rigidbody2D` Kinematic e `BoxCollider2D`.
    - Atualização da posição da câmera e da barreira no `FixedUpdate`.
    - Lógica de detecção de esmagamento no `FixedUpdate` ou evento de colisão com o Player usando `Physics2D.BoxCastNonAlloc` contra `obstacleMask`.
    - Disparo de `playerStats.Kill()` quando a distância entre o Player e o obstáculo frontal for menor que a tolerância de esmagamento.
    - Métodos públicos utilitários: `SetSpeed(float newSpeed)`, `PauseScrolling()`, `ResumeScrolling()`.
    - `OnDrawGizmosSelected` para inspeção visual dos limites e da barreira no editor.
  - **Assigned role:** developer
  - **Dependencies:** Step 1
  - **Parallelizable:** No

- **Step 3: Criar / Configurar componente auxiliar na barreira ou integração direta**
  - **Description:** Garantir que o colisor da barreira tenha o Layer correto (ex.: `Default` ou `Wall`), configure `Physics2D.IgnoreCollision` se necessário para projéteis de inimigos, e capture colisões contínuas com o Player sem gerar alocação de memória no garbage collector.
  - **Assigned role:** developer
  - **Dependencies:** Step 2
  - **Parallelizable:** No

- **Step 4: Validação em cena de teste e testes no Unity Editor**
  - **Description:** Configurar um teste funcional numa cena de protótipo ou criar um cenário de validação com paredes de teste para verificar:
    - Rolagem suave nos dois sentidos (esquerda e direita).
    - Player parado sendo empurrado sem tremer.
    - Player prensado entre a borda e uma parede sendo esmagado e recarregando a fase.
    - Player invulnerável (após dano) sendo esmagado normalmente.
    - Inspeção do Unity Profiler para certificar 0 B de alocação de memória (GC Alloc) no `FixedUpdate`.
  - **Assigned role:** developer
  - **Dependencies:** Step 2, Step 3
  - **Parallelizable:** No

# Verification & Testing

### 1. Testes de Movimento e Direção
- Configurar `direction = LeftToRight` (avanço para a direita, barreira na esquerda): verificar que a câmera se desloca uniformemente para a direita na velocidade configurada.
- Configurar `direction = RightToLeft` (avanço para a esquerda, barreira na direita): verificar que a câmera se desloca uniformemente para a esquerda e a barreira se posiciona na borda direita.
- Testar limites `useLimitX = true` com `minX` e `maxX`: verificar se a câmera para de avançar suavemente ao atingir a coordenada designada.

### 2. Teste de Empurrão Suave (Push)
- Deixar o Player completamente parado no chão aberto.
- Observar a aproximação da borda da câmera.
- **Critério de Aceitação:** Ao encostar no Player, a barreira cinemática o empurra sem sobressaltos, mantendo a velocidade horizontal constante da câmera, sem que o Player atravesse o colisor da borda.

### 3. Teste de Esmagamento contra Obstáculos (Crush Death)
- Posicionar o Player em frente a uma parede de `Ground` / `Wall` intransponível.
- Permitir que a borda da câmera o alcance e pressione contra a parede.
- **Critério de Aceitação:** No momento em que o jogador é encurralado entre a barreira da câmera e a parede, o esmagamento é acionado imediatamente, executando `Kill()` e recarregando a cena.

### 4. Teste de Edge Case: Esmagamento durante Invulnerabilidade (i-frames)
- Fazer o Player sofrer dano intencional de um perigo e, enquanto o sprite estiver piscando (i-frames), permitir que a câmera o esmague contra uma parede.
- **Critério de Aceitação:** O Player morre imediatamente pelo esmagamento, comprovando que `Kill()` ignora os frames de invulnerabilidade.

### 5. Teste de Otimização Mobile (Zero GC Alloc)
- Executar a cena com o Unity Profiler aberto no modo CPU Usage / Memory.
- Filtrar por `AutoScrolling.FixedUpdate`.
- **Critério de Aceitação:** Zero alocação de memória gerenciada (GC Alloc = 0 B) por frame durante o ciclo contínuo de movimentação e detecção.
