using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

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

    [Header("Current Mode")]
    public int currentModeIndex;

    [Header("unitManager")]
    public UnitManager unitManager;
    public float totalMovePoint;
    public float totalShootPoint;
    float totalActionPoint;

    [HideInInspector]
    public int currentSpecialCharge;

    int currentModeInputIndex;

    bool unitMoving = false;
    bool canShoot;
    bool canMove;

    TileScript cTile = null;
    TileScript goalTile;
    List<TileScript> path = new List<TileScript>();
    List<TileScript> allTiles = new List<TileScript>();
    List<TileScript> inRangeTiles = new List<TileScript>();

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

    #region ClientRpcMethods

    [ClientRpc]
    public void RechargeSpecialClientRpc()
    {
        currentSpecialCharge += specialGainPerRound;
        
        if(currentSpecialCharge > maxSpecialCharge) 
            currentSpecialCharge = maxSpecialCharge;

        SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.manaGain);
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

        SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.goldGain);
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

    [ClientRpc]
    public void UseManaClientRpc()
    {
        if (!IsOwner) return;
        currentSpecialCharge -= unitManager.ships[currentShipIndex].specialAbilityCost;
    }

    [ClientRpc]
    void LinkUnitToClientRpc(ulong unitID, int index)
    {
        foreach (NetworkObject obj in FindObjectsOfType<NetworkObject>())
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

    #region ServerRpcMethods

    [ServerRpc(RequireOwnership = false)]
    void SetHealthServerRpc(int health)
    {
        totalPlayerHealth.Value = health;
    }

    [ServerRpc(RequireOwnership = false)]
    void UnitNewPosServerRpc(Vector2 pos, int index)
    {
        unitManager.ships[index].unitPos.Value = pos;
    }

    [ServerRpc]
    void SpawnUnitServerRpc(Vector2 pos, ulong id, int index)
    {
        ShipUnit ship = Instantiate(NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>().unitManager.ships[index]);
        ship.damage.Value = ship.unitDamage;
        ship.GetComponent<NetworkObject>().SpawnWithOwnership(id);

        LinkUnitToClientRpc(ship.GetComponent<NetworkObject>().NetworkObjectId, index);

        //unitManager.ships[index] = ship;
        unitManager.ships[index].unitPos.Value = new Vector3(pos.x, pos.y, -1);
        unitManager.ships[index].SetShipColorClientRpc(id);
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

    void MyInputs()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 pos = new Vector2(mousePos.x, mousePos.y);
        transform.position = new Vector3(pos.x, pos.y, -5);

        RaycastHit2D? tile = GetCurrentTile(pos);

        if (!tile.HasValue) return;
        cTile = tile.Value.transform.GetComponent<TileScript>();

        if (!canPlay.Value)
        {
            shipSelected = false;
            HideTiles();
            return;
        }

        if (HUD.Instance.isInUpgradeWindow)
        {
            if (Input.GetButtonDown("Cancel"))
                HUD.Instance.UpgradeWindow(false,0);
            return;
        }

        HandleKeyboardInputs();

        if (Input.GetMouseButtonDown(0) && shipSelected)
        {
            switch (currentModeIndex)
            {
                case 0: //Interact Mode
                    if (!inRangeTiles.Contains(cTile))
                    {
                        if (cTile.shipOnTile.Value && currentModeIndex == 0 && unitManager.ships[currentModeIndex].canBeSelected.Value)
                        {
                            SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.deselectShip);
                            shipSelected = false;
                            HideTiles();
                            SelectShip(cTile);
                        }
                    }
                    else if (inRangeTiles.Contains(cTile))
                    {
                        if (cTile.shipOnTile.Value && unitManager.ships[currentShipIndex].canShoot.Value) //Aborder une autre unité
                        {
                            bool alliesUnit = false;
                            foreach (var ship in unitManager.ships)
                            {
                                if (ship.unitPos.Value == cTile.pos.Value && ship.clientIdOwner == NetworkManager.LocalClientId && ship.canBeSelected.Value)
                                {
                                    alliesUnit = true;
                                    break;
                                }
                            }
                            if (!alliesUnit)
                                GridManager.Instance.DamageAccostServerRpc(unitManager.ships[currentShipIndex].unitPos.Value, cTile.pos.Value, NetworkManager.LocalClientId);
                        }
                        else if(cTile.ShopTile && cTile.canAccessShop && unitManager.ships[currentShipIndex].canShoot.Value && unitManager.ships[currentShipIndex].canBeUpgrade) //Acheter amélioration
                        {
                            //Code to buy upgrade
                            Debug.Log("Acheter amélioration");
                            if (unitManager.ships[currentShipIndex].canBeUpgrade)
                                HUD.Instance.UpgradeWindow(true, cTile.shopIndex.Value);
                        }
                    }
                    break;
                case 1: //Move Mode
                    if (!inRangeTiles.Contains(cTile)) return;

                    if (CanMoveUnit(cTile) && unitManager.ships[currentShipIndex].canMove.Value)
                            StartCoroutine(UpdateShipPlacementOnGrid());
                    break;
                case 2: //Attack Mode
                    if (!inRangeTiles.Contains(cTile)) return;

                    if(cTile.shipOnTile.Value && canShoot && unitManager.ships[currentShipIndex].canShoot.Value)
                    {
                        GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].damage.Value, cTile.pos.Value, NetworkManager.LocalClientId, false, 0, false);
                        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.attack);
                    }
                    break;
                case 3: //Special Shoot Mode
                    if (!inRangeTiles.Contains(cTile)) return;

                    if (unitManager.ships[currentShipIndex].canShoot.Value && unitManager.ships[currentShipIndex].specialAbilityCost <= currentSpecialCharge)
                        HandleSpecialModifyTile(cTile);
                    break;
                case 4: //Special Tile Mode
                    if (!inRangeTiles.Contains(cTile)) return;

                    if (unitManager.ships[currentShipIndex].canShoot.Value && unitManager.ships[currentShipIndex].specialAbilityCost <= currentSpecialCharge)
                        HandleSpecialUnitAttackOnUnit(cTile);
                    break;
            }
        }
        else if (Input.GetMouseButtonDown(0) && !shipSelected)
        {
            if (cTile.shipOnTile.Value && unitManager.allShipSpawned.Value)
                SelectShip(cTile);
            else if (!unitManager.allShipSpawned.Value)
            {
                if (cTile.Walkable && !cTile.shipOnTile.Value)
                    SpawnShip(cTile.pos.Value, cTile);
                if (currentShipIndex >= unitManager.ships.Length) currentShipIndex = 0;
            }
        }

        if (Input.GetButtonDown("Cancel") || (shipSelected && !unitManager.ships[currentShipIndex].canBeSelected.Value))
        {
            shipSelected = false;
            SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.deselectShip);
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
                SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.selectShip);
                break;
            }
        }
    }

    void DisplayOnSelectedUnit()
    {
        if (unitManager.ships[currentShipIndex].canMove.Value)
        {
            currentModeIndex = 1;
            currentModeInputIndex = currentModeIndex;
            canMove = true;
            canShoot = false;
            GetInRangeTiles(unitManager.ships[currentShipIndex].unitMoveRange);
        }
        else if (unitManager.ships[currentShipIndex].canBeSelected.Value)
        {
            currentModeIndex = 0;
            currentModeInputIndex = currentModeIndex;
            canMove = false;
            canShoot = false;
            GetInRangeInteractTile();
        }
    }

    void HandleCurrentMode()
    {
        if (currentModeIndex > 4) currentModeIndex = 0;
        else if (currentModeIndex < 0) currentModeIndex = 4;

        if (!shipSelected || !unitManager.ships[currentShipIndex].canBeSelected.Value)
        {
            HideTiles();
            return;
        }

        switch (currentModeIndex)
        {

            case 0: //InteractMode
                if (currentModeInputIndex != currentModeIndex)
                {
                    currentModeInputIndex = currentModeIndex;
                    GetInRangeInteractTile();
                }
                break;
            case 1: //Move Mode
                if (currentModeInputIndex != currentModeIndex)
                {
                    if (!unitManager.ships[currentShipIndex].canMove.Value)
                    {
                        currentModeIndex++;
                        return;
                    }
                    currentModeInputIndex = currentModeIndex;
                    GetInRangeTiles(unitManager.ships[currentShipIndex].unitMoveRange);
                }
                break;
            case 2: //shoot mode
                
                if (currentModeInputIndex != currentModeIndex)
                {
                    if (!unitManager.ships[currentShipIndex].canShoot.Value)
                    {
                        currentModeIndex++;
                        return;
                    }
                    currentModeInputIndex = currentModeIndex;
                    GetInRangeShootTiles(unitManager.ships[currentShipIndex].unitShootRange);
                }
                break;
            case 3: //Special tile
                if (currentModeInputIndex != currentModeIndex)
                {
                    if (!unitManager.ships[currentShipIndex].canShoot.Value)
                    {
                        currentModeIndex++;
                        return;
                    }
                    currentModeInputIndex = currentModeIndex;
                    GetInRangeTiles(unitManager.ships[currentShipIndex].specialTileRange);
                }
                break;
            case 4: //Special Shot
                if (currentModeInputIndex != currentModeIndex)
                {
                    if (!unitManager.ships[currentShipIndex].canShoot.Value)
                    {
                        currentModeIndex++;
                        return;
                    }
                    currentModeInputIndex = currentModeIndex;
                    GetInRangeShootTiles(unitManager.ships[currentShipIndex].specialShootRange);
                }
                break;
        }

        switch (currentModeIndex)
        {
            case 0:
                canMove = false;
                canShoot = false;
                break;
            case 1:
                canMove = true;
                canShoot = false;
                break;
            case 2:
                canMove = false;
                canShoot = true;
                break;
            case 3:
                canMove = false;
                canShoot = false;
                break;
            case 4:
                canMove = false;
                canShoot = false;
                break;
        }

        if(currentModeIndex != currentModeInputIndex)
            SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.changeShipMode);
    }

    #region SpecialCapacities

    void HandleSpecialModifyTile(TileScript t)
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

        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.tileCapacity);
    }

    void HandleSpecialUnitAttackOnUnit(TileScript t)
    {
        switch (unitManager.ships[currentShipIndex].unitSpecialShot)
        {
            case ShipUnit.UnitSpecialShot.PushUnit:
                GridManager.Instance.PushUnitServerRpc(t.pos.Value, unitManager.ships[currentShipIndex].unitPos.Value, NetworkManager.LocalClientId);
                break;
            case ShipUnit.UnitSpecialShot.TShot:
                StartCoroutine(TShotFunction(t));
                break;
            case ShipUnit.UnitSpecialShot.FireShot:
                GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].specialAbilityDamage, t.pos.Value, NetworkManager.LocalClientId, true, unitManager.ships[currentShipIndex].specialAbilityPassiveDuration, true);
                break;
            case ShipUnit.UnitSpecialShot.None:
                break;

        }

        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.offensiveCapacity);
    }

    void TeleportShip(TileScript t)
    {
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

        currentSpecialCharge -= unitManager.ships[currentShipIndex].specialAbilityCost;
        totalMovePoint--;
        totalShootPoint--;
        unitManager.ships[currentShipIndex].canMove.Value = false;
        unitManager.ships[currentShipIndex].canShoot.Value = false;
        TotalActionPoint();
    }

    IEnumerator TShotFunction(TileScript t)
    {
        if (t.pos.Value.x == unitManager.ships[currentShipIndex].currentTile.pos.Value.x)
        {
            GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].specialAbilityDamage, t.pos.Value, NetworkManager.LocalClientId, false, 0, true);

            Debug.Log("Same X");

            yield return new WaitForSeconds(.5f);

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
        }
        else if (t.pos.Value.y == unitManager.ships[currentShipIndex].currentTile.pos.Value.y)
        {
            GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].specialAbilityDamage, t.pos.Value, NetworkManager.LocalClientId, false, 0, true);

            Debug.Log("Same Y");

            yield return new WaitForSeconds(.5f);

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

                    GridManager.Instance.DamageUnitTShotServerRpc(unitManager.ships[currentShipIndex].specialAbilityDamage / 2, posToCheck, NetworkManager.LocalClientId, false, 0);
                    break;
                }
            }
        }
    }

    #endregion

    #region Tiles Function
    void GetInRangeTiles(int shipRange)
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);

        inRangeTiles.Clear();
        inRangeTiles = PathfindScript.GetInRangeTiles(unitManager.ships[currentShipIndex].currentTile, shipRange);

        foreach (var t in inRangeTiles) t.HighLightRange(true);
    }

    void GetInRangeShootTiles(int shootRange)
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);

        inRangeTiles.Clear();
        inRangeTiles = PathfindScript.GetInRangeTilesCross(unitManager.ships[currentShipIndex].currentTile, shootRange);

        foreach (var t in inRangeTiles) t.HighLightRange(true);
    }

    void GetInRangeInteractTile()
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);

        inRangeTiles.Clear();
        inRangeTiles = PathfindScript.GetInRangeInteractTiles(unitManager.ships[currentShipIndex].currentTile, 1);

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
        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.startMoving);
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

        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.stopMoving);

        if(stepOnMine)
            GridManager.Instance.DamageUnitByMineServerRpc(GridManager.Instance.mineDamage, unitManager.ships[currentShipIndex].currentTile.pos.Value, false, 0);

        GetInRangeTiles(unitManager.ships[currentShipIndex].unitMoveRange);
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

        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.spawnShip);
    }

    public void SetShopToInactive(int shopIndex)
    {
        foreach(var tile in allTiles.Where(t => t.ShopTile))
        {
            if (tile.shopIndex.Value == shopIndex)
            {
                tile.canAccessShop = false;
                //indication visuelle temporaire amener a changer
                tile._highlightBlocked.SetActive(true);
                break;
            }
        }
    }

    RaycastHit2D? GetCurrentTile(Vector2 pos)
    {
        RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero);
        if (hits.Length > 0) return hits.OrderByDescending(i => i.collider.transform.position.z).First();
        return null;
    }

    #region Boolean

    bool CanMoveUnit(TileScript t)
    {
        return unitManager.allShipSpawned.Value && shipSelected && path.Count > 0 && !unitMoving && inRangeTiles.Contains(t) && canMove && !t.blockedTile.Value;
    }

    bool CantPathfind(TileScript tile)
    {
        return unitManager.ships[currentShipIndex].currentTile == null || unitMoving || !inRangeTiles.Contains(tile) || !canPlay.Value || !shipSelected || !unitManager.ships[currentShipIndex].canMove.Value || !canMove || tile.blockedTile.Value;
    }

    #endregion
}
