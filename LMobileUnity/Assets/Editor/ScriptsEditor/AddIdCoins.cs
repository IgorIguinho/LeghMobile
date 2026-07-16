using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement; // Adicionado para gerenciar o salvamento da cena
using UnityEngine;

public class AddIdCoins : EditorWindow
{
    public FaseManager manager;
    public List<GameObject> coinContainers = new List<GameObject>();
    private Vector2 scrollPos;

    [MenuItem("Tools/Add ID Coins")]
    public static void ShowWindow()
    {
        GetWindow<AddIdCoins>("Add ID Coins");
    }

    public void OnGUI()
    {
        GUILayout.Label("Add ID Coins to FaseManager", EditorStyles.boldLabel);
        manager = (FaseManager)EditorGUILayout.ObjectField("Fase Manager", manager, typeof(FaseManager), true);

        if (manager != null)
        {

            if (GUILayout.Button("Add Coin Containers"))
            {
                AddCoinOnFaseManager();
            }
           
            if (GUILayout.Button("Add IDs"))
            {
                AddIDs();
            }
            EditorGUILayout.Space(10);

            if (GUILayout.Button("Clean Null Spaces"))
            {
                CleanNullIndex();
            }

            if (GUILayout.Button("Save file"))
            {
                SaveAll();
            }
        }
    }

    void AddIDs()
    {
        // Registra o estado dos componentes antes de alterá-los
        for (int i = 0; i < manager.coinsList.Count; i++)
        {
            if (manager.coinsList[i] == null) continue;

            CoinScript coinScript = manager.coinsList[i].GetComponent<CoinScript>();
            if (coinScript != null)
            {
                // Registra para o Unity saber que esse script específico mudou
                Undo.RecordObject(coinScript, "Assign Coin ID");
                coinScript.id = i;

                // Força o Unity a marcar o componente como modificado
                EditorUtility.SetDirty(coinScript);
                Debug.Log($"Added ID {i} to coin {manager.coinsList[i].name}");
            }
            else
            {
                Debug.LogWarning($"Coin at index {i} does not have a CoinScript component.");
            }
        }

        // Marca a cena ativa como modificada (suja)
        MarkSceneAsDirty();
    }

    void CleanNullIndex()
    {
        // Registra a alteração no FaseManager da cena
        Undo.RecordObject(manager, "Clean Null Coins");

        if (manager.actualFase != null)
        {
            // Registra a alteração no ScriptableObject (actualFase)
            Undo.RecordObject(manager.actualFase, "Update Total Coin Count");
        }

        for (int i = manager.coinsList.Count - 1; i >= 0; i--)
        {
            if (manager.coinsList[i] == null)
            {
                manager.coinsList.RemoveAt(i);
                Debug.Log($"Removed null coin at index {i}");
            }
        }

        if (manager.actualFase != null)
        {
            manager.actualFase.totalCoin = manager.coinsList.Count;
            EditorUtility.SetDirty(manager.actualFase);
            Debug.Log($"Atualização do total de moedas ({manager.actualFase.totalCoin}) para {manager.coinsList.Count} ");
        }

        EditorUtility.SetDirty(manager);
        MarkSceneAsDirty();
    }

    void AddCoinOnFaseManager()
    {


        // 1. Garante que o FaseManager foi arrastado/selecionado no campo do Editor
        if (manager == null)
        {
            Debug.LogError("Por favor, selecione o FaseManager no campo antes de adicionar as moedas!");
            return;
        }

        // 2. Registra o FaseManager no sistema de Undo da Unity (permite dar Ctrl+Z)
        Undo.RecordObject(manager, "Populate Coins List");

        // 3. Inicializa ou limpa a lista do manager para receber uma nova contagem limpa
        if (manager.coinsList == null)
        {
            manager.coinsList = new List<GameObject>();
        }
        else
        {
            manager.coinsList.Clear();
        }

        // Limpa também a lista visual de containers do editor
        coinContainers.Clear();

        // 4. Procura o objeto "Grid" na cena
        GameObject gridObj = GameObject.Find("Grid");
        if (gridObj != null)
        {
            // Tenta encontrar o container de moedas (pode ser "Coin" ou "Coins")
            Transform container = gridObj.transform.Find("Coin");
            if (container == null)
            {
                container = gridObj.transform.Find("Coins");
            }

            if (container != null)
            {
                // Adiciona o container pai à lista visual do editor (opcional)
                coinContainers.Add(container.gameObject);

                // 5. Varre todos os filhos diretos do container (cada moeda individual)
                for (int i = 0; i < container.childCount; i++)
                {
                    Transform child = container.GetChild(i);

                    // Verifica se o objeto filho possui o componente CoinScript
                    CoinScript coinScript = child.GetComponent<CoinScript>();
                    if (coinScript != null)
                    {
                        // Adiciona a moeda à lista de moedas do FaseManager
                        manager.coinsList.Add(child.gameObject);
                    }
                }

                Debug.Log($"Sucesso! {manager.coinsList.Count} moedas foram adicionadas ao FaseManager vindas de '{container.name}'.");
            }
            else
            {
                Debug.LogWarning("Nenhum container de moedas ('Coin' ou 'Coins') foi encontrado sob o objeto 'Grid'.");
            }
        }
        else
        {
            Debug.LogError("O objeto pai 'Grid' não foi encontrado nesta cena.");
        }
        // 6. Atualiza dinamicamente a quantidade total de moedas no ScriptableObject (actualFase)
        if (manager.actualFase != null)
        {
            Undo.RecordObject(manager.actualFase, "Update Scriptable Total Coin Count");
            manager.actualFase.totalCoin = manager.coinsList.Count;
            EditorUtility.SetDirty(manager.actualFase);
            Debug.Log($"O total de moedas no ScriptableObject ({manager.actualFase.name}) foi atualizado para: {manager.actualFase.totalCoin}");
        }

        // 7. Notifica o Unity que o FaseManager e a cena foram modificados e precisam ser salvos
        EditorUtility.SetDirty(manager);
        MarkSceneAsDirty();
    }

    void SaveAll()
    {
        // 1. Salva o ScriptableObject (se houver)
        if (manager.actualFase != null)
        {
            EditorUtility.SetDirty(manager.actualFase);
        }

        // 2. Salva todos os assets do projeto (ScriptableObjects, Prefabs, etc.)
        AssetDatabase.SaveAssets();

        // 3. Salva a cena ativa explicitamente para garantir que as moedas e o manager fiquem salvos
        EditorSceneManager.SaveScene(manager.gameObject.scene);

        Debug.Log("FaseManager, Coins and Scene saved successfully!");
    }

    void MarkSceneAsDirty()
    {
        // Método auxiliar para avisar o Unity que a cena mudou e precisa ser salva (aparecerá o "*" do lado do nome da cena)
        if (!Application.isPlaying)
        {
            EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        }
    }
}