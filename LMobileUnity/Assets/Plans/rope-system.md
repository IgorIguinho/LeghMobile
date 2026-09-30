# Project Overview

- **Game Title:** LeghMobile (projeto existente)
- **High-Level Concept:** Jogo de plataforma 2D mobile com movimentação ágil e habilidades especiais. Este plano implementa o sistema de corda completo e otimizado atrelado ao tileset `Grid/Rope`, substituindo o protótipo simplificado atual por uma mecânica bipartida de descida passiva e salto direcional.
- **Players:** Single player (controles de plataforma 2D em touchscreen / teclado / gamepad).
- **Inspiration / Reference Games:** Mecânicas clássicas de escalada/deslizamento em cordas de 2D platformers (ex.: Donkey Kong Country, Celeste, Mega Man X).
- **Tone / Art Direction:** Pixel Art 2D / Tilemap com renderização URP 2D.
- **Target Platform:** WebGL / Mobile (Android & iOS). Foco em alta performance, zero alocação de GC no loop de física (`FixedUpdate`).
- **Screen Orientation / Resolution:** Landscape (resolução nativa configurada no projeto).
- **Render Pipeline:** URP2D (Universal Render Pipeline 2D).

---

# Game Mechanics

## Core Gameplay Loop
A corda é um elemento interativo que funciona como uma zona de travamento temporário no ar e controle de ritmo:
1. **Engajamento Imediato:** Ao saltar ou cair em direção a uma corda (`Grid/Rope`), o jogador se fixa imediatamente a ela, pausando a inércia da queda.
2. **Descida Passiva Contínua:** A gravidade é anulada ($g = 0$) e o jogador desliza verticalmente para baixo em velocidade constante e reduzida ($v_y = -v_{\text{descida}}$). O jogador não tem comandos para subir ou parar a descida voluntariamente.
3. **Trava na Extremidade Inferior (Clamp):** Ao atingir o nó inferior da corda ($Y_{\text{min}}$), a velocidade vertical é zerada ($v_y = 0$). O jogador permanece preso na ponta da corda sem cair no abismo até decidir saltar.
4. **Posicionamento Bipartido (Eixo X):** O personagem se posiciona com um offset fixo à esquerda ou à direita do centro da corda ($X = X_{\text{corda}} \pm \text{Offset}$). Utilizando os comandos direcionais (Esquerda/Direita), o jogador alterna de lado instantaneamente, invertendo a orientação visual do sprite.
   - **Lado da Entrada:** Definido pela posição de contato (se entrou pela esquerda do centro, fixa no lado esquerdo; se pela direita, no lado direito).
   - **Orientação:** O sprite fica voltado para fora (olhando na direção do salto de saída).
5. **Salto Direcional de Saída:** Ao pressionar o botão de pulo, a corda aplica um impulso diagonal fixo baseado unicamente no lado atual onde o jogador está pendurado:
   - Lado Esquerdo: $\vec{v} = [-v_x, +v_y]$ (impulsiona para cima e para a esquerda).
   - Lado Direito: $\vec{v} = [+v_x, +v_y]$ (impulsiona para cima e para a direita).
   - A gravidade padrão é restaurada e o estado transiciona para o ar/pulo normal.

## Controls and Input Methods
- **Entrada Direcional (Esquerda / Direita via `input.Direction`):** Alterna o jogador entre o lado esquerdo e direito da corda.
- **Botão de Pulo (`input.JumpTriggered`):** Executa o salto direcional para fora da corda.
- **Botão de Dash (`input.DashTriggered`):** Permite soltar da corda através de um dash aéreo caso o dash esteja desbloqueado.

---

# UI
- Não requer elementos novos de HUD/UI.
- O sistema é 100% responsivo aos botões virtuais já existentes de movimentação, pulo e dash da HUD mobile.
- **Gizmos de Debug no Editor:** O script desenhará gizmos visuais nas cordas para visualização das linhas centrais ($X_{\text{corda}}$), offsets laterais e limites inferiores de clamp ($Y_{\text{min}}$).

---

# Key Asset & Context

### Estrutura Existente no Projeto
- **GameObject do Tilemap:** `Grid/Rope` (Layer 9: `Rope`).
  - Possui `Tilemap`, `TilemapRenderer`, `TilemapCollider2D` (usado por Composite), `CompositeCollider2D` (`isTrigger = true`, `geometryType = Outlines`) e `Rigidbody2D` (`Static`).
  - Na cena `Fase1`, existem 7 cordas verticais distribuídas nos eixos $X$: 38, 58, 79, 308, 347, 359, 384. Cada corda é uma coluna vertical de largura exata de 1 tile (largura 1.0).
- **`PlayerMovements.cs`:**
  - Controla velocidade, pulo, flip, colisões e estados.
  - Possui variáveis legadas do protótipo de corda (`isRope`, `ropeJumpForce`, `ropeHorizontalJumpForce`, `ropeFall`, `layerRope`).

### Arquivos a Modificar / Criar
1. **`Assets/Scripts/Fases/RopeTilemap.cs` (NOVO):**
   - Componente leve anexado ao `Grid/Rope` (ou instanciado/detectado automaticamente caso falte).
   - No `Awake()`, escaneia o `Tilemap` uma única vez e monta um array indexado com as colunas de corda da fase:
     - `cellX` (int)
     - `centerX` (float, centro mundo do tile)
     - `minY` (float, limite inferior do nó da corda)
     - `maxY` (float, limite superior)
   - Fornece busca $O(1)$ / $O(N)$ direta sem alocações no heap (GC Zero).
2. **`Assets/Scripts/Player/PlayerMovements.cs` (MODIFICADO):**
   - Substituição do protótipo antigo pela implementação oficial das especificações:
     - Detecção ao colidir com o trigger da corda.
     - Posicionamento bipartido ($X_{\text{corda}} \pm \text{Offset}$) e controle de alternância por input.
     - Descida passiva contínua com clamp em $Y_{\text{min}}$.
     - Salto direcional atrelado exclusivamente ao lado em que o jogador está pendurado.
     - Restauração de gravidade e desprendimento limpo sem quebrar os outros sistemas (Dash, InvertGravity, Rewind, etc.).

---

# Implementation Steps

### Step 1: Criar o componente `RopeTilemap.cs` para indexação otimizada das cordas
- **Description:** Criar `Assets/Scripts/Fases/RopeTilemap.cs`. O componente escaneia o `Tilemap` no `Awake()` e calcula as colunas verticais de corda existentes na fase, armazenando os limites ($X_{\text{corda}}$, $Y_{\text{min}}$, $Y_{\text{max}}$) em uma estrutura de dados de valor (`struct RopeColumnData`). Inclui método utilitário `TryGetRopeAt(Vector2 worldPos, out RopeColumnData column)` para busca rápida e sem alocação de memória (otimizado para mobile). Adiciona também `OnDrawGizmosSelected()` para exibir as linhas de eixo e nós inferiores no Editor.
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** Yes

### Step 2: Atualizar `PlayerMovements.cs` com a máquina de estado e física da corda
- **Description:** Modificar `Assets/Scripts/Player/PlayerMovements.cs`:
  1. Manter e expandir as variáveis de configuração de corda: `ropeSlideSpeed` (velocidade de descida passiva), `ropeHorizontalOffset` (distância do centro, padrão 0.35f), `ropeBottomOffsetY` (compensação do pé/centro do personagem no nó inferior), `ropeJumpForce` e `ropeHorizontalJumpForce`.
  2. Implementar entrada no estado `NaCorda` via trigger / detecção de camada da corda:
     - Zera gravidade (`rb.gravityScale = 0`).
     - Determina lado inicial baseado na posição de entrada ($x < X_{\text{corda}} \implies$ Esquerda, senão Direita).
     - Alinha a orientação do sprite para olhar para fora (salto).
  3. No `FixedUpdate()`:
     - Bloquear movimentação terrestre tradicional (`if (isRope) { ProcessRopeMovement(); return; }`).
     - Leitura de `input.Direction`: se negativo alterna para a Esquerda; se positivo alterna para a Direita. Atualiza o Flip do sprite de acordo.
     - Travar a posição $X = X_{\text{corda}} \pm \text{Offset}$.
     - Descer continuamente com $v_y = -v_{\text{descida}}$ e aplicar trava (clamp) ao atingir $Y_{\text{min}}$.
  4. No método `Jump()`:
     - Se `isRope`: aplicar impulso $\vec{v} = [-v_x, +v_y]$ se na esquerda, ou $[+v_x, +v_y]$ se na direita.
     - Restaurar `rb.gravityScale = defaultGravity` e setar `isRope = false`.
     - Invocar `OnJump?.Invoke()`.
  5. No método `Dash()`:
     - Se estiver na corda e acionar dash, desconectar da corda antes do dash.
- **Assigned role:** developer
- **Dependencies:** Step 1
- **Parallelizable:** No

### Step 3: Configurar o GameObject `Grid/Rope` na cena `Fase1`
- **Description:** Garantir que o componente `RopeTilemap` esteja anexado ao GameObject `Grid/Rope` da cena `Fase1`, validando os colliders e a camada `Rope`.
- **Assigned role:** developer
- **Dependencies:** Step 1, Step 2
- **Parallelizable:** No

### Step 4: Validação em PlayMode e Testes
- **Description:** Rodar script de validação / teste no Unity Editor em PlayMode na cena `Fase1` para comprovar:
  - Detecção ao entrar na corda e anulação de gravidade.
  - Descida contínua sem comando do jogador.
  - Clamp no nó inferior $Y_{\text{min}}$ sem atravessar ou cair.
  - Alternância de lado (Esquerda/Direita) e flip de sprite.
  - Salto diagonal correto para cada lado e desprendimento limpo.
- **Assigned role:** developer
- **Dependencies:** Step 3
- **Parallelizable:** No

---

# Verification & Testing

1. **Teste de Entrada e Gravidade:**
   - Saltar na direção de uma corda (ex.: corda no $X=38$).
   - Verificar se o estado `isRope` torna-se `true` e a gravidade do `Rigidbody2D` torna-se 0.
2. **Teste de Descida e Clamp Inferior:**
   - Deixar o personagem descer sem pressionar direcionais.
   - Constatar que a velocidade vertical permanece $-v_{\text{descida}}$ até atingir $Y_{\text{min}}$, onde a velocidade $v_y$ é travada em 0 e o personagem fica suspenso.
3. **Teste de Alternância de Lado e Flip:**
   - Pressionar analógico/tecla para a esquerda: personagem move-se para $X_{\text{corda}} - \text{Offset}$ e vira para a esquerda.
   - Pressionar analógico/tecla para a direita: personagem move-se para $X_{\text{corda}} + \text{Offset}$ e vira para a direita.
4. **Teste de Salto Direcional:**
   - No lado esquerdo: pressionar Pulo. Verificar se o vetor de velocidade aplicado tem $v_x < 0$ e $v_y > 0$.
   - No lado direito: pressionar Pulo. Verificar se o vetor de velocidade aplicado tem $v_x > 0$ e $v_y > 0$.
   - Verificar se o personagem não fica preso na corda após o salto e a gravidade padrão é restaurada.
5. **Teste de Compatibilidade com Outras Mecânicas:**
   - Verificar que Dash, Inversão de Gravidade e Rewind continuam operando normalmente sem erros no console.
