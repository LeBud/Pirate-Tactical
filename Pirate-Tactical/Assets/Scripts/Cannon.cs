using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Cannon : NetworkBehaviour
{
    [Header("Stats")]
    public int damage;
    public int upgradedDamage = 7;
    public NetworkVariable<ulong> ID = new NetworkVariable<ulong>();
    public bool upgraded = false;

    public int index;
    public List<TileScript> tiles = new List<TileScript>();

    public Vector2 cannonPos;

    [Header("Colors")]
    public Color player1;
    public Color player2;

    [Header("Textures")]
    public bool useTexture = true;
    public Sprite player1Sprite;
    public Sprite player2Sprite;

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

        if (foundShip)
        {
            if(!upgraded)
                GridManager.Instance.DamageUnitNoActionServerRpc(damage, shipMovedPos, ID.Value, false, 0, false);
            else if(upgraded)
                GridManager.Instance.DamageUnitNoActionServerRpc(upgradedDamage, shipMovedPos, ID.Value, false, 0, false);

            GridManager.Instance.DisplayDamageServerRpc("Canon", new Vector2(shipMovedPos.x, shipMovedPos.y + 0.5f));
        }

    }

    [ClientRpc]
    public void SetColorClientRpc(ulong id)
    {
        _renderer = GetComponent<SpriteRenderer>();
        if (useTexture)
        {
            if (id == 0) _renderer.sprite = player1Sprite;
            else _renderer.sprite = player2Sprite;
        }
        else
        {
            if (id == 0) _renderer.color = player1;
            else _renderer.color = player2;
        }
    }
}
