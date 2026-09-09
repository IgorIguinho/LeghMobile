using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Ponto único de Save/Load do jogo. Singleton persistente (DontDestroyOnLoad).
///
/// USO: de qualquer script do projeto, basta chamar:
///     SaveManager.Instance.SaveGame();
/// para gravar o progresso atual em disco.
///
/// O carregamento acontece sozinho em Start(): se existir um arquivo de save,
/// ele é lido e aplicado aos managers; se não existir, um save novo é criado
/// e gravado imediatamente.
/// </summary>
public class SaveManager : MonoBehaviour
{
    public static SaveManager Instance { get; private set; }

    private const string SaveFileName = "legh_save.json";
    private const string TempSuffix = ".tmp";
    private const string BackupSuffix = ".bak";

    public event Action OnGameLoaded;
    public event Action OnGameSaved;

    public RootSaveData CurrentSave { get; private set; }

    private string SavePath => Path.Combine(Application.persistentDataPath, SaveFileName);
    private string TempPath => SavePath + TempSuffix;
    private string BackupPath => SavePath + BackupSuffix;

  

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        // Carregamos em Start (não em Awake) de propósito: a Unity garante que
        // TODOS os Awake() da cena rodam antes de QUALQUER Start(). Assim,
        // PlayerSkillsManager.Instance e ReadDialogueData.Instance já existem
        // quando formos aplicar os dados carregados neles.
        LoadGame();
    }

   

    // No mobile, quando o app vai pra background (troca de app, notificação,
    // ligação) o sistema operacional pode matar o processo sem aviso nenhum.
    // OnApplicationPause(true) é o gatilho de autosave mais importante no mobile.
    private void OnApplicationPause(bool pauseStatus)
    {
        if (pauseStatus)
        {
            SaveGame();
        }
    }

    // Cobre o caso desktop (Alt+F4 / fechar janela) e Android com botão "voltar".
    private void OnApplicationQuit()
    {
        SaveGame();
    }

    // ---------------------------------------------------------------
    // LOAD
    // ---------------------------------------------------------------

    public void LoadGame()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                CurrentSave = JsonUtility.FromJson<RootSaveData>(json);

                if (CurrentSave == null)
                    throw new Exception("Arquivo de save vazio ou corrompido.");
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveManager] Falha ao ler save principal: {e.Message}. Tentando backup...");
                CurrentSave = TryLoadBackup();
            }
        }
        else
        {
            Debug.Log("[SaveManager] Nenhum save encontrado. Criando um novo.");
            CurrentSave = CreateNewSave();
            SaveGame(); // grava o arquivo inicial em disco imediatamente
        }

        ApplySaveToManagers();
        OnGameLoaded?.Invoke();
    }

    private RootSaveData TryLoadBackup()
    {
        if (File.Exists(BackupPath))
        {
            try
            {
                string json = File.ReadAllText(BackupPath);
                var data = JsonUtility.FromJson<RootSaveData>(json);
                if (data != null) return data;
            }
            catch (Exception e)
            {
                Debug.LogError($"[SaveManager] Backup também corrompido: {e.Message}");
            }
        }

        Debug.LogWarning("[SaveManager] Nenhum backup válido. Iniciando save novo.");
        return CreateNewSave();
    }

    private RootSaveData CreateNewSave()
    {
        return new RootSaveData
        {
            saveVersion = 1,
            lastPlayedScene = null,
            skills = new SkillsSaveData(),
            phases = new List<PhaseSaveData>(),
            readDialogues = new List<string>()
        };
    }

    // Aplica os dados carregados do disco de volta para os managers, ao iniciar o jogo.
    private void ApplySaveToManagers()
    {
        if (PlayerSkillsManager.Instance != null)
            PlayerSkillsManager.Instance.LoadSaveData(CurrentSave.skills);

        if (ReadDialogueData.Instance != null)
            ReadDialogueData.Instance.LoadSaveData(CurrentSave.readDialogues);

        if (FaseProgressSaveManager.Instance != null)
            FaseProgressSaveManager.Instance.LoadSaveData(CurrentSave.phases);
    }

    // ---------------------------------------------------------------
    // SAVE
    // ---------------------------------------------------------------

    // Pega os dados atuais dos managers (fonte da verdade em runtime) e
    // preenche o CurrentSave antes de gravar em disco.
    private void CollectFromManagers()
    {
        if (PlayerSkillsManager.Instance != null)
            CurrentSave.skills = PlayerSkillsManager.Instance.GetSaveData();

        if (ReadDialogueData.Instance != null)
            CurrentSave.readDialogues = ReadDialogueData.Instance.GetSaveData();

        if (FaseProgressSaveManager.Instance != null)
            CurrentSave.phases = FaseProgressSaveManager.Instance.GetSaveData();

        var scene = SceneManager.GetActiveScene();
        CurrentSave.lastPlayedScene = scene.name;
        CurrentSave.lastSaveDateUtc = DateTime.UtcNow.ToString("o");
    }

    /// <summary>
    /// Gatilho de save. Chame de onde quiser no projeto:
    /// coleta de moeda importante, fim de fase, pause menu, checkpoint, etc.
    ///     SaveManager.Instance.SaveGame();
    /// </summary>
    public void SaveGame()
    {
        if (CurrentSave == null)
            CurrentSave = CreateNewSave();

        CollectFromManagers();

        try
        {
            string json = JsonUtility.ToJson(CurrentSave, true);

            // Escrita atômica: grava primeiro em .tmp; só troca o arquivo real
            // se a escrita terminar com sucesso. Isso evita corromper o save
            // se o app for morto pelo SO no meio da gravação (comum no mobile).
            File.WriteAllText(TempPath, json);

            if (File.Exists(SavePath))
                File.Copy(SavePath, BackupPath, true);

            File.Copy(TempPath, SavePath, true);
            File.Delete(TempPath);

            Debug.Log($"[SaveManager] Jogo salvo em: {SavePath}");
            OnGameSaved?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveManager] Erro ao salvar: {e.Message}");
        }
    }

    /// <summary>Apaga o save (botão "Novo Jogo" / "Resetar progresso").</summary>
    public void DeleteSave()
    {
        if (File.Exists(SavePath)) File.Delete(SavePath);
        if (File.Exists(BackupPath)) File.Delete(BackupPath);
        CurrentSave = CreateNewSave();
        ApplySaveToManagers();
    }
}