using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

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
    List<TileScript> pathHighlight = new List<TileScript>();
    List<TileScript> allTiles = new List<TileScript>();
    List<TileScript> inRangeTiles = new List<TileScript>();

    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    int blockedOrientation = 0;

    [Header("Visual Feedbacks")]
    public VisualsFeedbacks vs;

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

        if (!IsOwner) return;
        GetComponent<SpriteRenderer>().enabled = true;
    }

    void Update()
    {
        if(!IsOwner) return;

        MyInputs();
        HandleCurrentMode();
        DisplayPath();
        DisplayVisuals();
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
            if(s != null)
                newHealth += s.unitLife.Value;
        }

        SetHealthServerRpc(newHealth);
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
            if (unitManager.ships[i] != null)
            {
                unitManager.ships[i].canBeSelected.Value = true;
                unitManager.ships[i].canShoot.Value = true;
                unitManager.ships[i].canMove.Value = true;
                totalShootPoint++;
                totalMovePoint++;
            }
        }

        TotalActionPoint();
    }

    [ClientRpc]
    public void UseManaClientRpc()
    {
        if (!IsOwner) return;
        currentSpecialCharge -= unitManager.ships[currentShipIndex].shotCapacity.specialAbilityCost;
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

        DisplayOnSelectedUnit();

        totalShootPoint--;
        unitManager.ships[currentShipIndex].canShoot.Value = false;
        if (unitManager.ships[currentShipIndex].canMove.Value)
        {
            totalMovePoint--;
            unitManager.ships[currentShipIndex].canMove.Value = false;
        }
        TotalActionPoint();
    }

    [ClientRpc]
    public void SetSpawnableTileClientRpc(int player)
    {
        if (GameManager.Instance.spawnShipAnyWhere)
        {
            foreach (var t in allTiles)
                t.canSpawnShip = true;

            return;
        }

        int xPos = 0;
        switch (player)
        {
            case 0:
                if (NetworkManager.LocalClientId != (ulong)player) return;
                xPos = GridManager.Instance.map.cellBounds.min.x;
                for (int i = GridManager.Instance.map.cellBounds.min.y + 1; i < GridManager.Instance.map.cellBounds.max.y - 1; i++)
                {
                    TileScript t = GridManager.Instance.GetTileAtPosition(new Vector2(xPos, i));
                    if (t.Walkable)
                    {
                        t.canSpawnShip = true;
                        t._highlightSpawn.SetActive(true);
                    }
                }
                break;
            case 1:
                if (NetworkManager.LocalClientId != (ulong)player) return;
                xPos = GridManager.Instance.map.cellBounds.max.x - 1;
                for (int i = GridManager.Instance.map.cellBounds.min.y + 1; i < GridManager.Instance.map.cellBounds.max.y - 1; i++)
                {
                    TileScript t = GridManager.Instance.GetTileAtPosition(new Vector2(xPos, i));
                    if (t.Walkable)
                    {
                        t.canSpawnShip = true;
                        t._highlightSpawn.SetActive(true);
                    }
                }
                break;
        }
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

    [ServerRpc(RequireOwnership = false)]
    void UnitTpNewPosServerRpc(Vector2 targetShip, Vector2 newPos)
    {
        ShipUnit unit = GridManager.Instance.GetShipAtPos(targetShip);
        unit.unitPos.Value = newPos;
    }

    [ServerRpc(RequireOwnership = false)]
    void SpawnUnitServerRpc(Vector2 pos, ulong id, int index, bool barque)
    {
        if (barque)
        {
            ShipUnit ship = Instantiate(NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>().unitManager.barque, pos, Quaternion.identity);
            ship.damage.Value = ship.unitDamage;
            ship.GetComponent<NetworkObject>().SpawnWithOwnership(id);

            LinkUnitToClientRpc(ship.GetComponent<NetworkObject>().NetworkObjectId, index);

            unitManager.ships[index].unitPos.Value = new Vector3(pos.x, pos.y, -1);
            unitManager.ships[index].SetShipColorClientRpc(id);
        }
        else
        {
            ShipUnit ship = Instantiate(NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>().unitManager.ships[index], pos, Quaternion.identity);
            ship.damage.Value = ship.unitDamage;
            ship.GetComponent<NetworkObject>().SpawnWithOwnership(id);

            LinkUnitToClientRpc(ship.GetComponent<NetworkObject>().NetworkObjectId, index);

            unitManager.ships[index].unitPos.Value = new Vector3(pos.x, pos.y, -1);
            unitManager.ships[index].SetShipColorClientRpc(id);
        }

        HUD.Instance.SetShipOnHUD();
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
                if (unitManager.ships[i] == null) continue;
                unitManager.ships[i].canBeSelected.Value = false;
                unitManager.ships[i].canShoot.Value = false;
                unitManager.ships[i].canMove.Value = false;
            }
            GameManager.Instance.UpdateGameStateServerRpc();
        }
    }

    void MyInputs()
    {
        if (Input.GetButtonDown("Cancel") && !shipSelected && !HUD.Instance.isInUpgradeWindow)
            HUD.Instance.PauseGame();

        if (HUD.Instance.inPauseMenu)
            return;

        if (HUD.Instance.isInUpgradeWindow)
        {
            if (Input.GetButtonDown("Cancel"))
                HUD.Instance.UpgradeWindow(false, 0);
            return;
        }

        HandleKeyboardInputs();

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

        if (unitMoving) return;

        if (Input.GetMouseButtonDown(0) && shipSelected)
        {
            switch (currentModeIndex)
            {
                case 0: //Interact Mode
                    if (!inRangeTiles.Contains(cTile))
                    {
                        SelectShip(cTile);
                    }
                    else if (inRangeTiles.Contains(cTile))
                    {
                        if (cTile.shipOnTile.Value && unitManager.ships[currentShipIndex].canShoot.Value) //Aborder une autre unité
                        {
                            bool alliesUnit = false;
                            foreach (var ship in unitManager.ships)
                            {
                                if(ship != null)
                                {
                                    if (ship.unitPos.Value == cTile.pos.Value && ship.clientIdOwner == NetworkManager.LocalClientId && ship.canBeSelected.Value)
                                    {
                                        alliesUnit = true;
                                        SelectShip(cTile);
                                        break;
                                    }
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
                        else if (cTile.shipwrek.Value) //Récupérer une amélioration
                        {
                            if (unitManager.ships[currentShipIndex].canBeUpgrade)
                            {
                                HandleUpgradeSystem.Instance.GetUpgradeFromShipwreckServerRpc(cTile.pos.Value, NetworkManager.LocalClientId);
                            }
                        }
                    }
                    break;
                case 1: //Move Mode
                    if (!inRangeTiles.Contains(cTile)) 
                    { 
                        SelectShip(cTile);
                        return;
                    } 

                    if (CanMoveUnit(cTile) && unitManager.ships[currentShipIndex].canMove.Value)
                            StartCoroutine(UpdateShipPlacementOnGrid());
                    break;
                case 2: //Attack Mode
                    if (!inRangeTiles.Contains(cTile))
                    {
                        SelectShip(cTile);
                        return;
                    }

                    if (cTile.shipOnTile.Value && canShoot && unitManager.ships[currentShipIndex].canShoot.Value)
                    {
                        GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].damage.Value, cTile.pos.Value, NetworkManager.LocalClientId, false, 0, false, HasGoThroughWaterCapacity(cTile), false);
                        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.attack);
                    }
                    else if (cTile.cannonInTile.Value && canShoot && unitManager.ships[currentShipIndex].canShoot.Value)
                    {
                        GridManager.Instance.DamageCannonOnServerRpc(cTile.pos.Value);
                        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.attack);
                    }
                    break;
                case 3: //Special Shoot Mode
                    if (!inRangeTiles.Contains(cTile))
                    {
                        SelectShip(cTile);
                        return;
                    }

                    if (unitManager.ships[currentShipIndex].canShoot.Value && unitManager.ships[currentShipIndex].tileCapacity.specialAbilityCost <= currentSpecialCharge)
                        HandleSpecialModifyTile(cTile);
                    break;
                case 4: //Special Tile Mode
                    if (!inRangeTiles.Contains(cTile))
                    {
                        SelectShip(cTile);
                        return;
                    }

                    if (unitManager.ships[currentShipIndex].canShoot.Value && unitManager.ships[currentShipIndex].shotCapacity.specialAbilityCost <= currentSpecialCharge && cTile.shipOnTile.Value)
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
                if (cTile.Walkable && !cTile.shipOnTile.Value && cTile.canSpawnShip)
                    SpawnShip(cTile.pos.Value, cTile);
                if (currentShipIndex >= unitManager.ships.Length) currentShipIndex = 0;
            }
        }

        if (Input.GetButtonDown("Cancel") || (shipSelected && !unitManager.ships[currentShipIndex].canBeSelected.Value))
            DeselectShip();
    }

    void DisplayVisuals()
    {
        if (shipSelected)
        {
            if (unitManager.ships[currentShipIndex].unitSpecialTile.Value == ShipUnit.UnitSpecialTile.BlockTile && unitManager.ships[currentShipIndex].upgradedCapacity && inRangeTiles.Contains(cTile) && currentModeIndex == 3)
                vs.DisplayBlockedTile(cTile, blockedOrientation);

            if(cTile.ShopTile && currentModeIndex == 0 && inRangeTiles.Contains(cTile))
                vs.CursorDisplay(6, true);
            else if(cTile.shipOnTile.Value && currentModeIndex == 0 && inRangeTiles.Contains(cTile))
                vs.CursorDisplay(5, true);
            else
                vs.CursorDisplay(currentModeIndex, true);
        }
        else
        {
            vs.StopDisplayBlocked();
            vs.CursorDisplay(0, false);
        }
    }

    void HandleKeyboardInputs()
    {
        if (Input.GetAxisRaw("Mouse ScrollWheel") > 0)
            currentModeIndex++;
        else if (Input.GetAxisRaw("Mouse ScrollWheel") < 0)
            currentModeIndex--;

        if (Input.GetKeyDown(KeyCode.Alpha1))
            currentModeIndex = 1;
        if(Input.GetKeyDown(KeyCode.Alpha2))
            currentModeIndex = 2;
        if(Input.GetKeyDown(KeyCode.Alpha3))
            currentModeIndex = 4;
        if(Input.GetKeyDown(KeyCode.Alpha4))
            currentModeIndex = 3;

        if (Input.GetButtonDown("Jump"))
            blockedOrientation++;

        if (blockedOrientation > 1)
            blockedOrientation = 0;
    }

    void SelectShip(TileScript t)
    {
        if(shipSelected)
        {
            unitManager.ships[currentShipIndex].highlight.SetActive(false);
            SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.deselectShip);
            shipSelected = false;
            HideTiles();
        }

        if (!t.shipOnTile.Value) return;

        foreach (var ship in unitManager.ships)
        {
            if(ship == null) continue;
            if (ship.unitPos.Value == t.pos.Value && ship.clientIdOwner == NetworkManager.LocalClientId && ship.canBeSelected.Value)
            {
                currentShipIndex = ship.index;
                shipSelected = true;
                unitManager.ships[currentShipIndex].highlight.SetActive(true);
                DisplayOnSelectedUnit();
                SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.selectShip);
                break;
            }
        }
    }

    public void DeselectShip()
    {
        if (shipSelected)
        {
            unitManager.ships[currentShipIndex].highlight.SetActive(false);
            SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.deselectShip);
            shipSelected = false;
            HideTiles();
        }
    }

    public void SelectShipHUD(int i)
    {
        if (!unitManager.ships[i].canBeSelected.Value) return;

        currentShipIndex = i;
        shipSelected = true;
        DisplayOnSelectedUnit();
        SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.selectShip);
    }

    void DisplayOnSelectedUnit()
    {
        if (!unitManager.ships[currentShipIndex].canBeSelected.Value)
        {
            SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.deselectShip);
            shipSelected = false;
            HideTiles();
        }
        else if (unitManager.ships[currentShipIndex].canMove.Value)
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

        if (currentModeIndex != currentModeInputIndex)
            SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.changeShipMode);

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
                    GetInRangeSpecialTiles(unitManager.ships[currentShipIndex].tileCapacity.specialTileRange);
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
                    GetInRangeShootTiles(unitManager.ships[currentShipIndex].shotCapacity.specialShootRange);
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
    }

    #region SpecialCapacities

    void HandleSpecialModifyTile(TileScript t)
    {
        if (!t.Walkable && t.shipOnTile.Value) return;

        if (!unitManager.ships[currentShipIndex].upgradedCapacity)
        {
            switch (unitManager.ships[currentShipIndex].unitSpecialTile.Value)
            {
                case ShipUnit.UnitSpecialTile.BlockTile:
                    GridManager.Instance.BlockedTileServerRpc(t.pos.Value, NetworkManager.LocalClientId, unitManager.ships[currentShipIndex].tileCapacity.tilePassiveDuration, false, 0);
                    break;
                case ShipUnit.UnitSpecialTile.Mine:
                    if (t.Walkable)
                        GridManager.Instance.SetMineOnTileServerRpc(t.pos.Value, NetworkManager.LocalClientId, true, false);
                    break;
                case ShipUnit.UnitSpecialTile.Teleport:
                    TeleportShip(t);
                    break;
                case ShipUnit.UnitSpecialTile.FouilleOr:
                    SearchGold(false);
                    break;
                case ShipUnit.UnitSpecialTile.CanonSurIle:
                    GridManager.Instance.AddCannonToTileServerRpc(cTile.pos.Value, NetworkManager.LocalClientId, false);
                    break;
                case ShipUnit.UnitSpecialTile.Barque:
                    StartCoroutine(SpawnBarque(t.pos.Value, t));
                    break;
                case ShipUnit.UnitSpecialTile.None:
                    break;
            }
        }
        else //Capacités upgrade
        {
            switch (unitManager.ships[currentShipIndex].unitSpecialTile.Value)
            {
                case ShipUnit.UnitSpecialTile.BlockTile:
                    GridManager.Instance.BlockedTileServerRpc(t.pos.Value, NetworkManager.LocalClientId, unitManager.ships[currentShipIndex].tileCapacity.tilePassiveDuration, true, blockedOrientation);
                    break;
                case ShipUnit.UnitSpecialTile.Mine:
                    if (t.Walkable)
                        GridManager.Instance.SetMineOnTileServerRpc(t.pos.Value, NetworkManager.LocalClientId, true, true);
                    break;
                case ShipUnit.UnitSpecialTile.Teleport:
                    UpgradedTP(t);
                    break;
                case ShipUnit.UnitSpecialTile.FouilleOr:
                    SearchGold(true);
                    break;
                case ShipUnit.UnitSpecialTile.CanonSurIle:
                    GridManager.Instance.AddCannonToTileServerRpc(cTile.pos.Value, NetworkManager.LocalClientId, true);
                    break;
                case ShipUnit.UnitSpecialTile.Barque:
                    StartCoroutine(SpawnBarque(t.pos.Value, t));
                    break;
                case ShipUnit.UnitSpecialTile.ExplodeBarque:
                    ExplodeBarque();
                    break;
                case ShipUnit.UnitSpecialTile.None:
                    break;
            }
        }

        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.tileCapacity);
    }

    void HandleSpecialUnitAttackOnUnit(TileScript t)
    {
        if (!unitManager.ships[currentShipIndex].upgradedCapacity)
        {
            switch (unitManager.ships[currentShipIndex].unitSpecialShot.Value)
            {
                case ShipUnit.UnitSpecialShot.PushUnit:
                    GridManager.Instance.PushUnitServerRpc(t.pos.Value, unitManager.ships[currentShipIndex].unitPos.Value, NetworkManager.LocalClientId, false, false);
                    break;
                case ShipUnit.UnitSpecialShot.TShot:
                    StartCoroutine(TShotFunction(t, false));
                    break;
                case ShipUnit.UnitSpecialShot.FireShot:
                    GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, t.pos.Value, NetworkManager.LocalClientId, unitManager.ships[currentShipIndex].shotCapacity.specialPassifDamage, unitManager.ships[currentShipIndex].shotCapacity.shootPassiveDuration, true, HasGoThroughWaterCapacity(cTile), false);
                    break;
                case ShipUnit.UnitSpecialShot.TirBrochette:
                    StartCoroutine(BrochetteShot(t, false));
                    break;
                case ShipUnit.UnitSpecialShot.VentContraire:
                    GridManager.Instance.ApplyEffectOnShipServerRpc(t.pos.Value, unitManager.ships[currentShipIndex].shotCapacity.shootPassiveDuration, NetworkManager.LocalClientId, false);
                    break;
                case ShipUnit.UnitSpecialShot.Grappin:
                    GridManager.Instance.PullUnitServerRpc(t.pos.Value, unitManager.ships[currentShipIndex].unitPos.Value, NetworkManager.LocalClientId, false);
                    break;
                case ShipUnit.UnitSpecialShot.None:
                    break;
            }
        }
        else //Capacités upgrade
        {
            switch (unitManager.ships[currentShipIndex].unitSpecialShot.Value)
            {
                case ShipUnit.UnitSpecialShot.PushUnit:
                    GridManager.Instance.PushUnitServerRpc(t.pos.Value, unitManager.ships[currentShipIndex].unitPos.Value, NetworkManager.LocalClientId, true, false);
                    break;
                case ShipUnit.UnitSpecialShot.TShot:
                    StartCoroutine(TShotFunction(t, true));
                    break;
                case ShipUnit.UnitSpecialShot.FireShot:
                    GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, t.pos.Value, NetworkManager.LocalClientId, unitManager.ships[currentShipIndex].shotCapacity.specialPassifDamage, unitManager.ships[currentShipIndex].shotCapacity.shootPassiveDuration, true, HasGoThroughWaterCapacity(cTile), true);
                    break;
                case ShipUnit.UnitSpecialShot.TirBrochette:
                    StartCoroutine(BrochetteShot(t, true));
                    break;
                case ShipUnit.UnitSpecialShot.VentContraire:
                    GridManager.Instance.ApplyEffectOnShipServerRpc(t.pos.Value, unitManager.ships[currentShipIndex].shotCapacity.shootPassiveDuration - 1, NetworkManager.LocalClientId, true);
                    break;
                case ShipUnit.UnitSpecialShot.Grappin:
                    GridManager.Instance.PullUnitServerRpc(t.pos.Value, unitManager.ships[currentShipIndex].unitPos.Value, NetworkManager.LocalClientId, true);
                    break;
                case ShipUnit.UnitSpecialShot.None:
                    break;
            }
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
            GridManager.Instance.SetMineOnTileServerRpc(t.pos.Value, NetworkManager.LocalClientId, false, false);
        }

        if (stepOnMine)
            GridManager.Instance.DamageUnitByMineServerRpc(GridManager.Instance.mineDamage, t.pos.Value, false, 0);

        currentSpecialCharge -= unitManager.ships[currentShipIndex].tileCapacity.specialAbilityCost;
        totalMovePoint--;
        totalShootPoint--;
        unitManager.ships[currentShipIndex].canMove.Value = false;
        unitManager.ships[currentShipIndex].canShoot.Value = false;
        TotalActionPoint();
    }

    void UpgradedTP(TileScript t)
    {
        if (!t.Walkable || t.blockedTile.Value) return;

        if (t.shipOnTile.Value)
        {
            //Swap ships placement
            ShipUnit ship = GridManager.Instance.GetShipAtPos(t.pos.Value);
            UnitTpNewPosServerRpc(ship.unitPos.Value, unitManager.ships[currentShipIndex].currentTile.pos.Value);
            ship.UpdateCurrentTileClientRpc(unitManager.ships[currentShipIndex].currentTile.pos.Value);

            UnitNewPosServerRpc(t.pos.Value, currentShipIndex);
            unitManager.ships[currentShipIndex].currentTile = t;
        }
        else
        {
            GridManager.Instance.SetShipOnTileServerRpc(unitManager.ships[currentShipIndex].currentTile.pos.Value, false);
            UnitNewPosServerRpc(t.pos.Value, currentShipIndex);
            GridManager.Instance.SetShipOnTileServerRpc(t.pos.Value, true);
            unitManager.ships[currentShipIndex].currentTile = t;
        }


        Vector2 check = t.pos.Value;
        if(GridManager.Instance.GetTileAtPosition(new Vector2(check.x + 1, check.y)).shipOnTile.Value)
            GridManager.Instance.DamageUnitNoActionServerRpc(3, new Vector2(check.x + 1, check.y), NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(t));
        if (GridManager.Instance.GetTileAtPosition(new Vector2(check.x - 1, check.y)).shipOnTile.Value)
            GridManager.Instance.DamageUnitNoActionServerRpc(3, new Vector2(check.x - 1, check.y), NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(t));
        if (GridManager.Instance.GetTileAtPosition(new Vector2(check.x, check.y + 1)).shipOnTile.Value)
            GridManager.Instance.DamageUnitNoActionServerRpc(3, new Vector2(check.x, check.y + 1), NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(t));
        if (GridManager.Instance.GetTileAtPosition(new Vector2(check.x, check.y - 1)).shipOnTile.Value)
            GridManager.Instance.DamageUnitNoActionServerRpc(3, new Vector2(check.x, check.y - 1), NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(t));

        bool stepOnMine = false;
        if (t.mineInTile.Value)
        {
            stepOnMine = true;
            GridManager.Instance.SetMineOnTileServerRpc(t.pos.Value, NetworkManager.LocalClientId, false, false);
        }

        if (stepOnMine)
            GridManager.Instance.DamageUnitByMineServerRpc(GridManager.Instance.mineDamage, t.pos.Value, false, 0);

        currentSpecialCharge -= unitManager.ships[currentShipIndex].tileCapacity.specialAbilityCost;
        totalMovePoint--;
        totalShootPoint--;
        unitManager.ships[currentShipIndex].canMove.Value = false;
        unitManager.ships[currentShipIndex].canShoot.Value = false;
        TotalActionPoint();
    }

    void ExplodeBarque()
    {
        Vector2 check = unitManager.ships[currentShipIndex].unitPos.Value;

        if (GridManager.Instance.GetTileAtPosition(new Vector2(check.x + 1, check.y)).shipOnTile.Value)
            GridManager.Instance.DamageUnitNoActionServerRpc(6, new Vector2(check.x + 1, check.y), NetworkManager.LocalClientId, false, 0, false);
        if (GridManager.Instance.GetTileAtPosition(new Vector2(check.x - 1, check.y)).shipOnTile.Value)
            GridManager.Instance.DamageUnitNoActionServerRpc(6, new Vector2(check.x - 1, check.y), NetworkManager.LocalClientId, false, 0, false);
        if (GridManager.Instance.GetTileAtPosition(new Vector2(check.x, check.y + 1)).shipOnTile.Value)
            GridManager.Instance.DamageUnitNoActionServerRpc(6, new Vector2(check.x, check.y + 1), NetworkManager.LocalClientId, false, 0, false);
        if (GridManager.Instance.GetTileAtPosition(new Vector2(check.x, check.y - 1)).shipOnTile.Value)
            GridManager.Instance.DamageUnitNoActionServerRpc(6, new Vector2(check.x, check.y - 1), NetworkManager.LocalClientId, false, 0, false);

        currentSpecialCharge -= unitManager.ships[currentShipIndex].tileCapacity.specialAbilityCost;
        totalMovePoint--;
        totalShootPoint--;
        unitManager.ships[currentShipIndex].TakeDamageServerRpc(24, unitManager.ships[currentShipIndex].unitPos.Value, false, 0, false);
        TotalActionPoint();
    }

    void SearchGold(bool upgraded)
    {
        int rdm = Random.Range(0, 101);
        int foundGold = 0;
        int foundMana = 0;

        if (rdm >= 75)
            foundGold = 15;
        else if (rdm >= 50)
            foundGold = 10;
        else if (rdm >= 25)
            foundGold = 5;
        else
            foundGold = 0;

        if (upgraded)
        {
            if (rdm >= 75)
                foundMana = 30;
            else if (rdm >= 50)
                foundMana = 20;
            else if (rdm >= 25)
                foundMana = 10;
            else
                foundMana = 0;
        }

        playerGold += foundGold;

        currentSpecialCharge -= unitManager.ships[currentShipIndex].tileCapacity.specialAbilityCost;

        currentSpecialCharge += foundMana;
        
        totalMovePoint--;
        totalShootPoint--;
        unitManager.ships[currentShipIndex].canMove.Value = false;
        unitManager.ships[currentShipIndex].canShoot.Value = false;
        TotalActionPoint();
    }

    IEnumerator SpawnBarque(Vector2 pos, TileScript t)
    {
        if (!IsOwner) yield break;

        if (unitManager.ships[currentShipIndex].barqueSpawn) yield break;

        currentSpecialCharge -= unitManager.ships[currentShipIndex].tileCapacity.specialAbilityCost;
        unitManager.ships[currentShipIndex].canMove.Value = false;
        unitManager.ships[currentShipIndex].canShoot.Value = false;
        TotalActionPoint();

        int index = unitManager.ships[currentShipIndex].barqueIndex;

        SpawnUnitServerRpc(pos, NetworkManager.LocalClientId, index, true);

        yield return new WaitForSeconds(.5f);

        unitManager.ships[index].currentTile = t;
        unitManager.ships[index].index = index;
        unitManager.ships[index].clientIdOwner = NetworkManager.LocalClientId;
        unitManager.ships[index].shipIndexFrom.Value = currentShipIndex;

        GridManager.Instance.SetShipOnTileServerRpc(pos, true);
        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.spawnShip);
    }

    IEnumerator TShotFunction(TileScript t, bool upgraded)
    {
        if (t.pos.Value.x == unitManager.ships[currentShipIndex].currentTile.pos.Value.x)
        {
            GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, t.pos.Value, NetworkManager.LocalClientId, false, 0, true, HasGoThroughWaterCapacity(cTile), false);

            Debug.Log("Same X");

            yield return new WaitForSeconds(.5f);

            //Getships on Y axis with for loop
            Vector2 posToCheck;
            for (int x = 1; x < 3; x++)
            {
                posToCheck = new Vector2(t.pos.Value.x + x, t.pos.Value.y);
                if (GridManager.Instance.GetTileAtPosition(posToCheck).shipOnTile.Value)
                {
                    if (!upgraded)
                        GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage / 2, posToCheck, NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(cTile));
                    else if(upgraded)
                        GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, posToCheck, NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(cTile));
                    break;
                }
            }
            for (int x = 1; x < 3; x++)
            {
                posToCheck = new Vector2(t.pos.Value.x - x, t.pos.Value.y);
                if (GridManager.Instance.GetTileAtPosition(posToCheck).shipOnTile.Value)
                {
                    if (!upgraded)
                        GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage / 2, posToCheck, NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(cTile));
                    else if(upgraded)
                        GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, posToCheck, NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(cTile));
                    break;
                }
            }

            if (upgraded)
            {
                //Check si c'est au dessus ou en dessous
                if(unitManager.ships[currentShipIndex].unitPos.Value.y > cTile.pos.Value.y)
                    GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, new Vector2(t.pos.Value.x, t.pos.Value.y - 1), NetworkManager.LocalClientId, false, 0, false);
                else
                    GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, new Vector2(t.pos.Value.x, t.pos.Value.y + 1), NetworkManager.LocalClientId, false, 0, false);
            }
        }
        else if (t.pos.Value.y == unitManager.ships[currentShipIndex].currentTile.pos.Value.y)
        {
            GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, t.pos.Value, NetworkManager.LocalClientId, false, 0, true, HasGoThroughWaterCapacity(cTile), false);

            Debug.Log("Same Y");

            yield return new WaitForSeconds(.5f);

            //Getships on X axis with for loop
            Vector2 posToCheck;
            for (int y = 1; y < 3; y++)
            {
                posToCheck = new Vector2(t.pos.Value.x, t.pos.Value.y + y);
                if (GridManager.Instance.GetTileAtPosition(posToCheck).shipOnTile.Value)
                {
                    if (!upgraded)
                        GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage / 2, posToCheck, NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(cTile));
                    else if (upgraded)
                        GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, posToCheck, NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(cTile));
                    break;
                }
            }
            for (int y = 1; y < 3; y++)
            {
                posToCheck = new Vector2(t.pos.Value.x, t.pos.Value.y - y);
                if (GridManager.Instance.GetTileAtPosition(posToCheck).shipOnTile.Value)
                {
                    if (!upgraded)
                        GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage / 2, posToCheck, NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(cTile));
                    else if (upgraded)
                        GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, posToCheck, NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(cTile));
                    break;
                }
            }

            if (upgraded)
            {
                //Check si c'est au dessus ou en dessous
                if (unitManager.ships[currentShipIndex].unitPos.Value.x > cTile.pos.Value.x)
                    GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, new Vector2(t.pos.Value.x - 1, t.pos.Value.y), NetworkManager.LocalClientId, false, 0, false);
                else
                    GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, new Vector2(t.pos.Value.x + 1, t.pos.Value.y), NetworkManager.LocalClientId, false, 0, false);
            }
        }
    }

    IEnumerator BrochetteShot(TileScript t, bool upgraded)
    {
        Vector2 unitPos = unitManager.ships[currentShipIndex].unitPos.Value;
        Vector2 targetUnit = t.pos.Value;

        if (t.pos.Value.x == unitManager.ships[currentShipIndex].currentTile.pos.Value.x)
        {
            GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, t.pos.Value, NetworkManager.LocalClientId, false, 0, true, HasGoThroughWaterCapacity(cTile), false);

            yield return new WaitForSeconds(.5f);

            //Getships on Y axis with for loop

            if (unitPos.y > targetUnit.y)
            {
                //descend
                for (int i = 1; i < 3; i++)
                {
                    if (GridManager.Instance.GetTileAtPosition(new Vector2(targetUnit.x, targetUnit.y - i)).shipOnTile.Value)
                    {
                        GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage - i, new Vector2(targetUnit.x, targetUnit.y - i), NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(cTile));
                    }
                    yield return new WaitForSeconds(.5f);
                }

            }
            else if (unitPos.y < targetUnit.y)
            {
                //ca monte
                for (int i = 1; i < 3; i++)
                {
                    if (GridManager.Instance.GetTileAtPosition(new Vector2(targetUnit.x, targetUnit.y + i)).shipOnTile.Value)
                    {
                        GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage - i, new Vector2(targetUnit.x, targetUnit.y + i), NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(cTile));
                    }
                    yield return new WaitForSeconds(.5f);
                }
            }

        }
        else if (t.pos.Value.y == unitManager.ships[currentShipIndex].currentTile.pos.Value.y)
        {
            GridManager.Instance.DamageUnitServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage, t.pos.Value, NetworkManager.LocalClientId, false, 0, true, HasGoThroughWaterCapacity(cTile), false);

            yield return new WaitForSeconds(.5f);

            //Getships on X axis with for loop

            if (unitPos.x > targetUnit.x)
            {
                //a gauche
                for (int i = 1; i < 3; i++)
                {
                    if (GridManager.Instance.GetTileAtPosition(new Vector2(targetUnit.x - i, targetUnit.y)).shipOnTile.Value)
                    {
                        GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage - i, new Vector2(targetUnit.x - i, targetUnit.y), NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(cTile));
                    }
                    yield return new WaitForSeconds(.5f);
                }
            }
            else if (unitPos.x < targetUnit.x)
            {
                //a droite
                for (int i = 1; i < 3; i++)
                {
                    if (GridManager.Instance.GetTileAtPosition(new Vector2(targetUnit.x + i, targetUnit.y)).shipOnTile.Value)
                    {
                        GridManager.Instance.DamageUnitNoActionServerRpc(unitManager.ships[currentShipIndex].shotCapacity.specialAbilityDamage - i, new Vector2(targetUnit.x + i, targetUnit.y), NetworkManager.LocalClientId, false, 0, HasGoThroughWaterCapacity(cTile));
                    }
                    yield return new WaitForSeconds(.5f);
                }
            }
        }

        if(upgraded)
            GridManager.Instance.PushUnitServerRpc(t.pos.Value, unitManager.ships[currentShipIndex].unitPos.Value, NetworkManager.LocalClientId,false, true);
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

    void GetInRangeSpecialTiles(int range)
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);

        inRangeTiles.Clear();
        inRangeTiles = PathfindScript.GetInRangeSpecialTiles(unitManager.ships[currentShipIndex].currentTile, range);

        foreach (var t in inRangeTiles) t.HighLightRange(true);
    }

    void GetInRangeInteractTile()
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);

        inRangeTiles.Clear();
        inRangeTiles = PathfindScript.GetInRangeInteractTiles(unitManager.ships[currentShipIndex].currentTile, 1);

        foreach (var t in inRangeTiles) t.HighLightRange(true);

    }

    public void HideTiles()
    {
        foreach (var t in inRangeTiles) t.HighLightRange(false);
        inRangeTiles.Clear();
    }
    #endregion

    #region Pathfind
    void OnTileHover(TileScript tile)
    {
        Debug.Log("Called once when clicked or everyframe ?");

        if (!unitManager.allShipSpawned.Value) return;
        if (CantPathfind(tile)) return;

        goalTile = tile;
        path.Clear();
        path = PathfindScript.Pathfind(unitManager.ships[currentShipIndex].currentTile, goalTile);
    }

    void DisplayPath()
    {
        if (inRangeTiles.Contains(cTile) && currentModeIndex == 1 && shipSelected && !unitMoving)
        {
            foreach (var item in allTiles)
                item.SetColor(3);

            goalTile = cTile;
            if (pathHighlight != null)
                pathHighlight.Clear();
            pathHighlight = PathfindScript.Pathfind(unitManager.ships[currentShipIndex].currentTile, goalTile);
        }
        else
        {
            foreach (var item in allTiles)
                item.SetColor(3);
        }

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

                GridManager.Instance.SetMineOnTileServerRpc(path[value].pos.Value, NetworkManager.LocalClientId, false, false);

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

        GridManager.Instance.CheckForUnitInCannonRangesServerRpc(NetworkManager.LocalClientId, unitManager.ships[currentShipIndex].unitPos.Value);

        DisplayOnSelectedUnit();
        TotalActionPoint();
        unitMoving = false;
    }

    void SpawnShip(Vector2 pos, TileScript t)
    {
        if (!IsOwner) return;

        SpawnUnitServerRpc(pos, NetworkManager.LocalClientId, currentShipIndex, false);

        unitManager.ships[currentShipIndex].currentTile = t;
        unitManager.ships[currentShipIndex].index = currentShipIndex;
        unitManager.ships[currentShipIndex].clientIdOwner = NetworkManager.LocalClientId;

        if (currentShipIndex == 3) unitManager.ships[currentShipIndex].barqueIndex = 5;
        else if (currentShipIndex == 4) unitManager.ships[currentShipIndex].barqueIndex = 6;

        unitManager.numShipSpawned++;
        currentShipIndex++;


        GridManager.Instance.SetShipOnTileServerRpc(pos, true);

        if (unitManager.numShipSpawned >= 5 && !unitManager.allShipSpawned.Value)
        {
            unitManager.allShipSpawned.Value = true;
            TotalActionPoint();
            foreach(var tiles in allTiles.Where(tile => tile.canSpawnShip))
            {
                tiles.canSpawnShip = false;
                tiles._highlightSpawn.SetActive(false);
            }
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

    public void ResetShipBarque(int index)
    {
        unitManager.ships[index].barqueSpawn = false;
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
        if (unitManager.ships[currentShipIndex] == null) return true;
        return unitManager.ships[currentShipIndex].currentTile == null || unitMoving || !inRangeTiles.Contains(tile) || !canPlay.Value || !shipSelected || !unitManager.ships[currentShipIndex].canMove.Value || !canMove || tile.blockedTile.Value || tile.shipOnTile.Value || !tile.Walkable;
    }

    bool HasGoThroughWaterCapacity(TileScript targerTiles)
    {
        Vector2 unitPos = unitManager.ships[currentShipIndex].unitPos.Value;
        Vector2 targetUnit = targerTiles.pos.Value;

        bool hasGoneThroughtWater = false;

        if (unitPos.x == targetUnit.x)
        {
            if(unitPos.y > targetUnit.y)
            {
                //descend
                for(int i = (int)unitPos.y; i > targetUnit.y; i--)
                {
                    if(GridManager.Instance.GetTileAtPosition(new Vector2(targetUnit.x, i)).blockedTile.Value)
                    {
                        hasGoneThroughtWater = true;
                        break;
                    }
                }

            }
            else if(unitPos.y < targetUnit.y)
            {
                //ca monte
                for (int i = (int)unitPos.y; i < targetUnit.y; i++)
                {
                    if (GridManager.Instance.GetTileAtPosition(new Vector2(targetUnit.x, i)).blockedTile.Value)
                    {
                        hasGoneThroughtWater = true;
                        break;
                    }
                }
            }
        }
        else if (unitPos.y == targetUnit.y)
        {
            if (unitPos.x > targetUnit.x)
            {
                //a gauche
                for (int i = (int)unitPos.x; i > targetUnit.x; i--)
                {
                    if (GridManager.Instance.GetTileAtPosition(new Vector2(i, targetUnit.y)).blockedTile.Value)
                    {
                        hasGoneThroughtWater = true;
                        break;
                    }
                }
            }
            else if (unitPos.x < targetUnit.x)
            {
                //a droite
                for (int i = (int)unitPos.x; i < targetUnit.x; i++)
                {
                    if (GridManager.Instance.GetTileAtPosition(new Vector2(i, targetUnit.y)).blockedTile.Value)
                    {
                        hasGoneThroughtWater = true;
                        break;
                    }
                }
            }
        }

        Debug.Log(hasGoneThroughtWater);

        return hasGoneThroughtWater;
    }

    #endregion
}
