using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Cursor : NetworkBehaviour
{
    [Header("Ship Selected")]
    public int currentShipIndex;
    public bool shipSelected = false;

    public NetworkVariable<bool> canPlay = new NetworkVariable<bool>(false);

    public List<TileScript> inRangeTiles = new List<TileScript>();

    public int currentModeIndex;

    [Header("unitManager")]
    public UnitManager unitManager;
    public float totalMovePoint;
    public float totalShootPoint;
    float totalActionPoint;

    int currentModeInputIndex;

    bool unitMoving = false;
    bool canShoot;
    bool canMove;

    TileScript goalTile;
    List<TileScript> path = new List<TileScript>();
    List<TileScript> allTiles = new List<TileScript>();
    
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
        if (!IsOwner) return;

        if(totalShootPoint < 0) totalShootPoint = 0;
        if (totalMovePoint < 0) totalMovePoint = 0;
        
        totalActionPoint = totalMovePoint + totalShootPoint;

        if (GameManager.Instance.gametesting.Value) return;

        if(totalActionPoint <= 0 && unitManager.allShipSpawned.Value)
        {
            totalMovePoint = 0;
            totalShootPoint = 0;

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
        if (!IsOwner) return;

        for (int i = 0; i < unitManager.ships.Length; i++)
        {
            if (unitManager.ships[i] == null) continue;

            unitManager.ships[i].canBeSelected.Value = true;
            unitManager.ships[i].canShoot.Value = true;
            unitManager.ships[i].canMove.Value = true;
            totalShootPoint++;
            totalMovePoint++;
        }

        TotalActionPoint();
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
            currentModeIndex--;
        else if(Input.GetAxis("Mouse ScrollWheel") < 0)
            currentModeIndex++;

        if (Input.GetMouseButtonDown(0) && shipSelected)
        {
            if (t.shipOnTile.Value && canShoot && inRangeTiles.Contains(t))
            {
                //Classic attack on another unit

                if (!unitManager.ships[currentShipIndex].canShoot.Value) return;

                GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].damage, t.pos.Value, NetworkManager.LocalClientId, false, 0);
            }
            else if (CanMoveUnit(t))
            {
                //Move unit to new position

                if (unitManager.ships[currentShipIndex].canMove.Value)
                    StartCoroutine(UpdateShipPlacementOnGrid());
            }
            else if (currentModeIndex == 2 && inRangeTiles.Contains(t))
            {
                //Special mode that act on terrain/tiles
                if (!unitManager.ships[currentShipIndex].canShoot.Value) return;

                HandleSpecialUnitAttackOnTile(t);
            }
            else if (currentModeIndex == 3 && inRangeTiles.Contains(t))
            {
                //Special mode that act on unit
                if (!unitManager.ships[currentShipIndex].canShoot.Value) return;

                HandleSpecialUnitAttackOnUnit(t);
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
        if (currentModeIndex > 3) currentModeIndex = 0;
        else if (currentModeIndex < 0) currentModeIndex = 3;

        if (!shipSelected || !unitManager.ships[currentShipIndex].canBeSelected.Value) return;

        if (currentModeIndex == 0) 
        {
            //Move Mode
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
            //shoot mode
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
        else if (currentModeIndex == 2)
        {
            //Special tile
            canMove = false;
            canShoot = false;
            if (currentModeInputIndex != currentModeIndex)
            {
                currentModeInputIndex = currentModeIndex;
                HideTiles();
                GetInRangeTiles();
            }
        }
        else if (currentModeIndex == 3)
        {
            //Special Shot
            canMove = false;
            canShoot = false;
            if (currentModeInputIndex != currentModeIndex)
            {
                currentModeInputIndex = currentModeIndex;
                HideTiles();
                GetInRangeShootTiles();
            }
        }

    }

    void HandleSpecialUnitAttackOnTile(TileScript t)
    {
        //If statement to check what is the special of the current unit
        if (unitManager.ships[currentShipIndex].unitSpecialTile == ShipUnit.UnitSpecialTile.BlockTile)
        {
            GridManager.Instance.BlockedTileServerRpc(t.pos.Value);
        }
        else if(unitManager.ships[currentShipIndex].unitSpecialTile == ShipUnit.UnitSpecialTile.Mine)
        {
            GridManager.Instance.SetMineOnTileServerRpc(t.pos.Value, NetworkManager.LocalClientId, true);
        }
        else if(unitManager.ships[currentShipIndex].unitSpecialTile == ShipUnit.UnitSpecialTile.None)
        {
            //Do nothing, can't select if there is no special to the unit
            return;
        }

        totalShootPoint--;
        unitManager.ships[currentShipIndex].canShoot.Value = false;
        
        if (unitManager.ships[currentShipIndex].canMove.Value)
        {
            totalMovePoint--;
            unitManager.ships[currentShipIndex].canMove.Value = false;
        }
        
        TotalActionPoint();
    }

    void HandleSpecialUnitAttackOnUnit(TileScript t)
    {
        //If statement to check what is the special of the current unit
        if (unitManager.ships[currentShipIndex].unitSpecialShot == ShipUnit.UnitSpecialShot.PushUnit)
        {
            //A décider comment cette mécanique fonctionne concretement
        }
        else if (unitManager.ships[currentShipIndex].unitSpecialShot == ShipUnit.UnitSpecialShot.TShot)
        {
            TShotFunction(t);
        }
        else if (unitManager.ships[currentShipIndex].unitSpecialShot == ShipUnit.UnitSpecialShot.FireShot)
        {
            GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].damage, t.pos.Value, NetworkManager.LocalClientId, true, unitManager.ships[currentShipIndex].specialAbilityPassiveDuration);
        }
        else if (unitManager.ships[currentShipIndex].unitSpecialShot == ShipUnit.UnitSpecialShot.None)
        {
            //Do nothing, can't select if there is no special to the unit
        }
    }

    void TShotFunction(TileScript t)
    {
        if (t.pos.Value.x == unitManager.ships[currentShipIndex].currentTile.pos.Value.x)
        {
            Debug.Log("Same X");

            //GetShips on Y axis with for loop
            Vector2 posToCheck;
            for (int x = 1; x < 3; x++)
            {
                posToCheck = new Vector2(t.pos.Value.x + x, t.pos.Value.y);
                if (GridManager.Instance.GetTileAtPosition(posToCheck).shipOnTile.Value)
                {
                    Debug.Log("Ship");

                    GridManager.Instance.DamageUnitTShotServerRpc(unitManager.ships[currentShipIndex].damage, posToCheck, NetworkManager.LocalClientId, false, 0);
                    break;
                }
            }
            for (int x = 1; x < 3; x++)
            {
                posToCheck = new Vector2(t.pos.Value.x - x, t.pos.Value.y);
                if (GridManager.Instance.GetTileAtPosition(posToCheck).shipOnTile.Value)
                {
                    Debug.Log("Ship");

                    GridManager.Instance.DamageUnitTShotServerRpc(unitManager.ships[currentShipIndex].damage, posToCheck, NetworkManager.LocalClientId, false, 0);
                    break;
                }
            }
            GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].damage, t.pos.Value, NetworkManager.LocalClientId, false, 0);

        }
        else if (t.pos.Value.y == unitManager.ships[currentShipIndex].currentTile.pos.Value.y)
        {
            Debug.Log("Same Y");

            //GetShips on X axis with for loop
            Vector2 posToCheck;
            for (int y = 1; y < 3; y++)
            {
                posToCheck = new Vector2(t.pos.Value.x, t.pos.Value.y + y);
                if (GridManager.Instance.GetTileAtPosition(posToCheck).shipOnTile.Value)
                {
                    Debug.Log("Ship");
                    GridManager.Instance.DamageUnitTShotServerRpc(unitManager.ships[currentShipIndex].damage, posToCheck, NetworkManager.LocalClientId, false, 0);
                    break;
                }
            }
            for (int y = 1; y < 3; y++)
            {
                posToCheck = new Vector2(t.pos.Value.x, t.pos.Value.y - y);
                if (GridManager.Instance.GetTileAtPosition(posToCheck).shipOnTile.Value)
                {
                    Debug.Log("Ship");

                    GridManager.Instance.DamageUnitTShotServerRpc(unitManager.ships[currentShipIndex].damage, posToCheck, NetworkManager.LocalClientId, false, 0);
                    break;
                }
            }

            GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].damage, t.pos.Value, NetworkManager.LocalClientId, false, 0);

        }
    }

    #region Tiles Function
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
    #endregion

    IEnumerator UpdateShipPlacementOnGrid()
    {
        unitMoving = true;
        int value = path.Count - 1;
        bool stepOnMine = false;

        GridManager.Instance.SetShipOnTileServerRpc(unitManager.ships[currentShipIndex].currentTile.pos.Value, false);

        while (path.Count > 0)
        {
            UnitNewPosServerRpc(path[value].pos.Value, currentShipIndex);

            if (path[value].mineInTile.Value)
            {
                unitManager.ships[currentShipIndex].currentTile = path[value];
                stepOnMine = true;

                foreach (var item in allTiles)
                    item.SetColor(3);

                GridManager.Instance.SetMineOnTileServerRpc(path[value].pos.Value, NetworkManager.LocalClientId, false);

                break;
            }
            
            if(path.Count == 1) 
                unitManager.ships[currentShipIndex].currentTile = path[0];
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

        if(stepOnMine)
            GridManager.Instance.DamageUnitByMineServerRpc(13, unitManager.ships[currentShipIndex].currentTile.pos.Value, false, 0);

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

    #endregion

    RaycastHit2D? GetCurrentTile(Vector2 pos)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero);
        if (hits.Length > 0) return hits.OrderByDescending(i => i.collider.transform.position.z).First();
        return null;
    }

    bool CanMoveUnit(TileScript t)
    {
        return unitManager.allShipSpawned.Value && shipSelected && path.Count > 0 && !unitMoving && inRangeTiles.Contains(t) && canMove && !t.blockedTile.Value;
    }

    bool CantPathfind(TileScript tile)
    {
        return unitManager.ships[currentShipIndex].currentTile == null || unitMoving || !inRangeTiles.Contains(tile) || !canPlay.Value || !shipSelected || !unitManager.ships[currentShipIndex].canMove.Value || !canMove || tile.blockedTile.Value;
    }

}
