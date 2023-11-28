using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PathfindScript : MonoBehaviour
{
    public static List<TileScript> Pathfind(TileScript startNode, TileScript targetNode)
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

            foreach (var neighbor in current.Neighbors.Where(t => t.Walkable && !processed.Contains(t) && t != startNode && !t.shipOnTile.Value && !t.blockedTile.Value))
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
                if (!tile.Walkable || tile.blockedTile.Value) continue; 
                    if(tile != startTile && tile.shipOnTile.Value) continue;
                    
                surroundingTiles.AddRange(tile.Neighbors);
            }

            inRangeTile.AddRange(surroundingTiles);
            tileForPreviousStep = surroundingTiles.Distinct().ToList();
            stepCount++;
        }

        for(int i = inRangeTile.Count - 1; i >= 0; i--)
        {
            if (!inRangeTile[i].Walkable || inRangeTile[i].shipOnTile.Value || inRangeTile[i].blockedTile.Value)
                inRangeTile.RemoveAt(i);
        }


        return inRangeTile.Distinct().ToList();
    }

    public static List<TileScript> GetInRangeSpecialTiles(TileScript startTile, int range)
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
                if (tile.blockedTile.Value) continue;
                if (tile != startTile && tile.shipOnTile.Value) continue;

                surroundingTiles.AddRange(tile.Neighbors);
            }

            inRangeTile.AddRange(surroundingTiles);
            tileForPreviousStep = surroundingTiles.Distinct().ToList();
            stepCount++;
        }

        for (int i = inRangeTile.Count - 1; i >= 0; i--)
        {
            if (inRangeTile[i].shipOnTile.Value || inRangeTile[i].blockedTile.Value)
                inRangeTile.RemoveAt(i);
        }


        return inRangeTile.Distinct().ToList();
    }


    public static List<TileScript> GetInRangeTilesCross(TileScript startTile, int range)
    {
        TileScript[] neighbors = FindObjectsOfType<TileScript>();

        var inRangeTile = new List<TileScript>();
        var surroundingTiles = new List<TileScript>();
            
        for(int x = 1; x <= range; x++)
        {
            bool breakForLoop = false;
            Vector2 posToAdd = new Vector2(startTile.pos.Value.x + x, startTile.pos.Value.y);
            if (GridManager.Instance.dictionnary.Contains(posToAdd))
                foreach (var n in neighbors)
                    if (n.pos.Value == posToAdd && !n.Mountain)
                        if (n.shipOnTile.Value)
                        {
                            surroundingTiles.Add(n);
                            breakForLoop = true;
                        }
                        else
                        {
                            surroundingTiles.Add(n);
                        }
                    else if (n.pos.Value == posToAdd && n.Mountain)
                        breakForLoop = true;
            if (breakForLoop) break;
        }
        for (int x = 1; x <= range; x++)
        {
            bool breakForLoop = false;
            Vector2 posToAdd = new Vector2(startTile.pos.Value.x - x, startTile.pos.Value.y);
            if (GridManager.Instance.dictionnary.Contains(posToAdd))
                foreach (var n in neighbors)
                    if (n.pos.Value == posToAdd && !n.Mountain)
                        if (n.shipOnTile.Value)
                        {
                            surroundingTiles.Add(n);
                            breakForLoop = true;
                        }
                        else
                        {
                            surroundingTiles.Add(n);
                        }
                    else if (n.pos.Value == posToAdd && n.Mountain)
                        breakForLoop = true;
            if (breakForLoop) break;
        }
        for (int y = 1; y <= range; y++)
        {
            bool breakForLoop = false;
            Vector2 posToAdd = new Vector2(startTile.pos.Value.x, startTile.pos.Value.y + y);
            if (GridManager.Instance.dictionnary.Contains(posToAdd))
                foreach (var n in neighbors)
                    if (n.pos.Value == posToAdd && !n.Mountain)
                        if (n.shipOnTile.Value)
                        {
                            surroundingTiles.Add(n);
                            breakForLoop = true;
                        }
                        else
                        {
                            surroundingTiles.Add(n);
                        }
                    else if (n.pos.Value == posToAdd && n.Mountain)
                        breakForLoop = true;
            if (breakForLoop) break;
        }
        for (int y = 1; y <= range; y++)
        {
            bool breakForLoop = false;
            Vector2 posToAdd = new Vector2(startTile.pos.Value.x, startTile.pos.Value.y - y);
            if (GridManager.Instance.dictionnary.Contains(posToAdd))
                foreach (var n in neighbors)
                    if (n.pos.Value == posToAdd && !n.Mountain)
                        if (n.shipOnTile.Value)
                        {
                            surroundingTiles.Add(n);
                            breakForLoop = true;
                        }
                        else
                        {
                            surroundingTiles.Add(n);
                        }
                    else if (n.pos.Value == posToAdd && n.Mountain)
                        breakForLoop = true;
            if (breakForLoop) break;
        }
        surroundingTiles.Add(startTile);
        inRangeTile.AddRange(surroundingTiles);
        
        return inRangeTile.Distinct().ToList();
    }

    public static List<TileScript> GetInRangeInteractTiles(TileScript startTile, int range)
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
                if ((!tile.Walkable && !tile.ShopTile) || tile.blockedTile.Value) continue;

                surroundingTiles.AddRange(tile.Neighbors);
            }

            inRangeTile.AddRange(surroundingTiles);
            tileForPreviousStep = surroundingTiles.Distinct().ToList();
            stepCount++;
        }

        for (int i = inRangeTile.Count - 1; i >= 0; i--)
        {
            if ((!inRangeTile[i].Walkable && !inRangeTile[i].ShopTile) || inRangeTile[i].blockedTile.Value)
                inRangeTile.RemoveAt(i);
        }


        return inRangeTile.Distinct().ToList();
    }

    public static List<TileScript> GetCombatZoneSize(TileScript startTile, int range)
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
                surroundingTiles.AddRange(tile.Neighbors);
            }

            inRangeTile.AddRange(surroundingTiles);
            tileForPreviousStep = surroundingTiles.Distinct().ToList();
            stepCount++;
        }

        return inRangeTile.Distinct().ToList();
    }


}
