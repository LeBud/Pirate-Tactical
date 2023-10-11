using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Cursor : NetworkBehaviour
{
    [SerializeField] ShipUnit shipUnit;
    [SerializeField] bool shipSpawn = false;

    TileScript currentTile;

    void Update()
    {
        if(!IsOwner) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 pos = new Vector2(mousePos.x, mousePos.y);
        transform.position = new Vector3(pos.x, pos.y, -5);

        RaycastHit2D? tile = GetCurrentTile(pos);

        if (!tile.HasValue) return;
        TileScript t = tile.Value.transform.GetComponent<TileScript>();

        if (Input.GetMouseButtonDown(0) && !shipSpawn)
            SpawnShip(t.pos.Value, t);
        else if (Input.GetMouseButtonDown(0) && shipSpawn)
            ValueServerRpc(t.pos.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    void ValueServerRpc(Vector2 pos)
    {
        shipUnit.unitPos.Value = pos;
    }

    void SpawnShip(Vector2 pos, TileScript t)
    {
        if (!IsOwner) return;
        shipSpawn = true;
        SpawnShipServerRpc(pos);

        //Test pathFind
        currentTile = t;
        GridManager.Instance.playerTile = t;
    }

    [ServerRpc]
    void SpawnShipServerRpc(Vector2 pos)
    {
        ShipUnit ship = Instantiate(shipUnit);
        ship.transform.position = new Vector3(pos.x, pos.y, -1);
        shipUnit = ship;
        shipUnit.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
    }

    RaycastHit2D? GetCurrentTile(Vector2 pos)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero);
        if (hits.Length > 0) return hits.OrderByDescending(i => i.collider.transform.position.z).First();
        return null;
    }

}
