# Project Overview
- **Game Title:** LeghMobile (LMobileUnity)
- **High-Level Concept:** Um jogo de plataforma e ação 2D com foco em mobilidade rápida e combate dinâmico. A "Fase 7" é uma arena de sobrevivência progressiva em 4 níveis (Grid 1, 2, 3, 4) com câmera fixa/pouco movimento, ondas de inimigos, escolta/suporte de NPC, perigos ambientais e uma batalha final contra chefe.
- **Players:** Single-player.
- **Inspiration / Reference Games:** Hollow Knight, Celeste (para movimento), Tower Defense/Survival Arenas.
- **Tone / Art Direction:** Pixel art / Sprite-based 2D com iluminação built-in.
- **Target Platform:** WebGL / Mobile (Android + iOS).
- **Screen Orientation / Resolution:** Landscape (Resolução de aspecto livre/16:9).
- **Render Pipeline:** Built-in Render Pipeline.
- **Unity Version:** 6000.4.7f1 (New Input System ativo).

---

# Game Mechanics

## Core Gameplay Loop
A Fase 7 funciona como uma arena de sobrevivência e progressão tática dividida em 4 estágios distintos. O jogador deve derrotar ondas de inimigos e gerenciar prioridades de combate enquanto se adapta às mudanças de mecânicas de cada nível:
1. **Level 1 (Defesa do NPC):** Sobrevivência mútua. O jogador deve proteger um NPC em preparação que possui vida própria. Os inimigos focam aleatoriamente entre o Player e o NPC. Se o NPC morrer, a fase é reiniciada.
2. **Level 2 (Suporte Ativo):** O NPC, agora preparado, fica oculto e ajuda o jogador periodicamente, aparecendo a cada X segundos para desferir um golpe devastador em um inimigo aleatório e desaparecer. Os inimigos agora ignoram o NPC e atacam exclusivamente o Player.
3. **Level 3 (Divisão de Tela):** Adiciona-se o perigo ambiental "Feixes de Energia". A tela é dividida em 3 seções horizontais (Cima, Meio, Baixo). Periodicamente, uma seção é sinalizada (Aviso/Carga) e disparada (Dano), exigindo do jogador posicionamento preciso.
4. **Level 4 (Boss Fight):** Batalha final contra um chefe com múltiplos estados e ataques probabilísticos (Melee, Projétil, Feixe Ambiental) dependendo da sua vida atual, incluindo uma fase de Fúria ao chegar a 50% de HP.

## Controls and Input Methods
- **Controles do Jogador:** Movimentação lateral, pulo, dash e ataque através dos botões virtuais mobile na tela (Canvas/ControlsMobile) integrados via New Input System (`PlayerControl`).
- **Comunicação de Eventos:** Para garantir performance mobile extrema, os sistemas comunicam-se de forma reativa através de eventos C# (`System.Action` e eventos estáticos), evitando buscas custosas de frames no `Update`.

---

# UI
- **Barra de Vida do NPC (Level 1):** Slider UI na tela que exibe a integridade do NPC em preparação.
- **Contador de Ondas / Timer:** Texto HUD informando a onda atual e progresso para o próximo nível.
- **Barra de Vida do Boss (Level 4):** Uma barra de vida horizontal proeminente na parte inferior da tela, ativada ao iniciar o estágio 4.
- **Indicadores Visuais dos Feixes (Level 3 e 4):** Painéis ou overlays semitransparentes vermelhos que piscam nas seções correspondentes da tela para sinalizar o perigo antes do disparo.

---

# Key Asset & Context

## Scripts Existentes no Projeto (Contexto)
- `RangedEnemy.cs` e `ObserverEnemy.cs`: IAs existentes que usam varreduras físicas de OverlapBox de forma otimizada para detectar o jogador.
- `EnemyStats.cs`: Gerencia vida e morte do inimigo. Atualmente chama `Destroy(gameObject)` na morte, o que viola as restrições mobile e precisa ser adaptado via eventos para Object Pooling.
- `PlayerStats.cs`: Controla a vida do jogador.
- `FaseManager.cs`: Singleton central para gerenciar moedas e estado da fase.

## Novos Assets a Criar

### 1. ScriptableObjects de Configuração
- `Fase7WaveConfig.cs`: ScriptableObject que define as configurações de ondas para os Levels 1, 2 e 3 (intervalos, quantidade, prefabs de inimigos para spawnar).
- `Fase7BossConfig.cs`: ScriptableObject contendo os atributos e probabilidades do Boss (Vida máxima, velocidade, tempos de recarga de ataques melee/projétil, chances de estados).

### 2. Gerenciamento e Infraestrutura
- `Fase7PoolManager.cs`: Gerenciador de Object Pooling global e leve. Armazena filas (`Queue<GameObject>`) para inimigos (Atirador, Observer, Ranged), projéteis e efeitos visuais dos feixes, evitando `Instantiate` e `Destroy`.
- `Fase7LevelManager.cs`: Controlador central da Fase 7. Controla a transição entre os níveis 1, 2, 3 e 4, gerencia a ativação física de `Grid1`, `Grid2`, `Grid3` e `Grid4`, e ativa a transição de tela escura (Fade).

### 3. Comportamentos de Personagens & Inimigos
- `Fase7NPC.cs`: Gerencia o comportamento do NPC (Level 1: barra de vida, vulnerabilidade, animação de preparação; Level 2/3: invisibilidade, ataque temporizado de suporte).
- `AtiradorEnemy.cs`: Nova IA baseada em Rigidbody2D. Move-se até o jogador, para na distância mínima e dispara projéteis (reutilizando projéteis do pool).
- `Fase7ObserverEnemy.cs` (ou modificação direta em `ObserverEnemy.cs`): Nova versão ou modo adaptado. Move-se até o player, para ao chegar perto (Telegraph/Aviso) e aplica um `AddForce2D` físico em direção ao player antes de sumir.
- `Fase7Boss.cs`: Controla o Boss no Level 4 usando uma máquina de estados finitos estruturada, baseada nos limites de HP e tabelas de probabilidade.

### 4. Sistemas Ambientais
- `Fase7BeamSystem.cs`: Gerencia a divisão de tela em 3 seções horizontais. Realiza checagem de Y-bounds matemática rápida (sem trigger físico constante) para detectar quem está na área de dano, proporcionando máxima performance mobile.

---

# Software Architecture Diagram

```
                 ┌────────────────────────────────┐
                 │       Fase7LevelManager        │◄─── (Central Flow & Stage Progression)
                 └───────┬────────────────┬───────┘
                         │                │
                         ▼                ▼
         ┌───────────────────────┐┌───────────────┴───────┐
         │   Fase7PoolManager    ││   Fase7BeamSystem     │◄─── (Zero-Physics Y-bounds Damage)
         └───────────────────────┘└───────────────────────┘
                         ▲
                         │ (Get / Release Objects)
                         │
  ┌──────────────────────┼───────────────────────┬───────────────────────┐
  │                      │                       │                       │
  ▼                      ▼                       ▼                       ▼
┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐   ┌──────────────────┐
│   AtiradorEnemy  │   │  ObserverEnemy   │   │    Fase7NPC      │   │    Fase7Boss     │
└──────────────────┘   └──────────────────┘   └──────────────────┘   └──────────────────┘
```

---

# Implementation Steps

## Step 1: Object Pooling Infrastructure (`Fase7PoolManager`)
- **Description**: Criar o script `Fase7PoolManager.cs` como um Singleton de alto desempenho para WebGL/Mobile. Ele pré-instancia inimigos, projéteis e feixes de energia sob demanda ou no `Start()`. Ele escuta eventos de morte de inimigos para desativar e retornar os objetos ao pool ao invés de usar `Destroy`.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 2: Adaptação de Ciclo de Vida (`EnemyStats` e `ProjectileEnemy`)
- **Description**: Modificar o script `EnemyStats.cs` para adicionar um evento `public event System.Action<EnemyStats> OnEnemyDeath`. Caso haja ouvintes (como o `Fase7PoolManager`), o inimigo é retornado ao pool; caso contrário, usa o fallback `Destroy()`. Fazer adaptação similar no `ProjectileEnemy` para desativação limpa.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: No

## Step 3: Implementação do Target Override no Ranged e Observer
- **Description**: Adicionar o campo `public Transform targetOverride` nas classes `RangedEnemy` e `ObserverEnemy`. Se `targetOverride` estiver definido, os inimigos utilizam checagem direta de distância e direção (`Vector3.Distance` e subtração vetorial) em vez de varreduras físicas de OverlapBox periódicas. Isso otimiza imensamente a CPU mobile.
- **Assigned role**: developer
- **Dependencies**: None
- **Parallelizable**: Yes

## Step 4: Novo Inimigo Atirador (`AtiradorEnemy`)
- **Description**: Criar o comportamento do novo inimigo `AtiradorEnemy.cs`. Ele rastreia o player/NPC, move-se em sua direção usando velocidade física linear do `Rigidbody2D`, para quando estiver abaixo da distância configurável no Inspector e inicia uma corrotina de disparo (pegando projéteis do `Fase7PoolManager`).
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 3
- **Parallelizable**: Yes

## Step 5: Novo Comportamento do `ObserverEnemy`
- **Description**: Modificar o `ObserverEnemy.cs` para adicionar o estado de ataque com Telegraph (Telegrafar): quando o inimigo estiver a uma distância curta do player, ele para seu movimento físico por um curto tempo (`telegraphDuration`), executa um aviso visual de carga, e em seguida aplica um impulso físico de investida rápida (`Rigidbody2D.AddForce` com `ForceMode2D.Impulse`) na direção do alvo, desativando-se e retornando ao pool logo após.
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 3
- **Parallelizable**: Yes

## Step 6: O NPC da Fase 7 (`Fase7NPC`)
- **Description**: Criar o script `Fase7NPC.cs`. Ele gerencia os dois estados do NPC:
  - **Level 1 (Preparação):** Ativa vida, barra de vida visual no HUD e animação de preparação. Se a vida zerar, aciona recarga de fase.
  - **Level 2/3 (Suporte Ativo):** NPC fica invisível. Um timer a cada X segundos o torna visível, aciona animação de ataque, causa dano imediato a um inimigo aleatório ativo na fase, e o esconde novamente.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

## Step 7: Sistema de Feixes de Energia (`Fase7BeamSystem`)
- **Description**: Criar o script `Fase7BeamSystem.cs`. A tela é dividida matematicamente em 3 faixas horizontais de acordo com limites Y configurados no Inspector. Periodicamente, o sistema seleciona uma faixa aleatória, ativa um indicador visual piscando (Carga) do pool, e depois ativa o dano (Disparo) por Y segundos.
- **Optimization**: O dano é aplicado de forma ultra-otimizada: ao invés de colisores físicos ativos a cada frame, é feita uma checagem de posição no eixo Y para o Player e NPC apenas no frame de disparo.
- **Assigned role**: developer
- **Dependencies**: Step 1
- **Parallelizable**: Yes

## Step 8: Máquina de Estados do Boss (`Fase7Boss`)
- **Description**: Criar a IA do Chefe em `Fase7Boss.cs` com ScriptableObject de dados (`Fase7BossConfig.cs`). 
  - Controla o estado de HP. Se cair para 50%, dispara a investida especial imediata (Sequência de Feixes consecutivos Cima->Meio->Baixo).
  - Executa ataques probabilísticos (Melee, Projétil, Feixe) usando pesos parametrizados no ScriptableObject baseados em HP (>50% vs <=50%).
- **Assigned role**: developer
- **Dependencies**: Step 1, Step 7
- **Parallelizable**: Yes

## Step 9: Fluxo Geral e Transições (`Fase7LevelManager`)
- **Description**: Criar o script central `Fase7LevelManager.cs`. Ele coordena a sequência de Levels (1 ao 4).
  - **Level 1:** Inicializa o Grid 1, NPC em preparação, e spawna ondas do ScriptableObject. Inimigos spawnam escolhendo alvo aleatório (NPC ou Player). Ao fim das ondas, transita.
  - **Transição:** Ativa efeito de tela preta (fade coroutine), desativa grid atual, ativa próximo grid (`Grid2`/`Grid3`/`Grid4`), reposiciona o jogador, e desfaz o fade.
  - **Level 2:** Ativa Grid 2, NPC no modo suporte invisível, spawna ondas. Todos os inimigos miram apenas no Player.
  - **Level 3:** Ativa Grid 3, mantém suporte do NPC e inicia o `Fase7BeamSystem`.
  - **Level 4:** Ativa Grid 4, para o spawner comum, ativa o Boss e sua barra de vida no HUD.
- **Assigned role**: developer
- **Dependencies**: All previous steps
- **Parallelizable**: No

---

# Verification & Testing

## Testes Unitários e Manuais
1. **Verificação de Desempenho (Zero-GC no Spawner):**
   - Abrir o Unity Profiler na aba "CPU Usage" e monitorar as alocações de GC durante o spawn e morte de inimigos nos Levels 1-3. Confirmar que não há picos de "GC Alloc" vindos de `Instantiate`, `Destroy` ou `OverlapBox`.
2. **Teste de Transição de Grid:**
   - Forçar a conclusão da onda 1 e garantir que a corrotina de fade escureça a tela, mude a ativação de `Grid1` para `Grid2` com sucesso, redefina a posição do jogador para a origem correta do novo Grid e restaure a luz sem travamentos ou quebras visuais.
3. **Teste de Colisão do Feixe por Coordenada Y (Level 3):**
   - Posicionar o Player na seção Cima, Meio e Baixo respectivamente enquanto o feixe de dano é acionado nessas faixas. Confirmar que a vida do Player é reduzida corretamente por checagem de posição sem uso de colliders dinâmicos.
4. **Teste de Probabilidade e Fúria do Boss (Level 4):**
   - Reduzir manualmente a vida do Boss para menos de 50% no Inspector em tempo de execução. Verificar se ele dispara instantaneamente o ataque Sequência de Feixes nas 3 seções em sequência e se as probabilidades dos ataques mudam para a tabela de fúria.
5. **Teste de Morte do NPC (Level 1):**
   - Permitir que os inimigos derrotem o NPC em preparação. Verificar se o reinício da Fase 7 é engajado imediatamente de forma limpa.
