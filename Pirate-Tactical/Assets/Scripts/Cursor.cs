using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Cursor : NetworkBehaviour
{
    public int currentShipIndex;
    public bool shipSelected = false;

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

    public float currentModeIndex;
    float currentModeInputIndex;
    bool canShoot;
    bool canMove;

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
        HandleCurrentMode();
    }

    public void TotalActionPoint()
    {
        if(totalShootPoint < 0) totalShootPoint = 0;
        if (totalMovePoint < 0) totalMovePoint = 0;
        
        totalActionPoint = totalMovePoint + totalShootPoint;

        if (GameManager.Instance.gametesting.Value) return;

        if(totalActionPoint <= 0 && unitManager.allShipSpawned.Value)
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

    [ClientRpc]
    public void ResetShipsActionClientRpc()
    {
        for (int i = 0; i < unitManager.ships.Length; i++)
        {
            unitManager.ships[i].canBeSelected.Value = true;
            unitManager.ships[i].canShoot.Value = true;
            unitManager.ships[i].canMove.Value = true;
            totalShootPoint++;
            totalMovePoint++;
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

        if (Input.GetAxis("Mouse ScrollWheel") > 0)
            currentModeIndex++;
        else if(Input.GetAxis("Mouse ScrollWheel") < 0)
            currentModeIndex--;

        if (Input.GetMouseButtonDown(0) && shipSelected)
        {
            if (t.shipOnTile.Value && canShoot)
            {
                if (!unitManager.ships[currentShipIndex].canShoot.Value) return;

                GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].damage, t.pos.Value, NetworkManager.LocalClientId);
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
            if (!unitManager.allShipSpawned.Value)
            {
                SpawnShip(t.pos.Value, t);
                if (currentShipIndex >= unitManager.ships.Length) currentShipIndex = 0;

            }
            else if (t.shipOnTile.Value && unitManager.allShipSpawned.Value)
            {
                foreach(var ship in unitManager.ships)
                {
                    if(ship.unitPos.Value == t.pos.Value && ship.clientIdOwner == NetworkManager.LocalClientId && ship.canBeSelected.Value)
                    {
                        currentShipIndex = ship.index;
                        shipSelected = true;
                        DisplayOnSelectedUnit();
                        break;
                    }
                }
            }
        }

        if (Input.GetButtonDown("Cancel") || shipSelected && !unitManager.ships[currentShipIndex].canBeSelected.Value)
        {
            shipSelected = false;
            HideTiles();
        }

    }

    void DisplayOnSelectedUnit()
    {
        if (unitManager.ships[currentShipIndex].canMove.Value)
        {
            currentModeIndex = 0;
            currentModeInputIndex = currentModeIndex;
            canMove = true;
            canShoot = false;
            HideTiles();
            GetInRangeTiles();
        }
        else
        {
            currentModeIndex = 1;
            currentModeInputIndex = currentModeIndex;
            canMove = false;
            canShoot = true;
            HideTiles();
            GetInRangeShootTiles();
        }
    }

    void HandleCurrentMode()
    {
        if (currentModeIndex > 1) currentModeIndex = 0;
        else if (currentModeIndex < 0) currentModeIndex = 1;

        if (!shipSelected || !unitManager.ships[currentShipIndex].canBeSelected.Value) return;

        if (currentModeIndex == 0) 
        {
            if (!unitManager.ships[currentShipIndex].canMove.Value) return;
            canMove = true;
            canShoot = false;
            if(currentModeInputIndex != currentModeIndex)
            {
                currentModeInputIndex = currentModeIndex;
                HideTiles();
                GetInRangeTiles();
            }
        }
        else if(currentModeIndex == 1)
        {
            canMove = false;
            canShoot = true;
            if (!unitManager.ships[currentShipIndex].canShoot.Value) return;
            if (currentModeInputIndex != currentModeIndex)
            {
                currentModeInputIndex = currentModeIndex;
                HideTiles();
                GetInRangeShootTiles();
            }
        }
    }

    [ClientRpc]
    public void HasAttackedEnemyClientRpc()
    {
        if (!IsOwner) return;

        totalShootPoint--;
        unitManager.ships[currentShipIndex].canShoot.Value = false;
        if (unitManager.ships[currentShipIndex].canMove.Value)
        {
            totalMovePoint--;
            unitManager.ships[currentShipIndex].canMove.Value = false;
        }
        TotalActionPoint();
    }

    void GetInRangeTiles()
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);

        inRangeTiles.Clear();
        inRangeTiles = PathFindTesting.GetInRangeTiles(unitManager.ships[currentShipIndex].currentTile, unitManager.ships[currentShipIndex].unitMoveRange);

        foreach (var t in inRangeTiles) t.HighLightRange(true);
    }

    void GetInRangeShootTiles()
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);

        inRangeTiles.Clear();
        inRangeTiles = PathFindTesting.GetInRangeTilesCross(unitManager.ships[currentShipIndex].currentTile, unitManager.ships[currentShipIndex].unitShootRange);

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
        unitManager.ships[currentShipIndex].canMove.Value = false;
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

        if (unitManager.numShipSpawned >= unitManager.ships.Length && !unitManager.allShipSpawned.Value)
        {
            unitManager.allShipSpawned.Value = true;
            TotalActionPoint();
        }
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

    #endregion

    RaycastHit2D? GetCurrentTile(Vector2 pos)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero);
        if (hits.Length > 0) return hits.OrderByDescending(i => i.collider.transform.position.z).First();
        return null;
    }

    bool CanMoveUnit(TileScript t)
    {
        return unitManager.allShipSpawned.Value && shipSelected && path.Count > 0 && !unitMoving && inRangeTiles.Contains(t) && canMove;
    }

    bool CantPathfind(TileScript tile)
    {
        return unitManager.ships[currentShipIndex].currentTile == null || unitMoving || !inRangeTiles.Contains(tile) || !canPlay.Value || !shipSelected || !unitManager.ships[currentShipIndex].canMove.Value || !canMove;
    }

}
