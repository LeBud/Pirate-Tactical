using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ShipManager : MonoBehaviour
{

    [Header("Ships")]
    public PirateShip[] ships;
    public int shipIndex;

    public int remainShipToSpawn;

    public bool shipCurrentlySelected;
    public bool allShipsSpawned;

    private void Start()
    {
        remainShipToSpawn = ships.Length;
    }

    public void CheckIfAllSpawn()
    {
        if (remainShipToSpawn <= 0)
        {
            allShipsSpawned = true;
            shipIndex = 0;
        }
    }

}
