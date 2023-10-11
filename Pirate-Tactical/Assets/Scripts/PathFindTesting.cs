using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PathFindTesting
{

    public static List<TileScript> PathTest(TileScript startTile, TileScript targetTile)
    {
        var toSearch = new List<TileScript>() { startTile} ;
        var processed = new List<TileScript>();

        while (toSearch.Any())
        {
            var current = toSearch[0];
            foreach(var t in toSearch)
                if (t.F < current.F || t.F == current.F && t.H < current.H) current = t;

            processed.Add(current);
            toSearch.Remove(current);

            current.SetColor(2);

            if(current == targetTile)
            {
                var currentPathTile = targetTile;
                var path = new List<TileScript>();
                var count = 100;

                while(currentPathTile != startTile)
                {
                    path.Add(currentPathTile);
                    currentPathTile = null;
                    count--;
                    if (count < 0) return null;
                }

                foreach(var t in path)
                {
                    t.SetColor(0);
                }
                startTile.SetColor(0);
                Debug.Log("Path Find : There is " + path.Count + " tiles in the path");
                return path;
            }

            foreach(var neighbor in current.Neighbors.Where(t => t.Walkable && !processed.Contains(t)))
            {
                var inSearch = toSearch.Contains(neighbor);

                var costToNeighbor = current.G + current.GetDistance(neighbor);

                if(!inSearch ||costToNeighbor < neighbor.G)
                {
                    neighbor.SetG(costToNeighbor);
                    neighbor.SetConnection(current);

                    if (!inSearch)
                    {
                        neighbor.SetH(neighbor.GetDistance(targetTile));
                        toSearch.Add(neighbor);
                        neighbor.SetColor(1);
                    }
                }
            }
        }

        return null;
    }

}
