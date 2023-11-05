using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance { get; private set; }

    public int _width, _height;
    [Header("Tiles")]
    [SerializeField] TileScript seaTile;
    [SerializeField] TileScript landTile;

    public NetworkList<Vector2> dictionnary;
    public NetworkVariable<int> combatZoneSize = new NetworkVariable<int>();
    
    List<TileScript> tilesGrid = new List<TileScript>();
    List<TileScript> blockedTiles = new List<TileScript>();
    List<TileScript> outOfCombatZoneTiles = new List<TileScript>();

    public int combatZoneDamage = 4;

    [Header("TileMap Editor")]
    public Tilemap map;
    [SerializeField] Sprite waterSprite;
    [SerializeField] Sprite landSprite;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;

        dictionnary = new NetworkList<Vector2>();

        combatZoneSize.Value = _width / 2 + (_height - 2) / 2;
    }

    //Génère la grille de jeu et la setup
    [ServerRpc]
    public void GenerateGridServerRpc()
    {
        if(!IsServer) return;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var spawnedTile = Instantiate(seaTile, new Vector3(x, y), Quaternion.identity);
                spawnedTile.name = $"Tile {x} {y}";
                tilesGrid.Add(spawnedTile);

                spawnedTile.GetComponent<NetworkObject>().Spawn();
                spawnedTile.transform.parent = transform;

                spawnedTile.pos.Value = new Vector2(x, y);
                spawnedTile.offsetTile.Value = (x + y) % 2 == 1;

                dictionnary.Add(new Vector2(x, y));
            }
        }

    }

    [ServerRpc]
    public void GenerateGridOnTileMapServerRpc()
    {
        if (!IsServer) return;

        BoundsInt bounds = map.cellBounds;

        combatZoneSize.Value = bounds.max.x / 2 + (bounds.max.y - 2) / 2;
        
        GameManager.Instance.cameraPos.Value = new Vector3((float)bounds.center.x - 0.5f, (float)bounds.center.y - 0.5f, -10);

        for (int  x = bounds.min.x; x < bounds.max.x; x++)
        {
            for(int  y = bounds.min.y; y < bounds.max.y; y++)
            {
                if(map.HasTile(new Vector3Int(x, y)))
                {
                    TileScript toSpawn = seaTile;
                    if (map.GetSprite(new Vector3Int(x, y)) == waterSprite)
                        toSpawn = seaTile;
                    else
                        toSpawn = landTile;

                    var spawnedTile = Instantiate(toSpawn, new Vector3Int(x, y), Quaternion.identity);
                    spawnedTile.name = $"Tile {x} {y}";
                    tilesGrid.Add(spawnedTile);

                    spawnedTile.GetComponent<NetworkObject>().Spawn();
                    spawnedTile.transform.parent = transform;

                    spawnedTile.pos.Value = new Vector2(x, y);
                    spawnedTile.offsetTile.Value = Mathf.Abs(x + y) % 2 == 1;

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

    [ServerRpc(RequireOwnership = false)]
    public void DamageUnitServerRpc(int damage, Vector2 pos, ulong id, bool passiveAttack, int effectDuration)
    {
        ShipUnit[] ships = FindObjectsOfType<ShipUnit>();

        bool isEnemy = false;

        for(int i = 0; i < ships.Length; i++)
        {
            if (ships[i].unitPos.Value == pos)
            {
                if (ships[i].GetComponent<NetworkObject>().OwnerClientId != id)
                {
                    isEnemy = true;
                    ships[i].TakeDamageServerRpc(damage, pos, passiveAttack, effectDuration);
                    break;
                }

            }
        }

        if (isEnemy)
        {
            Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();
            p.HasAttackedEnemyClientRpc();
        }

    }

    [ServerRpc(RequireOwnership = false)]
    public void DamageUnitTShotServerRpc(int damage, Vector2 pos, ulong id, bool passiveAttack, int effectDuration)
    {
        ShipUnit[] ships = FindObjectsOfType<ShipUnit>();

        for (int i = 0; i < ships.Length; i++)
        {
            if (ships[i].unitPos.Value == pos)
            {
                if (ships[i].GetComponent<NetworkObject>().OwnerClientId != id)
                {
                    ships[i].TakeDamageServerRpc(damage, pos, passiveAttack, effectDuration);
                    break;
                }

            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void DamageUnitByMineServerRpc(int damage, Vector2 pos, bool passiveAttack, int effectDuration)
    {
        ShipUnit[] ships = FindObjectsOfType<ShipUnit>();

        for (int i = 0; i < ships.Length; i++)
        {
            if (ships[i].unitPos.Value == pos)
            {
                ships[i].TakeDamageServerRpc(damage, pos, passiveAttack, effectDuration);
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
    public void BlockedTileServerRpc(Vector2 tilePos)
    {
        if (dictionnary.Contains(tilePos))
        {
            foreach(var t in tilesGrid)
                if(t.pos.Value == tilePos)
                {
                    t.SetTileToBlockTileClientRpc(true);
                    t.blockedTile.Value = true;
                    blockedTiles.Add(t);
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
                    t.SetMineTileToClientRpc(id, false);
                    break;
                }
        }
        else if (active && dictionnary.Contains(tilePos))
        {
            foreach (var t in tilesGrid)
                if (t.pos.Value == tilePos)
                {
                    t.mineInTile.Value = true;
                    t.SetMineTileToClientRpc(id, true);
                    break;
                }
        }
    }

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
        TileScript midTile = GetTileAtPosition(new Vector2(bounds.center.x, bounds.center.y));
        List<TileScript> rangeTiles = PathFindTesting.GetCombatZoneSize(midTile, combatZoneSize.Value);

        foreach(var t in tilesGrid)
        {
            if (!rangeTiles.Contains(t) && !outOfCombatZoneTiles.Contains(t))
            {
                outOfCombatZoneTiles.Add(t);
                t.tileOutOfCombatZone.Value = true;
                t.SetTileToOutOfZoneClientRpc();
            }
        }

    }
}
