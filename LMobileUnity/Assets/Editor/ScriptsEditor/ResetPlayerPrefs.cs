using UnityEngine;
using UnityEditor;

[InitializeOnLoad]
public class ResetPlayerPrefs 
{

    // O construtor estático é chamado automaticamente pelo [InitializeOnLoad]
    static ResetPlayerPrefs()
    {
        // Inscreve o método no evento de mudança de estado do Editor
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        // Detecta o exato momento em que o botão Play foi pressionado (antes de rodar)
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            ExecutarLogicaDeEditor();
        }
    }

    private static void ExecutarLogicaDeEditor()
    {
        PlayerPrefs.DeleteAll();
    }
}
