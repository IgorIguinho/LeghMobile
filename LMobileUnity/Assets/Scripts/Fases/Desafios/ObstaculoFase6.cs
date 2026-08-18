using System.Collections;
using UnityEngine;

public class ObstaculoFase6 : MonoBehaviour
{ 
    public Animator animator;
    public void TakeHit()
    {
        //Desabilitar o obstáculo
        //Aqui você chama o ManagerFase6 para atualizar o estado do obstáculo, quando ele for desabilitado ele deve mudar para a lista de obstaculosInativos.

        StartCoroutine(StartHitAnimation());

    }

    public IEnumerator StartHitAnimation() 
    {
        animator.SetTrigger("Hit");
        yield return new WaitForSeconds(0.3f);
        ManagerFase6.Instance.RegisterHitAlvo(this.gameObject);
    }
}
