using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
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

    public Dictionary<Vector2Int, OverlayTile> map = new Dictionary<Vector2Int, OverlayTile>();
    public GetNeighborTiles getNeighborTiles = new GetNeighborTiles();

    public NetworkList<tileMapDictionnary> dictionnary;

    public struct tileMapDictionnary : INetworkSerializable, IEquatable<tileMapDictionnary>
    {
        public Vector2Int keyPos;
        public int indexPos;

        public tileMapDictionnary(Vector2Int _keyPos, int _indexPos)
        {
            keyPos = _keyPos; indexPos = _indexPos;
        }

        public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
        {
            serializer.SerializeValue(ref keyPos);
            serializer.SerializeValue(ref indexPos);
        }

        public bool Equals(tileMapDictionnary other)
        {
            return keyPos == other.keyPos && indexPos == other.indexPos;
        }
    }

    private void Awake()
    {
        if(Instance != null && Instance != this)
            Destroy(Instance);
        else
            Instance = this;

        dictionnary = new NetworkList<tileMapDictionnary>();
    }

    private void OnDisable()
    {
        if (!IsServer) return;
        dictionnary.Dispose();
    }

    [ClientRpc]
    public void SetClientInstanceClientRpc()
    {
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
                        dictionnary.Add(new tileMapDictionnary { keyPos = tileKey, indexPos = overlayContainer.childCount - 1 });
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

        if (searchableTiles.Count > 0)
        {
            foreach (var tile in searchableTiles)
            {
                tilesToSearch.Add(new Vector2Int(tile.posX.Value, tile.posY.Value), tile);
            }
        }
        else
        {
            foreach (var tile in MapManager.Instance.dictionnary)
                tilesToSearch.Add(tile.keyPos, MapManager.Instance.overlayContainer.GetChild(tile.indexPos).GetComponent<OverlayTile>());
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
                if (Mathf.Abs(currentTile.posZ.Value - tilesToSearch[locationToCheck].posZ.Value) <= 1)
                    neighbors.Add(tilesToSearch[locationToCheck]);
            }
        }

        return neighbors;
    }
}
