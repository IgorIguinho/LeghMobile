
using UnityEngine;


public class ParallaxScrollingBG: MonoBehaviour
{



    public BackGroundInfos[] listBG;
    private Camera cam;
    private Transform player;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        player = GameObject.FindGameObjectWithTag("Player").GetComponent<Transform>();
        cam = Camera.main;
        for (int i = 0; i < listBG.Length; i++) 
        {
            if (listBG[i].backGroundObj == null) continue;

            listBG[i].startpos = listBG[i].backGroundObj.position;

            listBG[i].length = listBG[i].backGroundObj.GetComponent<SpriteRenderer>().bounds.size;

        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        Parallax();
    }

    public void Parallax()

    {
        for (int i = 0; i < listBG.Length; i++)
        {
            float speedParallax = listBG[i].speedParallaxEffect;

            float tempX = (cam.transform.position.x * (1 - speedParallax));
            float distX = (cam.transform.position.x * speedParallax);

            float tempY = (cam.transform.position.y * (1 - speedParallax));
            float distY = (cam.transform.position.y * speedParallax);

            Transform transformBG = listBG[i].backGroundObj;

            
            ParallaxMov(listBG[i], new Vector2(distX, distY), transformBG, listBG[i].haveParallaxY);
            LoopingParallax(new Vector2(tempX, tempY), listBG[i]);

        }
    }
    

    void ParallaxMov(BackGroundInfos bgInfo, Vector2 dist, Transform bg, bool parallaxY)
    {
        if (parallaxY)
        {
            bg.position = new Vector3(bgInfo.startpos.x + dist.x, bgInfo.startpos.y + dist.y, bg.position.z);
        }
        else
        {
            bg.position = new Vector3(bgInfo.startpos.x + dist.x, bg.position.y, bg.position.z);
        }
    }

    void LoopingParallax(Vector2 temp, BackGroundInfos bgInfo)
    {
        // Ajuste no eixo X: se a posição "temp.x" sair do bloco atual, reposiciona startpos.x
        if (temp.x > bgInfo.startpos.x + bgInfo.length.x)
        {
            bgInfo.startpos.x += bgInfo.length.x;
        }
        else if (temp.x < bgInfo.startpos.x - bgInfo.length.x)
        {
            bgInfo.startpos.x -= bgInfo.length.x;
        }

        // Se o background possui parallax em Y, não faz looping em Y
        if (bgInfo.haveParallaxY == true) return;

        // Ajuste no eixo Y: comporta-se igual ao X quando não há parallax Y
        if (temp.y > bgInfo.startpos.y + bgInfo.length.y)
        {
            bgInfo.startpos.y += bgInfo.length.y;
        }
        else if (temp.y < bgInfo.startpos.y - bgInfo.length.y)
        {
            bgInfo.startpos.y -= bgInfo.length.y;
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
