using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Cannon : NetworkBehaviour
{
    [Header("Stats")]
    public int damage;
    public NetworkVariable<ulong> ID = new NetworkVariable<ulong>();

    public int index;
    public List<TileScript> tiles = new List<TileScript>();

    public Vector2 cannonPos;

    [Header("Colors")]
    public Color player1;
    public Color player2;

    SpriteRenderer _renderer;

    [ServerRpc]
    public void CannonDamageInRangeServerRpc(Vector2 shipMovedPos)
    {
        if (!IsServer) return;

        bool foundShip = false;

        foreach(var tile in tiles)
            if(tile.pos.Value == shipMovedPos)
            {
                foundShip = true;
                break;
            }

        if(foundShip)
            GridManager.Instance.DamageUnitNoActionServerRpc(damage, shipMovedPos, ID.Value, false, 0, false);

    }

    [ClientRpc]
    public void SetColorClientRpc(ulong id)
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (id == 0) _renderer.color = player1;
        else _renderer.color = player2;
    }
}
