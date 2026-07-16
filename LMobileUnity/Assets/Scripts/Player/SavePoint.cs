using Newtonsoft.Json;
using NUnit.Framework;
using UnityEngine;

public class SavePoint : MonoBehaviour
{

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        if (PlayerPrefs.HasKey("PosX") || PlayerPrefs.HasKey("PosY"))
        {
            LoadPosition();
        }
    }

   public void LoadPosition()
    {
        float posX = PlayerPrefs.GetFloat("PosX");
        float posY = PlayerPrefs.GetFloat("PosY");
        transform.position = new Vector3(posX, posY, transform.position.z);
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("SavePoint"))
        {
            float savePosX = transform.position.x;
            float savePosY = transform.position.y;

            PlayerPrefs.SetFloat("PosX", savePosX);
            PlayerPrefs.SetFloat("PosY", savePosY);
            PlayerPrefs.SetInt("SecondTimeFase",1);

            string json = JsonConvert.SerializeObject(FaseManager.Instance.colectedIDCoin);
            PlayerPrefs.SetString("Coins", json);

            PlayerPrefs.Save();
        }
    }

 
}
