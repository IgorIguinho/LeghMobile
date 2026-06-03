using UnityEngine;

public class ParallaxManager : MonoBehaviour
{
    public ParallaxScrollingBG bg1;
    public ParallaxScrollingBG bg2;
    public Transform transitionBG;
    public Transform player;

    [Header("Imagens de Referência para Limites")]
    [Tooltip("Imagem da FRENTE do BACKGROUND 1")]
    public Transform bg1FrontImage;
    [Tooltip("Imagem de TRÁS do BACKGROUND 2")]
    public Transform bg2BackImage;

    [Header("Anti-Deadlock")]
    [Tooltip("Folga extra (em unidades) aplicada ao empurrar o BG para liberar a borda da linha de transição.")]
    public float clearMargin = 0.05f;

    // Cache de Performance para Mobile
    private SpriteRenderer sr1;
    private SpriteRenderer sr2;
    private float width1;
    private float width2;

    void Start()
    {
        // Estado inicial: BG2 começa parado até o player cruzar a linha
        if (bg2 != null) bg2.lockX = true;

        // Caching de componentes e valores fixos para otimização mobile
        if (bg1FrontImage != null)
        {
            sr1 = bg1FrontImage.GetComponent<SpriteRenderer>();
            if (sr1 != null) width1 = sr1.bounds.size.x;
        }
        if (bg2BackImage != null)
        {
            sr2 = bg2BackImage.GetComponent<SpriteRenderer>();
            if (sr2 != null) width2 = sr2.bounds.size.x;
        }
    }

    void Update()
    {
        if (player == null || transitionBG == null) return;

        float lineX = transitionBG.position.x;

        // --- LÓGICA DO BACKGROUND 1 ---
        if (player.position.x < lineX)
        {
            if (bg1 == null || bg1FrontImage == null) return;

            float bg1Edge = GetCachedEdgeX(bg1FrontImage, sr1, true); // Borda Direita

            if (bg1Edge >= lineX)
            {
                // Anti-deadlock: se o player já voltou para trás do MEIO do BG1
                float bg1MiddleX = bg1FrontImage.position.x - (width1/2);

                if (player.position.x < bg1MiddleX)
                {
                    float overlap = (bg1Edge - lineX) + clearMargin;
                    bg1.NudgeX(-overlap);     // empurra para a ESQUERDA
                    bg1.UnlockAndSync();      // retoma o parallax
                }
                else
                {
                    bg1.lockX = true; 
                }
            }
            else
            {
                if (bg1.lockX) bg1.UnlockAndSync(); 
            }
        }
        else
        {
            if (bg1 != null) bg1.lockX = true;
        }

        // --- LÓGICA DO BACKGROUND 2 ---
        if (player.position.x >= lineX)
        {
            if (bg2 == null || bg2BackImage == null) return;

            float bg2Edge = GetCachedEdgeX(bg2BackImage, sr2, false); // Borda Esquerda

            if (bg2Edge <= lineX)
            {
                // Anti-deadlock para o BG2: se o player passou para a frente do MEIO do BG2
                float bg2MiddleX = bg2BackImage.position.x ;

                if (player.position.x > bg2MiddleX)
                {
                    Debug.Log(bg2MiddleX  + " " + bg2Edge + " Solta o bicho" );
                    float overlap = (lineX - bg2Edge) + clearMargin;
                    bg2.NudgeX(overlap);      // empurra para a DIREITA
                    bg2.UnlockAndSync();      // retoma o parallax
                }
                else
                {
                    bg2.lockX = true; 
                }
            }
            else
            {
                if (bg2.lockX) bg2.UnlockAndSync(); 
            }
        }
        else
        {
            if (bg2 != null) bg2.lockX = true;
        }
    }

    private float GetCachedEdgeX(Transform t, SpriteRenderer sr, bool rightSide)
    {
        if (sr == null) return t.position.x;
        return rightSide ? sr.bounds.max.x : sr.bounds.min.x;
    }
}

[System.Serializable]
public class ParallaxGameObject
{
    public GameObject parallaxObj;
    public ParallaxScrollingBG parallaxScript;
    public Collider2D areaCollison;
    public bool isActive;
}