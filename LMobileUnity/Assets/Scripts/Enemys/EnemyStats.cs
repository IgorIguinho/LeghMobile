using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyStats : MonoBehaviour
{
    public void Death()
    {
        Destroy(this.gameObject);
    }
}
