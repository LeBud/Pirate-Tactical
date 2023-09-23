using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;

public class MouseController : NetworkBehaviour
{
    [SerializeField] float speed;

    PathFinder pathFinder;
    RangeFinder rangeFinder;
    DirectionTranslator directionTranslator;

    public List<OverlayTile> path = new List<OverlayTile>();
    public List<OverlayTile> inRangeTiles = new List<OverlayTile>();

    OverlayTile currentMouseTile;
    List<OverlayTile> tilesMap = new List<OverlayTile>();

    ShipManager sm;
    public PirateShip currentShip;

    public bool shipMoving = false;

    private void Start()
    {
        pathFinder = new PathFinder();
        rangeFinder = new RangeFinder();
        directionTranslator = new DirectionTranslator();
        sm = GetComponent<ShipManager>();

        tilesMap = MapManager.Instance.overlayTilesMap;
    }

    private void LateUpdate()
    {
        if (!IsOwner)
            return;

        RaycastHit2D? focusedTile = GetFocusedOnTile();

        if (focusedTile.HasValue)
        {
            Collider2D overlayTile = focusedTile.Value.collider;
            transform.position = overlayTile.transform.position;
            GetComponent<SpriteRenderer>().sortingOrder = overlayTile.GetComponent<SpriteRenderer>().sortingOrder;
            currentMouseTile = overlayTile.GetComponent<OverlayTile>();

            if(AllShipsSpawned())
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
            Debug.Log(currentMouseTile.name);
            RefreshBlockedTile();
            currentMouseTile.ShowTile();

            sm.ExecuteAction(currentMouseTile);
        }
    }

    public void PathThind(OverlayTile tile)
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

    public void PositionShipOnMap(OverlayTile tile)
    {
        currentShip.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y + .0001f, tile.transform.position.z - 1);
        currentShip.GetComponent<SpriteRenderer>().sortingOrder = tile.GetComponent<SpriteRenderer>().sortingOrder;
        currentShip.currentTile = tile.GetComponent<OverlayTile>();
    }

    public void GetInRangeTiles()
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

    public void RefreshBlockedTile()
    {
        foreach (var tile in tilesMap)
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

    public RaycastHit2D? GetFocusedOnTile()
    {
        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 pos = new Vector2(mousePos.x, mousePos.y);

        RaycastHit2D[] hits = Physics2D.RaycastAll(pos, Vector2.zero);
        if (hits.Length > 0) return hits.OrderByDescending(i => i.collider.transform.position.z).First();
        return null;
    }

    #region Boolean

    bool AllShipsSpawned()
    {
        return sm.allShipsSpawned && sm.shipCurrentlySelected;
    }

    #endregion
}
