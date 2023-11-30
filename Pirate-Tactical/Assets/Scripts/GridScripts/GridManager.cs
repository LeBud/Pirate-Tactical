using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance { get; private set; }

    public int accostAttackBoost = 3;

    public int _width, _height;
    [Header("Tiles")]
    [SerializeField] TileScript seaTile;
    [SerializeField] TileScript landTile;
    [SerializeField] TileScript moutainTile;
    [SerializeField] TileScript shopTile;

    public NetworkList<Vector2> dictionnary;
    public NetworkVariable<int> combatZoneSize = new NetworkVariable<int>();
    
    List<TileScript> tilesGrid = new List<TileScript>();
    List<TileScript> blockedTiles = new List<TileScript>();
    List<TileScript> outOfCombatZoneTiles = new List<TileScript>();

    public int combatZoneDamage = 4;
    public int mineDamage = 13;

    [Header("TileMap Editor")]
    public Tilemap map;
    [SerializeField] Sprite waterSprite;
    [SerializeField] Sprite landSprite;
    [SerializeField] Sprite mountainSprite;
    [SerializeField] Sprite shopSprite;

    TileScript midTile;

    [Header("Cannon")]
    public Cannon cannonPrefab;

    [Header("Ship wreck")]
    public Shipwrek shipwreckPrefab;

    List<Cannon> cannonsOnMap = new List<Cannon>();

    private void Awake()
    {
        if(Instance == null)
            Instance = this;

        dictionnary = new NetworkList<Vector2>();
    }

    private void Start()
    {
        if (map.cellBounds.size.x > map.cellBounds.size.y)
            combatZoneSize.Value = map.cellBounds.size.x / 2;
        else
            combatZoneSize.Value = map.cellBounds.size.y / 2;
    }

    [ServerRpc]
    public void GenerateGridOnTileMapServerRpc()
    {
        if (!IsServer) return;

        BoundsInt bounds = map.cellBounds;
        
        GameManager.Instance.cameraPos.Value = new Vector3((float)bounds.center.x - 1, (float)bounds.center.y - 0.5f, -10);

        int shopIndex = 0;

        for (int  x = bounds.min.x; x < bounds.max.x; x++)
        {
            for(int  y = bounds.min.y; y < bounds.max.y; y++)
            {
                if(map.HasTile(new Vector3Int(x, y)))
                {
                    TileScript toSpawn = seaTile;
                    if (map.GetSprite(new Vector3Int(x, y)) == waterSprite)
                        toSpawn = seaTile;
                    else if (map.GetSprite(new Vector3Int(x, y)) == landSprite)
                        toSpawn = landTile;
                    else if (map.GetSprite(new Vector3Int(x, y)) == mountainSprite)
                        toSpawn = moutainTile;
                    else
                        toSpawn = shopTile;

                    var spawnedTile = Instantiate(toSpawn, new Vector3Int(x, y), Quaternion.identity);
                    spawnedTile.name = $"Tile {x} {y}";
                    tilesGrid.Add(spawnedTile);

                    spawnedTile.GetComponent<NetworkObject>().Spawn();
                    spawnedTile.transform.parent = transform;

                    spawnedTile.pos.Value = new Vector2(x, y);
                    spawnedTile.offsetTile.Value = Mathf.Abs(x + y) % 2 == 1;

                    if(spawnedTile.ShopTile)
                    {
                        spawnedTile.shopIndex.Value = shopIndex;
                        shopIndex++;
                    }

                    dictionnary.Add(new Vector2(x, y));
                }
            }
        }
    }

    //Permet d'obtenir une tile avec sa position
    public TileScript GetTileAtPosition(Vector2 pos)
    {
        if (dictionnary.Contains(pos))
        {
            foreach(Transform t in transform)
            {
                if (t.GetComponent<TileScript>().pos.Value == pos)
                    return t.GetComponent<TileScript>();
            }
        }
        return null;
    }

    public ShipUnit GetShipAtPos(Vector2 pos)
    {
        ShipUnit[] ships = FindObjectsOfType<ShipUnit>();

        for (int i = 0; i < ships.Length; i++)
        {
            if (ships[i].unitPos.Value == pos)
            {
                return ships[i];
            }
        }

        return null;
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyEffectOnShipServerRpc(Vector2 pos, int duration, ulong id)
    {
        ShipUnit unit = GetShipAtPos(pos);
        unit.GiveWindEffectClientRpc(duration);

        Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
        p.HasDidAnActionClientRpc();
        p.UseManaClientRpc();
    }

    #region DamageUnit

    [ServerRpc(RequireOwnership = false)]
    public void DamageUnitServerRpc(int damage, Vector2 pos, ulong id, bool passiveAttack, int effectDuration, bool special, bool hasGoThroughWater)
    {
        ShipUnit ships = GetShipAtPos(pos);

        bool isEnemy = false;

        if(ships != null)
        {
            if (ships.unitPos.Value == pos && ships.GetComponent<NetworkObject>().OwnerClientId != id)
            {
                isEnemy = true;
                ships.TakeDamageServerRpc(damage, pos, passiveAttack, effectDuration, hasGoThroughWater);
            }
        }

        if (isEnemy)
        {
            Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
            p.HasDidAnActionClientRpc();
            if (special)
                p.UseManaClientRpc();
        }
        else if (special && !isEnemy)
        {
            Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
            p.HasDidAnActionClientRpc();
            p.UseManaClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DamageUnitNoActionServerRpc(int damage, Vector2 pos, ulong id, bool passiveAttack, int effectDuration, bool hasGoThroughWater)
    {
        if (hasGoThroughWater) return;

        ShipUnit ships = GetShipAtPos(pos);

        if (ships.unitPos.Value == pos && ships.GetComponent<NetworkObject>().OwnerClientId != id)
        {
            ships.TakeDamageServerRpc(damage, pos, passiveAttack, effectDuration, hasGoThroughWater);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DamageAccostServerRpc(Vector2 allyPos, Vector2 enemyPos, ulong id)
    {
        ShipUnit ally = GetShipAtPos(allyPos);
        ShipUnit enemy = GetShipAtPos(enemyPos);

        //Calculate Dmg
        int allyDmg = ally.unitAccostDamage+ accostAttackBoost + ally.accostDmgBoost.Value;
        enemy.TakeDamageServerRpc(allyDmg, enemyPos, false, 0, false);
        int enemyDmg = enemy.unitAccostDamage + enemy.accostDmgBoost.Value;
        ally.TakeDamageServerRpc(enemyDmg, allyPos, false, 0, false);
        
        //Dépenser les points
        Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
        p.HasDidAnActionClientRpc();

        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.accost);
    }


    #endregion

    #region Push/Pull Ship
    [ServerRpc(RequireOwnership = false)]
    public void PushUnitServerRpc(Vector2 pushShip, Vector2 currentShip, ulong id)
    {
        ShipUnit ships = GetShipAtPos(pushShip);
        Vector2 posToCheck = new Vector2();

        bool hasPush = false;

        if (ships.unitPos.Value == pushShip)
        {
            SetShipOnTileServerRpc(pushShip, false);
            if(currentShip.y == pushShip.y)
            {
                if(currentShip.x > pushShip.x)
                {
                    //alors -1
                    posToCheck = new Vector2(ships.unitPos.Value.x - 1, ships.unitPos.Value.y);
                    if (dictionnary.Contains(posToCheck))
                    {
                        TileScript t = GetTileAtPosition(posToCheck);
                        if(t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                        {
                            ships.unitPos.Value = new Vector3(posToCheck.x, posToCheck.y, -1);
                            ships.SetNewTileClientRpc(posToCheck);
                            SetShipOnTileServerRpc(ships.unitPos.Value, true);
                            hasPush = true;
                        }
                    }
                }
                else
                {
                    //alors +1
                    posToCheck = new Vector2(ships.unitPos.Value.x + 1, ships.unitPos.Value.y);
                    if (dictionnary.Contains(posToCheck))
                    {
                        TileScript t = GetTileAtPosition(posToCheck);
                        if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                        {
                            ships.unitPos.Value = new Vector3(posToCheck.x, posToCheck.y, -1);
                            ships.SetNewTileClientRpc(posToCheck);
                            SetShipOnTileServerRpc(ships.unitPos.Value, true);
                            hasPush = true;
                        }
                    }
                }
            }
            else if (currentShip.x == pushShip.x)
            {
                if (currentShip.y > pushShip.y)
                {
                    //alors -1
                    posToCheck = new Vector2(ships.unitPos.Value.x, ships.unitPos.Value.y - 1);
                    if (dictionnary.Contains(posToCheck))
                    {
                        TileScript t = GetTileAtPosition(posToCheck);
                        if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                        {
                            ships.unitPos.Value = new Vector3(posToCheck.x, posToCheck.y, -1);
                            ships.SetNewTileClientRpc(posToCheck);
                            SetShipOnTileServerRpc( ships.unitPos.Value, true);
                            hasPush = true;
                        }
                    }
                }
                else
                {
                    //alors +1
                    posToCheck = new Vector2(ships.unitPos.Value.x, ships.unitPos.Value.y + 1);
                    if (dictionnary.Contains(posToCheck))
                    {
                        TileScript t = GetTileAtPosition(posToCheck);
                        if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                        {
                            ships.unitPos.Value = new Vector3(posToCheck.x, posToCheck.y, -1);
                            ships.SetNewTileClientRpc(posToCheck);
                            SetShipOnTileServerRpc(ships.unitPos.Value, true);
                            hasPush = true;
                        }
                    }
                }
            }

        }
        

        if (hasPush)
        {
            if (GetTileAtPosition(posToCheck).mineInTile.Value)
            {
                SetMineOnTileServerRpc(posToCheck, 0, false);
                DamageUnitByMineServerRpc(mineDamage, posToCheck, false, 0);
            }
            Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
            p.HasDidAnActionClientRpc();
            p.UseManaClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PullUnitServerRpc(Vector2 pushShip, Vector2 currentShip, ulong id)
    {
        ShipUnit ships = GetShipAtPos(pushShip);
        Vector2 posToCheck = new Vector2();

        bool hasPull = false;

        if (ships.unitPos.Value == pushShip)
        {
            SetShipOnTileServerRpc(pushShip, false);
            if (currentShip.y == pushShip.y)
            {
                if (currentShip.x > pushShip.x)
                {
                    //alors -1
                    posToCheck = new Vector2(ships.unitPos.Value.x + 1, ships.unitPos.Value.y);
                    if (dictionnary.Contains(posToCheck))
                    {
                        TileScript t = GetTileAtPosition(posToCheck);
                        if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                        {
                            ships.unitPos.Value = new Vector3(posToCheck.x, posToCheck.y, -1);
                            ships.SetNewTileClientRpc(posToCheck);
                            SetShipOnTileServerRpc(ships.unitPos.Value, true);
                            hasPull = true;
                        }
                    }
                }
                else
                {
                    //alors +1
                    posToCheck = new Vector2(ships.unitPos.Value.x - 1, ships.unitPos.Value.y);
                    if (dictionnary.Contains(posToCheck))
                    {
                        TileScript t = GetTileAtPosition(posToCheck);
                        if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                        {
                            ships.unitPos.Value = new Vector3(posToCheck.x, posToCheck.y, -1);
                            ships.SetNewTileClientRpc(posToCheck);
                            SetShipOnTileServerRpc(ships.unitPos.Value, true);
                            hasPull = true;
                        }
                    }
                }
            }
            else if (currentShip.x == pushShip.x)
            {
                if (currentShip.y > pushShip.y)
                {
                    //alors -1
                    posToCheck = new Vector2(ships.unitPos.Value.x, ships.unitPos.Value.y + 1);
                    if (dictionnary.Contains(posToCheck))
                    {
                        TileScript t = GetTileAtPosition(posToCheck);
                        if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                        {
                            ships.unitPos.Value = new Vector3(posToCheck.x, posToCheck.y, -1);
                            ships.SetNewTileClientRpc(posToCheck);
                            SetShipOnTileServerRpc(ships.unitPos.Value, true);
                            hasPull = true;
                        }
                    }
                }
                else
                {
                    //alors +1
                    posToCheck = new Vector2(ships.unitPos.Value.x, ships.unitPos.Value.y - 1);
                    if (dictionnary.Contains(posToCheck))
                    {
                        TileScript t = GetTileAtPosition(posToCheck);
                        if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                        {
                            ships.unitPos.Value = new Vector3(posToCheck.x, posToCheck.y, -1);
                            ships.SetNewTileClientRpc(posToCheck);
                            SetShipOnTileServerRpc(ships.unitPos.Value, true);
                            hasPull = true;
                        }
                    }
                }
            }

        }

        if (hasPull)
        {
            if (GetTileAtPosition(posToCheck).mineInTile.Value)
            {
                SetMineOnTileServerRpc(posToCheck, 0, false);
                DamageUnitByMineServerRpc(mineDamage, posToCheck, false, 0);
            }
            Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
            p.HasDidAnActionClientRpc();
            p.UseManaClientRpc();
        }
    }

    #endregion

    #region ModifyTile

    [ServerRpc(RequireOwnership = false)]
    public void DamageUnitByMineServerRpc(int damage, Vector2 pos, bool passiveAttack, int effectDuration)
    {
        ShipUnit[] ships = FindObjectsOfType<ShipUnit>();

        for (int i = 0; i < ships.Length; i++)
        {
            if (ships[i].unitPos.Value == pos)
            {
                ships[i].TakeDamageServerRpc(damage, pos, passiveAttack, effectDuration, false);
                break;

            }
        }

    }


    [ServerRpc(RequireOwnership = false)]
    public void SetShipOnTileServerRpc(Vector2 tilePos, bool active)
    {
        if(!IsServer) return;

        if (!dictionnary.Contains(tilePos)) return;

        TileScript t = GetTileAtPosition(tilePos);
        t.shipOnTile.Value = active;
    }

    [ServerRpc(RequireOwnership = false)]
    public void BlockedTileServerRpc(Vector2 tilePos, ulong id, int duration)
    {
        if (dictionnary.Contains(tilePos))
        {
            foreach(var t in tilesGrid)
                if(t.pos.Value == tilePos && !t.shipOnTile.Value)
                {
                    t.SetTileToBlockTileClientRpc(true, duration);
                    t.blockedTile.Value = true;
                    blockedTiles.Add(t);

                    if (t.mineInTile.Value)
                        SetMineOnTileServerRpc(t.pos.Value, 0, false);

                    Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
                    p.UseManaClientRpc();
                    p.HasDidAnActionClientRpc();
                    break;
                }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMineOnTileServerRpc(Vector2 tilePos, ulong id, bool active)
    {
        if (!active && dictionnary.Contains(tilePos))
        {
            foreach (var t in tilesGrid)
                if (t.pos.Value == tilePos)
                {
                    t.mineInTile.Value = false;
                    foreach(ulong _id in NetworkManager.ConnectedClientsIds)
                        t.SetMineTileToClientRpc(_id, false);
                    SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.mineExploding);
                    break;
                }
        }
        else if (active && dictionnary.Contains(tilePos))
        {
            foreach (var t in tilesGrid)
                if (t.pos.Value == tilePos && !t.shipOnTile.Value)
                {
                    t.mineInTile.Value = true;
                    t.SetMineTileToClientRpc(id, true);
                    Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
                    p.UseManaClientRpc();
                    p.HasDidAnActionClientRpc();
                    break;
                }
        }
    }

    #endregion

    [ServerRpc(RequireOwnership = false)]
    public void UpdateTilesServerRpc()
    {
        if (!IsServer) return;

        if(GameManager.Instance.currentRound.Value >= GameManager.Instance.startRoundCombatZone)
            CombatZoneTiles();
        
        if(blockedTiles.Count > 0)
        {
            foreach(var t in blockedTiles)
            {
                if (t.blockedTile.Value)
                    t.UnblockTileServerRpc();
                if(!t.blockedTile.Value) blockedTiles.Remove(t);
            }
        }
    }

    void CombatZoneTiles()
    {
        BoundsInt bounds = map.cellBounds;
        float xPos = bounds.center.x;
        float yPos = bounds.center.y;

        xPos = Mathf.FloorToInt(xPos - 1);
        yPos = Mathf.FloorToInt(yPos);

        midTile = GetTileAtPosition(new Vector2(xPos, yPos));
        Debug.Log(new Vector2(xPos, yPos));

        List<TileScript> rangeTiles = PathfindScript.GetCombatZoneSize(midTile, combatZoneSize.Value);

        foreach(var t in tilesGrid)
        {
            if (!rangeTiles.Contains(t) && !outOfCombatZoneTiles.Contains(t))
            {
                outOfCombatZoneTiles.Add(t);
                t.tileOutOfCombatZone.Value = true;
                t.SetTileToOutOfZoneClientRpc();
                SetMineOnTileServerRpc(t.pos.Value, 0, false);
            }
        }

        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.zoneShrinking);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddCannonToTileServerRpc(Vector2 pos, ulong id)
    {
        TileScript t = GetTileAtPosition(pos);

        if (t.Walkable || t.Mountain || t.ShopTile) return;

        Cannon c = Instantiate(cannonPrefab, new Vector3(pos.x, pos.y, -1), Quaternion.identity);
        c.GetComponent<NetworkObject>().Spawn();
        c.ID.Value = id;
        c.tiles = PathfindScript.GetCombatZoneSize(t, 3);
        c.SetColorClientRpc(id);

        t.cannonInTile.Value = true;

        cannonsOnMap.Add(c);

        Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
        p.HasDidAnActionClientRpc();
        p.UseManaClientRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void CheckForUnitInCannonRangesServerRpc(ulong id, Vector2 newShipPos)
    {
        foreach (Cannon c in cannonsOnMap)
        {
            if (c.ID.Value == id) continue;
            c.CannonDamageInRangeServerRpc(newShipPos);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DamageCannonOnServerRpc(Vector2 cannonPos)
    {
        int getIndex = 0;
        bool foundCannon = false;

        Cannon cannon = null;

        foreach(var c in cannonsOnMap)
        {
            if(c.cannonPos == cannonPos)
            {
                cannon = c;
                foundCannon = true;
                break;
            }
        }

        getIndex = cannon.index;

        if (foundCannon)
        {
            for(int i = 0; i < cannonsOnMap.Count; i++)
            {
                if (cannonsOnMap[i].index == getIndex)
                    cannonsOnMap.RemoveAt(i);
            }

            TileScript t = GetTileAtPosition(cannonPos);
            t.cannonInTile.Value = false;

            Destroy(cannon.gameObject);
        }

        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.shipDestroyed);
    }

    [ServerRpc]
    public void SpawnShipwrekServerRpc(UpgradeSystem.UpgradeType upgrade, Vector2 pos)
    {
        Shipwrek shipwrek = Instantiate(shipwreckPrefab, pos, Quaternion.identity);
        shipwrek.GetComponent<NetworkObject>().Spawn();
        shipwrek.upgradeType.Value = upgrade;

        SetShipwreckOnMapServerRpc(pos, true);
    }

    [ServerRpc]
    public void SetShipwreckOnMapServerRpc(Vector2 pos, bool active)
    {
        TileScript t = GetTileAtPosition(pos);
        t.shipwrek.Value = active;
    }

}
