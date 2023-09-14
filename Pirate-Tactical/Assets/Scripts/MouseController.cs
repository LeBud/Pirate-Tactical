using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Tilemaps;

public class MouseController : MonoBehaviour
{
    [SerializeField] float speed;

    [SerializeField] GameObject shipPrefab;
    PirateShip ship;

    PathFinder pathFinder;
    RangeFinder rangeFinder;
    List<OverlayTile> path = new List<OverlayTile>();
    List<OverlayTile> inRangeTiles = new List<OverlayTile>();

    private void Start()
    {
        pathFinder = new PathFinder();
        rangeFinder = new RangeFinder();
    }

    private void LateUpdate()
    {
        RaycastHit2D? focusedTile = GetFocusedOnTile();

        if (focusedTile.HasValue)
        {
            Collider2D overlayTile = focusedTile.Value.collider;
            transform.position = overlayTile.transform.position;
            GetComponent<SpriteRenderer>().sortingOrder = overlayTile.GetComponent<SpriteRenderer>().sortingOrder;

            if (Input.GetMouseButtonDown(0))
            {
                OverlayTile selectedTile = overlayTile.GetComponent<OverlayTile>();
                selectedTile.ShowTile();

                if(ship == null)
                {
                    ship = Instantiate(shipPrefab).GetComponent<PirateShip>();
                    PositionShipOnMap(overlayTile.GetComponent<OverlayTile>());
                    GetInRangeTiles();
                    return;
                }

                path = pathFinder.FindPath(ship.currentTile, selectedTile, inRangeTiles);
            }
        }

        if(path.Count > 0)
        {
            MoveAlongPath();
        }
    }

    void MoveAlongPath()
    {
        var step = speed * Time.deltaTime;

        var zIndex = path[0].transform.position.z;
        ship.transform.position = Vector2.MoveTowards(ship.transform.position, path[0].transform.position, step);
        Vector3 pos = ship.transform.position;

        ship.transform.position = new Vector3(pos.x, pos.y, zIndex);

        if(Vector2.Distance(ship.transform.position, path[0].transform.position) < .0001f)
        {
            PositionShipOnMap(path[0]);
            path.RemoveAt(0);
        }

        //Update Range Display -- Change this if u don't want it to be display once the ship has traveled
        if(path.Count <= 0)
        {
            GetInRangeTiles();
        }
    }
    void PositionShipOnMap(OverlayTile tile)
    {
        ship.transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y + .0001f, tile.transform.position.z - 1);
        ship.GetComponent<SpriteRenderer>().sortingOrder = tile.GetComponent<SpriteRenderer>().sortingOrder;
        ship.currentTile = tile;
    }

    void GetInRangeTiles()
    {
        foreach(var tile in inRangeTiles)
        {
            tile.HideTile();
        }

        inRangeTiles = rangeFinder.GetTilesInRange(ship.currentTile, ship.travelRange);

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

}
