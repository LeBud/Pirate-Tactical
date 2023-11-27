using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Cannon : NetworkBehaviour
{

    public int damage;
    public ulong ID;

    public int index;
    public List<TileScript> tiles = new List<TileScript>();

    public Vector2 cannonPos;

    [ServerRpc]
    public void CannonDamageInRangeServerRpc()
    {
        if (!IsServer) return;

        foreach (TileScript tile in tiles.Where(t => t.shipOnTile.Value))
        {
            //GetShip
        }
    }
}
