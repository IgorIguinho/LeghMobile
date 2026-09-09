using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Guarda o progresso de coleta de CADA fase (moedas, medalhas, "parti" e se
/// foi concluída) e faz a ponte com o SaveManager.
///
/// Por que isso existe separado do FaseScriptable:
/// FaseScriptable é um ScriptableObject = um ASSET compartilhado. Escrever o
/// progresso do jogador diretamente nos campos dele não é confiável em build
/// (não persiste em disco sozinho) e mistura "definição da fase" (sprite,
/// nome, cena) com "estado do save" (o que ESTE jogador já coletou). Este
/// manager guarda o estado real; o FaseScriptable continua sendo só a
/// definição estática da fase.
/// </summary>
public class FaseProgressSaveManager : MonoBehaviour
{
    public static FaseProgressSaveManager Instance { get; private set; }

    // Runtime: faseId (= FaseScriptable.sceneFase) -> progresso
    private readonly Dictionary<string, PhaseSaveData> _progress = new Dictionary<string, PhaseSaveData>();

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

    private PhaseSaveData GetOrCreate(string faseId)
    {
        if (!_progress.TryGetValue(faseId, out var data))
        {
            data = new PhaseSaveData { faseId = faseId };
            _progress[faseId] = data;
        }
        return data;
    }

    public void RegisterCoinCollected(string faseId, int coinId)
    {
        var data = GetOrCreate(faseId);
        if (!data.collectedCoinIds.Contains(coinId))
            data.collectedCoinIds.Add(coinId);
    }

    public void RegisterMedalCollected(string faseId, int amount = 1)
    {
        GetOrCreate(faseId).collectedMedal += amount;
    }

    public void RegisterPartiCollected(string faseId, int amount = 1)
    {
        GetOrCreate(faseId).collectedParti += amount;
    }

    public void MarkFaseFinished(string faseId)
    {
        GetOrCreate(faseId).finishFase = true;
    }

    public bool IsFaseFinished(string faseId)
    {
        return _progress.TryGetValue(faseId, out var data) && data.finishFase;
    }

    /// <summary>
    /// Preenche um FaseScriptable EM RUNTIME com o progresso salvo — útil
    /// para a tela de seleção de fase mostrar coletáveis/checkmarks corretos.
    /// Nunca salve o asset de volta a partir daqui.
    /// </summary>
    public void ApplyToScriptable(FaseScriptable fase)
    {
        if (fase == null) return;

        if (!_progress.TryGetValue(fase.sceneFase, out var data))
        {
            fase.colectedCoin = new List<int>();
            fase.colectedMedal = 0;
            fase.colectedParti = 0;
            fase.finishFase = false;
            return;
        }

        fase.colectedCoin = new List<int>(data.collectedCoinIds);
        fase.colectedMedal = data.collectedMedal;
        fase.colectedParti = data.collectedParti;
        fase.finishFase = data.finishFase;
    }

    public List<PhaseSaveData> GetSaveData()
    {
        return _progress.Values.ToList();
    }

    public void LoadSaveData(List<PhaseSaveData> data)
    {
        _progress.Clear();
        if (data == null) return;

        foreach (var p in data)
        {
            _progress[p.faseId] = p;
            ApplyToScriptable(FaseSelectManager.Instance.faseList.FirstOrDefault(f => f.sceneFase == p.faseId)); // Atualiza o ScriptableObject em runtime
        }
    }
}