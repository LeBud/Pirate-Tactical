using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Cannon : NetworkBehaviour
{
    [Header("Stats")]
    public int damage;
    public ulong ID;

    public int index;
    public List<TileScript> tiles = new List<TileScript>();

    public Vector2 cannonPos;

    [Header("Colors")]
    public Color player1;
    public Color player2;

    SpriteRenderer renderer;


    [ServerRpc]
    public void CannonDamageInRangeServerRpc()
    {
        if (!IsServer) return;

        foreach (TileScript tile in tiles.Where(t => t.shipOnTile.Value))
        {
            if(tile.shipOnTile.Value)
                GridManager.Instance.DamageUnitNoActionServerRpc(damage, tile.pos.Value, ID, false, 0, false);
        }
    }

    [ClientRpc]
    public void SetColorClientRpc()
    {
        renderer = GetComponent<SpriteRenderer>();
        if (ID == 0) renderer.color = player1;
        else renderer.color = player2;
    }
}
