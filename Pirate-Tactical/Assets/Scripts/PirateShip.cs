using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PirateShip : NetworkBehaviour
{
    public PirateShipsObject shipInfo;

    public OverlayTile currentTile;

    public int index;

    public NetworkVariable<Vector3> position = new NetworkVariable<Vector3>(Vector3.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.A))
        {
            PositionShipOnMap(currentTile);
        }

    }


    public void PositionShipOnMap(OverlayTile tile)
    {
        if (!IsOwner) return;
        if(currentTile == null) return;
        currentTile = tile;
        GetComponent<SpriteRenderer>().sortingOrder = currentTile.GetComponent<SpriteRenderer>().sortingOrder;
        transform.position = currentTile.transform.position;
    }
}
