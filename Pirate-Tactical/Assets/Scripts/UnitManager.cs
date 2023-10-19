using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitManager : MonoBehaviour
{

    public ShipUnit[] ships;

    public int numShipSpawned = 0;
    public bool allShipSpawned = false;


    private void Update()
    {
        if (allShipSpawned) return;
        else if (numShipSpawned >= ships.Length) allShipSpawned = true;
    }
}
