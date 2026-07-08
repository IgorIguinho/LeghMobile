using UnityEngine;

public class ObstaculoFase6 : MonoBehaviour
{ 
    public void TakeHit()
    {
        //Desabilitar o obstáculo
        //Aqui você chama o ManagerFase6 para atualizar o estado do obstáculo, quando ele for desabilitado ele deve mudar para a lista de obstaculosInativos.
        ManagerFase6.Instance.RegisterHitAlvo(this.gameObject);
    }
}
