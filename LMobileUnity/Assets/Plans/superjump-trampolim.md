# Project Overview

- **Game Title:** LeghMobile (LMobileUnity)
- **High-Level Concept:** Plataforma 2D mobile com mecânicas de desafio baseadas em terreno (zonas de gatilho sobre tilemaps) que alteram o comportamento do player ao passar por cima.
- **Players:** Single player
- **Inspiration / Reference Games:** Plataformas 2D com gimmicks de terreno (trampolins, zonas de velocidade, gravidade invertida)
- **Tone / Art Direction:** (existente no projeto — não alterado por este plano)
- **Target Platform:** WebGL
- **Screen Orientation / Resolution:** (existente — não alterado)
- **Render Pipeline:** Built-in
- **Unity Version:** 6000.4.7f1
- **Input System:** New Input System

> **Escopo deste plano:** SOMENTE o sistema **SuperJump** (trampolim). Os outros 3 sistemas (`InvertGravity`, `PlusSpeed`, `DontJump`) NÃO fazem parte deste plano, apenas definimos a convenção de nome para mantê-los consistentes depois.

---

# Game Mechanics

## Core Gameplay Loop (recorte deste sistema)
O player se move pela fase. Ao passar por cima dos tiles do tilemap **SuperJump**, ele é arremessado para cima como um trampolim, ganhando altura. O boost vertical persiste de forma natural (a gravidade desacelera o player até o ápice). O único jeito de cancelar/zerar esse impulso é usar o **Dash()** (que já zera a velocidade no `PlayerMovements`).

## Controls and Input Methods
- O sistema SuperJump é **automático por contato** — não consome nenhum input novo.
- O `Dash` (input já existente) continua sendo a única ação que zera o impulso do trampolim, por consequência natural do código atual do `PlayerMovements.Dash()`.

---

# UI
Nenhuma mudança de UI é necessária para o sistema SuperJump.

---

# Decisões de Design (confirmadas)
1. **Nome da família de scripts:** sufixo **`Zone`** → este: `SuperJumpZone`. Os futuros seguirão o mesmo padrão: `InvertGravityZone`, `PlusSpeedZone`, `DontJumpZone`.
2. **Disparo:** `OnTriggerEnter2D` — impulsiona **uma vez por entrada**; só repete se o player sair da área e entrar de novo.
3. **Ajuste de altura:** por **altura real em unidades** no Inspector. O script calcula a velocidade necessária pela física, garantindo um ápice consistente independente da velocidade de queda.
4. **PlayerMovements:** **não será modificado** (ver seção dedicada abaixo com a justificativa da análise).

---

# Key Asset & Context

## Estado atual relevante (verificado)
- **GameObject `Grid/SuperJump`** (cena `Fase5`): possui `Tilemap`, `TilemapRenderer`, `Rigidbody2D`, `TilemapCollider2D` e `CompositeCollider2D`. O collider já está com **`isTrigger = true`**. Infraestrutura de gatilho pronta — só falta o script de comportamento.
- **Convenção da pasta `Assets/Scripts/Fases/Desafios/`:** scripts simples de `MonoBehaviour` que usam `OnTriggerEnter2D` e checam `collision.CompareTag("Player")`. Referência: `SwitchSpeed.cs` (usado no GameObject `PlusSpeed`), além de `SpikeDmg.cs` e `DoorTeleport.cs`.
- **`PlayerMovements.cs`** (`Assets/Scripts/Player/PlayerMovements.cs`):
  - `Moviment()` sempre **preserva** `rb.linearVelocity.y` (linhas ~121/124/126) → não zera o impulso vertical.
  - `Jump()` só faz `rb.linearVelocity = Vector2.zero` quando há input E (`isGrounded` | `isWall` | `isRope`) → não interfere no boost em pleno ar.
  - `Dash()` faz `rb.linearVelocity = Vector2.zero` → **é exatamente a exceção desejada** (única coisa que zera o impulso).
  - O Rigidbody2D do player tem `gravityScale` (usado em `Dash`), necessário para o cálculo da altura.

## Script a ser criado
**`Assets/Scripts/Fases/Desafios/SuperJumpZone.cs`**

Campos expostos no Inspector:
- `[SerializeField] private float jumpHeight;` — altura desejada do pulo, em unidades de mundo (ajustável no Inspector). Tooltip explicando que é a altura aproximada do ápice.
- (Opcional) `[SerializeField] private string playerTag = "Player";` para manter flexível.

Lógica (resumo / pseudo-assinatura):
```csharp
using UnityEngine;

public class SuperJumpZone : MonoBehaviour
{
    [Header("SuperJump (Trampolim)")]
    [Tooltip("Altura aproximada (em unidades) que o player atinge ao tocar o trampolim.")]
    [SerializeField] private float jumpHeight = 5f;

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;

        Rigidbody2D rb = collision.attachedRigidbody;          // rb do player
        if (rb == null) return;

        // g efetivo do player (gravidade global * gravityScale do rb)
        float g = Mathf.Abs(Physics2D.gravity.y) * rb.gravityScale;
        if (g <= 0f) return;                                   // evita divisão por zero / dash em andamento

        // v = sqrt(2 * g * h)  -> velocidade para atingir a altura desejada
        float launchVelocity = Mathf.Sqrt(2f * g * jumpHeight);

        // Define o Y diretamente (sobrescreve a queda) e PRESERVA o X.
        // Não usamos Vector2.zero em nenhum momento -> o impulso não é zerado aqui.
        rb.linearVelocity = new Vector2(rb.linearVelocity.x, launchVelocity);
    }
}
```

Pontos de design importantes:
- **Por que setar a velocidade direto (e não `AddForce`)?** Garante a altura definida no Inspector independentemente da velocidade de queda no momento do contato (ápice consistente). Preserva o `x` para não cortar o movimento horizontal.
- **Por que `Mathf.Sqrt(2*g*h)`?** Fórmula de cinemática (energia): a velocidade inicial vertical para atingir altura `h` sob gravidade `g`. Como usamos `gravityScale` do player, a altura sai correta mesmo se a gravidade do projeto mudar.
- **Regra "não zerar exceto pelo Dash":** o script nunca chama `Vector2.zero`; apenas escreve o `y`. O resto do `PlayerMovements` preserva o `y` (ver análise). O `Dash()` continua sendo o único ponto que zera tudo — comportamento desejado preservado sem nenhuma alteração extra.
- **Guarda `g <= 0f`:** durante o `Dash`, o `PlayerMovements` zera o `gravityScale` temporariamente. Se o player entrar no trampolim no meio de um dash, o impulso é ignorado (coerente com "só o Dash controla nesse momento").

---

# Modificações no PlayerMovements (para sua análise)

**Conclusão: NENHUMA modificação é necessária.** Esta seção existe apenas para você validar o raciocínio, conforme pedido.

Análise de cada ponto do `PlayerMovements` que toca em `rb.linearVelocity`:

| Método | O que faz com a velocidade | Afeta o impulso do trampolim? |
|--------|----------------------------|-------------------------------|
| `Moviment()` (FixedUpdate) | `rb.linearVelocity = new Vector2(speed * dir, rb.linearVelocity.y)` em todas as ramificações | **Não.** Sempre preserva o `y`. |
| `Jump()` | `rb.linearVelocity = Vector2.zero` + `AddForce` | **Não interfere no boost.** Só ocorre com input E (`isGrounded`/`isWall`/`isRope`). Em pleno ar sem essas condições, não faz nada. |
| `Dash()` | `rb.linearVelocity = Vector2.zero` | **Sim — e é o desejado.** É a única exceção autorizada para zerar o impulso. |
| `WallFall()` | seta `y = -wallFallForce` se `isWall` e caindo rápido | Só em colisão com parede (caso de borda, não relacionado ao trampolim em piso aberto). |
| `InRope()` | seta `y = -ropeFall` se `isRope` | Só quando na corda (não relacionado). |

Portanto, o requisito **"a velocidade do impulso não pode ser zerada, exceto pelo `Dash()`"** já é satisfeito pelo código atual + a forma como o `SuperJumpZone` aplica a velocidade. Nada precisa ser editado em `PlayerMovements.cs`.

> Se no teste real você observar algum caso de borda zerando o impulso (ex.: trampolim encostado numa parede acionando `WallFall`), aí sim avaliaríamos uma flag de proteção. Isso fica como contingência, fora do escopo atual.

---

# Implementation Steps

### Step 1 — Criar o script `SuperJumpZone.cs`
- **Description:** Criar o arquivo `Assets/Scripts/Fases/Desafios/SuperJumpZone.cs` com a classe `SuperJumpZone : MonoBehaviour`, campo `jumpHeight` (Inspector), e `OnTriggerEnter2D` que calcula `v = sqrt(2*g*h)` e aplica `rb.linearVelocity = new Vector2(rb.linearVelocity.x, v)`, preservando o X e incluindo as guardas (`CompareTag`, `attachedRigidbody != null`, `g > 0`).
- **Assigned role:** developer
- **Dependencies:** None
- **Parallelizable:** No (é a base de tudo)

### Step 2 — Anexar o componente ao GameObject `SuperJump`
- **Description:** Adicionar o componente `SuperJumpZone` ao GameObject `Grid/SuperJump` na cena `Fase5`. Confirmar/definir `jumpHeight` no Inspector (valor inicial sugerido: 5). Garantir que o `CompositeCollider2D`/`TilemapCollider2D` permanece com `isTrigger = true` (já está).
- **Assigned role:** developer
- **Dependencies:** Depends on Step 1
- **Parallelizable:** No

### Step 3 — Garantir que o Player atende às condições do gatilho
- **Description:** Verificar que o GameObject do player está com a tag `Player` (confirmado) e possui `Rigidbody2D` + `Collider2D` (confirmado: `BoxCollider2D` + `Rigidbody2D`). Nenhuma mudança esperada; apenas validação.
- **Assigned role:** developer
- **Dependencies:** Depends on Step 2
- **Parallelizable:** No

---

# Verification & Testing

1. **Funcional básico:** Entrar em Play na `Fase5`, mover o player por cima dos tiles do `SuperJump` → o player deve ser arremessado para cima.
2. **Ajuste de altura:** Alterar `jumpHeight` no Inspector (ex.: 3, 6, 10) e confirmar que o ápice atingido corresponde aproximadamente ao valor (medir contra a grade/tilemap). Espera-se ápice consistente mesmo chegando em queda rápida.
3. **Preservação do X:** Pular no trampolim enquanto se move horizontalmente → o movimento horizontal não deve travar (X preservado).
4. **Regra do Dash:** Acionar o trampolim e, durante a subida, executar o `Dash` → a velocidade do impulso deve ser cancelada (comportamento esperado). Sem usar Dash, nenhum outro evento deve zerar a subida.
5. **Re-trigger:** Sair completamente da área e voltar → o trampolim deve disparar de novo. Permanecer parado dentro sem sair NÃO deve disparar repetidamente (validação do `OnTriggerEnter2D`).
6. **Caso de borda (Dash em andamento):** Entrar no trampolim no meio de um Dash (gravityScale = 0) → impulso é ignorado, sem erros no Console.
7. **Console limpo:** Sem `NullReferenceException` nem warnings novos relacionados ao `SuperJumpZone`.
