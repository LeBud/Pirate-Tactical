using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Shipwrek : NetworkBehaviour
{

    public NetworkVariable<UpgradeSystem.UpgradeType> upgradeType = new NetworkVariable<UpgradeSystem.UpgradeType>();

    public int roundUntilDisapear;

    int roundToDisapear;


}
