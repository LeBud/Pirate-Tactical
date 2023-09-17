using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PirateShip : MonoBehaviour
{
    public PirateShipsObject shipInfo;

    public OverlayTile currentTile;

    public int index;

    private void Start()
    {
        Debug.Log("PirateShip Spawned !");
    }
}
