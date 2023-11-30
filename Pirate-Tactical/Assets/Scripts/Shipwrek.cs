using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Shipwrek : NetworkBehaviour
{

    public NetworkVariable<UpgradeSystem.UpgradeType> upgradeType = new NetworkVariable<UpgradeSystem.UpgradeType>();
    public Vector2 pos;
    public int roundUntilDisapear;

    [HideInInspector]
    public int roundToDisapear;


    [ServerRpc]
    public void CheckForRoundToDisappearServerRpc()
    {
        Debug.Log("Before if");
        if (roundToDisapear > GameManager.Instance.currentRound.Value)
        {
            Debug.Log("In If statement");
            GridManager.Instance.SetShipwreckOnMapServerRpc(pos, false);
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
