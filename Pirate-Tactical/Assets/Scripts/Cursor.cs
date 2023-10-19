using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Cursor : NetworkBehaviour
{
    [SerializeField] ShipUnit shipUnit;
    [SerializeField] bool shipSpawn = false;
    [SerializeField] int unitRange = 4;
    [SerializeField] int damage = 4;
    public NetworkVariable<bool> canPlay = new NetworkVariable<bool>(false);

    TileScript playerTile, goalTile;

    List<TileScript> path = new List<TileScript>();
    List<TileScript> allTiles = new List<TileScript>();
    public List<TileScript> inRangeTiles = new List<TileScript>();

    bool unitMoving = false;

    private void Start()
    {
        TileScript.OnHoverTile += OnTileHover;
        GameManager.Instance.JoinServerServerRpc();

        TileScript[] tiles = FindObjectsOfType<TileScript>();
        foreach (var t in tiles)
        {
            allTiles.Add(t);
            t.SetColor(3);
        }
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

        if (Input.GetMouseButtonDown(0))
        {
            if (!canPlay.Value) return;

            if(!shipSpawn)
                SpawnShip(t.pos.Value, t);
            else if (t.shipOnTile.Value)
            {
                GridManager.Instance.DamageUnitServerRpc(damage, t.pos.Value, NetworkManager.LocalClientId);
                GameManager.Instance.UpdateGameStateServerRpc();
            }
            else if (CanMoveUnit(t))
            {
                StartCoroutine(UpdateShipPlacementOnGrid());
            }
        }
    }

    void GetInRangeTiles()
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);
        inRangeTiles.Clear();
        inRangeTiles = PathFindTesting.GetInRangeTiles(playerTile, unitRange);

        foreach (var t in inRangeTiles) t.HighLightRange(true);
    }

    void OnTileHover(TileScript tile)
    {
        if (playerTile == null || unitMoving || !inRangeTiles.Contains(tile) || !canPlay.Value) return;

        goalTile = tile;
        path.Clear();
        path = PathFindTesting.PathTest(playerTile, goalTile);
    }

    IEnumerator UpdateShipPlacementOnGrid()
    {
        unitMoving = true;
        int value = path.Count - 1;

        SetShipOnTileServerRpc(playerTile.pos.Value, false);

        while (path.Count > 0)
        {
            UnitNewPosServerRpc(path[value].pos.Value);
            
            if(path.Count == 1) playerTile = path[0];
            path.RemoveAt(value);
            value--;

            yield return new WaitForSeconds(.25f);

            if(path.Count == 0)
            {
                foreach (var item in allTiles)
                    item.SetColor(3);
            }
        }

        GameManager.Instance.UpdateGameStateServerRpc();
        SetShipOnTileServerRpc(playerTile.pos.Value, true);
        GetInRangeTiles();
        unitMoving = false;
    }

    void SpawnShip(Vector2 pos, TileScript t)
    {
        if (!IsOwner) return;
        shipSpawn = true;
        SpawnUnitServerRpc(pos, NetworkManager.LocalClientId);
        playerTile = t;

        SetShipOnTileServerRpc(pos, true);
        GetInRangeTiles();
    }

    #region ServerRpcMethods

    [ServerRpc(RequireOwnership = false)]
    void UnitNewPosServerRpc(Vector2 pos)
    {
        shipUnit.unitPos.Value = pos;
    }

    [ServerRpc]
    void SpawnUnitServerRpc(Vector2 pos, ulong id)
    {
        ShipUnit ship = Instantiate(shipUnit);

        shipUnit = ship;
        shipUnit.GetComponent<NetworkObject>().SpawnWithOwnership(OwnerClientId);
        ship.unitPos.Value = new Vector3(pos.x, pos.y, -1);
        shipUnit.SetShipColorClientRpc(id);
    }

    [ServerRpc(RequireOwnership = false)]
    void SetShipOnTileServerRpc(Vector2 tilePos, bool active)
    {
        if (!GridManager.Instance.dictionnary.Contains(tilePos)) return;

        TileScript t = GridManager.Instance.GetTileAtPosition(tilePos);
        t.shipOnTile.Value = active;
    }

    #endregion

    RaycastHit2D? GetCurrentTile(Vector2 pos)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero);
        if (hits.Length > 0) return hits.OrderByDescending(i => i.collider.transform.position.z).First();
        return null;
    }

    bool CanMoveUnit(TileScript t)
    {
        return shipSpawn && path.Count > 0 && !unitMoving && inRangeTiles.Contains(t);
    }

}
