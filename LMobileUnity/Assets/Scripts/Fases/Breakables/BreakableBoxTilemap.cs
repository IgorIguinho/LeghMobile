using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Sistema de "caixas" quebráveis baseado em Tilemap.
/// Uma "caixa" é um cluster de tiles conectados (vizinhos) dentro do mesmo Tilemap.
/// Ao receber um golpe numa área, faz flood-fill a partir das células atingidas,
/// encontra o cluster inteiro e remove todos os seus tiles de uma vez (1 golpe destrói a caixa).
///
/// Otimizado para mobile:
/// - Coleções reutilizadas como campos (sem alocação por chamada).
/// - Flood-fill só roda quando uma caixa é realmente atingida.
/// - Um único CompositeCollider2D para todo o Tilemap (configurado na cena).
/// </summary>
[RequireComponent(typeof(Tilemap))]
public class BreakableBoxTilemap : MonoBehaviour
{
    [Tooltip("Se ativo, tiles em diagonal também são considerados parte da mesma caixa (8 vizinhos). Padrão: 4 vizinhos.")]
    [SerializeField] private bool includeDiagonals = false;

    private Tilemap _tilemap;
    private CompositeCollider2D _composite;

    // Coleções reutilizadas para minimizar alocação/GC (mobile).
    private readonly Stack<Vector3Int> _frontier = new Stack<Vector3Int>();
    private readonly HashSet<Vector3Int> _visited = new HashSet<Vector3Int>();
    private readonly List<Vector3Int> _cluster = new List<Vector3Int>();

    private static readonly Vector3Int[] Neighbors4 =
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
    };

    private static readonly Vector3Int[] Neighbors8 =
    {
        new Vector3Int(1, 0, 0),
        new Vector3Int(-1, 0, 0),
        new Vector3Int(0, 1, 0),
        new Vector3Int(0, -1, 0),
        new Vector3Int(1, 1, 0),
        new Vector3Int(1, -1, 0),
        new Vector3Int(-1, 1, 0),
        new Vector3Int(-1, -1, 0),
    };

    private void Awake()
    {
        _tilemap = GetComponent<Tilemap>();
        _composite = GetComponent<CompositeCollider2D>();
    }

    /// <summary>
    /// Tenta quebrar todas as caixas (clusters) que tenham ao menos um tile dentro da área informada.
    /// Retorna true se ao menos uma caixa foi destruída.
    /// </summary>
    public bool TryBreakInArea(Vector2 worldCenter, Vector2 worldSize)
    {
        if (_tilemap == null) return false;

        Vector2 half = worldSize * 0.5f;
        Vector2 min = worldCenter - half;
        Vector2 max = worldCenter + half;

        // Converte os cantos da área para células. WorldToCell pode não vir ordenado
        // dependendo do alinhamento do grid, então normalizamos min/max por eixo.
        Vector3Int cellA = _tilemap.WorldToCell(new Vector3(min.x, min.y, 0f));
        Vector3Int cellB = _tilemap.WorldToCell(new Vector3(max.x, max.y, 0f));

        int xMin = Mathf.Min(cellA.x, cellB.x);
        int xMax = Mathf.Max(cellA.x, cellB.x);
        int yMin = Mathf.Min(cellA.y, cellB.y);
        int yMax = Mathf.Max(cellA.y, cellB.y);
        int z = cellA.z;

        bool brokeAny = false;
        _visited.Clear();

        for (int x = xMin; x <= xMax; x++)
        {
            for (int y = yMin; y <= yMax; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, z);

                if (_visited.Contains(cell)) continue;
                if (!_tilemap.HasTile(cell)) continue;

                // Achou uma célula com tile dentro da área: quebra o cluster inteiro.
                if (BreakClusterAt(cell))
                {
                    brokeAny = true;
                }
            }
        }

        if (brokeAny)
        {
            // Garante a regeneração da geometria de colisão após remover os tiles.
            if (_composite != null)
            {
                _composite.GenerateGeometry();
            }
        }

        return brokeAny;
    }

    /// <summary>
    /// Flood-fill a partir de uma célula inicial, coletando todo o cluster conectado e removendo seus tiles.
    /// </summary>
    private bool BreakClusterAt(Vector3Int start)
    {
        Vector3Int[] neighbors = includeDiagonals ? Neighbors8 : Neighbors4;

        _frontier.Clear();
        _cluster.Clear();

        _frontier.Push(start);
        _visited.Add(start);

        while (_frontier.Count > 0)
        {
            Vector3Int current = _frontier.Pop();
            _cluster.Add(current);

            for (int i = 0; i < neighbors.Length; i++)
            {
                Vector3Int next = current + neighbors[i];

                if (_visited.Contains(next)) continue;
                if (!_tilemap.HasTile(next)) continue;

                _visited.Add(next);
                _frontier.Push(next);
            }
        }

        if (_cluster.Count == 0) return false;

        for (int i = 0; i < _cluster.Count; i++)
        {
            _tilemap.SetTile(_cluster[i], null);
        }

        return true;
    }
}
