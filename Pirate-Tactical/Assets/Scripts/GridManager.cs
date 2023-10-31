using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance { get; private set; }

    public int _width, _height;
    [SerializeField] TileScript _tilePrefab;

    public NetworkList<Vector2> dictionnary;
    List<TileScript> tilesGrid = new List<TileScript>();
    List<TileScript> blockedTiles = new List<TileScript>();
    List<TileScript> outOfCombatZoneTiles = new List<TileScript>();

    //int combatZoneSize;
    public NetworkVariable<int> combatZoneSize = new NetworkVariable<int>();

    public int combatZoneDamage = 4;
    
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
                var spawnedTile = Instantiate(_tilePrefab, new Vector3(x, y), Quaternion.identity);
                spawnedTile.name = $"Tile {x} {y}";
                tilesGrid.Add(spawnedTile);

                spawnedTile.GetComponent<NetworkObject>().Spawn();
                spawnedTile.transform.parent = transform;

                spawnedTile.pos.Value = new Vector2(x, y);
                spawnedTile.offsetTile.Value = (x + y) % 2 == 1;

                dictionnary.Add(new Vector2(x, y));
            }
        }

        //_cam.transform.position = new Vector3((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f, -10);
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

    //Ca marche mais ca fait buggé de fou malade dri POW plaplaplaplaplapla PLA ca me saoule
    [ServerRpc(RequireOwnership = false)]
    public void DamageUnitServerRpc(int damage, Vector2 pos, ulong id)
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
                    ships[i].TakeDamageServerRpc(damage, pos);

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

        TileScript midTile = GetTileAtPosition(new Vector2(_width/ 2, _height/2));
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
