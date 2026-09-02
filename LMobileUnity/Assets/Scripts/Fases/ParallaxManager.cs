using UnityEngine;

/// <summary>
/// Orquestra N zonas de ParallaxScrollingBG dispostas em sequência ao longo do eixo X.
///
/// Máquina de estados simples e implícita:
///   Zona A  -->  Transição(A, B)  -->  Zona B  -->  Transição(B, C)  --> ...
///
/// Não empurra nem trava posição de nada (chega de lockX/UnlockAndSync/NudgeX e de
/// heurística de "ponto médio"). Dentro da janela de cada ZoneBoundary, faz um
/// crossfade de alpha entre a zona atual e a próxima. Zonas fora do alcance do
/// player são desativadas (SetZoneActive(false)) para economizar CPU no mobile.
/// </summary>
public class ParallaxManager : MonoBehaviour
{
    [Tooltip("Zonas na ordem em que aparecem no mundo, da esquerda para a direita.")]
    public ParallaxScrollingBG[] zones;

    [Tooltip("Um item para cada par de zonas consecutivas: boundaries.Length deve ser zones.Length - 1.")]
    public ZoneBoundary[] boundaries;

    public Transform player;

    void Update()
    {
        if (player == null || zones.Length == 0) return;

        float playerX = player.position.x;

        int zoneA = 0;
        int zoneB = -1;
        float t = 0f;

        // Percorre os limites em ordem. Poucas zonas => custo desprezível, mesmo em mobile.
        for (int i = 0; i < boundaries.Length; i++)
        {
            var b = boundaries[i];
            float half = b.width * 0.5f;
            float min = b.centerX - half;
            float max = b.centerX + half;

            if (playerX < min)
            {
                zoneA = i;
                zoneB = -1;
                break;
            }

            if (playerX <= max)
            {
                zoneA = i;
                zoneB = i + 1;
                t = Mathf.InverseLerp(min, max, playerX);
                break;
            }

            // Já passou completamente este limite: segue avaliando o próximo par.
            zoneA = i + 1;
            zoneB = -1;
        }

        ApplyState(zoneA, zoneB, t);
    }

    private void ApplyState(int zoneA, int zoneB, float t)
    {
        bool transitioning = zoneB >= 0;

        for (int i = 0; i < zones.Length; i++)
        {
            bool isCurrent = i == zoneA;
            bool isNext = i == zoneB;

            // Mantém "quentes" (ativas) a zona atual, a próxima (se em transição)
            // e as vizinhas imediatas, para reativação instantânea sem soluço.
            bool keepWarm = isCurrent || isNext
                || Mathf.Abs(i - zoneA) <= 1
                || (zoneB >= 0 && Mathf.Abs(i - zoneB) <= 1);

            if (keepWarm != zones[i].enabled)
                zones[i].SetZoneActive(keepWarm);

            if (!keepWarm) continue;

            if (isCurrent) zones[i].SetAlpha(transitioning ? 1f - t : 1f);
            else if (isNext) zones[i].SetAlpha(t);
            else zones[i].SetAlpha(0f); // vizinha "quente" mas fora da faixa visível
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmosSelected()
    {
        if (boundaries == null) return;

        foreach (var b in boundaries)
        {
            Vector3 top = new Vector3(b.centerX, transform.position.y + 10f, 0);
            Vector3 bottom = new Vector3(b.centerX, transform.position.y - 10f, 0);

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(top, bottom);

            Gizmos.color = new Color(1f, 1f, 0f, 0.15f);
            Gizmos.DrawCube(new Vector3(b.centerX, transform.position.y, 0), new Vector3(b.width, 20f, 0.1f));
        }
    }
#endif
}

[System.Serializable]
public class ZoneBoundary
{
    [Tooltip("Posição X (mundo) do centro da transição entre duas zonas consecutivas.")]
    public float centerX;

    [Tooltip("Largura da faixa de crossfade. Maior = transição mais longa e suave.")]
    public float width = 4f;
}