using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShipManager : NetworkBehaviour
{

    [Header("Ships")]
    public PirateShip[] ships;
    public int shipIndex;
    public bool shipCurrentlySelected;

    [Header("SpawnShips")]
    public int remainShipToSpawn;
    public bool allShipsSpawned;

    MouseController mc;


    private void Start()
    {
        mc = GetComponent<MouseController>();
        remainShipToSpawn = ships.Length;
    }


    public void ExecuteAction(OverlayTile currentTile)
    {
        if (!allShipsSpawned)
        {
            if (currentTile != null) SpawnShips(currentTile);
            return;
        }

        if (!shipCurrentlySelected)
        {
            SelectShip(currentTile);
            return;
        }

        if (shipCurrentlySelected && !mc.inRangeTiles.Contains(currentTile))
        {
            DeselectShip(currentTile);
            return;
        }

        if (mc.inRangeTiles.Contains(currentTile))
            mc.shipMoving = true;

    }

    #region shipsBaseActions
    void SelectShip(OverlayTile tile)
    {
        for (int i = 0; i < ships.Length; i++)
        {
            if (ships[i].currentTile == tile)
            {
                shipIndex = ships[i].index;
                shipCurrentlySelected = true;
                mc.currentShip = ships[shipIndex];
                mc.GetInRangeTiles();

                break;
            }
        }
    }

    void DeselectShip(OverlayTile tile)
    {
        shipCurrentlySelected = false;
        mc.RefreshBlockedTile();
        tile.ShowTile();
        mc.path.Clear();
        mc.shipMoving = false;
        mc.currentShip = null;
    }

    void SpawnShips(OverlayTile tile)
    {
        int index = shipIndex;

        shipIndex++;
        remainShipToSpawn--;
        CheckIfAllSpawn();

        ships[index].index = index;
        ships[index].currentTile = tile;

        SpawnOnServerRpc(index);
        ships[index].PositionShipOnMap(tile);
        mc.currentShip = ships[index];
    }

    [ServerRpc]
    void SpawnOnServerRpc(int index)
    {
        PirateShip s = Instantiate(ships[index]);
        s.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        ships[index] = s;
    }

    public void CheckIfAllSpawn()
    {
        if (remainShipToSpawn <= 0)
        {
            allShipsSpawned = true;
            shipIndex = 0;
        }
    }

    #endregion
}
