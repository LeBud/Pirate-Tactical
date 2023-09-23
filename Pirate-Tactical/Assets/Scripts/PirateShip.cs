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

    public void PositionShipOnMap(OverlayTile tile)
    {
        currentTile = tile.GetComponent<OverlayTile>();
        GetComponent<SpriteRenderer>().sortingOrder = tile.GetComponent<SpriteRenderer>().sortingOrder;
        Vector3 pos = new Vector3(tile.transform.position.x, tile.transform.position.y, tile.transform.position.z);
        if(IsOwner)
            ChangePositionServerRpc(pos.x, pos.y + .0001f, pos.z - 1);
        
        //transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y + .0001f, tile.transform.position.z - 1);
    }

    [ServerRpc]
    void ChangePositionServerRpc(float x, float y, float z)
    {
        transform.position = new Vector3(x, y, z);

    }

}
