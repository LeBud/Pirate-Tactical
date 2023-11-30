using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Shipwrek : NetworkBehaviour
{

    public NetworkVariable<UpgradeSystem.UpgradeType> upgradeType = new NetworkVariable<UpgradeSystem.UpgradeType>();
    public Vector2 pos;
    public int roundUntilDisapear;

    int roundToDisapear;

    private void Start()
    {
        roundToDisapear = GameManager.Instance.currentRound.Value + roundUntilDisapear;
    }

    [ServerRpc]
    public void CheckForRoundToDisappearServerRpc()
    {
        if (roundToDisapear > GameManager.Instance.currentRound.Value)
        {
            GridManager.Instance.SetShipwreckOnMapServerRpc(pos, false);
            GetComponent<NetworkObject>().Despawn();
        }
    }
}
