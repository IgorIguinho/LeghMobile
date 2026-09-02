using UnityEngine;

/// <summary>
/// Representa UMA zona de parallax: um conjunto de camadas (céu, montanhas, chão...)
/// que se movem em velocidades diferentes em relação à câmera.
///
/// Esta classe NÃO sabe que existem outras zonas nem que existe uma "linha de
/// transição". Ela só faz parallax contínuo. Toda a lógica de troca entre zonas
/// fica em ParallaxManager, que ativa/desativa e ajusta o alpha desta zona de fora.
/// (Antes: lockX / UnlockAndSync / NudgeX. Agora: SetZoneActive / SetAlpha.)
/// </summary>
public class ParallaxScrollingBG : MonoBehaviour
{
    public BackGroundInfos[] listBG;

    private Camera cam;
    private SpriteRenderer[][] cachedRenderers; // cache por camada, feito 1x em Awake
    private float currentAlpha = 1f;

    void Awake()
    {
        cam = Camera.main;
        cachedRenderers = new SpriteRenderer[listBG.Length][];

        for (int i = 0; i < listBG.Length; i++)
        {
            var bg = listBG[i];
            if (bg.backGroundObj == null) continue;
            bg.startpos = bg.backGroundObj.position;

            var renderer = bg.backGroundObj.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = bg.backGroundObj.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null) bg.length = renderer.bounds.size;

            // Cacheia TODOS os SpriteRenderers da camada (inclusive filhos), para o fade.
            // Feito uma única vez: nunca mais chamamos GetComponent em runtime.
            cachedRenderers[i] = bg.backGroundObj.GetComponentsInChildren<SpriteRenderer>();
        }
    }

    void LateUpdate()
    {
        if (cam == null) return;

        foreach (var bg in listBG)
        {
            if (bg.backGroundObj == null) continue;

            float distX = (cam.transform.position.x * bg.speedParallaxEffect);
            float distY = (cam.transform.position.y * bg.speedParallaxEffect);

            float targetX = bg.startpos.x + distX;
            float targetY = bg.haveParallaxY ? bg.startpos.y + distY : bg.backGroundObj.position.y;

            bg.backGroundObj.position = new Vector3(targetX, targetY, bg.backGroundObj.position.z);

            // Looping Infinito (mesma lógica de antes, sem alocações por frame)
            float tempX = (cam.transform.position.x * (1 - bg.speedParallaxEffect));
            if (tempX > bg.startpos.x + bg.length.x) bg.startpos.x += bg.length.x;
            else if (tempX < bg.startpos.x - bg.length.x) bg.startpos.x -= bg.length.x;
        }
    }

    /// <summary>
    /// Realinha o startpos de cada camada com a posição atual, para que a zona
    /// retome o parallax sem "pulo" visual ao ser reativada depois de ficar desligada.
    /// </summary>
    public void ResyncToCamera()
    {
        if (cam == null) cam = Camera.main;
        float camX = cam.transform.position.x;
        float camY = cam.transform.position.y;

        foreach (var bg in listBG)
        {
            if (bg.backGroundObj == null) continue;
            bg.startpos.x = bg.backGroundObj.position.x - (camX * bg.speedParallaxEffect);
            if (bg.haveParallaxY)
                bg.startpos.y = bg.backGroundObj.position.y - (camY * bg.speedParallaxEffect);
        }
    }

    /// <summary>
    /// Liga/desliga o cálculo de parallax desta zona (economia de CPU quando ela
    /// está longe do player). Ao reativar, resincroniza automaticamente.
    /// </summary>
    public void SetZoneActive(bool active)
    {
        if (active && !enabled) ResyncToCamera();
        enabled = active;
    }

    /// <summary>
    /// Define a opacidade de todas as camadas desta zona (usado no crossfade da
    /// transição). Não escreve nada se o alpha não mudou.
    /// </summary>
    public void SetAlpha(float alpha)
    {
        if (Mathf.Approximately(alpha, currentAlpha)) return;
        currentAlpha = alpha;

        for (int i = 0; i < cachedRenderers.Length; i++)
        {
            var srArray = cachedRenderers[i];
            if (srArray == null) continue;

            for (int j = 0; j < srArray.Length; j++)
            {
                var sr = srArray[j];
                if (sr == null) continue;
                Color c = sr.color;
                c.a = alpha;
                sr.color = c;
            }
        }
    }
}

[System.Serializable]
public class BackGroundInfos
{
    public Transform backGroundObj;

    public bool haveParallaxY;

    [Tooltip("Valores entre 0 e 1. 0 = move com a câmera, 1 = fica parado (fundo distante).")]
    [Range(0f, 1f)]
    public float speedParallaxEffect;

    [HideInInspector] public Vector2 startpos;
    [HideInInspector] public Vector2 length;
}