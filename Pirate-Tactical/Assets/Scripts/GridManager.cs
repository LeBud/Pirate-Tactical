using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GridManager : NetworkBehaviour
{
    public static GridManager Instance { get; private set; }

    [SerializeField] int _width, _height;
    [SerializeField] TileScript _tilePrefab1, _tilePrefab2;
    [SerializeField] Transform _cam;

    //Dictionary<Vector2, TileScript> _tiles;
    public NetworkList<Vector2> dictionnary;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;

        dictionnary = new NetworkList<Vector2>();
    }

    [ServerRpc]
    public void GenerateGridServerRpc()
    {
        if(!IsServer) return;

        for (int x = 0; x < _width; x++)
        {
            for (int y = 0; y < _height; y++)
            {
                var isOffset = (x % 2 == 0 && y % 2 != 0) || (x % 2 != 0 && y % 2 == 0);

                var spawnedTile = Instantiate(isOffset ? _tilePrefab1 : _tilePrefab2, new Vector3(x, y), Quaternion.identity);
                spawnedTile.name = $"Tile {x} {y}";

                spawnedTile.GetComponent<NetworkObject>().Spawn();
                spawnedTile.transform.parent = transform;

                spawnedTile.pos.Value = new Vector2(x, y);
                dictionnary.Add(new Vector2(x, y));

            }
        }

        _cam.transform.position = new Vector3((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f, -10);
    }

    [ServerRpc]
    public void JoinServerServerRpc()
    {
        if(!IsOwner) return;
        Camera.main.transform.position = new Vector3((float)_width / 2 - 0.5f, (float)_height / 2 - 0.5f, -10);
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
}
