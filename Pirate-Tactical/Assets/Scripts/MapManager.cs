using System.Collections;
using System.Collections.Generic;
using System.Linq;
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
    public GetNeighborTiles getNeighborTiles = new GetNeighborTiles();
    private void Awake()
    {
        if(Instance != null && Instance != this)
            Destroy(Instance);
        else
            Instance = this;
    }

    public IEnumerator SetClientInstance()
    {
        yield return new WaitForSeconds(1);

        if (Instance != null && Instance != this)
            Destroy(Instance);
        else
            Instance = this;

    }

    [ServerRpc]
    public void InitialiseServerRpc()
    {
        if (Instance != null && Instance != this)
            Destroy(Instance);
        else
            Instance = this;

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
                        overlayTile.posX.Value = x;
                        overlayTile.posY.Value = y;
                        overlayTile.posZ.Value = z;

                        map.Add(tileKey, overlayTile);
                        overlayTilesMap.Add(overlayTile);
                    }
                }
            }
        }

    }

}

public class GetNeighborTiles
{
    public List<OverlayTile> GetNeighborsTiles(OverlayTile currentTile, List<OverlayTile> searchableTiles)
    {
        Dictionary<Vector2Int, OverlayTile> tilesToSearch = new Dictionary<Vector2Int, OverlayTile>();

        Debug.Log(MapManager.Instance.map.Count + " : key in map");

        if (searchableTiles.Count > 0)
        {
            foreach (var tile in searchableTiles)
            {
                tilesToSearch.Add(new Vector2Int(tile.posX.Value, tile.posY.Value), tile);
            }
        }
        else
        {
            tilesToSearch = MapManager.Instance.map;
        }

        //tileToSearch est == null -> probleme, trouver un moyen de le rendre non null -- C'est pas une variable serveur -- trouver un moyen détourner d'obtenir le dictionnary
        Debug.Log(tilesToSearch.Count + " : tile To search");

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

            Debug.Log(tilesToSearch.ContainsKey(locationToCheck) + " : exist in tile map");

            if (tilesToSearch.ContainsKey(locationToCheck))
            {
                //Not necessary if no differents heights in the game -- If it broke something remove it
                if (Mathf.Abs(currentTile.posZ.Value - tilesToSearch[locationToCheck].posZ.Value) <= 1)
                    neighbors.Add(tilesToSearch[locationToCheck]);
            }
        }

        return neighbors;

    }
}
