using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PirateShip : MonoBehaviour
{
    public OverlayTile currentTile;
    public int travelRange = 4;

    private void Start()
    {
        Debug.Log("PirateShip Spawned !");
    }
}
