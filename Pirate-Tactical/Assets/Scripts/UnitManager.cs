using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class UnitManager : NetworkBehaviour
{
    //public List<ShipUnit> spawnedShips = new List<ShipUnit>();

    public ShipUnit[] ships;
    public ShipUnit barque;

    public int numShipSpawned = 0;
    public NetworkVariable<bool> allShipSpawned = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
}
