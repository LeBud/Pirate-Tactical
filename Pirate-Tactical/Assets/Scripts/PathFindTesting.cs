using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public static class PathFindTesting
{
    public static List<TileScript> PathTest(TileScript startNode, TileScript targetNode)
    {
        var toSearch = new List<TileScript>() { startNode };
        var processed = new List<TileScript>();

        while (toSearch.Any())
        {
            var current = toSearch[0];
            foreach (var t in toSearch)
                if (t.F < current.F || t.F == current.F && t.H < current.H) current = t;

            processed.Add(current);
            toSearch.Remove(current);

            current.SetColor(2);

            if (current == targetNode)
            {
                var currentPathTile = targetNode;
                var path = new List<TileScript>();
                var count = 100;
                while (currentPathTile != startNode)
                {
                    path.Add(currentPathTile);
                    currentPathTile = currentPathTile.Connection;
                    count--;
                    if (count < 0) throw new Exception();
                }

                foreach (var tile in path) tile.SetColor(0);
                startNode.SetColor(0);
                return path;
            }

            foreach (var neighbor in current.Neighbors.Where(t => t.Walkable && !processed.Contains(t) && (t != startNode && !t.shipOnTile.Value)))
            {
                var inSearch = toSearch.Contains(neighbor);

                var costToNeighbor = current.G + current.GetTileDistance(neighbor.pos.Value);

                if (!inSearch || costToNeighbor < neighbor.G)
                {
                    neighbor.SetG(costToNeighbor);
                    neighbor.SetConnection(current);

                    if (!inSearch)
                    {
                        neighbor.SetH(neighbor.GetTileDistance(targetNode.pos.Value));
                        toSearch.Add(neighbor);
                        neighbor.SetColor(1);
                    }
                }
            }
        }
        return null;
    }

    public static List<TileScript> GetInRangeTiles(TileScript startTile, int range)
    {
        var inRangeTile = new List<TileScript>();
        int stepCount = 0;

        inRangeTile.Add(startTile);

        var tileForPreviousStep = new List<TileScript>();
        tileForPreviousStep.Add(startTile);

        while (stepCount < range)
        {
            var surroundingTiles = new List<TileScript>();
            foreach (var tile in tileForPreviousStep)
            {
                if (!tile.Walkable) continue; 
                    if(tile != startTile && tile.shipOnTile.Value) continue;
                surroundingTiles.AddRange(tile.Neighbors);
            }

            inRangeTile.AddRange(surroundingTiles);
            tileForPreviousStep = surroundingTiles.Distinct().ToList();
            stepCount++;
        }

        return inRangeTile.Distinct().ToList();
    }

}
