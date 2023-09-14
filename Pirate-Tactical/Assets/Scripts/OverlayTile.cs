using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class OverlayTile : MonoBehaviour
{
    public int g;
    public int h;
    public int f { get { return g + h; } }

    public bool isBLocked;

    public OverlayTile previous;

    public Vector3Int gridLocation;
    public Vector2Int grid2DPos { get { return new Vector2Int(gridLocation.x, gridLocation.y); } }

    public List<Sprite> direction;

    public void ShowTile()
    {
        gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
    }

    public void HideTile()
    {
        gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
    }
}
