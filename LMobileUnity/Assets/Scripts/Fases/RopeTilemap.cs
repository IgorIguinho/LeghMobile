using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[RequireComponent(typeof(Tilemap))]
public class RopeTilemap : MonoBehaviour
{
    [Serializable]
    public struct RopeColumnData
    {
        public int cellX;
        public float centerX;
        public float minY;
        public float maxY;
    }

    public static RopeTilemap Instance { get; private set; }

    [SerializeField] private RopeColumnData[] columns = Array.Empty<RopeColumnData>();
    private Tilemap tilemap;

    public RopeColumnData[] Columns => columns;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            // Se já houver uma instância ativa, atualiza para a mais recente desta cena
            Instance = this;
        }
        else
        {
            Instance = this;
        }

        tilemap = GetComponent<Tilemap>();
        BuildRopeColumns();
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    [ContextMenu("Rebuild Columns")]
    public void BuildRopeColumns()
    {
        if (tilemap == null)
            tilemap = GetComponent<Tilemap>();

        if (tilemap == null) return;

        Dictionary<int, List<int>> cellsByX = new Dictionary<int, List<int>>();

        foreach (Vector3Int pos in tilemap.cellBounds.allPositionsWithin)
        {
            if (tilemap.HasTile(pos))
            {
                if (!cellsByX.TryGetValue(pos.x, out List<int> yList))
                {
                    yList = new List<int>();
                    cellsByX[pos.x] = yList;
                }
                yList.Add(pos.y);
            }
        }

        List<RopeColumnData> list = new List<RopeColumnData>(cellsByX.Count);

        foreach (var kvp in cellsByX)
        {
            int cellX = kvp.Key;
            List<int> yList = kvp.Value;
            yList.Sort();

            int minCellY = yList[0];
            int maxCellY = yList[yList.Count - 1];

            // Posição de centro no eixo X
            Vector3 centerPos = tilemap.GetCellCenterWorld(new Vector3Int(cellX, minCellY, 0));
            // Limite inferior (borda de baixo do tile mais baixo)
            Vector3 minWorld = tilemap.CellToWorld(new Vector3Int(cellX, minCellY, 0));
            // Limite superior (topo do tile mais alto)
            Vector3 maxWorld = tilemap.CellToWorld(new Vector3Int(cellX, maxCellY, 0));
            float cellHeight = tilemap.cellSize.y;

            list.Add(new RopeColumnData
            {
                cellX = cellX,
                centerX = centerPos.x,
                minY = minWorld.y,
                maxY = maxWorld.y + cellHeight
            });
        }

        columns = list.ToArray();
    }

    /// <summary>
    /// Busca a corda mais próxima de um ponto do mundo, verificando tolerância no eixo X e Y.
    /// Operação O(N) com N pequeno (colunas de corda da fase), sem alocação no heap (Zero GC).
    /// </summary>
    public bool TryGetRopeAt(Vector2 worldPos, out RopeColumnData column, float toleranceX = 1.2f, float paddingY = 1.0f)
    {
        if (columns == null || columns.Length == 0)
        {
            BuildRopeColumns();
        }

        float bestDistX = float.MaxValue;
        int bestIndex = -1;

        for (int i = 0; i < columns.Length; i++)
        {
            ref readonly RopeColumnData col = ref columns[i];
            float distX = Mathf.Abs(worldPos.x - col.centerX);
            if (distX <= toleranceX)
            {
                if (worldPos.y >= (col.minY - paddingY) && worldPos.y <= (col.maxY + paddingY))
                {
                    if (distX < bestDistX)
                    {
                        bestDistX = distX;
                        bestIndex = i;
                    }
                }
            }
        }

        if (bestIndex >= 0)
        {
            column = columns[bestIndex];
            return true;
        }

        column = default;
        return false;
    }

    private void OnDrawGizmosSelected()
    {
        if (columns == null || columns.Length == 0)
        {
            if (tilemap == null) tilemap = GetComponent<Tilemap>();
            if (tilemap != null) BuildRopeColumns();
        }

        if (columns == null) return;

        Gizmos.color = Color.cyan;
        for (int i = 0; i < columns.Length; i++)
        {
            Vector3 bottom = new Vector3(columns[i].centerX, columns[i].minY, 0f);
            Vector3 top = new Vector3(columns[i].centerX, columns[i].maxY, 0f);
            Gizmos.DrawLine(bottom, top);

            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(bottom, 0.2f);
            Gizmos.color = Color.cyan;
        }
    }
}
