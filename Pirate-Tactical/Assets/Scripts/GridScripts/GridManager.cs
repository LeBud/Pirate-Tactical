using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

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

    [Header("Damages")]
    public int combatZoneDamage = 4;
    public int mineDamage = 10;
    public int upgradedMineDamage = 15;
    public int upgradedMineZoneDamage = 7;
    public int collisionDamage;

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

    [Header("DisplayTxt")]
    public List<GameObject> displayTxts = new List<GameObject>();

    List<Cannon> cannonsOnMap = new List<Cannon>();

    //Pooling
    int poolIndex = 0;

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

        combatZoneSize.Value += 4;
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
    public void ApplyEffectOnShipServerRpc(Vector2 pos, int duration, ulong id, bool upgraded)
    {
        ShipUnit unit = GetShipAtPos(pos);
        unit.GiveWindEffectClientRpc(duration, upgraded);

        Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
        p.HasDidAnActionClientRpc();
        p.UseManaClientRpc();
    }

    #region DamageUnit

    [ServerRpc(RequireOwnership = false)]
    public void DamageUnitServerRpc(int damage, Vector2 pos, ulong id, bool passiveAttack, int effectDuration, bool special, bool hasGoThroughWater, bool fireUpgraded)
    {
        ShipUnit ships = GetShipAtPos(pos);

        bool isEnemy = false;

        if(ships != null)
        {
            if (ships.GetComponent<NetworkObject>().OwnerClientId != id)
            {
                isEnemy = true;
                ships.TakeDamageServerRpc(damage, pos, passiveAttack, effectDuration, hasGoThroughWater);
            }
        }

        if (fireUpgraded && !hasGoThroughWater)
        {
            Vector2 check = pos;
            if (GetShipAtPos(new Vector2(check.x + 1, check.y)))
                GetShipAtPos(new Vector2(check.x + 1, check.y)).TakeDamageServerRpc(damage, new Vector2(check.x + 1, check.y), passiveAttack, 1, hasGoThroughWater);
            if (GetShipAtPos(new Vector2(check.x - 1, check.y)))
                GetShipAtPos(new Vector2(check.x - 1, check.y)).TakeDamageServerRpc(damage, new Vector2(check.x - 1, check.y), passiveAttack, 1, hasGoThroughWater);
            if (GetShipAtPos(new Vector2(check.x, check.y + 1)))
                GetShipAtPos(new Vector2(check.x, check.y + 1)).TakeDamageServerRpc(damage, new Vector2(check.x, check.y + 1), passiveAttack, 1, hasGoThroughWater);
            if (GetShipAtPos(new Vector2(check.x, check.y - 1)))
                GetShipAtPos(new Vector2(check.x, check.y - 1)).TakeDamageServerRpc(damage, new Vector2(check.x, check.y - 1), passiveAttack, 1, hasGoThroughWater);
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

        if (ships.GetComponent<NetworkObject>().OwnerClientId != id)
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
        int allyDmg = ally.unitAccostDamage + accostAttackBoost + ally.accostDmgBoost.Value;
        int enemyDmg = enemy.unitAccostDamage + enemy.accostDmgBoost.Value;

        ally.TakeDamageServerRpc(enemyDmg, allyPos, false, 0, false);
        enemy.TakeDamageServerRpc(allyDmg, enemyPos, false, 0, false);
        
        //Dépenser les points
        Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
        p.HasDidAnActionClientRpc();

        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.accost);
    }


    #endregion

    #region Push/Pull Ship
    [ServerRpc(RequireOwnership = false)]
    public void PushUnitServerRpc(Vector2 pushShip, Vector2 currentShip, ulong id, bool upgraded, bool isBrochetteShot)
    {
        ShipUnit ships = GetShipAtPos(pushShip);
        Vector2 posToCheck = new Vector2();

        bool hasPush = false;

        int amountPush = 1;
        int finished = 0;

        bool hasStop = false;

        Vector2 finalPos = pushShip;

        if (upgraded)
        {
            if (ships.unitName == ShipUnit.UnitType.Galion)
                amountPush = 1;
            else if (ships.unitName == ShipUnit.UnitType.Brigantin)
                amountPush = 2;
            else if (ships.unitName == ShipUnit.UnitType.Sloop)
                amountPush = 3;

            Debug.Log("Force de poussé : " + amountPush);
        }

        for(int i = 0; i < amountPush; i++)
        {
            Debug.Log("Boucle se joue " + i);
            if (ships.unitPos.Value == pushShip)
            {
                SetShipOnTileServerRpc(pushShip, false);
                if (currentShip.y == pushShip.y)
                {
                    if (currentShip.x > pushShip.x)
                    {
                        //alors -1
                        posToCheck = new Vector2(finalPos.x - 1, finalPos.y);
                        if (dictionnary.Contains(posToCheck))
                        {
                            TileScript t = GetTileAtPosition(posToCheck);
                            if (!t.Walkable && !t.shipOnTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPush = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && (t.shipOnTile.Value || t.blockedTile.Value) && !t.mineInTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                GetShipAtPos(posToCheck).TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPush = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && t.mineInTile.Value)
                            {
                                finalPos = posToCheck;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                            {
                                finalPos = new Vector2(finalPos.x - 1, finalPos.y);
                                hasPush = true;
                            }
                        }
                    }
                    else
                    {
                        //alors +1
                        posToCheck = new Vector2(finalPos.x + 1, finalPos.y);
                        if (dictionnary.Contains(posToCheck))
                        {
                            TileScript t = GetTileAtPosition(posToCheck);
                            if (!t.Walkable && !t.shipOnTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPush = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && (t.shipOnTile.Value ||t.blockedTile.Value) && !t.mineInTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                GetShipAtPos(posToCheck).TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPush = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && t.mineInTile.Value)
                            {
                                finalPos = posToCheck;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                            {
                                finalPos = new Vector2(finalPos.x + 1, finalPos.y);
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
                        posToCheck = new Vector2(finalPos.x, finalPos.y - 1);
                        if (dictionnary.Contains(posToCheck))
                        {
                            TileScript t = GetTileAtPosition(posToCheck);
                            if (!t.Walkable && !t.shipOnTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPush = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && (t.shipOnTile.Value || t.blockedTile.Value) && !t.mineInTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                GetShipAtPos(posToCheck).TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPush = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && t.mineInTile.Value)
                            {
                                finalPos = posToCheck;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                            {
                                finalPos = new Vector2(finalPos.x, finalPos.y - 1);
                                hasPush = true;
                            }
                        }
                    }
                    else
                    {
                        //alors +1
                        posToCheck = new Vector2(finalPos.x, finalPos.y + 1);
                        if (dictionnary.Contains(posToCheck))
                        {
                            TileScript t = GetTileAtPosition(posToCheck);
                            if (!t.Walkable && !t.shipOnTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPush = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && (t.shipOnTile.Value || t.blockedTile.Value) && !t.mineInTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                GetShipAtPos(posToCheck).TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPush = true;

                                hasStop = true;
                                break;
                            }
                            else if(t.Walkable && !t.shipOnTile.Value && t.mineInTile.Value)
                            {
                                finalPos = posToCheck;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                            {
                                finalPos = new Vector2(finalPos.x, finalPos.y + 1);
                                hasPush = true;
                            }
                        }
                    }
                }

            }
            //Ici pour dire fini
            finished++;
        }

        ships.unitPos.Value = new Vector3(finalPos.x, finalPos.y, -1);
        ships.SetNewTileClientRpc(finalPos);
        SetShipOnTileServerRpc(ships.unitPos.Value, true);

        if (hasPush && !isBrochetteShot && amountPush == finished)
        {
            if (GetTileAtPosition(finalPos).mineInTile.Value)
            {
                SetMineOnTileServerRpc(finalPos, 0, false, false);
                DamageUnitByMineServerRpc(mineDamage, finalPos, false, 0);
            }
            Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
            p.HasDidAnActionClientRpc();
            p.UseManaClientRpc();
        }
        else if (hasStop)
        {
            if (GetTileAtPosition(finalPos).mineInTile.Value)
            {
                SetMineOnTileServerRpc(finalPos, 0, false, false);
                DamageUnitByMineServerRpc(mineDamage, finalPos, false, 0);
            }
            Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
            p.HasDidAnActionClientRpc();
            p.UseManaClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void PullUnitServerRpc(Vector2 pushShip, Vector2 currentShip, ulong id, bool upgraded)
    {
        ShipUnit ships = GetShipAtPos(pushShip);
        Vector2 posToCheck = new Vector2();

        bool hasPull = false;
        int amountPull = 1;
        int finished = 0;
        bool hasStop = false;

        Vector2 finalPos = pushShip;

        if (upgraded)
        {
            if (ships.unitName == ShipUnit.UnitType.Galion)
                amountPull = 1;
            else if (ships.unitName == ShipUnit.UnitType.Brigantin)
                amountPull = 2;
            else if (ships.unitName == ShipUnit.UnitType.Sloop)
                amountPull = 3;
        }

        for(int i = 0; i < amountPull; i++)
        {
            if (ships.unitPos.Value == pushShip)
            {
                SetShipOnTileServerRpc(pushShip, false);
                if (currentShip.y == pushShip.y)
                {
                    if (currentShip.x > pushShip.x)
                    {
                        //alors -1
                        posToCheck = new Vector2(finalPos.x + 1, finalPos.y);
                        if (dictionnary.Contains(posToCheck))
                        {
                            TileScript t = GetTileAtPosition(posToCheck);
                            if (!t.Walkable && !t.shipOnTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPull = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && (t.shipOnTile.Value || t.blockedTile.Value) && !t.mineInTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                GetShipAtPos(posToCheck).TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPull = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && t.mineInTile.Value)
                            {
                                finalPos = posToCheck;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                            {
                                finalPos = new Vector2(finalPos.x + 1, finalPos.y);
                                hasPull = true;
                            }
                        }
                    }
                    else
                    {
                        //alors +1
                        posToCheck = new Vector2(finalPos.x - 1, finalPos.y);
                        if (dictionnary.Contains(posToCheck))
                        {
                            TileScript t = GetTileAtPosition(posToCheck);
                            if (!t.Walkable && !t.shipOnTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPull = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && (t.shipOnTile.Value || t.blockedTile.Value) && !t.mineInTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                GetShipAtPos(posToCheck).TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPull = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && t.mineInTile.Value)
                            {
                                finalPos = posToCheck;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                            {
                                finalPos = new Vector2(finalPos.x - 1, finalPos.y);
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
                        posToCheck = new Vector2(finalPos.x, finalPos.y + 1);
                        if (dictionnary.Contains(posToCheck))
                        {
                            TileScript t = GetTileAtPosition(posToCheck);
                            if (!t.Walkable && !t.shipOnTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPull = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && (t.shipOnTile.Value || t.blockedTile.Value) && !t.mineInTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                GetShipAtPos(posToCheck).TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPull = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && t.mineInTile.Value)
                            {
                                finalPos = posToCheck;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                            {
                                finalPos = new Vector2(finalPos.x, finalPos.y + 1);
                                hasPull = true;
                            }
                        }
                    }
                    else
                    {
                        //alors +1
                        posToCheck = new Vector2(finalPos.x, finalPos.y - 1);
                        if (dictionnary.Contains(posToCheck))
                        {
                            TileScript t = GetTileAtPosition(posToCheck);
                            if (!t.Walkable && !t.shipOnTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPull = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && (t.shipOnTile.Value || t.blockedTile.Value) && !t.mineInTile.Value)
                            {
                                ships.TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                GetShipAtPos(posToCheck).TakeDamageServerRpc(collisionDamage, ships.unitPos.Value, false, 0, false);
                                hasPull = true;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && t.mineInTile.Value)
                            {
                                finalPos = posToCheck;

                                hasStop = true;
                                break;
                            }
                            else if (t.Walkable && !t.shipOnTile.Value && !t.blockedTile.Value)
                            {
                                finalPos = new Vector2(finalPos.x, finalPos.y - 1);
                                hasPull = true;
                            }
                        }
                    }
                }
            }
            finished++;
        }

        ships.unitPos.Value = new Vector3(finalPos.x, finalPos.y, -1);
        ships.SetNewTileClientRpc(finalPos);
        SetShipOnTileServerRpc(ships.unitPos.Value, true);

        if (hasPull && finished == amountPull)
        {
            if (GetTileAtPosition(finalPos).mineInTile.Value)
            {
                SetMineOnTileServerRpc(finalPos, 0, false, false);
                DamageUnitByMineServerRpc(mineDamage, finalPos, false, 0);
            }
            Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
            p.HasDidAnActionClientRpc();
            p.UseManaClientRpc();
        }
        else if (hasStop)
        {
            if (GetTileAtPosition(finalPos).mineInTile.Value)
            {
                SetMineOnTileServerRpc(finalPos, 0, false, false);
                DamageUnitByMineServerRpc(mineDamage, finalPos, false, 0);
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
        ShipUnit ships = GetShipAtPos(pos);

        if (GetTileAtPosition(pos).upgradedMine)
            damage = upgradedMineDamage;

        if (ships != null)
            ships.TakeDamageServerRpc(damage, pos, passiveAttack, effectDuration, false);

        DisplayDamageClientRpc("mine", new Vector2(pos.x, pos.y + 0.5f));

        #region UpgradedMineAoE
        if (GetTileAtPosition(pos).upgradedMine)
        {
            Vector2 check = pos;
            ShipUnit unit = GetShipAtPos(pos);
            if(GetTileAtPosition(new Vector2(check.x + 1, check.y)).shipOnTile.Value)
            {
                unit = GetShipAtPos(new Vector2(check.x + 1, check.y));
                unit.TakeDamageServerRpc(upgradedMineZoneDamage, new Vector2(check.x + 1, check.y), false, 0, false);
            }
            if (GetTileAtPosition(new Vector2(check.x - 1, check.y)).shipOnTile.Value)
            {
                unit = GetShipAtPos(new Vector2(check.x - 1, check.y));
                unit.TakeDamageServerRpc(upgradedMineZoneDamage, new Vector2(check.x - 1, check.y), false, 0, false);
            }
            if (GetTileAtPosition(new Vector2(check.x, check.y + 1)).shipOnTile.Value)
            {
                unit = GetShipAtPos(new Vector2(check.x, check.y + 1));
                unit.TakeDamageServerRpc(upgradedMineZoneDamage, new Vector2(check.x, check.y + 1), false, 0, false);
            }
            if (GetTileAtPosition(new Vector2(check.x, check.y - 1)).shipOnTile.Value)
            {
                unit = GetShipAtPos(new Vector2(check.x, check.y - 1));
                unit.TakeDamageServerRpc(upgradedMineZoneDamage, new Vector2(check.x, check.y - 1), false, 0, false);
            }
            if (GetTileAtPosition(new Vector2(check.x + 1, check.y + 1)).shipOnTile.Value)
            {
                unit = GetShipAtPos(new Vector2(check.x + 1, check.y + 1));
                unit.TakeDamageServerRpc(upgradedMineZoneDamage, new Vector2(check.x + 1, check.y + 1), false, 0, false);
            }
            if (GetTileAtPosition(new Vector2(check.x + 1, check.y - 1)).shipOnTile.Value)
            {
                unit = GetShipAtPos(new Vector2(check.x + 1, check.y - 1));
                unit.TakeDamageServerRpc(upgradedMineZoneDamage, new Vector2(check.x + 1, check.y - 1), false, 0, false);
            }
            if (GetTileAtPosition(new Vector2(check.x - 1, check.y + 1)).shipOnTile.Value)
            {
                unit = GetShipAtPos(new Vector2(check.x - 1, check.y + 1));
                unit.TakeDamageServerRpc(upgradedMineZoneDamage, new Vector2(check.x - 1, check.y + 1), false, 0, false);
            }
            if (GetTileAtPosition(new Vector2(check.x - 1, check.y - 1)).shipOnTile.Value)
            {
                unit = GetShipAtPos(new Vector2(check.x - 1, check.y - 1));
                unit.TakeDamageServerRpc(upgradedMineZoneDamage, new Vector2(check.x - 1, check.y - 1), false, 0, false);
            }
        }
        #endregion

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
    public void BlockedTileServerRpc(Vector2 tilePos, ulong id, int duration, bool upgraded, int dir)
    {
        Vector2 leftPos = new Vector2();
        Vector2 rightPos = new Vector2();

        if (upgraded)
        {
            switch (dir)
            {
                case 0:
                    leftPos = new Vector2(tilePos.x - 1, tilePos.y);
                    rightPos = new Vector2(tilePos.x + 1, tilePos.y);
                    break;
                case 1:
                    leftPos = new Vector2(tilePos.x, tilePos.y - 1);
                    rightPos = new Vector2(tilePos.x, tilePos.y + 1);
                    break;
            }

            TileScript l = GetTileAtPosition(leftPos);
            TileScript r = GetTileAtPosition(rightPos);

            if(l.Walkable && !l.shipOnTile.Value)
            {
                l.SetTileToBlockTileClientRpc(true, duration);
                l.blockedTile.Value = true;
                blockedTiles.Add(l);

                if (l.mineInTile.Value)
                    SetMineOnTileServerRpc(l.pos.Value, 0, false, false);
            }
            
            if(r.Walkable && !r.shipOnTile.Value)
            {
                r.SetTileToBlockTileClientRpc(true, duration);
                r.blockedTile.Value = true;
                blockedTiles.Add(r);

                if (r.mineInTile.Value)
                    SetMineOnTileServerRpc(r.pos.Value, 0, false, false);
            }
        }

        if (dictionnary.Contains(tilePos))
        {
            TileScript t = GetTileAtPosition(tilePos);

            t.SetTileToBlockTileClientRpc(true, duration);
            t.blockedTile.Value = true;
            blockedTiles.Add(t);

            if (t.mineInTile.Value)
                SetMineOnTileServerRpc(t.pos.Value, 0, false, false);

            Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
            p.UseManaClientRpc();
            p.HasDidAnActionClientRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetMineOnTileServerRpc(Vector2 tilePos, ulong id, bool active, bool upgraded)
    {
        TileScript t = GetTileAtPosition(tilePos);

        if (!active && dictionnary.Contains(tilePos))
        {
            if (t != null)
            {
                //Ajouter un check si la tile est upgrade pour les dégats de zone;
                t.mineInTile.Value = false;
                t.upgradedMine = false;
                foreach(ulong _id in NetworkManager.ConnectedClientsIds)
                    t.SetMineTileToClientRpc(_id, false);
                SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.mineExploding);
            }
        }
        else if (active && dictionnary.Contains(tilePos))
        {
            if (t.pos.Value == tilePos && !t.shipOnTile.Value)
            {
                t.mineInTile.Value = true;
                t.upgradedMine = upgraded;
                t.SetMineTileToClientRpc(id, true);
                Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
                p.UseManaClientRpc();
                p.HasDidAnActionClientRpc();
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
            for(int t = blockedTiles.Count - 1; t > 0; t--)
            {
                if (blockedTiles[t].blockedTile.Value)
                    blockedTiles[t].UnblockTileServerRpc();
                if(!blockedTiles[t].blockedTile.Value) blockedTiles.RemoveAt(t);
            }
        }
    }

    void CombatZoneTiles()
    {
        BoundsInt bounds = map.cellBounds;
        float xPos = bounds.center.x;
        float yPos = bounds.center.y;

        xPos = Mathf.FloorToInt(xPos);
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
                SetMineOnTileServerRpc(t.pos.Value, 0, false, false);
            }
        }

        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.zoneShrinking);
    }

    [ServerRpc(RequireOwnership = false)]
    public void AddCannonToTileServerRpc(Vector2 pos, ulong id, bool upgraded)
    {
        TileScript t = GetTileAtPosition(pos);

        if (t.Walkable || t.Mountain || t.ShopTile) return;

        Cannon c = Instantiate(cannonPrefab, new Vector3(pos.x, pos.y, -1), Quaternion.identity);
        c.GetComponent<NetworkObject>().Spawn();
        c.cannonPos = pos;
        c.ID.Value = id;
        c.index = Random.Range(1, 1000);

        if(!upgraded)
            c.tiles = PathfindScript.GetCombatZoneSize(t, 3);
        else if (upgraded)
            c.tiles = PathfindScript.GetCombatZoneSize(t, 4);

        if(upgraded)
            c.upgraded = true;

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
        shipwrek.pos = pos;
        shipwrek.roundToDisapear = GameManager.Instance.currentRound.Value + shipwrek.roundUntilDisapear;


        SetShipwreckOnMapServerRpc(pos, true);
    }

    [ServerRpc]
    public void SetShipwreckOnMapServerRpc(Vector2 pos, bool active)
    {
        TileScript t = GetTileAtPosition(pos);
        t.shipwrek.Value = active;
    }

    [ClientRpc]
    public void DisplayDamageClientRpc(string damage, Vector2 pos)
    {
        StartCoroutine(DisplayTxt(damage, pos));
    }

    IEnumerator DisplayTxt(string damage, Vector2 pos)
    {
        int txtNum = poolIndex;

        poolIndex++;
        if (poolIndex >= displayTxts.Count)
            poolIndex = 0;

        displayTxts[txtNum].SetActive(true);
        displayTxts[txtNum].transform.position = new Vector3(pos.x + 0.5f, pos.y, -5);
        displayTxts[txtNum].GetComponent<TMP_Text>().text = "-" + damage.ToString();

        yield return new WaitForSeconds(2);

        displayTxts[txtNum].SetActive(false);
    }

}
