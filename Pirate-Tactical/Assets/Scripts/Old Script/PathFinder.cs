using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinder
{

    public List<OverlayTile> FindPath(OverlayTile start, OverlayTile end, List<OverlayTile> searchableTiles)
    {
        List<OverlayTile> openList = new List<OverlayTile>();
        List<OverlayTile> closeList = new List<OverlayTile>();

        openList.Add(start);

        while (openList.Count > 0)
        {
            OverlayTile currentTile = openList.OrderBy(x => x.f).First();

            openList.Remove(currentTile);
            closeList.Add(currentTile);

            if(currentTile == end)
            {
                return GetFinishedList(start, end);
            }
            
            List<OverlayTile> neighborTiles = MapManager.Instance.getNeighborTiles.GetNeighborsTiles(currentTile, searchableTiles);

            foreach (var t in neighborTiles)
            {
                if(t.isBLocked || closeList.Contains(t))
                {
                    continue;
                }

                t.g = CalculateDistance(start, t);
                t.h = CalculateDistance(end, t);

                t.previous = currentTile;

                if(!openList.Contains(t))
                {
                    openList.Add(t);
                }
            }
        }

        return new List<OverlayTile>();
    }

    int CalculateDistance(OverlayTile start, OverlayTile neighbor)
    {
        return Mathf.Abs(start.gridLocation.x - neighbor.gridLocation.x) + Mathf.Abs(start.gridLocation.y - neighbor.gridLocation.y);
    }

    List<OverlayTile> GetFinishedList(OverlayTile start, OverlayTile end)
    {
        List<OverlayTile> finishedList = new List<OverlayTile>();

        OverlayTile currentTile = end;

        while(currentTile != start)
        {
            finishedList.Add(currentTile);
            currentTile = currentTile.previous;
        }

        finishedList.Reverse();
        return finishedList;
    }


}

public class RangeFinder
{
    public List<OverlayTile> GetTilesInRange(OverlayTile startTile, int range)
    {
        var inRangeTile = new List<OverlayTile>();
        int stepCount = 0;

        inRangeTile.Add(startTile);

        var tileForPreviousStep = new List<OverlayTile>();
        tileForPreviousStep.Add(startTile);

        while(stepCount < range)
        {
            var surroundingTiles = new List<OverlayTile>();

            foreach(var tile in tileForPreviousStep)
            {
                surroundingTiles.AddRange(MapManager.Instance.getNeighborTiles.GetNeighborsTiles(tile, new List<OverlayTile>()));
            }

            inRangeTile.AddRange(surroundingTiles);
            tileForPreviousStep = surroundingTiles.Distinct().ToList();
            stepCount++;
        }

        return inRangeTile.Distinct().ToList();
    }
}

public class DirectionTranslator
{
    public enum Directions
    {
        None = 0,
        Up = 1,
        Down = 2,
        Right = 3,
        Left = 4,
        TopRight = 5,
        BottomRight = 6,
        TopLeft = 7,
        BottomLeft = 8,
        Finished = 9
    }

    public Directions TranslateDirection(OverlayTile previousTile, OverlayTile currentTile, OverlayTile futureTile)
    {
        bool isFinal = futureTile == null;

        Vector2Int pastDir = previousTile != null ? currentTile.grid2DPos - previousTile.grid2DPos : new Vector2Int(0, 0);
        Vector2Int futurDir = futureTile != null ? futureTile.grid2DPos - currentTile.grid2DPos : new Vector2Int(0, 0);
        Vector2Int dir = pastDir != futurDir ? pastDir + futurDir : futurDir;

        if(dir == new Vector2Int(0, 1) && !isFinal)
            return Directions.Up;
        if (dir == new Vector2Int(0, -1) && !isFinal)
            return Directions.Down;
        if(dir == new Vector2Int(1, 0) && !isFinal)
            return Directions.Right;
        if (dir == new Vector2Int(-1, 0) && !isFinal)
            return Directions.Left;
        if (dir == new Vector2Int(1, 1) && !isFinal)
        {
            if(pastDir.y < futurDir.y)
                return Directions.BottomLeft;
            else
                return Directions.TopRight;
        }
        if (dir == new Vector2Int(-1, 1) && !isFinal)
        {
            if (pastDir.y < futurDir.y)
                return Directions.BottomRight;
            else
                return Directions.TopLeft;
        }
        if (dir == new Vector2Int(1, -1) && !isFinal)
        {
            if (pastDir.y > futurDir.y)
                return Directions.TopLeft;
            else
                return Directions.BottomRight;
        }
        if (dir == new Vector2Int(-1, -1) && !isFinal)
        {
            if (pastDir.y > futurDir.y)
                return Directions.TopRight;
            else
                return Directions.BottomLeft;
        }
        if (dir != Vector2.zero && isFinal)
            return Directions.Finished;

        return Directions.None;
        
    }
}
