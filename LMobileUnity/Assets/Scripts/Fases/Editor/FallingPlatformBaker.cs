using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

/// <summary>
/// Editor tool (not included in build) that converts the source Tilemap
/// "Grid/FallPlataform" into individual falling-platform GameObjects, one per
/// connected cluster of tiles (4-connectivity flood fill). Zero runtime cost:
/// all generation happens in the Editor.
/// </summary>
public static class FallingPlatformBaker
{
    private const string SourcePath = "Grid/FallPlataform";
    private const string PlatformPrefix = "FallingPlatform_";

    [MenuItem("Tools/Fase2/Bake Falling Platforms")]
    public static void Bake()
    {
        Tilemap source = FindSourceTilemap(out GameObject gridGo);
        if (source == null)
        {
            EditorUtility.DisplayDialog("Bake Falling Platforms",
                $"Não encontrei o Tilemap fonte '{SourcePath}' na cena ativa.", "OK");
            return;
        }

        // Re-bake idempotente: remove plataformas anteriores primeiro.
        ClearInternal(gridGo, reEnableSource: false, source: source);

        TilemapRenderer srcRenderer = source.GetComponent<TilemapRenderer>();
        int groundLayer = LayerMask.NameToLayer("Ground");
        int playerLayer = LayerMask.NameToLayer("Player");

        List<List<Vector3Int>> clusters = FindClusters(source);
        if (clusters.Count == 0)
        {
            EditorUtility.DisplayDialog("Bake Falling Platforms",
                "Nenhum tile ocupado encontrado no Tilemap fonte.", "OK");
            return;
        }

        int created = 0;
        for (int i = 0; i < clusters.Count; i++)
        {
            CreatePlatform(clusters[i], source, srcRenderer, gridGo.transform,
                groundLayer, playerLayer, i);
            created++;
        }

        // Desabilita o renderer do fonte (mantém os dados de tiles para re-bake).
        if (srcRenderer != null)
        {
            Undo.RecordObject(srcRenderer, "Disable Source Renderer");
            srcRenderer.enabled = false;
            EditorUtility.SetDirty(srcRenderer);
        }

        EditorSceneManager.MarkSceneDirty(source.gameObject.scene);
        Debug.Log($"[FallingPlatformBaker] {created} plataforma(s) gerada(s) a partir de '{SourcePath}'.");
    }

    [MenuItem("Tools/Fase2/Clear Baked Platforms")]
    public static void Clear()
    {
        Tilemap source = FindSourceTilemap(out GameObject gridGo);
        if (gridGo == null)
        {
            EditorUtility.DisplayDialog("Clear Baked Platforms",
                $"Não encontrei o objeto 'Grid' / '{SourcePath}' na cena ativa.", "OK");
            return;
        }

        int removed = ClearInternal(gridGo, reEnableSource: true, source: source);
        EditorSceneManager.MarkSceneDirty(gridGo.scene);
        Debug.Log($"[FallingPlatformBaker] {removed} plataforma(s) baked removida(s).");
    }

    // ---- Localização --------------------------------------------------------

    private static Tilemap FindSourceTilemap(out GameObject gridGo)
    {
        gridGo = null;
        GameObject sourceGo = null;

        Scene active = SceneManager.GetActiveScene();
        foreach (GameObject root in active.GetRootGameObjects())
        {
            Transform grid = root.name == "Grid" ? root.transform : root.transform.Find("Grid");
            if (grid == null && root.name == "Grid") grid = root.transform;
            if (grid != null)
            {
                Transform fp = grid.Find("FallPlataform");
                if (fp != null)
                {
                    gridGo = grid.gameObject;
                    sourceGo = fp.gameObject;
                    break;
                }
            }
        }

        return sourceGo != null ? sourceGo.GetComponent<Tilemap>() : null;
    }

    // ---- Flood fill (4-conectividade) --------------------------------------

    private static List<List<Vector3Int>> FindClusters(Tilemap source)
    {
        var clusters = new List<List<Vector3Int>>();
        var visited = new HashSet<Vector3Int>();
        BoundsInt bounds = source.cellBounds;

        var occupied = new HashSet<Vector3Int>();
        foreach (Vector3Int pos in bounds.allPositionsWithin)
            if (source.HasTile(pos))
                occupied.Add(pos);

        var neighbors = new Vector3Int[]
        {
            new Vector3Int(1, 0, 0), new Vector3Int(-1, 0, 0),
            new Vector3Int(0, 1, 0), new Vector3Int(0, -1, 0)
        };

        foreach (Vector3Int start in occupied)
        {
            if (visited.Contains(start)) continue;

            var cluster = new List<Vector3Int>();
            var stack = new Stack<Vector3Int>();
            stack.Push(start);
            visited.Add(start);

            while (stack.Count > 0)
            {
                Vector3Int cell = stack.Pop();
                cluster.Add(cell);

                foreach (Vector3Int n in neighbors)
                {
                    Vector3Int next = cell + n;
                    if (occupied.Contains(next) && !visited.Contains(next))
                    {
                        visited.Add(next);
                        stack.Push(next);
                    }
                }
            }

            clusters.Add(cluster);
        }

        return clusters;
    }

    // ---- Criação de plataforma ---------------------------------------------

    private static void CreatePlatform(List<Vector3Int> cluster, Tilemap source,
        TilemapRenderer srcRenderer, Transform gridParent, int groundLayer,
        int playerLayer, int index)
    {
        // Bounding box em células.
        Vector3Int minCell = cluster[0];
        Vector3Int maxCell = cluster[0];
        foreach (Vector3Int c in cluster)
        {
            minCell = Vector3Int.Min(minCell, c);
            maxCell = Vector3Int.Max(maxCell, c);
        }

        // Célula âncora (inteira) próxima do centro — garante alinhamento exato
        // dos tiles após mover o transform.
        Vector3Int anchorCell = new Vector3Int(
            Mathf.RoundToInt((minCell.x + maxCell.x) * 0.5f),
            Mathf.RoundToInt((minCell.y + maxCell.y) * 0.5f),
            0);

        // GameObject raiz da plataforma.
        var go = new GameObject(PlatformPrefix + index);
        Undo.RegisterCreatedObjectUndo(go, "Create Falling Platform");
        go.transform.SetParent(gridParent, false);
        go.transform.position = source.CellToWorld(anchorCell);
        if (groundLayer >= 0) go.layer = groundLayer;

        // Tilemap + Renderer.
        var tilemap = go.AddComponent<Tilemap>();
        var renderer = go.AddComponent<TilemapRenderer>();
        tilemap.tileAnchor = source.tileAnchor;
        tilemap.orientation = source.orientation;
        if (srcRenderer != null)
        {
            renderer.sharedMaterial = srcRenderer.sharedMaterial;
            renderer.sortingLayerID = srcRenderer.sortingLayerID;
            renderer.sortingOrder = srcRenderer.sortingOrder;
        }

        // Copia os tiles, deslocados pela célula âncora.
        foreach (Vector3Int c in cluster)
        {
            Vector3Int local = c - anchorCell;
            tilemap.SetTile(local, source.GetTile(c));
            tilemap.SetTransformMatrix(local, source.GetTransformMatrix(c));
            tilemap.SetColor(local, source.GetColor(c));
        }
        tilemap.RefreshAllTiles();

        // Física: Rigidbody2D Kinematic + Composite collider sólido.
        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.simulated = true;
        rb.constraints = RigidbodyConstraints2D.FreezePositionX | RigidbodyConstraints2D.FreezeRotation;

        var composite = go.AddComponent<CompositeCollider2D>();
        composite.geometryType = CompositeCollider2D.GeometryType.Polygons; // sólido

        var tmCollider = go.AddComponent<TilemapCollider2D>();
        tmCollider.compositeOperation = Collider2D.CompositeOperation.Merge;

        // Componente runtime + áreas de detecção pré-calculadas dos bounds.
        var platform = go.AddComponent<FallingTilePlatform>();

        Vector3 worldMin = source.CellToWorld(minCell);
        Vector3 worldMax = source.CellToWorld(maxCell + new Vector3Int(1, 1, 0));
        Vector2 centerWorld = (worldMin + worldMax) * 0.5f;
        float width = worldMax.x - worldMin.x;
        float height = worldMax.y - worldMin.y;
        Vector2 delta = centerWorld - (Vector2)go.transform.position; // ajuste sub-célula

        // Caixa "passar por baixo": ponta esquerda, logo abaixo do fundo.
        Vector2 underSize = new Vector2(Mathf.Min(2f, width), 1.5f);
        Vector2 underOffset = delta + new Vector2(
            -width * 0.5f + underSize.x * 0.5f,
            -height * 0.5f - underSize.y * 0.5f + 0.1f);

        // Caixa "pisar em cima": largura total, logo acima do topo.
        Vector2 stepSize = new Vector2(width, 0.5f);
        Vector2 stepOffset = delta + new Vector2(0f, height * 0.5f + stepSize.y * 0.5f - 0.1f);

        platform.underBoxOffset = underOffset;
        platform.underBoxSize = underSize;
        platform.stepBoxOffset = stepOffset;
        platform.stepBoxSize = stepSize;
        platform.crushBoxOffsetExtra = delta; // mantém a caixa de esmagamento centrada

        if (playerLayer >= 0) platform.playerMask = 1 << playerLayer;
        if (groundLayer >= 0) platform.groundLayer = 1 << groundLayer;

        EditorUtility.SetDirty(go);
    }

    // ---- Limpeza ------------------------------------------------------------

    private static int ClearInternal(GameObject gridGo, bool reEnableSource, Tilemap source)
    {
        int removed = 0;
        if (gridGo != null)
        {
            var toRemove = new List<GameObject>();
            foreach (Transform child in gridGo.transform)
                if (child.name.StartsWith(PlatformPrefix))
                    toRemove.Add(child.gameObject);

            foreach (GameObject go in toRemove)
            {
                Undo.DestroyObjectImmediate(go);
                removed++;
            }
        }

        if (reEnableSource && source != null)
        {
            TilemapRenderer srcRenderer = source.GetComponent<TilemapRenderer>();
            if (srcRenderer != null && !srcRenderer.enabled)
            {
                Undo.RecordObject(srcRenderer, "Re-enable Source Renderer");
                srcRenderer.enabled = true;
                EditorUtility.SetDirty(srcRenderer);
            }
        }

        return removed;
    }
}
