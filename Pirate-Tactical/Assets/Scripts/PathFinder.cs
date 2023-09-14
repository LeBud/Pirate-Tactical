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

            List<OverlayTile> neighborTiles = MapManager.Instance.GetNeighborTiles(currentTile, searchableTiles);

            foreach(var t in neighborTiles)
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
                surroundingTiles.AddRange(MapManager.Instance.GetNeighborTiles(tile, new List<OverlayTile>()));
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

}
