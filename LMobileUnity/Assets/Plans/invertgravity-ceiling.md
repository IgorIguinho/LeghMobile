# Project Overview

- **Game Title:** LeghMobile (LMobileUnity)
- **High-Level Concept:** Plataforma 2D mobile com mecânicas de desafio baseadas em terreno (zonas de gatilho sobre tilemaps) que alteram o comportamento do player ao passar por cima.
- **Players:** Single player
- **Target Platform:** WebGL
- **Render Pipeline:** Built-in
- **Unity Version:** 6000.4.7f1
- **Input System:** New Input System

> **Escopo deste plano:** SOMENTE o sistema **InvertGravity** (inversão de gravidade / andar no teto). É o 4º e último dos sistemas de desafio. Script novo em `Assets/Scripts/Fases/Desafios`.

---

# Decisões de Design (confirmadas)

- **Disparo:** `OnTriggerEnter2D` — **alterna** a gravidade a cada entrada (normal → invertida → normal). Mesmo padrão de gatilho dos outros sistemas.
- **Inversão:** ao ativar, `rb.gravityScale` troca de sinal (3 → -3) e o player **rotaciona 180° no eixo X** (fica de cabeça para baixo, andando no teto).
- **Forças verticais:** **todas invertem** junto com a gravidade — pulo, wall jump, rope jump e os clamps de queda (`WallFall`/`InRope`).
- **Câmera:** **sem alterações** — o `FollowCam` segue só a posição (lerp + clamp), nunca a rotação. A sensação de "andar no teto" já acontece naturalmente.
- **Respawn:** ver seção "Outras Alterações / Pontos a Validar" — a morte recarrega a cena, então a gravidade já reseta sozinha.

---

# Game Mechanics

## InvertGravity — Andar no teto
Ao tocar os tiles da zona `InvertGravity`, a gravidade do player é invertida e ele gira 180° no eixo X, passando a "cair para cima" e a andar no teto. Entrar de novo na zona desfaz a inversão. O movimento horizontal continua respondendo ao input normalmente (esquerda/direita no mundo); o pulo passa a empurrar o player em direção ao teto-relativo "para cima" (para baixo no mundo). Dash, movimento em X e checagem de chão continuam funcionando (o `GroundCheck`, sendo filho do player, gira junto e passa a detectar o teto como chão).

## Controles e Input
- Sistema automático por contato — não consome input novo.
- Pulo e dash continuam nos mesmos inputs, agora coerentes com a gravidade invertida.

---

# UI
Nenhuma mudança de UI necessária.

---

# Key Asset & Context

## Estado atual verificado
- **Player** (tag/layer `Player`): `Rigidbody2D` (`gravityScale = 3`, `linearDamping = 2`, `constraints = FreezeRotation`, `Dynamic`), `BoxCollider2D`, `PlayerMovements`, `PlayerStats`, `PlayerAttack`, `RewindObj`, `Animator`, `InputReader`.
  - Filhos: `GroundCheck` (localPos ≈ (0.05, **-0.64**, 0)), `WallCheck` (≈ (0.04, -0.18, 0)), `TrailsGroup`, `AttackArea`. → Por serem **filhos**, ao rotacionar o player 180° em X eles vão para o lado do teto automaticamente; `CheckGround()` não precisa de mudança.
- **`PlayerMovements`** valores atuais: `speed = 10`, `speedOnAir = 7`, `direction = 1`, `jumpForce = 15`, `wallJumpForce`/`wallHorizontalJumpForce`, `dashForce = 30`.
  - `Jump()`, wall jump e rope jump usam `AddForce` com Y **positivo** fixo → precisam multiplicar pelo sinal da gravidade.
  - `WallFall()` / `InRope()` fazem clamp de `linearVelocity.y` para baixo → precisam acompanhar a inversão.
  - `Dash()` salva e restaura `rb.gravityScale` localmente → **já preserva** a gravidade invertida (sem mudança).
  - `CheckGround()` usa `OverlapBox` na posição dos checkers (filhos) → **sem mudança**.
- **`Grid/InvertGravity`** (cena `Fase5`): `Tilemap` + `TilemapCollider2D` + `CompositeCollider2D` (`isTrigger = true`), layer `Default`. Infraestrutura de gatilho pronta.
- **Câmera** `Main Camera` com `FollowCam` (segue só posição) → **sem mudança**.
- **Respawn:** `PlayerStats.TakeDmg()` → `SceneManager.LoadScene(actualScene)` quando HP ≤ 0 (recarrega a cena). `RewindObj` grava/replica `transform.rotation` mas **não** grava `gravityScale`.

## Script a ser criado

### `Assets/Scripts/Fases/Desafios/InvertGravityZone.cs`
```csharp
using UnityEngine;

public class InvertGravityZone : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        var pm = collision.GetComponent<PlayerMovements>();
        if (pm != null) pm.ToggleGravity();
    }
}
```
- Mantém o padrão simples da pasta (`OnTriggerEnter2D` + `CompareTag("Player")`).
- A lógica fica centralizada no `PlayerMovements` (`ToggleGravity()`) para manter pulo/forças/rotação coerentes num só lugar.

---

# Modificações no PlayerMovements (para sua análise)

> Todas aditivas e não-quebráveis: com `isGravityInverted = false` (padrão) o jogo se comporta exatamente como hoje.

## 1) Novo campo + método público `ToggleGravity()`
```csharp
[Header("Invert Gravity")]
public bool isGravityInverted = false;

public void ToggleGravity()
{
    isGravityInverted = !isGravityInverted;

    // Troca o SINAL da gravidade preservando a magnitude (3 -> -3 -> 3)
    rb.gravityScale = Mathf.Abs(rb.gravityScale) * (isGravityInverted ? -1f : 1f);

    // Zera o Y para o flip ficar limpo (OPCIONAL - remova se preferir manter o momento)
    rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

    // Rotaciona 180 no eixo X (de cabeca para baixo / volta ao normal)
    transform.Rotate(180f, 0f, 0f);
}
```

## 2) `Jump()` — inverter forças verticais
Adicionar um fator de sinal e aplicar em todos os `AddForce` verticais:
```csharp
void Jump()
{
    if (isDash) { return; }
    float g = isGravityInverted ? -1f : 1f;   // <-- novo

    if (isGrounded)
    {
        numberJump = 0;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(0f, jumpForce * g), ForceMode2D.Impulse);   // * g
        numberJump++;
    }
    else if (isWall)
    {
        numberJump = 0;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(wallHorizontalJumpForce * -direction, wallJumpForce * g), ForceMode2D.Impulse); // * g no Y
        Flip();
        numberJump++;
        StartCoroutine(StopMove());
    }
    else if (isRope)
    {
        numberJump = 0;
        rb.linearVelocity = Vector2.zero;
        rb.AddForce(new Vector2(ropeHorizontalJumpForce * direction, ropeJumpForce * g), ForceMode2D.Impulse); // * g no Y
    }
}
```

## 3) `WallFall()` e `InRope()` — acompanhar a inversão
```csharp
void WallFall()
{
    float g = isGravityInverted ? -1f : 1f;
    if (isWall && rb.linearVelocity.y * g < wallFallForce)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -wallFallForce * g);
    }
}

void InRope()
{
    float g = isGravityInverted ? -1f : 1f;
    if (isRope && rb.linearVelocity.y * g < ropeFall)
    {
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, -ropeFall * g);
    }
}
```

## O que NÃO muda (e por quê)
- **`CheckGround()`**: os checkers são filhos do player; a rotação 180° em X os leva para o lado do teto automaticamente.
- **`Dash()`**: já salva/restaura `rb.gravityScale` localmente → preserva a gravidade invertida.
- **`Moviment()`**: o controle horizontal (`speed * direction`) age no X do mundo independentemente da rotação; pressionar direita continua indo para a direita do mundo. O `Flip()` (rotação em Y) coexiste com a rotação em X.

---

# Outras Alterações / Pontos a Validar (separado, para sua análise)

> Nenhuma destas exige código obrigatório agora — são pontos de decisão/validação.

1. **Câmera (`FollowCam`):** **sem alteração necessária.** Segue só posição; a inversão dá a sensação de teto naturalmente, como você pediu.
2. **Respawn / Morte:** **sem código necessário.** `PlayerStats` recarrega a cena ao morrer, então `gravityScale` (3) e rotação (0) voltam ao padrão sozinhos.
3. **Rewind (`RewindObj`) — edge case:** o rewind grava/replica `transform.rotation` (a rotação visual volta certo), mas **não** grava `gravityScale`. Se o player rebobinar atravessando um ponto de inversão, a rotação pode voltar sem a gravidade correspondente, ficando inconsistente. Além disso, durante o rewind o corpo fica `Kinematic` e a posição é setada direto — os eventos de trigger podem não disparar de forma confiável. **Recomendação:** tratar como limitação conhecida nesta fase; se for um problema no teste, um follow-up seria gravar/restaurar `gravityScale` (e o flag `isGravityInverted`) dentro do `RewindObj`. Quer que eu inclua esse follow-up no plano ou deixamos como validação?
4. **Flip horizontal de cabeça para baixo:** com a rotação X + a rotação Y do `Flip()`, vale um check visual de que o sprite vira para o lado certo ao mudar de direção no teto (incluído nos testes).

---

# Implementation Steps

### Step 1 — Modificar `PlayerMovements.cs`
- **Description:** Adicionar `isGravityInverted` + `ToggleGravity()`; aplicar o fator `g` em `Jump()` (3 AddForce), `WallFall()` e `InRope()`, conforme a seção "Modificações no PlayerMovements".
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No (base do sistema)

### Step 2 — Criar `InvertGravityZone.cs`
- **Description:** Criar `Assets/Scripts/Fases/Desafios/InvertGravityZone.cs` com `OnTriggerEnter2D` chamando `pm.ToggleGravity()`.
- **Assigned role:** developer
- **Dependencies:** Depends on Step 1
- **Parallelizable:** No

### Step 3 — Configuração de cena (por você)
- **Description:** Anexar `InvertGravityZone` ao `Grid/InvertGravity`. Trigger já está ativo. Nenhuma config de câmera/respawn necessária.
- **Assigned role:** developer (ou você manualmente)
- **Dependencies:** Depends on Step 2
- **Parallelizable:** No

---

# Verification & Testing

1. **Inversão básica:** andar sobre a zona → gravidade inverte, player gira 180° no X e "cai para cima"/anda no teto.
2. **Alternância:** entrar de novo na zona → volta ao normal (gravidade e rotação). Repetir várias vezes mantém consistente.
3. **Pulo invertido:** no teto, pular → o player é empurrado em direção ao teto (para baixo no mundo), retornando ao teto. No chão normal, pulo continua igual.
4. **Chão no teto:** `isGrounded` fica `true` ao encostar no teto (checkers giram junto). `numberJump` reseta corretamente.
5. **Movimento X:** esquerda/direita continuam indo para os lados corretos do mundo; `Flip()` vira o sprite para o lado certo (check visual de cabeça para baixo).
6. **Dash:** dash funciona invertido e **mantém** a gravidade invertida após terminar (gravityScale restaurado para -3).
7. **Câmera:** a câmera não rotaciona; segue a posição normalmente.
8. **Respawn:** morrer com gravidade invertida → cena recarrega e o player volta com gravidade/rotação normais.
9. **Edge case Rewind:** rebobinar atravessando um ponto de inversão — observar se gravidade/rotação ficam consistentes (ponto de validação do item 3 da seção "Outras Alterações").
10. **Console limpo:** sem `NullReferenceException`/warnings novos.
11. **Regressão:** com nenhuma zona ativa (`isGravityInverted = false`), o player se comporta exatamente como antes.
