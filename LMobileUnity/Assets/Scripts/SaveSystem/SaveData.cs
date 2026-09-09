using System;
using System.Collections.Generic;

/// <summary>
/// Estrutura raiz do arquivo de save. Tudo que precisa sobreviver a um fechamento
/// do app (e não apenas a uma troca de cena) vive aqui.
///
/// IMPORTANTE: JsonUtility (usado pelo SaveManager) não serializa Dictionary
/// nem HashSet — por isso tudo aqui é List. As conversões acontecem nos
/// métodos GetSaveData()/LoadSaveData() de cada manager.
/// </summary>
[Serializable]
public class RootSaveData
{
    public int saveVersion = 1;

    // Progresso geral
    public string lastPlayedScene;      // cena/fase em que o jogador estava
    public string lastCheckpointId;     // opcional: checkpoint dentro da fase


    // Skills e arma equipada
    public SkillsSaveData skills = new SkillsSaveData();

    // Progresso por fase (uma entrada por faseId)
    public List<PhaseSaveData> phases = new List<PhaseSaveData>();

    // Diálogos já lidos
    public List<string> readDialogues = new List<string>(); // IDs, não objetos

    // Metadado útil para debug e futura tela de "slots" de save
    public string lastSaveDateUtc;
}

[Serializable]
public class SkillsSaveData
{
    public List<SkillType> unlockedSkills = new List<SkillType>();
    public PlayerSkillsManager.WeaponType currentWeapon = PlayerSkillsManager.WeaponType.Sword;
}

/// <summary>
/// Progresso de UMA fase.
/// faseId deve ser único e ESTÁVEL — recomendo usar o campo `sceneFase` do
/// FaseScriptable (nome da cena), nunca `nameFase` (texto de exibição, que
/// pode mudar por localização/tradução e quebraria saves antigos).
/// </summary>
[Serializable]
public class PhaseSaveData
{
    public string faseId;
    public List<int> collectedCoinIds = new List<int>();
    public int collectedMedal;
    public int collectedParti;
    public bool finishFase;
}