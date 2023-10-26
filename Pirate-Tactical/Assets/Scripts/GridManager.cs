using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] int _width, _height;
    [SerializeField] TileScript _tilePrefab;
    [SerializeField] Transform _cam;

    public NetworkList<Vector2> dictionnary;
    List<TileScript> tilesGrid = new List<TileScript>();

    private void Awake()
    {
        if(Instance == null)
            Instance = this;

        dictionnary = new NetworkList<Vector2>();
    }

    //G�n�re la grille de jeu et la setup
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

    //Ca marche mais ca fait bugg� de fou malade dri POW plaplaplaplaplapla PLA ca me saoule
    [ServerRpc(RequireOwnership = false)]
    public void DamageUnitServerRpc(int damage, Vector2 pos, ulong id)
    {
        ShipUnit[] ships = FindObjectsOfType<ShipUnit>();

        for(int i = 0; i < ships.Length; i++)
        {
            if (ships[i].unitPos.Value == pos)
            {
                if (ships[i].GetComponent<NetworkObject>().OwnerClientId != id)
                {
                    ships[i].TakeDamage(damage);


                    float percent = (float)ships[i].unitLife.Value / ships[i].maxHealth;
                    ships[i].SetHealthBarClientRpc(percent);
                    break;
                }

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

}
