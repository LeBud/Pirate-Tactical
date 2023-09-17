using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MapManager : MonoBehaviour
{
    public static MapManager _instance;
    public static MapManager Instance { get { return _instance; } }

    [Header("TileMap Component")]
    public Tilemap tileMap;

    [Header("Tile Settings")]
    public OverlayTile overlayTilePref;
    public GameObject overlayContainer;

    public Dictionary<Vector2Int, OverlayTile> map;

    private void Awake()
    {
        if (_instance != null && _instance != this)
            Destroy(this.gameObject);
        else
            _instance = this;
    }


    private void Start()
    {
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
                        var overlayTile = Instantiate(overlayTilePref, overlayContainer.transform);
                        var cellWorldPos = tileMap.GetCellCenterWorld(tileLocation);

                        overlayTile.transform.position = new Vector3(cellWorldPos.x, cellWorldPos.y, cellWorldPos.z - 1);
                        overlayTile.GetComponent<SpriteRenderer>().sortingOrder = tileMap.GetComponent<TilemapRenderer>().sortingOrder;
                        overlayTile.gridLocation = tileLocation;
                        map.Add(tileKey, overlayTile);
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

        Vector2Int locationToCheck = new Vector2Int(currentTile.gridLocation.x, currentTile.gridLocation.y + 1);

        for (int i = 0; i < 4; i++)
        {
            switch (i)
            {
                case 0:
                    locationToCheck = new Vector2Int(currentTile.gridLocation.x, currentTile.gridLocation.y + 1);
                    break;
                case 1:
                    locationToCheck = new Vector2Int(currentTile.gridLocation.x, currentTile.gridLocation.y - 1);
                    break;
                case 2:
                    locationToCheck = new Vector2Int(currentTile.gridLocation.x + 1, currentTile.gridLocation.y);
                    break;
                case 3:
                    locationToCheck = new Vector2Int(currentTile.gridLocation.x - 1, currentTile.gridLocation.y);
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
