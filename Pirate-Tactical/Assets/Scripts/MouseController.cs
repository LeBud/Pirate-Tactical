using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static DirectionTranslator;

public class MouseController : MonoBehaviour
{
    [SerializeField] float speed;

    [SerializeField] GameObject shipPrefab;
    PirateShip[] ship = new PirateShip[3];

    PathFinder pathFinder;
    RangeFinder rangeFinder;
    DirectionTranslator directionTranslator;

    List<OverlayTile> path = new List<OverlayTile>();
    List<OverlayTile> inRangeTiles = new List<OverlayTile>();

    bool shipMoving = false;
    bool shipSelected;

    int shipIndex = 0;
    int spawnedShips = 0;
    private void Start()
    {
        pathFinder = new PathFinder();
        rangeFinder = new RangeFinder();
        directionTranslator = new DirectionTranslator();
    }

    private void LateUpdate()
    {
        RaycastHit2D? focusedTile = GetFocusedOnTile();

        if (focusedTile.HasValue)
        {
            Collider2D overlayTile = focusedTile.Value.collider;
            transform.position = overlayTile.transform.position;
            GetComponent<SpriteRenderer>().sortingOrder = overlayTile.GetComponent<SpriteRenderer>().sortingOrder;
            OverlayTile selectedTile = overlayTile.GetComponent<OverlayTile>();


            if (inRangeTiles.Contains(selectedTile) && !shipMoving && shipSelected && ship[shipIndex] != null)
            {
                path = pathFinder.FindPath(ship[shipIndex].currentTile, selectedTile, inRangeTiles);

                ClearTile();

                for(int i = 0; i < path.Count; i++)
                {
                    var previousTile = i > 0 ? path[i - 1] : ship[shipIndex].currentTile;
                    var futureTile = i < path.Count - 1 ? path[i + 1] : null;

                    var dir = directionTranslator.TranslateDirection(previousTile, path[i], futureTile);
                    path[i].SetDirSprite(dir);
                }
            }

            if (Input.GetMouseButtonDown(0))
            {

                selectedTile.ShowTile();


                if(spawnedShips < ship.Length)
                {
                    ClearTile();

                    ship[shipIndex] = Instantiate(shipPrefab).GetComponent<PirateShip>();
                    PositionShipOnMap(overlayTile.GetComponent<OverlayTile>());
                    GetInRangeTiles();

                    shipSelected = true;
                    ship[shipIndex].index = shipIndex;
                    if(shipIndex < ship.Length-1)
                        shipIndex++;

                    spawnedShips++;
                    
                    return;
                }

                if (!shipSelected && Input.GetMouseButtonDown(0))
                {
                    for(int i = 0; i < ship.Length; i++)
                    {
                        if (ship[i].currentTile == selectedTile)
                        {
                            shipIndex = i;
                            shipSelected = true;
                            GetInRangeTiles();
                            return;
                        }
                    }
                    
                }

                if (!inRangeTiles.Contains(selectedTile))
                {
                    foreach (var tile in inRangeTiles)
                        tile.HideTile();

                    shipSelected = false;
                    return;
                }

                if(shipSelected)
                    shipMoving = true;
            }
        }

        if(path.Count > 0 && shipMoving)
        {
            MoveAlongPath();
        }

    }

    void MoveAlongPath()
    {
        var step = speed * Time.deltaTime;

        var zIndex = path[0].transform.position.z;
        ship[shipIndex].transform.position = Vector2.MoveTowards(ship[shipIndex].transform.position, path[0].transform.position, step);
        Vector3 pos = ship[shipIndex].transform.position;

        ship[shipIndex].transform.position = new Vector3(pos.x, pos.y, zIndex);

        if(Vector2.Distance(ship[shipIndex].transform.position, path[0].transform.position) < .0001f)
        {
            PositionShipOnMap(path[0]);
            path.RemoveAt(0);
        }

        //Update Range Display -- Change this if u don't want it to be display once the ship has traveled
        if(path.Count <= 0)
        {
            GetInRangeTiles();
            shipMoving = false;
        }
    }
    void PositionShipOnMap(OverlayTile tile)
    {
        ship[shipIndex].transform.position = new Vector3(tile.transform.position.x, tile.transform.position.y + .0001f, tile.transform.position.z - 1);
        ship[shipIndex].GetComponent<SpriteRenderer>().sortingOrder = tile.GetComponent<SpriteRenderer>().sortingOrder;
        ship[shipIndex].currentTile = tile;
    }

    void ClearTile()
    {
        foreach (var tile in inRangeTiles)
        {
            tile.HideTile();
        }
    }

    void GetInRangeTiles()
    {
        foreach(var tile in inRangeTiles)
        {
            tile.HideTile();
        }

        inRangeTiles = rangeFinder.GetTilesInRange(ship[shipIndex].currentTile, ship[shipIndex].travelRange);

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
