using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualsFeedbacks : MonoBehaviour
{

    public List<TileScript> displayBlock;

    public void DisplayBlockedTile(TileScript t, int dir)
    {
        Vector2 pos = t.pos.Value;

        if(displayBlock.Count > 0 )
            foreach (var d in displayBlock)
                d.SetColor(3);

        switch (dir)
        {
            case 0: // X
                displayBlock.Clear();
                if(GridManager.Instance.GetTileAtPosition(new Vector2(pos.x - 1, pos.y)) != null)
                    displayBlock.Add(GridManager.Instance.GetTileAtPosition(new Vector2(pos.x - 1, pos.y)));
                if(GridManager.Instance.GetTileAtPosition(new Vector2(pos.x + 1, pos.y)))
                    displayBlock.Add(GridManager.Instance.GetTileAtPosition(new Vector2(pos.x + 1, pos.y)));
                displayBlock.Add(GridManager.Instance.GetTileAtPosition(pos));
                break;
            case 1: // Y
                displayBlock.Clear();
                if(GridManager.Instance.GetTileAtPosition(new Vector2(pos.x, pos.y - 1)))
                    displayBlock.Add(GridManager.Instance.GetTileAtPosition(new Vector2(pos.x, pos.y - 1)));
                if(GridManager.Instance.GetTileAtPosition(new Vector2(pos.x, pos.y + 1)))
                    displayBlock.Add(GridManager.Instance.GetTileAtPosition(new Vector2(pos.x, pos.y + 1)));
                displayBlock.Add(GridManager.Instance.GetTileAtPosition(pos));
                break;
        }

        foreach (var d in displayBlock)
        {
            Debug.Log("read for each");
            d.SetColor(0);
        }
    }

    public void StopDisplayBlocked()
    {
        if(displayBlock.Count > 0)
            foreach (var d in displayBlock)
                d.SetColor(3);

        displayBlock.Clear();
    }
}
