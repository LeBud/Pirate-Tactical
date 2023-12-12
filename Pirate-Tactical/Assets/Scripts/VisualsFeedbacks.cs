using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VisualsFeedbacks : MonoBehaviour
{

    [Header("Cursor Visual")]
    public SpriteRenderer cursor;
    public Sprite classicCursor;
    public Sprite moveCursor;
    public Sprite attackCursor;
    public Sprite accostCursor;
    public Sprite specialShotCursor;
    public Sprite specialTileCursor;
    public Sprite upgradeCursor;

    List<TileScript> displayBlock = new List<TileScript>();

    public void DisplayBlockedTile(TileScript t, int dir)
    {
        Vector2 pos = t.pos.Value;

        if(displayBlock.Count > 0 )
            foreach (var d in displayBlock)
                d._blockedTileTemp.SetActive(false);

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
            d._blockedTileTemp.SetActive(true);
        }
    }

    public void StopDisplayBlocked()
    {
        if (displayBlock.Count > 0)
            foreach (var d in displayBlock)
                d._blockedTileTemp.SetActive(false);

        displayBlock.Clear();
    }

    public void CursorDisplay(int index, bool ship)
    {
        if (!ship)
        {
            cursor.sprite = classicCursor;
            return;
        }

        switch(index)
        {
            case 0:
                cursor.sprite = classicCursor; // a déterminer avec les conditions
                break;
            case 1:
                cursor.sprite = moveCursor;
                break;
            case 2:
                cursor.sprite = attackCursor;
                break;
            case 3:
                cursor.sprite = specialTileCursor;
                break;
            case 4:
                cursor.sprite = specialShotCursor;
                break;
            case 5:
                cursor.sprite = accostCursor;
                break;
            case 6:
                cursor.sprite = upgradeCursor;
                break;
        }
    }
}
