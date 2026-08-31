using UnityEditor;
using UnityEngine;
using UnityEditor.SceneManagement;
using UnityEngine.Tilemaps;

public class SpriteMaterialUpdater : Editor
{
    [MenuItem("Tools/Update Sprite Materials to URP Lit")]
    public static void UpdateMaterials()
    {
        // Carrega o material Lit do URP
        Material litMaterial = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat");

        if (litMaterial == null)
        {
            Debug.LogError("Material Sprite-Lit-Default não encontrado! Verifique se o URP está instalado.");
            return;
        }

        // Encontra todas as cenas na pasta Assets/Scenes
        string[] sceneGuids = AssetDatabase.FindAssets("t:Scene", new[] { "Assets/Scenes" });

        foreach (string guid in sceneGuids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var scene = EditorSceneManager.OpenScene(path);

            //SpriteRenderer[] sprites = Object.FindObjectsByType<SpriteRenderer>(FindObjectsSortMode.None);
            TilemapRenderer[] sprites = Object.FindObjectsByType<TilemapRenderer>(FindObjectsSortMode.None);
            int count = 0;

            foreach (var sprite in sprites)
            {
                // Troca apenas se for o material padrão antigo
                if (sprite.sharedMaterial == null || sprite.sharedMaterial.name == "Sprites-Default")
                {
                    sprite.sharedMaterial = litMaterial;
                    count++;
                }
            }

            if (count > 0)
            {
                EditorSceneManager.SaveScene(scene);
                Debug.Log($"Atualizados {count} sprites na cena: {path}");
            }
        }
        Debug.Log("Processo de atualização concluído em todas as cenas!");
    }
}