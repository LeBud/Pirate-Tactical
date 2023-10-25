using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using Unity.VisualScripting;
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

    public UnitManager unitManager;

    public float totalMovePoint;
    public float totalShootPoint;
    float totalActionPoint;

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

    public void TotalActionPoint()
    {
        if(totalShootPoint < 0) totalShootPoint = 0;
        if (totalMovePoint < 0) totalMovePoint = 0;
        
        totalActionPoint = totalMovePoint + totalShootPoint;

        if(totalActionPoint <= 0 && unitManager.allShipSpawned)
        {
            for(int i = 0; i < unitManager.ships.Length; i++)
            {
                unitManager.ships[i].canBeSelected.Value = false;
                unitManager.ships[i].canShoot.Value = false;
                unitManager.ships[i].canMove.Value = false;
            }
            GameManager.Instance.UpdateGameStateServerRpc();
        }
    }

    void MyInputs()
    {

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 pos = new Vector2(mousePos.x, mousePos.y);
        transform.position = new Vector3(pos.x, pos.y, -5);

        RaycastHit2D? tile = GetCurrentTile(pos);

        if (!tile.HasValue) return;
        TileScript t = tile.Value.transform.GetComponent<TileScript>();

        if (!canPlay.Value)
        {
            shipSelected = false;
            HideTiles();
            return;
        }

        if (Input.GetMouseButtonDown(0) && shipSelected)
        {
            if (t.shipOnTile.Value)
            {
                if (!unitManager.ships[currentShipIndex].canShoot.Value) return;

                GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].damage, t.pos.Value, NetworkManager.LocalClientId);
                SetUnitActionServerRpc(1);
                totalShootPoint--;
                if (unitManager.ships[currentShipIndex].canMove.Value)
                {
                    totalMovePoint--;
                    SetUnitActionServerRpc(2);
                }
                TotalActionPoint();
            }
            else if (CanMoveUnit(t))
            {
                if (unitManager.ships[currentShipIndex].canMove.Value)
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
                TotalActionPoint();
                if (currentShipIndex >= unitManager.ships.Length) currentShipIndex = 0;

            }
            else if (t.shipOnTile.Value && unitManager.allShipSpawned)
            {
                foreach(var ship in unitManager.ships)
                {
                    if(ship.unitPos.Value == t.pos.Value && ship.clientIdOwner == NetworkManager.LocalClientId && ship.canBeSelected.Value)
                    {
                        currentShipIndex = ship.index;
                        shipSelected = true;
                        GetInRangeTiles();
                        break;
                    }
                }
            }
        }

        if (Input.GetButtonDown("Cancel"))
        {
            shipSelected = false;
            HideTiles();
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
        if (CantPathfind(tile)) return;

        goalTile = tile;
        path.Clear();
        path = PathFindTesting.PathTest(unitManager.ships[currentShipIndex].currentTile, goalTile);
    }

    IEnumerator UpdateShipPlacementOnGrid()
    {
        unitMoving = true;
        int value = path.Count - 1;

        GridManager.Instance.SetShipOnTileServerRpc(unitManager.ships[currentShipIndex].currentTile.pos.Value, false);

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

        totalMovePoint--;
        SetUnitActionServerRpc(0);
        GridManager.Instance.SetShipOnTileServerRpc(unitManager.ships[currentShipIndex].currentTile.pos.Value, true);
        GetInRangeTiles();
        TotalActionPoint();
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

        GridManager.Instance.SetShipOnTileServerRpc(pos, true);

        if (unitManager.numShipSpawned >= unitManager.ships.Length && !unitManager.allShipSpawned) unitManager.allShipSpawned = true;
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
    void SetUnitActionServerRpc(int i)
    {
        if(i == 0)
            unitManager.ships[currentShipIndex].canMove.Value = false;
        else if(i == 1)
            unitManager.ships[currentShipIndex].canShoot.Value = false;
        else if(i == 2)
        {
            unitManager.ships[currentShipIndex].canShoot.Value = false;
            unitManager.ships[currentShipIndex].canMove.Value = false;
            unitManager.ships[currentShipIndex].canBeSelected.Value = false;
        }
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

    bool CantPathfind(TileScript tile)
    {
        return unitManager.ships[currentShipIndex].currentTile == null || unitMoving || !inRangeTiles.Contains(tile) || !canPlay.Value || !shipSelected || !unitManager.ships[currentShipIndex].canMove.Value;
    }

}
