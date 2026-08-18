using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

public class TradeSpriteTileMap : EditorWindow
{
    public Tilemap tileMap;
    public TileBase oldTile;
    public TileBase newTile;

    [MenuItem("Tools/Trade Sprite Tile Map")]
    public static void ShowWindow()
    {
        GetWindow<TradeSpriteTileMap>("Trade Sprite Tile Map");
    }

    public void OnGUI()
    {
        GUILayout.Label("Trade Sprite Tile Map", EditorStyles.boldLabel);

        tileMap = (Tilemap)EditorGUILayout.ObjectField("Tilemap", tileMap, typeof(Tilemap), true);

        GUILayout.Space(10);

        GUILayout.BeginHorizontal();

        GUILayout.BeginVertical();
        GUILayout.Label("Old Tile", EditorStyles.centeredGreyMiniLabel);
        oldTile = (TileBase)EditorGUILayout.ObjectField(oldTile, typeof(TileBase), false, GUILayout.Width(64), GUILayout.Height(64));
        GUILayout.EndVertical();

        GUILayout.FlexibleSpace();

        GUILayout.BeginVertical();
        GUILayout.Label("New Tile", EditorStyles.centeredGreyMiniLabel);
        newTile = (TileBase)EditorGUILayout.ObjectField(newTile, typeof(TileBase), false, GUILayout.Width(64), GUILayout.Height(64));
        GUILayout.EndVertical();

        GUILayout.EndHorizontal();

        GUILayout.Space(10);

        if (tileMap != null)
        {
            if (GUILayout.Button("Trade Sprites"))
            {
                TradeTiles();
            }
        }

        GUILayout.Space(15);
        EditorGUILayout.HelpBox("Use o botão abaixo para trocar automaticamente todos os tiles com sprite nulo ou ausente (aqueles rosa/magenta) pelo 'New Tile' definido acima. Não precisa preencher o 'Old Tile' para isso.", MessageType.Info);

        if (tileMap != null && newTile != null)
        {
            if (GUILayout.Button("Fix Missing/Null Sprites"))
            {
                TradeMissingSprites();
            }
        }
    }

    void TradeTiles()
    {
        if (tileMap == null || oldTile == null || newTile == null)
        {
            Debug.LogWarning("Please assign the Tilemap, Old Tile, and New Tile.");
            return;
        }
        tileMap.SwapTile(oldTile, newTile);
    }

    void TradeMissingSprites()
    {
        if (tileMap == null || newTile == null)
        {
            Debug.LogWarning("Please assign the Tilemap and New Tile.");
            return;
        }

        BoundsInt bounds = tileMap.cellBounds;
        TileBase[] allTiles = tileMap.GetTilesBlock(bounds);

        int replaced = 0;
        for (int i = 0; i < allTiles.Length; i++)
        {
            TileBase t = allTiles[i];

            // Célula vazia (sem tile nenhum) - ignora
            if (t == null)
                continue;

            bool isMissing = false;

            // Tile padrão do Unity (Tile) expõe a propriedade sprite
            if (t is Tile tile)
            {
                // Graças ao operador == sobrecarregado do Unity,
                // isso é true tanto para sprite nunca atribuído
                // quanto para referência quebrada (sprite deletado/movido)
                if (tile.sprite == null)
                {
                    isMissing = true;
                }
            }

            if (isMissing)
            {
                allTiles[i] = newTile;
                replaced++;
            }
        }

        tileMap.SetTilesBlock(bounds, allTiles);
        Debug.Log($"[TradeSpriteTileMap] {replaced} tile(s) com sprite ausente foram substituídos.");
    }
}