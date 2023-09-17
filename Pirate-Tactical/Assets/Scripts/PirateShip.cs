using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PirateShip : NetworkBehaviour
{
    public PirateShipsObject shipInfo;

    public OverlayTile currentTile;

    public int index;

    private void Start()
    {
        Debug.Log("PirateShip Spawned !");
    }
}
