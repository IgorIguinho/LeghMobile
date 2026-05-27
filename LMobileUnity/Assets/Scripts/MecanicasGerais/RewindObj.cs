using Microsoft.Win32.SafeHandles;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

public class RewindObj : MonoBehaviour
{
    public bool canRewind = false;
    public bool isRewind = false;
    public float timeRecord;
    [SerializeField]private List<TransformData> historyTransform;
  

    SpriteRenderer spriteRenderer;
    Rigidbody2D rb;
    Animator animator;
    // Start is called before the first frame update
    void Start()
    {
        rb = gameObject.GetComponent<Rigidbody2D>();
        spriteRenderer = gameObject.GetComponent<SpriteRenderer>();
        animator = gameObject.GetComponent<Animator>();
        historyTransform = new List<TransformData>();
        RewindManager.Register(this);
    }

    // Update is called once per frame
    void FixedUpdate()
    {
        if (canRewind){ Rewind(); }
        else { Record(); }
    }

    private void OnDestroy()
    {
        RewindManager.UnRegister(this);
    }

    void Record()
    {
        if (historyTransform.Count > Mathf.Round(timeRecord / Time.fixedDeltaTime))
        {
            historyTransform.RemoveAt(historyTransform.Count - 1); //Retira a ultima posição gravada
        }

        historyTransform.Insert(0, new TransformData(transform.position,transform.rotation, spriteRenderer.sprite));
    }

    void Rewind()
    {
        if (historyTransform.Count > 0 )
        {
            TransformData data = historyTransform[0];
            transform.position =data.position ;
            transform.rotation = data.rotation;
            spriteRenderer.sprite = data.sprite;
            isRewind = true;
            historyTransform.RemoveAt(0);
        }
        else { StopRewind(); }
    }

    public void StopRewind()
    {
        canRewind = false; 
        isRewind = false;
        if (rb != null) { rb.bodyType = RigidbodyType2D.Dynamic; animator.enabled = true; }
    }
    public void StartRewind()
    {
        canRewind = true;
        if (rb != null) { rb.bodyType = RigidbodyType2D.Kinematic; animator.enabled = false; }
    }


    private struct TransformData
    {
        public Vector2 position;
        public Quaternion rotation;
        public Sprite sprite;

        public TransformData( Vector2 pos, Quaternion rot, Sprite spr)
        {
            position = pos;
            rotation = rot;
            sprite = spr;
        }
    }
}
