using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : NetworkBehaviour
{
    //public static MapManager _instance;
    public static MapManager Instance { get; private set; }

    [Header("TileMap Component")]
    public Tilemap tileMap;

    [Header("Tile Settings")]
    public OverlayTile overlayTilePref;
    public Transform overlayContainer;

    public Dictionary<Vector2Int, OverlayTile> map;

    public List<PirateShip> tempSpawnShip = new List<PirateShip>();
    public List<OverlayTile> overlayTilesMap = new List<OverlayTile>();

    private void Awake()
    {
        if(Instance != null && Instance != this)
            Destroy(Instance);
        else
        Instance = this;
    }

    public void SetClientInstance()
    {
        if (Instance != null && Instance != this)
            Destroy(Instance);
        else
            Instance = this;
    }

    private void Start()
    {
        if (!IsHost) return;

        map = new Dictionary<Vector2Int, OverlayTile>();

        BoundsInt bounds = tileMap.cellBounds;

        for(int z = bounds.max.z; z >= bounds.min.z; z--)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                for (int x = bounds.min.x; x < bounds.max.x; x++)
                {
                    Vector3Int tileLocation = new Vector3Int(x, y, z);
                    Vector2Int tileKey = new Vector2Int(x, y);

                    if (tileMap.HasTile(tileLocation) && !map.ContainsKey(tileKey))
                    {
                        var overlayTile = Instantiate(overlayTilePref, overlayContainer);
                        overlayTile.name = "Tile : " + x + "," + y;
                        var cellWorldPos = tileMap.GetCellCenterWorld(tileLocation);

                        overlayTile.transform.position = new Vector3(cellWorldPos.x, cellWorldPos.y, cellWorldPos.z - 1);
                        overlayTile.GetComponent<SpriteRenderer>().sortingOrder = tileMap.GetComponent<TilemapRenderer>().sortingOrder;
                        overlayTile.gridLocation = tileLocation;
                        map.Add(tileKey, overlayTile);
                        overlayTilesMap.Add(overlayTile);
                    }
                }
            }
        }
    }

    [ServerRpc]
    public void InitialiseServerRpc()
    {
        map = new Dictionary<Vector2Int, OverlayTile>();

        BoundsInt bounds = tileMap.cellBounds;

        for (int z = bounds.max.z; z >= bounds.min.z; z--)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                for (int x = bounds.min.x; x < bounds.max.x; x++)
                {
                    Vector3Int tileLocation = new Vector3Int(x, y, z);
                    Vector2Int tileKey = new Vector2Int(x, y);

                    if (tileMap.HasTile(tileLocation) && !map.ContainsKey(tileKey))
                    {
                        var overlayTile = Instantiate(overlayTilePref);
                        NetworkObject tile = overlayTile.GetComponent<NetworkObject>();
                        tile.Spawn();
                        tile.TrySetParent(overlayContainer, true);
                        tile.name = "Tile : " + x + "," + y;
                        var cellWorldPos = tileMap.GetCellCenterWorld(tileLocation);

                        overlayTile.transform.position = new Vector3(cellWorldPos.x, cellWorldPos.y, cellWorldPos.z - 1);
                        overlayTile.GetComponent<SpriteRenderer>().sortingOrder = tileMap.GetComponent<TilemapRenderer>().sortingOrder;
                        overlayTile.gridLocation = tileLocation;
                        map.Add(tileKey, overlayTile);
                        overlayTilesMap.Add(overlayTile);
                    }
                }
            }
        }

    }

    public List<OverlayTile> GetNeighborTiles(OverlayTile currentTile, List<OverlayTile> searchableTiles)
    {
        Dictionary<Vector2Int, OverlayTile> tilesToSearch = new Dictionary<Vector2Int, OverlayTile>();

        if(searchableTiles.Count > 0)
        {
            foreach(var tile in searchableTiles)
            {
                tilesToSearch.Add(tile.grid2DPos, tile);
            }
        }
        else
        {
            tilesToSearch = map;
        }

        List<OverlayTile> neighbors = new List<OverlayTile>();

        Vector2Int locationToCheck = new Vector2Int(currentTile.posX.Value, currentTile.posY.Value);

        for (int i = 0; i < 4; i++)
        {
            switch (i)
            {
                case 0:
                    locationToCheck = new Vector2Int(currentTile.posX.Value, currentTile.posY.Value + 1);
                    break;
                case 1:
                    locationToCheck = new Vector2Int(currentTile.posX.Value, currentTile.posY.Value - 1);
                    break;
                case 2:
                    locationToCheck = new Vector2Int(currentTile.posX.Value + 1, currentTile.posY.Value);
                    break;
                case 3:
                    locationToCheck = new Vector2Int(currentTile.posX.Value - 1, currentTile.posY.Value);
                    break;
            }

            if (tilesToSearch.ContainsKey(locationToCheck))
            {
                //Not necessary if no differents heights in the game -- If it broke something remove it
                if (Mathf.Abs(currentTile.gridLocation.z - tilesToSearch[locationToCheck].gridLocation.z) <= 1)
                    neighbors.Add(tilesToSearch[locationToCheck]);
            }
        }

        return neighbors;

    }
}
