using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Janela de Editor (EditorWindow) para automação e geração modular de camadas de Parallax.
/// Permite instanciar a estrutura de 3 imagens (Meio, Frente e Trás) e registrá-las
/// automaticamente no componente ParallaxScrollingBG.
/// </summary>
public class ParallaxBuilderWindow : EditorWindow
{
    [Header("Configurações do Gerenciador")]
    [SerializeField] private GameObject managerObject;

    [Header("Configurações de Renderização")]
    [SerializeField] private int selectedSortingLayerID = 0;

    [Header("Lista de Sprites")]
    [SerializeField] private List<Sprite> parallaxSprites = new List<Sprite>();

    // Variáveis de controle de visualização
    private Vector2 scrollPosition;
    private GUIStyle headerStyle;
    private GUIStyle buttonStyle;
    private bool stylesInitialized;

    [MenuItem("Tools/Parallax Builder")]
    public static void ShowWindow()
    {
        var window = GetWindow<ParallaxBuilderWindow>("Parallax Builder");
        window.minSize = new Vector2(380f, 450f);
        window.Show();
    }

    #region Unity Editor Lifecycle

    private void OnEnable()
    {
        // Ponto de extensão: Carregar preferências ou estado anterior se necessário
    }

    private void OnGUI()
    {
        InitializeStyles();

        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins);

        DrawHeader();
        EditorGUILayout.Space(8);

        DrawManagerSelection();
        EditorGUILayout.Space(8);

        DrawRenderingSettings();
        EditorGUILayout.Space(8);

        DrawSpriteList();
        EditorGUILayout.Space(12);

        DrawActionButtons();

        EditorGUILayout.EndVertical();
        EditorGUILayout.EndScrollView();
    }

    #endregion

    #region GUI Drawing Methods (Camada de Apresentação)

    private void InitializeStyles()
    {
        if (stylesInitialized) return;

        headerStyle = new GUIStyle(EditorStyles.boldLabel)
        {
            fontSize = 14,
            alignment = TextAnchor.MiddleLeft
        };

        buttonStyle = new GUIStyle(GUI.skin.button)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            fixedHeight = 36f
        };

        stylesInitialized = true;
    }

    private void DrawHeader()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("Parallax Layer Builder", headerStyle);
        EditorGUILayout.LabelField("Gera automaticamente a estrutura de 3 imagens (Centro, Frente e Trás) e registra no ParallaxScrollingBG.", EditorStyles.wordWrappedMiniLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawManagerSelection()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("1. Gerenciador de Parallax", EditorStyles.boldLabel);

        EditorGUI.BeginChangeCheck();
        managerObject = (GameObject)EditorGUILayout.ObjectField(
            new GUIContent("Manager GameObject", "GameObject que contém o componente ParallaxScrollingBG ou ParallaxManager."),
            managerObject,
            typeof(GameObject),
            true
        );

        if (managerObject != null)
        {
            var parallaxScript = managerObject.GetComponent<ParallaxScrollingBG>();
            if (parallaxScript == null)
            {
                EditorGUILayout.HelpBox("O GameObject selecionado não possui o componente 'ParallaxScrollingBG'.", MessageType.Warning);
            }
            else
            {
                EditorGUILayout.HelpBox($"ParallaxScrollingBG detectado. Camadas atuais registradas: {parallaxScript.listBG?.Length ?? 0}", MessageType.Info);
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Selecione o GameObject pai do Parallax na cena (ex: BACKGROUND1 ou BACKGROUND2).", MessageType.None);
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawRenderingSettings()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        EditorGUILayout.LabelField("2. Configuração de Renderização", EditorStyles.boldLabel);

        var sortingLayers = SortingLayer.layers;
        string[] layerNames = new string[sortingLayers.Length];
        int selectedIndex = 0;

        for (int i = 0; i < sortingLayers.Length; i++)
        {
            layerNames[i] = sortingLayers[i].name;
            if (sortingLayers[i].id == selectedSortingLayerID)
            {
                selectedIndex = i;
            }
        }

        int newIndex = EditorGUILayout.Popup(
            new GUIContent("Sorting Layer", "Sorting Layer aplicada a todos os SpriteRenderers gerados."),
            selectedIndex,
            layerNames
        );

        if (newIndex >= 0 && newIndex < sortingLayers.Length)
        {
            selectedSortingLayerID = sortingLayers[newIndex].id;
        }

        EditorGUILayout.LabelField("Order in Layer:", "Calculado automaticamente como -i (0, -1, -2, ...)", EditorStyles.miniLabel);
        EditorGUILayout.EndVertical();
    }

    private void DrawSpriteList()
    {
        EditorGUILayout.BeginVertical(EditorStyles.helpBox);
        
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("3. Sprites das Camadas", EditorStyles.boldLabel);
        
        if (GUILayout.Button("+ Adicionar", EditorStyles.miniButton, GUILayout.Width(80)))
        {
            parallaxSprites.Add(null);
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);

        if (parallaxSprites.Count == 0)
        {
            EditorGUILayout.HelpBox("Nenhum sprite adicionado. Clique em '+ Adicionar' ou arraste sprites abaixo.", MessageType.Info);
        }
        else
        {
            for (int i = 0; i < parallaxSprites.Count; i++)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.LabelField($"Camada {i + 1}", GUILayout.Width(70));
                
                parallaxSprites[i] = (Sprite)EditorGUILayout.ObjectField(
                    parallaxSprites[i],
                    typeof(Sprite),
                    false
                );

                if (GUILayout.Button("X", EditorStyles.miniButtonRight, GUILayout.Width(24)))
                {
                    parallaxSprites.RemoveAt(i);
                    GUIUtility.ExitGUI();
                }
                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.Space(4);
            if (GUILayout.Button("Limpar Lista", EditorStyles.miniButton))
            {
                if (EditorUtility.DisplayDialog("Limpar Sprites", "Deseja remover todos os sprites da lista?", "Sim", "Não"))
                {
                    parallaxSprites.Clear();
                }
            }
        }

        EditorGUILayout.EndVertical();
    }

    private void DrawActionButtons()
    {
        bool isValid = ValidatePrerequisites(out string validationMessage);

        if (!isValid)
        {
            EditorGUILayout.HelpBox(validationMessage, MessageType.Warning);
        }

        EditorGUI.BeginDisabledGroup(!isValid);

        // Cor destacada para o botão principal
        Color defaultBg = GUI.backgroundColor;
        GUI.backgroundColor = isValid ? new Color(0.35f, 0.85f, 0.45f) : defaultBg;

        if (GUILayout.Button("Gerar Parallax", buttonStyle))
        {
            ExecuteParallaxGeneration();
        }

        GUI.backgroundColor = defaultBg;
        EditorGUI.EndDisabledGroup();
    }

    #endregion

    #region Validation & Business Logic (Camada de Domínio / Processamento)

    /// <summary>
    /// Valida se todos os parâmetros necessários estão prontos para a geração.
    /// </summary>
    private bool ValidatePrerequisites(out string message)
    {
        if (managerObject == null)
        {
            message = "Selecione o GameObject Gerenciador antes de gerar.";
            return false;
        }

        var parallaxScript = managerObject.GetComponent<ParallaxScrollingBG>();
        if (parallaxScript == null)
        {
            message = "O GameObject Gerenciador deve conter o componente 'ParallaxScrollingBG'.";
            return false;
        }

        if (parallaxSprites == null || parallaxSprites.Count == 0)
        {
            message = "Adicione ao menos um Sprite na lista.";
            return false;
        }

        bool hasAnyValidSprite = false;
        for (int i = 0; i < parallaxSprites.Count; i++)
        {
            if (parallaxSprites[i] != null)
            {
                hasAnyValidSprite = true;
                break;
            }
        }

        if (!hasAnyValidSprite)
        {
            message = "Preencha os campos de Sprite vazios antes de prosseguir.";
            return false;
        }

        message = string.Empty;
        return true;
    }

    /// <summary>
    /// Ponto de entrada do pipeline de geração.
    /// </summary>
    private void ExecuteParallaxGeneration()
    {
        if (!ValidatePrerequisites(out string errorMsg))
        {
            Debug.LogWarning($"[ParallaxBuilder] Validação falhou: {errorMsg}");
            return;
        }

        var parallaxScript = managerObject.GetComponent<ParallaxScrollingBG>();

        // Registra o estado atual para Undo
        Undo.IncrementCurrentGroup();
        int undoGroup = Undo.GetCurrentGroup();
        Undo.SetCurrentGroupName("Gerar Camadas de Parallax");

        try
        {
            int createdCount = 0;

            for (int i = 0; i < parallaxSprites.Count; i++)
            {
                var sprite = parallaxSprites[i];
                if (sprite == null) continue;

                // Calcula o Order in Layer como valor negativo do índice da iteração (-i)
                int orderInLayer = -i;

                // 1. Cria a estrutura de GameObjects da camada aplicando Sorting Layer e Order in Layer
                GameObject layerRoot = CreateLayerStructure(sprite, managerObject.transform, selectedSortingLayerID, orderInLayer);

                // 2. Registra no script ParallaxScrollingBG
                RegisterLayerInManager(parallaxScript, layerRoot.transform);

                createdCount++;
            }

            // Marca o componente e a cena como modificados
            EditorUtility.SetDirty(parallaxScript);
            EditorUtility.SetDirty(managerObject);

            Debug.Log($"[ParallaxBuilder] {createdCount} camada(s) de Parallax gerada(s) com sucesso em '{managerObject.name}'!");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[ParallaxBuilder] Erro durante a geração: {ex.Message}\n{ex.StackTrace}");
        }
        finally
        {
            Undo.CollapseUndoOperations(undoGroup);
        }
    }

    /// <summary>
    /// Cria o GameObject raiz da camada com seus dois filhos (Front e Back) para efeito de looping infinito.
    /// </summary>
    private GameObject CreateLayerStructure(Sprite sprite, Transform parent, int sortingLayerID, int orderInLayer)
    {
        // a. Instancia o GameObject pai com o nome do Sprite
        GameObject layerRoot = new GameObject(sprite.name);
        layerRoot.transform.SetParent(parent, false);
        layerRoot.transform.localPosition = Vector3.zero;

        // c. Adiciona SpriteRenderer no objeto do meio (Root da camada)
        var centerRenderer = layerRoot.AddComponent<SpriteRenderer>();
        centerRenderer.sprite = sprite;
        centerRenderer.sortingLayerID = sortingLayerID;
        centerRenderer.sortingOrder = orderInLayer;

        // Calcula a largura do sprite para posicionamento lateral dos clones (looping de 3 imagens)
        float spriteWidth = sprite.bounds.size.x;

        // d. Cria os filhos Front e Back
        CreateChildClone(layerRoot.transform, "Front", sprite, new Vector3(spriteWidth, 0f, 0f), sortingLayerID, orderInLayer);
        CreateChildClone(layerRoot.transform, "Back", sprite, new Vector3(-spriteWidth, 0f, 0f), sortingLayerID, orderInLayer);

        // Registro de Undo para o objeto raiz e sua hierarquia
        Undo.RegisterCreatedObjectUndo(layerRoot, $"Criar Camada Parallax '{sprite.name}'");

        // [PONTO DE EXTENSÃO FUTURA]: Configuração de Material, Colliders ou Prefabs personalizados
        return layerRoot;
    }

    /// <summary>
    /// Cria um clone filho (Front ou Back) com o mesmo Sprite, deslocamento lateral, Sorting Layer e Order in Layer.
    /// </summary>
    private GameObject CreateChildClone(Transform parent, string cloneName, Sprite sprite, Vector3 localOffset, int sortingLayerID, int orderInLayer)
    {
        GameObject child = new GameObject(cloneName);
        child.transform.SetParent(parent, false);
        child.transform.localPosition = localOffset;

        var renderer = child.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingLayerID = sortingLayerID;
        renderer.sortingOrder = orderInLayer;

        return child;
    }

    /// <summary>
    /// Adiciona a nova camada à lista listBG do ParallaxScrollingBG com suporte completo a Undo.
    /// </summary>
    private void RegisterLayerInManager(ParallaxScrollingBG managerScript, Transform layerTransform)
    {
        Undo.RecordObject(managerScript, "Registrar Camada em listBG");

        var newInfo = new BackGroundInfos
        {
            backGroundObj = layerTransform,
            haveParallaxY = false,
            speedParallaxEffect = 0.5f // [PONTO DE EXTENSÃO FUTURA]: Velocidade customizável via UI da janela
        };

        // Expande o array existente mantendo os itens anteriores
        if (managerScript.listBG == null || managerScript.listBG.Length == 0)
        {
            managerScript.listBG = new[] { newInfo };
        }
        else
        {
            var updatedList = new List<BackGroundInfos>(managerScript.listBG)
            {
                newInfo
            };
            managerScript.listBG = updatedList.ToArray();
        }

        // [PONTO DE EXTENSÃO FUTURA]: Auto-vincular no ParallaxManager se existir na cena
    }

    #endregion
}
