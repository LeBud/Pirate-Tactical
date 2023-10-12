using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Cursor : NetworkBehaviour
{
    [SerializeField] ShipUnit shipUnit;
    [SerializeField] bool shipSpawn = false;

    TileScript playerTile, goalTile;

    List<TileScript> path = new List<TileScript>();

    bool unitMoving = false;

    private void Start()
    {
        TileScript.OnHoverTile += OnTileHover;
    }

    void Update()
    {
        if(!IsOwner) return;

        MyInputs();
    }

    void MyInputs()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 pos = new Vector2(mousePos.x, mousePos.y);
        transform.position = new Vector3(pos.x, pos.y, -5);

        RaycastHit2D? tile = GetCurrentTile(pos);

        if (!tile.HasValue) return;
        TileScript t = tile.Value.transform.GetComponent<TileScript>();

        if (Input.GetMouseButtonDown(0) && !shipSpawn)
            SpawnShip(t.pos.Value, t);
        else if (Input.GetMouseButtonDown(0) && CanMoveUnit())
            StartCoroutine(UpdateShipPlacementOnGrid());
    }

    void OnTileHover(TileScript tile)
    {
        if (playerTile == null || unitMoving) return;
        goalTile = tile;
        path = PathFindTesting.PathTest(playerTile, goalTile);
    }

    IEnumerator UpdateShipPlacementOnGrid()
    {
        unitMoving = true;
        int value = path.Count - 1;

        while(path.Count > 0)
        {
            UnitNewPosServerRpc(path[value].pos.Value);
            
            if(path.Count == 0) playerTile = path[0];
            
            path.RemoveAt(value);
            value--;

            yield return new WaitForSeconds(.5f);
        }
        unitMoving = false;
    }

    void SpawnShip(Vector2 pos, TileScript t)
    {
        if (!IsOwner) return;
        shipSpawn = true;
        SpawnUnitServerRpc(pos);
        playerTile = t;
    }

    #region ServerRpcMethods

    [ServerRpc(RequireOwnership = false)]
    void UnitNewPosServerRpc(Vector2 pos)
    {
        shipUnit.unitPos.Value = pos;
    }

    [ServerRpc]
    void SpawnUnitServerRpc(Vector2 pos)
    {
        ShipUnit ship = Instantiate(shipUnit);
        ship.transform.position = new Vector3(pos.x, pos.y, -1);
        shipUnit = ship;
        shipUnit.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
    }

    #endregion

    RaycastHit2D? GetCurrentTile(Vector2 pos)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero);
        if (hits.Length > 0) return hits.OrderByDescending(i => i.collider.transform.position.z).First();
        return null;
    }

    bool CanMoveUnit()
    {
        return shipSpawn && path.Count > 0 && !unitMoving;
    }

}
