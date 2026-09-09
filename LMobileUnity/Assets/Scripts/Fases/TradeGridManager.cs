using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Orquestra N TradeGrids (cada um com uma lista de Tilemaps) dispostos em sequência
/// ao longo do eixo X. Mesma arquitetura do ParallaxManager: máquina de estados
/// implícita baseada em GridBoundary, com crossfade de alpha na janela de transição.
///
///   Grid A  -->  Transição(A, B)  -->  Grid B  -->  Transição(B, C)  --> ...
///
/// Grids fora do alcance do player são desativados (SetGridActive(false)) para
/// economizar CPU no mobile. Dentro da janela de cada GridBoundary, o alpha dos
/// Tilemaps é interpolado via Tilemap.color.
/// </summary>
public class TradeGridManager : MonoBehaviour
{
    [Tooltip("Grids na ordem em que aparecem no mundo, da esquerda para a direita.")]
    public TradeGrid[] grids;

    [Tooltip("Um item para cada par de grids consecutivos: boundaries.Length deve ser grids.Length - 1.")]
    public GridBoundary[] boundaries;

    public Transform player;

    void Awake()
    {
        // Sincroniza o estado interno de cada grid com o estado real dos GameObjects
        // na cena (permite deixar alguns grids desativados no editor por padrão).
        for (int i = 0; i < grids.Length; i++)
        {
            grids[i].SyncInitialState();
        }
    }

    void Update()
    {
        if (player == null || grids.Length == 0) return;

        float playerX = player.position.x;

        int gridA = 0;
        int gridB = -1;
        float t = 0f;

        // Percorre os limites em ordem. Poucos grids => custo desprezível, mesmo em mobile.
        for (int i = 0; i < boundaries.Length; i++)
        {
            var b = boundaries[i];
            float half = b.width * 0.5f;
            float min = b.centerX - half;
            float max = b.centerX + half;

            if (playerX < min)
            {
                gridA = i;
                gridB = -1;
                break;
            }

            if (playerX <= max)
            {
                gridA = i;
                gridB = i + 1;
                t = Mathf.InverseLerp(min, max, playerX);
                break;
            }

            // Já passou completamente este limite: segue avaliando o próximo par.
            gridA = i + 1;
            gridB = -1;
        }

        ApplyState(gridA, gridB, t);
    }

    private void ApplyState(int gridA, int gridB, float t)
    {
        bool transitioning = gridB >= 0;

        for (int i = 0; i < grids.Length; i++)
        {
            bool isCurrent = i == gridA;
            bool isNext = i == gridB;

            // Mantém "quentes" (ativos) o grid atual, o próximo (se em transição)
            // e os vizinhos imediatos, para reativação instantânea sem soluço.
            bool keepWarm = isCurrent || isNext
                || Mathf.Abs(i - gridA) <= 1
                || (gridB >= 0 && Mathf.Abs(i - gridB) <= 1);

            if (keepWarm != grids[i].IsActive)
                grids[i].SetGridActive(keepWarm);

            if (!keepWarm) continue;

            if (isCurrent) grids[i].SetAlpha(transitioning ? 1f - t : 1f);
            else if (isNext) grids[i].SetAlpha(t);
            else grids[i].SetAlpha(0f); // vizinho "quente" mas fora da faixa visível
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

            Gizmos.color = Color.cyan;
            Gizmos.DrawLine(top, bottom);

            Gizmos.color = new Color(0f, 1f, 1f, 0.15f);
            Gizmos.DrawCube(new Vector3(b.centerX, transform.position.y, 0), new Vector3(b.width, 20f, 0.1f));
        }
    }
#endif
}

/// <summary>
/// Um grupo de Tilemaps que forma uma "zona" do TradeGrid. Todos os Tilemaps da
/// lista recebem o mesmo alpha e são ativados/desativados juntos.
/// </summary>
[System.Serializable]
public class TradeGrid
{
    [Tooltip("Tilemaps que pertencem a este grid. Todos recebem o mesmo alpha e o mesmo estado ativo/inativo.")]
    public List<Tilemap> tilemaps = new List<Tilemap>();

    private bool isActive = true;

    public bool IsActive => isActive;

    /// <summary>
    /// Lê o estado real do primeiro Tilemap da lista para inicializar o cache interno.
    /// Chamado uma vez no Awake do TradeGridManager.
    /// </summary>
    public void SyncInitialState()
    {
        isActive = tilemaps.Count > 0 && tilemaps[0] != null && tilemaps[0].gameObject.activeSelf;
    }

    public void SetGridActive(bool active)
    {
        if (isActive == active) return;
        isActive = active;

        for (int i = 0; i < tilemaps.Count; i++)
        {
            if (tilemaps[i] == null) continue;
            tilemaps[i].gameObject.SetActive(active);
        }
    }

    public void SetAlpha(float alpha)
    {
        for (int i = 0; i < tilemaps.Count; i++)
        {
            var tm = tilemaps[i];
            if (tm == null) continue;

            Color c = tm.color;
            c.a = alpha;
            tm.color = c;
        }
    }
}

[System.Serializable]
public class GridBoundary
{
    [Tooltip("Posição X (mundo) do centro da transição entre dois grids consecutivos.")]
    public float centerX;

    [Tooltip("Largura da faixa de crossfade. Maior = transição mais longa e suave.")]
    public float width = 4f;
}