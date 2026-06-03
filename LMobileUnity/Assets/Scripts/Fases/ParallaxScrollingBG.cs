using UnityEngine;

public class ParallaxScrollingBG : MonoBehaviour
{
    public BackGroundInfos[] listBG;
    [HideInInspector] public bool lockX = false;
    private Camera cam;

    void Awake()
    {
        cam = Camera.main;
        foreach (var bg in listBG)
        {
            if (bg.backGroundObj == null) continue;
            bg.startpos = bg.backGroundObj.position;

            // Tenta pegar o renderer no objeto ou nos filhos para medir o tamanho
            var renderer = bg.backGroundObj.GetComponent<SpriteRenderer>();
            if (renderer == null) renderer = bg.backGroundObj.GetComponentInChildren<SpriteRenderer>();
            if (renderer != null) bg.length = renderer.bounds.size;
        }
    }

    void LateUpdate()
    {
        foreach (var bg in listBG)
        {
            float distX = (cam.transform.position.x * bg.speedParallaxEffect);
            float distY = (cam.transform.position.y * bg.speedParallaxEffect);

            // Calcula a nova posi��o baseada no lockX
            float targetX = lockX ? bg.backGroundObj.position.x : bg.startpos.x + distX;
            float targetY = bg.haveParallaxY ? bg.startpos.y + distY : bg.backGroundObj.position.y;

            bg.backGroundObj.position = new Vector3(targetX, targetY, bg.backGroundObj.position.z);

            // Looping Infinito
            float tempX = (cam.transform.position.x * (1 - bg.speedParallaxEffect));
            if (tempX > bg.startpos.x + bg.length.x) bg.startpos.x += bg.length.x;
            else if (tempX < bg.startpos.x - bg.length.x) bg.startpos.x -= bg.length.x;
        }
    }

    public void UnlockAndSync()
    {
        lockX = false;
        float camX = cam.transform.position.x;
        foreach (var bg in listBG)
        {
            // Ajusta o startpos para que a conta (startpos + camX * speed) resulte na posi��o atual
            bg.startpos.x = bg.backGroundObj.position.x - (camX * bg.speedParallaxEffect);
        }
    }

    // Desloca todas as camadas no eixo X de forma coesa (usado para liberar a borda da linha de transição)
    public void NudgeX(float deltaX)
    {
        foreach (var bg in listBG)
        {
            Vector3 p = bg.backGroundObj.position;
            bg.backGroundObj.position = new Vector3(p.x + deltaX, p.y, p.z);
        }
    }
}

[System.Serializable]
public class BackGroundInfos
{
    public Transform backGroundObj;

    public bool haveParallaxY;
  

    [Tooltip("Valores entre 0 e 1. 0 = move com a c�mera, 1 = fica parado (fundo distante).")]
    [Range(0f, 1f)]
    public float speedParallaxEffect;

    [HideInInspector] public Vector2 startpos;
    [HideInInspector] public Vector2 length;
   


}
