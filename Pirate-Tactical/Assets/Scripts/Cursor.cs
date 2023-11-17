using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class Cursor : NetworkBehaviour
{
    [Header("Gold")]
    public int playerGold;
    public int playerGoldGainPerRound;

    [Header("Special Ability")]
    public int maxSpecialCharge;
    public int specialGainPerRound;

    [Header("Ship Selected")]
    public int currentShipIndex;
    public bool shipSelected = false;

    public NetworkVariable<bool> canPlay = new NetworkVariable<bool>(false);
    public NetworkVariable<int> totalPlayerHealth = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public List<TileScript> inRangeTiles = new List<TileScript>();

    public int currentModeIndex;

    [Header("unitManager")]
    public UnitManager unitManager;
    public float totalMovePoint;
    public float totalShootPoint;
    float totalActionPoint;

    int currentModeInputIndex;
    [HideInInspector]
    public int currentSpecialCharge;

    bool unitMoving = false;
    bool canShoot;
    bool canMove;

    TileScript goalTile;
    List<TileScript> path = new List<TileScript>();
    List<TileScript> allTiles = new List<TileScript>();

    private void Start()
    {
        if (!IsClient) return;
        TileScript.OnHoverTile += OnTileHover;
        GameManager.Instance.JoinServerServerRpc();

        GridManager.Instance.map.gameObject.SetActive(false);

        TileScript[] tiles = FindObjectsOfType<TileScript>();
        foreach (var t in tiles)
        {
            allTiles.Add(t);
            t.SetColor(3);
        }

        currentSpecialCharge = maxSpecialCharge;
        GameManager.Instance.SendGameManagerNameServerRpc(LobbyScript.Instance.playerName, NetworkManager.LocalClientId);
    }

    void Update()
    {
        if(!IsOwner) return;

        MyInputs();
        HandleCurrentMode();
    }

    #region SetClientRpcValues

    [ClientRpc]
    public void RechargeSpecialClientRpc()
    {
        currentSpecialCharge += specialGainPerRound;
        
        if(currentSpecialCharge > maxSpecialCharge) 
            currentSpecialCharge = maxSpecialCharge;
    }

    [ClientRpc]
    public void CalculateHealthClientRpc()
    {
        int newHealth = 0;
        foreach (ShipUnit s in unitManager.ships)
        {
            newHealth += s.unitLife.Value;
        }

        SetHealthServerRpc(newHealth);
        if (GameManager.Instance.currentRound.Value > 0)
            HUD.Instance.UpdateHealthBarClientRpc();
    }

    [ClientRpc]
    public void GoldGainClientRpc()
    {
        playerGold += playerGoldGainPerRound;
    }

    #endregion
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


    [ServerRpc(RequireOwnership = false)]
    void SetHealthServerRpc(int health)
    {
        totalPlayerHealth.Value = health;
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

        HandleKeyboardInputs();

        if (Input.GetMouseButtonDown(0) && shipSelected)
        {
            if (t.shipOnTile.Value && canShoot && inRangeTiles.Contains(t))
            {
                //Classic attack on another unit

                if (!unitManager.ships[currentShipIndex].canShoot.Value) return;

                GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].damage, t.pos.Value, NetworkManager.LocalClientId, false, 0, false);
            }
            else if (CanMoveUnit(t))
            {
                //Move unit to new position

                if (unitManager.ships[currentShipIndex].canMove.Value)
                    StartCoroutine(UpdateShipPlacementOnGrid());
            }
            else if (currentModeIndex == 3 && inRangeTiles.Contains(t))
            {
                //Special mode that act on terrain/tiles
                if (!unitManager.ships[currentShipIndex].canShoot.Value) return;
                if (unitManager.ships[currentShipIndex].specialAbilityCost <= currentSpecialCharge)
                    HandleSpecialUnitAttackOnTile(t);
            }
            else if (currentModeIndex == 4 && inRangeTiles.Contains(t))
            {
                //Special mode that act on unit
                if (!unitManager.ships[currentShipIndex].canShoot.Value) return;
                if (unitManager.ships[currentShipIndex].specialAbilityCost <= currentSpecialCharge)
                    HandleSpecialUnitAttackOnUnit(t);
            }
            else if (!CanMoveUnit(t) && !unitMoving)
            {
                //Highlight tiles next to ship
                if (t.shipOnTile.Value && currentModeIndex == 0 && unitManager.ships[currentModeIndex].canBeSelected.Value)
                {
                    bool allyUnit = false;
                    foreach(var u in unitManager.ships)
                    {
                        if (t.pos.Value == u.unitPos.Value)
                        {
                            allyUnit = true;
                            break;
                        }
                    }

                    if (allyUnit)
                    {
                        SelectShip(t);
                        shipSelected = false;
                        HideTiles();
                    }
                    else if (!allyUnit && unitManager.ships[currentModeIndex].canShoot.Value)
                        AccostShip(t);
                }
            }
        }
        else if (Input.GetMouseButtonDown(0) && !shipSelected)
        {
            if (!unitManager.allShipSpawned.Value)
            {
                if(t.Walkable && !t.shipOnTile.Value)
                    SpawnShip(t.pos.Value, t);
                if (currentShipIndex >= unitManager.ships.Length) currentShipIndex = 0;

            }
            else if (t.shipOnTile.Value && unitManager.allShipSpawned.Value)
            {
                SelectShip(t);
            }
        }


        if (Input.GetButtonDown("Cancel") || (shipSelected && !unitManager.ships[currentShipIndex].canBeSelected.Value))
        {
            shipSelected = false;
            HideTiles();
        }

    }

    void HandleKeyboardInputs()
    {
        if(Input.GetKeyDown(KeyCode.Alpha1))
            currentModeIndex = 1;
        if(Input.GetKeyDown(KeyCode.Alpha2))
            currentModeIndex = 2;
        if(Input.GetKeyDown(KeyCode.Alpha3))
            currentModeIndex = 4;
        if(Input.GetKeyDown(KeyCode.Alpha4))
            currentModeIndex = 3;
    }

    void SelectShip(TileScript t)
    {
        foreach (var ship in unitManager.ships)
        {
            if (ship.unitPos.Value == t.pos.Value && ship.clientIdOwner == NetworkManager.LocalClientId && ship.canBeSelected.Value)
            {
                currentShipIndex = ship.index;
                shipSelected = true;
                DisplayOnSelectedUnit();
                break;
            }
        }
    }

    void AccostShip(TileScript t)
    {
        Vector2 posTocheck = unitManager.ships[currentShipIndex].unitPos.Value;

        bool canAccostShip = false;

        for (int i = 0; i < 4; i++)
        {
            switch (i)
            {
                case 0:
                    posTocheck = new Vector2(posTocheck.x + 1, posTocheck.y);
                    if (t.pos.Value == posTocheck) canAccostShip = true;
                    break;
                case 1:
                    posTocheck = new Vector2(posTocheck.x - 1, posTocheck.y);
                    if (t.pos.Value == posTocheck) canAccostShip = true;
                    break;
                case 2:
                    posTocheck = new Vector2(posTocheck.x, posTocheck.y + 1);
                    if (t.pos.Value == posTocheck) canAccostShip = true;
                    break;
                case 3:
                    posTocheck = new Vector2(posTocheck.x, posTocheck.y - 1);
                    if (t.pos.Value == posTocheck) canAccostShip = true;
                    break;
            }

            if (canAccostShip) break;
        }

        if (!canAccostShip) return;

        Debug.Log("Ship can be accost");

        GridManager.Instance.DamageAccostServerRpc(unitManager.ships[currentShipIndex].unitPos.Value, t.pos.Value, NetworkManager.LocalClientId);
    }

    void DisplayOnSelectedUnit()
    {
        if (unitManager.ships[currentShipIndex].canMove.Value)
        {
            currentModeIndex = 1;
            currentModeInputIndex = currentModeIndex;
            canMove = true;
            canShoot = false;
            HideTiles();
            GetInRangeTiles(unitManager.ships[currentShipIndex].unitMoveRange);
        }
        else if (unitManager.ships[currentShipIndex].canBeSelected.Value)
        {
            currentModeIndex = 0;
            currentModeInputIndex = currentModeIndex;
            canMove = false;
            canShoot = false;
            HideTiles();
        }
    }

    void HandleCurrentMode()
    {
        if (currentModeIndex > 4) currentModeIndex = 0;
        else if (currentModeIndex < 0) currentModeIndex = 4;

        if (!shipSelected || !unitManager.ships[currentShipIndex].canBeSelected.Value) return;

        if(currentModeIndex == 0)
        {
            //InteractMode
            if (!unitManager.ships[currentShipIndex].canBeSelected.Value) return;
            canMove = false;
            canShoot = false;

            if (currentModeInputIndex != currentModeIndex)
            {
                currentModeInputIndex = currentModeIndex;
                GetInRangeTiles(1);
            }

        }
        else if (currentModeIndex == 1) 
        {
            //Move Mode
            if (!unitManager.ships[currentShipIndex].canMove.Value) return;
            canMove = true;
            canShoot = false;
            if(currentModeInputIndex != currentModeIndex)
            {
                currentModeInputIndex = currentModeIndex;
                GetInRangeTiles(unitManager.ships[currentShipIndex].unitMoveRange);
            }
        }
        else if(currentModeIndex == 2)
        {
            //shoot mode
            canMove = false;
            canShoot = true;
            if (!unitManager.ships[currentShipIndex].canShoot.Value) return;
            if (currentModeInputIndex != currentModeIndex)
            {
                currentModeInputIndex = currentModeIndex;
                GetInRangeShootTiles(unitManager.ships[currentShipIndex].unitShootRange);
            }
        }
        else if (currentModeIndex == 3)
        {
            //Special tile
            canMove = false;
            canShoot = false;
            if (currentModeInputIndex != currentModeIndex)
            {
                currentModeInputIndex = currentModeIndex;
                GetInRangeTiles(unitManager.ships[currentShipIndex].specialTileRange);
            }
        }
        else if (currentModeIndex == 4)
        {
            //Special Shot
            canMove = false;
            canShoot = false;
            if (currentModeInputIndex != currentModeIndex)
            {
                currentModeInputIndex = currentModeIndex;
                GetInRangeShootTiles(unitManager.ships[currentShipIndex].specialShootRange);
            }
        }

    }

    void HandleSpecialUnitAttackOnTile(TileScript t)
    {
        if (!t.Walkable && t.shipOnTile.Value) return;

        switch (unitManager.ships[currentShipIndex].unitSpecialTile)
        {
            case ShipUnit.UnitSpecialTile.BlockTile:
                GridManager.Instance.BlockedTileServerRpc(t.pos.Value, NetworkManager.LocalClientId);
                break;
            case ShipUnit.UnitSpecialTile.Mine:
                GridManager.Instance.SetMineOnTileServerRpc(t.pos.Value, NetworkManager.LocalClientId, true);
                break;
            case ShipUnit.UnitSpecialTile.Teleport:
                TeleportShip(t);
                break;
            case ShipUnit.UnitSpecialTile.None:
                break;
        }
    }

    void HandleSpecialUnitAttackOnUnit(TileScript t)
    {
        switch (unitManager.ships[currentShipIndex].unitSpecialShot)
        {
            case ShipUnit.UnitSpecialShot.PushUnit:
                GridManager.Instance.PushUnitServerRpc(t.pos.Value, unitManager.ships[currentShipIndex].unitPos.Value, NetworkManager.LocalClientId);
                break;
            case ShipUnit.UnitSpecialShot.TShot:
                TShotFunction(t);
                break;
            case ShipUnit.UnitSpecialShot.FireShot:
                GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].specialAbilityDamage, t.pos.Value, NetworkManager.LocalClientId, true, unitManager.ships[currentShipIndex].specialAbilityPassiveDuration, true);
                break;
            case ShipUnit.UnitSpecialShot.None:
                break;

        }
    }

    [ClientRpc]
    public void UseManaClientRpc()
    {
        if(!IsOwner) return;
        currentSpecialCharge -= unitManager.ships[currentShipIndex].specialAbilityCost;
    }

    void TeleportShip(TileScript t)
    {
        //Ajouter tout des chekcs

        if (t.shipOnTile.Value || !t.Walkable || t.blockedTile.Value) return;
        GridManager.Instance.SetShipOnTileServerRpc(unitManager.ships[currentShipIndex].currentTile.pos.Value, false);
        UnitNewPosServerRpc(t.pos.Value, currentShipIndex);
        GridManager.Instance.SetShipOnTileServerRpc(t.pos.Value, true);
        unitManager.ships[currentShipIndex].currentTile = t;

        bool stepOnMine = false;
        if (t.mineInTile.Value)
        {
            stepOnMine = true;
            GridManager.Instance.SetMineOnTileServerRpc(t.pos.Value, NetworkManager.LocalClientId, false);
        }


        if (stepOnMine)
            GridManager.Instance.DamageUnitByMineServerRpc(GridManager.Instance.mineDamage, t.pos.Value, false, 0);

        UseManaClientRpc();
        HasDidAnActionClientRpc();
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

                    GridManager.Instance.DamageUnitTShotServerRpc(unitManager.ships[currentShipIndex].specialAbilityDamage / 2, posToCheck, NetworkManager.LocalClientId, false, 0);
                    break;
                }
            }
            for (int x = 1; x < 3; x++)
            {
                posToCheck = new Vector2(t.pos.Value.x - x, t.pos.Value.y);
                if (GridManager.Instance.GetTileAtPosition(posToCheck).shipOnTile.Value)
                {
                    Debug.Log("Ship");

                    GridManager.Instance.DamageUnitTShotServerRpc(unitManager.ships[currentShipIndex].specialAbilityDamage / 2, posToCheck, NetworkManager.LocalClientId, false, 0);
                    break;
                }
            }
            GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].specialAbilityDamage, t.pos.Value, NetworkManager.LocalClientId, false, 0, true);

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
                    GridManager.Instance.DamageUnitTShotServerRpc(unitManager.ships[currentShipIndex].specialAbilityDamage / 2, posToCheck, NetworkManager.LocalClientId, false, 0);
                    break;
                }
            }
            for (int y = 1; y < 3; y++)
            {
                posToCheck = new Vector2(t.pos.Value.x, t.pos.Value.y - y);
                if (GridManager.Instance.GetTileAtPosition(posToCheck).shipOnTile.Value)
                {
                    Debug.Log("Ship");

                    GridManager.Instance.DamageUnitTShotServerRpc(unitManager.ships[currentShipIndex].specialAbilityDamage / 2 , posToCheck, NetworkManager.LocalClientId, false, 0);
                    break;
                }
            }

            GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].specialAbilityDamage, t.pos.Value, NetworkManager.LocalClientId, false, 0, true);
        }
    }

    #region Tiles Function
    void GetInRangeTiles(int shipRange)
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);

        inRangeTiles.Clear();
        inRangeTiles = PathfindScript.GetInRangeTiles(unitManager.ships[currentShipIndex].currentTile, unitManager.ships[currentShipIndex].unitMoveRange);

        foreach (var t in inRangeTiles) t.HighLightRange(true);
    }

    void GetInRangeShootTiles(int shootRange)
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);

        inRangeTiles.Clear();
        inRangeTiles = PathfindScript.GetInRangeTilesCross(unitManager.ships[currentShipIndex].currentTile, unitManager.ships[currentShipIndex].unitShootRange);

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
        path = PathfindScript.Pathfind(unitManager.ships[currentShipIndex].currentTile, goalTile);
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
            GridManager.Instance.DamageUnitByMineServerRpc(GridManager.Instance.mineDamage, unitManager.ships[currentShipIndex].currentTile.pos.Value, false, 0);

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
    public void HasDidAnActionClientRpc()
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
