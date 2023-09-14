using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathFinder
{

    public List<OverlayTile> FindPath(OverlayTile start, OverlayTile end)
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

            List<OverlayTile> neighborTiles = GetNeighborTiles(currentTile);

            foreach(var t in neighborTiles)
            {
                //Last condition is to know if the player can jump the block (differents heights)
                if(t.isBLocked ||closeList.Contains(t) || Mathf.Abs(currentTile.gridLocation.z - t.gridLocation.z) > 1)
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

    List<OverlayTile> GetNeighborTiles(OverlayTile currentTile)
    {
        var map = MapManager.Instance.map;

        List<OverlayTile> neighbors = new List<OverlayTile>();

        Vector2Int locationToCheck = new Vector2Int(currentTile.gridLocation.x, currentTile.gridLocation.y + 1);

        for(int i = 0; i < 4; i++)
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

            if (map.ContainsKey(locationToCheck))
            {
                neighbors.Add(map[locationToCheck]);
            }
        }

        return neighbors;
        
    }

}
