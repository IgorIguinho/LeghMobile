# Project Overview

- **Game Title:** LeghMobile (LMobileUnity)
- **High-Level Concept:** Plataforma 2D mobile com mecânicas de desafio baseadas em terreno (zonas de gatilho sobre tilemaps) que alteram o comportamento do player ao passar por cima.
- **Players:** Single player
- **Target Platform:** WebGL
- **Render Pipeline:** Built-in
- **Unity Version:** 6000.4.7f1
- **Input System:** New Input System

> **Escopo deste plano:** DOIS sistemas independentes, em scripts separados, mas descritos no mesmo documento por serem simples:
> 1. **PlusSpeed** — impulso horizontal (boost) na direção em que o player caminha.
> 2. **DontJump** — zona que impede o player de pular enquanto estiver sobre os tiles.
>
> Os sistemas NÃO conversam entre si. Cada um é um `MonoBehaviour` próprio na pasta `Assets/Scripts/Fases/Desafios`.

---

# Decisões de Design (confirmadas)

### PlusSpeed
- **Disparo:** `OnTriggerEnter2D` — impulsiona **uma vez por entrada**; só repete se o player sair e voltar.
- **Direção:** usa a variável `direction` do `PlayerMovements` (1 = direita, -1 = esquerda) → impulso "para frente ou para trás" conforme o caminhar.
- **Velocidade do impulso:** ajustável no Inspector (campo no script da zona).
- **Desaceleração:** usa o **`linearDamping = 2`** atual do Rigidbody2D do player (decai naturalmente). O player **não pode sobrepor** a velocidade até ela voltar a ser `<= speed` (ou ~0).
- **Configuração do GameObject:** conforme sua escolha, este plano **só cria o script** `PlusSpeedZone`. A decisão sobre remover/manter o componente `SwitchSpeed` existente e a layer `SwitchSpeed` fica com você.

### DontJump
- **Comportamento do eixo Y:** **somente o pulo é bloqueado**. A gravidade continua agindo (o player ainda cai normalmente).
- **Movimento X e Dash:** continuam funcionando normalmente.
- **Detecção:** `OnTriggerEnter2D` / `OnTriggerExit2D` para ativar/desativar o bloqueio enquanto o player estiver na zona.

---

# Game Mechanics

## PlusSpeed — Impulso horizontal
Ao tocar os tiles, o player recebe uma velocidade horizontal alta na direção atual (`boostSpeed * direction`). Enquanto a velocidade estiver acima da velocidade normal de movimento, o controle do player é **suspenso no eixo X** (não consegue sobrepor o boost). O arrasto do Rigidbody (`linearDamping = 2`) reduz a velocidade gradualmente; quando `|vx| <= speed`, o controle normal volta.

## DontJump — Bloqueio de pulo
Enquanto o player estiver sobre os tiles, qualquer tentativa de pulo é ignorada. O player continua caindo (gravidade ativa), andando no X e dando dash. Ao sair da zona, o pulo é reabilitado.

---

# UI
Nenhuma mudança de UI necessária para os dois sistemas.

---

# Key Asset & Context

## Estado atual verificado
- **Player** (tag `Player`, layer `Player`): `Rigidbody2D` (`gravityScale = 3`, `linearDamping = 2`, `constraints = FreezeRotation`, `Dynamic`), `BoxCollider2D`, `PlayerMovements`, `InputReader`, `RewindObj`, `Animator`.
- **`PlayerMovements`** (`Assets/Scripts/Player/PlayerMovements.cs`): valores atuais `speed = 10`, `speedOnAir = 7`, `direction = 1`, `jumpForce = 15`, `dashForce = 30`.
  - `Moviment()` reescreve `rb.linearVelocity.x` **todo FixedUpdate** → é o motivo de o PlusSpeed exigir modificação (senão o boost é sobrescrito imediatamente).
  - `Jump()` é chamado pelo evento de input (`OnJumpInput`) → bloquear o pulo exige uma flag dentro do `PlayerMovements`.
- **`Grid/PlusSpeed`** (cena `Fase5`): `Tilemap` + `TilemapCollider2D` + `CompositeCollider2D` (`isTrigger = true`), na layer `SwitchSpeed`, **com componente `SwitchSpeed` existente** (`switchSpeed = 35`). (Config do GameObject fora do escopo — só script.)
- **`Grid/DontJump`** (cena `Fase5`): `Tilemap` + `TilemapCollider2D` + `CompositeCollider2D` (`isTrigger = true`), layer `Default`. Infraestrutura de gatilho pronta.
- **Convenção da pasta `Assets/Scripts/Fases/Desafios/`:** `MonoBehaviour` simples com `OnTriggerEnter2D` checando `collision.CompareTag("Player")` (ex.: `SwitchSpeed.cs`, `SpikeDmg.cs`, `DoorTeleport.cs`).

## Scripts a serem criados

### 1) `Assets/Scripts/Fases/Desafios/PlusSpeedZone.cs`
```csharp
using UnityEngine;

public class PlusSpeedZone : MonoBehaviour
{
    [Header("PlusSpeed (Impulso)")]
    [Tooltip("Velocidade horizontal do impulso aplicada na direcao em que o player caminha.")]
    [SerializeField] private float boostSpeed = 20f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var pm = collision.GetComponent<PlayerMovements>();
        var rb = collision.attachedRigidbody;
        if (pm == null || rb == null) return;

        // Direcao do caminhar (1 = direita, -1 = esquerda)
        rb.linearVelocity = new Vector2(boostSpeed * pm.direction, rb.linearVelocity.y);

        // Suspende o controle de X ate o boost decair (ver modificacao no PlayerMovements)
        pm.isPlusSpeedBoost = true;
    }
}
```

### 2) `Assets/Scripts/Fases/Desafios/DontJumpZone.cs`
```csharp
using UnityEngine;

public class DontJumpZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        var pm = collision.GetComponent<PlayerMovements>();
        if (pm != null) pm.canJump = false;   // bloqueia o pulo (gravidade continua)
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        var pm = collision.GetComponent<PlayerMovements>();
        if (pm != null) pm.canJump = true;     // reabilita o pulo ao sair
    }
}
```

---

# Modificações no PlayerMovements (para sua análise)

> Ambas as mudanças são **aditivas e não quebram** o comportamento atual: os novos campos têm valor padrão que mantém o jogo idêntico quando nenhuma zona está ativa.

## A) Para o PlusSpeed — suspender o controle de X durante o boost

**Por quê:** `Moviment()` reescreve `rb.linearVelocity.x` todo FixedUpdate. Sem isso, o impulso some no frame seguinte e o player "sobrepõe" a velocidade — o oposto do requisito.

**Novo campo:**
```csharp
[Header("PlusSpeed Boost")]
public bool isPlusSpeedBoost = false;
```

**Mudança em `Moviment()`** — adicionar logo após `if (isDash) return;`:
```csharp
void Moviment()
{
    if (isDash) return;

    // --- PlusSpeed boost ---
    if (isPlusSpeedBoost)
    {
        // o linearDamping (=2) do Rigidbody reduz a velocidade naturalmente.
        // enquanto |vx| > speed, NAO devolvemos o controle ao player.
        if (Mathf.Abs(rb.linearVelocity.x) <= speed)
        {
            isPlusSpeedBoost = false; // boost decaiu -> controle normal volta
        }
        else
        {
            animator.SetFloat("speed", Mathf.Abs((input != null) ? input.Direction : 0f));
            return; // ignora a reescrita normal de X enquanto boostando
        }
    }
    // --- fim PlusSpeed ---

    float currentDirection = (input != null) ? input.Direction : 0f;
    // ... resto do metodo permanece igual ...
}
```
- **Decaimento:** garantido pelo `linearDamping = 2` (sua escolha). Quando `|vx| <= speed`, o controle normal retorna e a velocidade se iguala a `speed` (ou 0 se sem input).
- **Comportamento vertical:** não é tocado — o Y continua normal.

## B) Para o DontJump — bloquear o pulo

**Por quê:** `Jump()` é disparado pelo evento de input dentro do `PlayerMovements`; a zona não tem como interceptar o input diretamente.

**Novo campo:**
```csharp
[Header("DontJump")]
public bool canJump = true;
```

**Mudança em `Jump()`** — adicionar como primeira verificação:
```csharp
void Jump()
{
    if (!canJump) return;   // DontJump: bloqueia o pulo
    if (isDash) { return; }
    // ... resto igual ...
}
```
- **Gravidade/queda:** continua normal (só o pulo é impedido). Dash e movimento no X seguem funcionando.
- **Segurança:** o `OnTriggerExit2D` do `DontJumpZone` restaura `canJump = true` ao sair, evitando que o player fique sem pular.

---

# Implementation Steps

### Step 1 — Modificar `PlayerMovements.cs` (campos + Jump + Moviment)
- **Description:** Adicionar os campos `isPlusSpeedBoost` e `canJump`; inserir o bloco do boost em `Moviment()` e o `if (!canJump) return;` em `Jump()`, conforme a seção "Modificações no PlayerMovements".
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No (base para os dois scripts)

### Step 2 — Criar `PlusSpeedZone.cs`
- **Description:** Criar `Assets/Scripts/Fases/Desafios/PlusSpeedZone.cs` com campo `boostSpeed` (Inspector) e `OnTriggerEnter2D` que aplica `boostSpeed * direction` no X e seta `isPlusSpeedBoost = true`.
- **Assigned role:** developer
- **Dependencies:** Depends on Step 1
- **Parallelizable:** Yes (independente do Step 3)

### Step 3 — Criar `DontJumpZone.cs`
- **Description:** Criar `Assets/Scripts/Fases/Desafios/DontJumpZone.cs` com `OnTriggerEnter2D` (canJump=false) e `OnTriggerExit2D` (canJump=true).
- **Assigned role:** developer
- **Dependencies:** Depends on Step 1
- **Parallelizable:** Yes (independente do Step 2)

### Step 4 — Configuração de cena (por você)
- **Description:** Anexar `PlusSpeedZone` ao `Grid/PlusSpeed` (definir `boostSpeed`; decidir sobre o componente `SwitchSpeed`/layer existentes) e `DontJumpZone` ao `Grid/DontJump`. Triggers já estão ativos.
- **Assigned role:** developer (ou você manualmente)
- **Dependencies:** Depends on Steps 2 e 3
- **Parallelizable:** No

---

# Verification & Testing

## PlusSpeed
1. Andar para a direita sobre os tiles → player é arremessado para a direita; andar para a esquerda → arremessado para a esquerda (usa `direction`).
2. Durante o boost, segurar a direção contrária → o player **não** consegue sobrepor a velocidade enquanto `|vx| > speed`.
3. O boost desacelera sozinho (via `linearDamping`) até `|vx| <= speed`; aí o controle normal volta. Sem input, a velocidade tende a ~0.
4. Ajustar `boostSpeed` no Inspector altera a intensidade do impulso.
5. O eixo Y (pulo/queda) continua normal durante e após o boost.
6. Sair e reentrar na zona dispara o boost de novo (validação do `OnTriggerEnter2D`).

## DontJump
7. Dentro da zona: pressionar pulo → nada acontece (pulo bloqueado).
8. Dentro da zona: movimento no X e Dash continuam funcionando; o player ainda cai (gravidade ativa).
9. Ao sair da zona: o pulo volta a funcionar imediatamente (`OnTriggerExit2D`).
10. Caso de borda: dar dash atravessando a zona não deixa `canJump` travado em false após sair.

## Geral
11. Console limpo (sem `NullReferenceException`/warnings novos).
12. Comportamento padrão do player inalterado quando nenhuma zona está ativa (`isPlusSpeedBoost = false`, `canJump = true`).
