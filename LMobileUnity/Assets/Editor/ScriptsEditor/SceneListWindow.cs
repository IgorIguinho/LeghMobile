using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using System.Collections.Generic;
using System.IO;

/// <summary>
/// Janela de editor que lista todas as cenas (.unity) do projeto
/// e permite abrir qualquer uma delas com um clique.
/// Coloque este script dentro de uma pasta chamada "Editor" (ex: Assets/Editor/).
/// </summary>
public class SceneListWindow : EditorWindow
{
    private Vector2 scrollPos;
    private List<string> scenePaths = new List<string>();
    private string searchFilter = "";

    // Cria o item de menu para abrir a janela: Window > Scene List
    [MenuItem("Window/Scene List")]
    public static void ShowWindow()
    {
        SceneListWindow window = GetWindow<SceneListWindow>("Scene List");
        window.minSize = new Vector2(300, 400);
        window.RefreshSceneList();
    }

    private void OnEnable()
    {
        RefreshSceneList();
    }

    private void RefreshSceneList()
    {
        scenePaths.Clear();

        // Procura todos os assets do tipo Scene no projeto inteiro
        string[] guids = AssetDatabase.FindAssets("t:Scene");

        foreach (string guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            scenePaths.Add(path);
        }

        scenePaths.Sort();
    }

    private void OnGUI()
    {
        GUILayout.Space(5);

        // Barra superior: refresh + busca
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Atualizar Lista", GUILayout.Width(120)))
        {
            RefreshSceneList();
        }
        searchFilter = EditorGUILayout.TextField("Buscar", searchFilter);
        EditorGUILayout.EndHorizontal();

        GUILayout.Space(5);
        EditorGUILayout.LabelField($"Total de cenas: {scenePaths.Count}", EditorStyles.miniLabel);
        GUILayout.Space(5);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        foreach (string path in scenePaths)
        {
            string sceneName = Path.GetFileNameWithoutExtension(path);

            // Filtro de busca (ignora maiúsculas/minúsculas)
            if (!string.IsNullOrEmpty(searchFilter) &&
                sceneName.IndexOf(searchFilter, System.StringComparison.OrdinalIgnoreCase) < 0)
            {
                continue;
            }

            EditorGUILayout.BeginHorizontal("box");

            EditorGUILayout.LabelField(sceneName, GUILayout.MinWidth(100));

            if (GUILayout.Button("Abrir", GUILayout.Width(60)))
            {
                OpenScene(path);
            }

            // Botão extra: só seleciona o asset no Project window (opcional, útil)
            if (GUILayout.Button("Localizar", GUILayout.Width(70)))
            {
                Object obj = AssetDatabase.LoadAssetAtPath<Object>(path);
                EditorGUIUtility.PingObject(obj);
                Selection.activeObject = obj;
            }

            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();
    }

    private void OpenScene(string path)
    {
        // Pergunta se quer salvar a cena atual antes de trocar,
        // caso haja alterações não salvas.
        if (EditorSceneManager.GetActiveScene().isDirty)
        {
            bool save = EditorUtility.DisplayDialog(
                "Cena com alterações",
                "A cena atual tem alterações não salvas. Deseja salvar antes de trocar de cena?",
                "Salvar",
                "Descartar"
            );

            if (save)
            {
                EditorSceneManager.SaveOpenScenes();
            }
        }

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
    }
}