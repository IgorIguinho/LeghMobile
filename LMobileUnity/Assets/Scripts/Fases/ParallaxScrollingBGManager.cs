using NUnit.Framework;
using UnityEngine;

public class ParallaxScrollingBGManager : MonoBehaviour
{

    public BackGroundInfos[] listBG;
    private Camera cam;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        cam = Camera.main;
        for (int i = 0; i < listBG.Length; i++) 
        {
            if (listBG[i].backGroundObj == null) continue;

            listBG[i].startpos = listBG[i].backGroundObj.position.x;

            listBG[i].length = listBG[i].backGroundObj.GetComponent<SpriteRenderer>().bounds.size.x;
        }
    }

    // Update is called once per frame
    void LateUpdate()
    {
        for (int i = 0; i < listBG.Length; i++)
        {
            float speedParallax = listBG[i].speedParallaxEffect;

            float temp = (cam.transform.position.x * (1 - speedParallax));
            float dist = (cam.transform.position.x *  speedParallax);

            Transform transformBG = listBG[i].backGroundObj;
            transformBG.position = new Vector3(listBG[i].startpos + dist, transformBG.position.y, transformBG.position.z);

            if (temp > listBG[i].startpos + listBG[i].length)
            {
                listBG[i].startpos += listBG[i].length;
            }
            else if(temp < listBG[i].startpos - listBG[i].length)
            {
                listBG[i].startpos -= listBG[i].length;
            }

        }
    }


}

[System.Serializable]
public class BackGroundInfos
{
    public Transform backGroundObj;

    [Tooltip("Valores entre 0 e 1. 0 = move com a câmera, 1 = fica parado (fundo distante).")]
    public float speedParallaxEffect;

    [HideInInspector] public float startpos;
    [HideInInspector] public float length;
}
