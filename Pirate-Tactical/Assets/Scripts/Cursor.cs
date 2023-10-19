using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Cursor : NetworkBehaviour
{
    [SerializeField] int currentShipIndex;
    [SerializeField] bool shipSelected = false;

    public NetworkVariable<bool> canPlay = new NetworkVariable<bool>(false);

    TileScript goalTile;

    List<TileScript> path = new List<TileScript>();
    List<TileScript> allTiles = new List<TileScript>();
    public List<TileScript> inRangeTiles = new List<TileScript>();

    bool unitMoving = false;

    [SerializeField] UnitManager unitManager;

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

        if (!canPlay.Value) return;

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

        if (Input.GetMouseButtonDown(0) && shipSelected)
        {
            if (t.shipOnTile.Value)
            {
                GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].damage, t.pos.Value, NetworkManager.LocalClientId);
                GameManager.Instance.UpdateGameStateServerRpc();
            }
            else if (CanMoveUnit(t))
            {
                StartCoroutine(UpdateShipPlacementOnGrid());
            }
            else if (!CanMoveUnit(t) && !unitMoving)
            {
                shipSelected = false;
                HideTiles();
            }
        }
        else if (Input.GetMouseButtonDown(0) && !shipSelected)
        {
            if (!unitManager.allShipSpawned)
            {
                SpawnShip(t.pos.Value, t);

                if (currentShipIndex >= unitManager.ships.Length) currentShipIndex = 0;

            }
            else if (t.shipOnTile.Value && unitManager.allShipSpawned)
            {
                foreach(var ship in unitManager.ships)
                {
                    if(ship.unitPos.Value == t.pos.Value && ship.clientIdOwner == NetworkManager.LocalClientId)
                    {
                        currentShipIndex = ship.index;
                        shipSelected = true;
                        GetInRangeTiles();
                        break;
                    }
                }
            }
        }
    }

    void GetInRangeTiles()
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);
        inRangeTiles.Clear();
        inRangeTiles = PathFindTesting.GetInRangeTiles(unitManager.ships[currentShipIndex].currentTile, unitManager.ships[currentShipIndex].unitRange);

        foreach (var t in inRangeTiles) t.HighLightRange(true);
    }

    void HideTiles()
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);
        inRangeTiles.Clear();
    }

    void OnTileHover(TileScript tile)
    {
        if (unitManager.ships[currentShipIndex].currentTile == null || unitMoving || !inRangeTiles.Contains(tile) || !canPlay.Value || !shipSelected) return;

        goalTile = tile;
        path.Clear();
        path = PathFindTesting.PathTest(unitManager.ships[currentShipIndex].currentTile, goalTile);
    }

    IEnumerator UpdateShipPlacementOnGrid()
    {
        unitMoving = true;
        int value = path.Count - 1;

        SetShipOnTileServerRpc(unitManager.ships[currentShipIndex].currentTile.pos.Value, false);

        while (path.Count > 0)
        {
            UnitNewPosServerRpc(path[value].pos.Value, currentShipIndex);
            
            if(path.Count == 1) unitManager.ships[currentShipIndex].currentTile = path[0];
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
        SetShipOnTileServerRpc(unitManager.ships[currentShipIndex].currentTile.pos.Value, true);
        GetInRangeTiles();
        unitMoving = false;
    }

    void SpawnShip(Vector2 pos, TileScript t)
    {
        if (!IsOwner) return;


        SpawnUnitServerRpc(pos, NetworkManager.LocalClientId, currentShipIndex);

        unitManager.ships[currentShipIndex].currentTile = t;
        unitManager.ships[currentShipIndex].index = currentShipIndex;
        unitManager.ships[currentShipIndex].clientIdOwner = NetworkManager.LocalClientId;
        unitManager.numShipSpawned++;
        currentShipIndex++;

        SetShipOnTileServerRpc(pos, true);
        //GetInRangeTiles();
    }

    #region ServerRpcMethods

    [ServerRpc(RequireOwnership = false)]
    void UnitNewPosServerRpc(Vector2 pos, int index)
    {
        unitManager.ships[index].unitPos.Value = pos;
    }

    [ServerRpc]
    void SpawnUnitServerRpc(Vector2 pos, ulong id, int index)
    {
        ShipUnit ship = Instantiate(NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>().unitManager.ships[index]);
        ship.GetComponent<NetworkObject>().SpawnWithOwnership(id);

        LinkUnitToClientRpc(ship.GetComponent<NetworkObject>().NetworkObjectId, index);

        //unitManager.ships[index] = ship;
        unitManager.ships[index].unitPos.Value = new Vector3(pos.x, pos.y, -1);
        unitManager.ships[index].SetShipColorClientRpc(id);
    }

    [ClientRpc]
    void LinkUnitToClientRpc(ulong unitID, int index)
    {
        foreach(NetworkObject obj in FindObjectsOfType<NetworkObject>())
        {
            if (obj.NetworkObjectId == unitID)
            {
                unitManager.ships[index] = obj.GetComponent<ShipUnit>();
                break;
            }
        }
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
        return unitManager.allShipSpawned && shipSelected && path.Count > 0 && !unitMoving && inRangeTiles.Contains(t);
    }

}
