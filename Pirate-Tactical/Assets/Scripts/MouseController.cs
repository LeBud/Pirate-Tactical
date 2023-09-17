using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using static GameManager;

public class MouseController : NetworkBehaviour
{
    [SerializeField] float speed;

    PathFinder pathFinder;
    RangeFinder rangeFinder;
    DirectionTranslator directionTranslator;

    List<OverlayTile> path = new List<OverlayTile>();
    List<OverlayTile> inRangeTiles = new List<OverlayTile>();

    ShipManager sm;

    PirateShip currentShip;

    bool shipMoving = false;

    OverlayTile currentMouseTile;

    List<OverlayTile> tilesMap = new List<OverlayTile>();

    public PlayerTurn player;

    private void Start()
    {
        pathFinder = new PathFinder();
        rangeFinder = new RangeFinder();
        directionTranslator = new DirectionTranslator();
        sm = GetComponent<ShipManager>();

        BoundsInt bounds = MapManager.Instance.tileMap.cellBounds;

        for (int z = bounds.max.z; z >= bounds.min.z; z--)
        {
            for (int y = bounds.min.y; y < bounds.max.y; y++)
            {
                for (int x = bounds.min.x; x < bounds.max.x; x++)
                {
                    Vector3Int tileLocation = new Vector3Int(x, y, z);
                    Vector2Int tileKey = new Vector2Int(x, y);

                    if (MapManager.Instance.tileMap.HasTile(tileLocation))
                    {
                        var overlayTiles = MapManager.Instance.map[tileKey];
                        tilesMap.Add(overlayTiles);
                    }
                }
            }
        }
    }

    private void LateUpdate()
    {
        if (!IsOwner)
        {
            return;
        }

        RaycastHit2D? focusedTile = GetFocusedOnTile();

        if (focusedTile.HasValue)
        {
            Collider2D overlayTile = focusedTile.Value.collider;
            transform.position = overlayTile.transform.position;
            GetComponent<SpriteRenderer>().sortingOrder = overlayTile.GetComponent<SpriteRenderer>().sortingOrder;
            currentMouseTile = overlayTile.GetComponent<OverlayTile>();

            //Make PathFinding
            if(sm.allShipsSpawned && sm.shipCurrentlySelected)
                PathThind(currentMouseTile);

            PlayerActions();
        }

        if(path.Count > 0 && shipMoving)
            MoveAlongPath();
        
    }

    void PlayerActions()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RefreshBlockedTile();
            currentMouseTile.ShowTile();

            if (!sm.allShipsSpawned)
            {
                SpawnShpis(currentMouseTile);
                return;
            }

            if (!sm.shipCurrentlySelected)
            {
                SelectShip();
                return;
            }

            if (sm.shipCurrentlySelected && !inRangeTiles.Contains(currentMouseTile))
            {
                DeselectShip();
                return;
            }

            //MoveShip
            if (inRangeTiles.Contains(currentMouseTile))
                shipMoving = true;
        }
    }

    #region ShipsActions
    void SelectShip()
    {
        for (int i = 0; i < sm.ships.Length; i++)
        {
            if (sm.ships[i].currentTile == currentMouseTile)
            {
                sm.shipIndex = sm.ships[i].index;
                currentShip = sm.ships[i];
                sm.shipCurrentlySelected = true;
                GetInRangeTiles();

                break;
            }
        }
    }

    void DeselectShip()
    {
        sm.shipCurrentlySelected = false;
        currentShip = null;
        RefreshBlockedTile();
        currentMouseTile.ShowTile();
        path.Clear();
        shipMoving = false;
    }

    void SpawnShpis(OverlayTile tile)
    {
        int index = sm.shipIndex;
        currentShip = Instantiate(sm.ships[index]);
        currentShip.GetComponent<NetworkObject>().SpawnWithOwnership(NetworkManager.Singleton.LocalClientId);
        sm.ships[index] = currentShip;
        PositionShipOnMap(currentMouseTile);
        //GetInRangeTiles();

        sm.ships[index].index = index;

        sm.shipIndex++;
        sm.remainShipToSpawn--;

        sm.CheckIfAllSpawn();
    }

    #endregion

    void PathThind(OverlayTile tile)
    {
        if (inRangeTiles.Contains(tile) && !shipMoving)
        {
            path = pathFinder.FindPath(currentShip.currentTile, tile, inRangeTiles);

            GetInRangeTiles();

            for (int i = 0; i < path.Count; i++)
            {
                var previousTile = i > 0 ? path[i - 1] : currentShip.currentTile;
                var futureTile = i < path.Count - 1 ? path[i + 1] : null;

                var dir = directionTranslator.TranslateDirection(previousTile, path[i], futureTile);
                path[i].SetDirSprite(dir);
            }
        }
    }


    void MoveAlongPath()
    {
        var step = speed * Time.deltaTime;

        var zIndex = path[0].transform.position.z;
        currentShip.transform.position = Vector2.MoveTowards(currentShip.transform.position, path[0].transform.position, step);
        Vector3 pos = currentShip.transform.position;

        currentShip.transform.position = new Vector3(pos.x, pos.y, zIndex);

        if(Vector2.Distance(currentShip.transform.position, path[0].transform.position) < .0001f)
        {
            PositionShipOnMap(path[0]);
            path.RemoveAt(0);
        }

        //Update Range Display -- Change this if u don't want it to be display once the ship has traveled
        if(path.Count <= 0)
        {
            RefreshBlockedTile();
            GetInRangeTiles();
            shipMoving = false;
        }
    }

    void PositionShipOnMap(OverlayTile tile)
    {
        currentShip.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y + .0001f, tile.transform.position.z - 1);
        currentShip.GetComponent<SpriteRenderer>().sortingOrder = tile.GetComponent<SpriteRenderer>().sortingOrder;
        currentShip.currentTile = tile;
    }

    void GetInRangeTiles()
    {
        foreach(var tile in inRangeTiles)
        {
            tile.HideTile();
        }

        inRangeTiles = rangeFinder.GetTilesInRange(currentShip.currentTile, currentShip.shipInfo.shipSpeed);

        foreach (var tile in inRangeTiles)
        {
            tile.ShowTile();
        }
    }

    public RaycastHit2D? GetFocusedOnTile()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 pos = new Vector2(mousePos.x, mousePos.y);

        RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero);
        if (hits.Length > 0) return hits.OrderByDescending(i => i.collider.transform.position.z).First();
        return null;
    }


    void RefreshBlockedTile()
    {
        foreach(var tile in tilesMap)
        {
            tile.HideTile();
            tile.isBLocked = false;
        }

        if (sm.allShipsSpawned)
        {
            for (int i = 0; i < sm.ships.Length; i++)
                sm.ships[i].currentTile.isBLocked = true;
        }
    }
}
