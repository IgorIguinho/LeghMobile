using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

public class ImportCSV : EditorWindow
{
    [SerializeField]
    public List<TextAsset> fileCSV = new List<TextAsset>();
    
    public string csvSourcePath = "Assets";
    public string savePath = "Assets";
    
    private string statusLog = "Ready";
    private Vector2 scrollPos;

    [MenuItem("Tools/Dialogue CSV Importer")]
    public static void ShowWindow()
    {
        GetWindow<ImportCSV>("Dialogue CSV Importer");
    }

    public void LoadCSVsFromFolder()
    {
        fileCSV.Clear();
        if (!Directory.Exists(csvSourcePath))
        {
            statusLog = $"Error: Source folder '{csvSourcePath}' does not exist.";
            return;
        }

        string[] files = Directory.GetFiles(csvSourcePath, "*.csv", SearchOption.AllDirectories);
        foreach (string filePath in files)
        {
            string assetPath = filePath.Replace("\\", "/");
            if (assetPath.StartsWith(Application.dataPath))
            {
                assetPath = "Assets" + assetPath.Substring(Application.dataPath.Length);
            }
            else
            {
                int assetsIndex = assetPath.IndexOf("Assets/");
                if (assetsIndex >= 0)
                {
                    assetPath = assetPath.Substring(assetsIndex);
                }
            }

            TextAsset csvAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (csvAsset != null)
            {
                fileCSV.Add(csvAsset);
            }
        }
        statusLog = $"Loaded {fileCSV.Count} CSV files from {csvSourcePath}.";
    }

    public void GenerateDialogue()
    {
        if (fileCSV == null || fileCSV.Count == 0)
        {
            statusLog = "Error: No CSV files loaded.";
            return;
        }
        
        if (string.IsNullOrEmpty(savePath))
        {
            statusLog = "Error: Save path is empty.";
            return;
        }

        if (!Directory.Exists(savePath))
        {
            try
            {
                Directory.CreateDirectory(savePath);
                AssetDatabase.Refresh();
            }
            catch (System.Exception e)
            {
                statusLog = $"Error creating save directory: {e.Message}";
                return;
            }
        }

        int filesProcessed = 0;

        for (int i = 0; i < fileCSV.Count; i++)
        {
            TextAsset csvFile = fileCSV[i];
            if (csvFile == null) continue;

            statusLog = $"Processing file [{i + 1}/{fileCSV.Count}]: {csvFile.name}...";
            
            DialogueData dialogueData = ScriptableObject.CreateInstance<DialogueData>();
            dialogueData.dialogues = new List<Dialogue>();

            // Parse lines
            string text = csvFile.text;
            string[] lines = text.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);

            foreach (string line in lines)
            {
                if (string.IsNullOrWhiteSpace(line)) continue;

                List<string> columns = ParseCSVLine(line);
                if (columns.Count < 2) continue; // Requires at least speakerName and dialogueText

                Dialogue dialog = new Dialogue();
                dialog.speakerName = columns[0];
                dialog.dialogueText = columns[1];

                /* ====================================================================
                 * FUTURE ADDITIONS MARKER / MARCAÇÃO PARA ADIÇÕES FUTURAS
                 * ====================================================================
                 * Se você (ou a IA da Unity) quiser adicionar mais colunas do CSV para
                 * mapear para novos campos do DialogueData/Dialogue, adicione aqui!
                 * Exemplo:
                 * if (columns.Count > 2) {
                 *     // Atribua novos campos aqui, ex: dialog.speakerPortrait = ...
                 * }
                 * ==================================================================== */

                dialogueData.dialogues.Add(dialog);
            }

            // Save Asset
            string assetPath = Path.Combine(savePath, $"{csvFile.name}.asset").Replace("\\", "/");
            
            AssetDatabase.CreateAsset(dialogueData, assetPath);
            filesProcessed++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        statusLog = $"Success! Generated {filesProcessed} DialogueData assets in {savePath}.";
    }

    private List<string> ParseCSVLine(string line)
    {
        List<string> result = new List<string>();
        bool inQuotes = false;
        System.Text.StringBuilder currentToken = new System.Text.StringBuilder();

        for (int i = 0; i < line.Length; i++)
        {
            char c = line[i];
            if (c == '\"')
            {
                inQuotes = !inQuotes;
            }
            else if (c == ',' && !inQuotes)
            {
                result.Add(currentToken.ToString().Trim(' ', '\"'));
                currentToken.Clear();
            }
            else
            {
                currentToken.Append(c);
            }
        }
        result.Add(currentToken.ToString().Trim(' ', '\"'));
        return result;
    }

    private void OnGUI()
    {
        GUILayout.Label("Dialogue CSV Importer", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 1. Source Folder Path
        EditorGUILayout.BeginHorizontal();
        csvSourcePath = EditorGUILayout.TextField("CSV Source Folder", csvSourcePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select CSV Source Folder", csvSourcePath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    csvSourcePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    csvSourcePath = selectedPath;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("Load CSVs from Folder"))
        {
            LoadCSVsFromFolder();
        }

        EditorGUILayout.Space();

        // 2. Display fileCSV List
        GUILayout.Label($"Loaded CSV Files ({fileCSV.Count} files):", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(150));
        for (int i = 0; i < fileCSV.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label($"[{i + 1}]", GUILayout.Width(30));
            fileCSV[i] = (TextAsset)EditorGUILayout.ObjectField(fileCSV[i], typeof(TextAsset), false);
            EditorGUILayout.EndHorizontal();
        }
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space();

        // 3. Save Folder Path
        EditorGUILayout.BeginHorizontal();
        savePath = EditorGUILayout.TextField("Save Path", savePath);
        if (GUILayout.Button("Browse", GUILayout.Width(60)))
        {
            string selectedPath = EditorUtility.OpenFolderPanel("Select Save Folder", savePath, "");
            if (!string.IsNullOrEmpty(selectedPath))
            {
                if (selectedPath.StartsWith(Application.dataPath))
                {
                    savePath = "Assets" + selectedPath.Substring(Application.dataPath.Length);
                }
                else
                {
                    savePath = selectedPath;
                }
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 4. Generate Dialogue Button
        bool isValid = fileCSV != null && fileCSV.Count > 0 && !string.IsNullOrEmpty(savePath);

        Color originalColor = GUI.color;
        Color originalBackgroundColor = GUI.backgroundColor;
        
        GUIStyle generateButtonStyle = new GUIStyle(GUI.skin.button);
        generateButtonStyle.fontSize = 14;
        generateButtonStyle.fontStyle = FontStyle.Bold;

        if (isValid)
        {
            GUI.backgroundColor = Color.white;
            generateButtonStyle.normal.textColor = Color.black;
            generateButtonStyle.hover.textColor = Color.black;
            generateButtonStyle.active.textColor = Color.black;
        }
        else
        {
            GUI.backgroundColor = Color.grey;
            generateButtonStyle.normal.textColor = Color.white;
        }

        EditorGUI.BeginDisabledGroup(!isValid);
        if (GUILayout.Button("Generate Dialogue", generateButtonStyle, GUILayout.Height(40)))
        {
            GenerateDialogue();
        }
        EditorGUI.EndDisabledGroup();

        GUI.color = originalColor;
        GUI.backgroundColor = originalBackgroundColor;

        EditorGUILayout.Space();

        // 5. Status Log / Progress
        GUILayout.Label("Status Log:", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(statusLog, MessageType.Info);
    }
}
