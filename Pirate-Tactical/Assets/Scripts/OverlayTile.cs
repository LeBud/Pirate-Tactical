using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using static DirectionTranslator;

public class OverlayTile : NetworkBehaviour
{
    public int g;
    public int h;
    public int f { get { return g + h; } }

    public bool isBLocked;

    public OverlayTile previous;

    public Vector3Int gridLocation;
    public Vector2Int grid2DPos { get { return new Vector2Int(gridLocation.x, gridLocation.y); } }

    public NetworkVariable<int> posX = new NetworkVariable<int>(0);
    public NetworkVariable<int> posY = new NetworkVariable<int>(0);
    public NetworkVariable<int> posZ = new NetworkVariable<int>(0);

    public List<Sprite> dirSprites;


    public void ShowTile()
    {
        gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 1);
    }

    public void HideTile()
    {
        gameObject.GetComponent<SpriteRenderer>().color = new Color(1, 1, 1, 0);
        SetDirSprite(Directions.None);
    }

    public void SetDirSprite(Directions d)
    {
        var sprite = GetComponentsInChildren<SpriteRenderer>()[1];
        if (d == Directions.None)
            sprite.color = new Color(1, 1, 1, 0);
        else
        {
            sprite.color = new Color(1, 1, 1, 1);
            sprite.sprite = dirSprites[(int)d];
            sprite.sortingOrder = GetComponent<SpriteRenderer>().sortingOrder;
        }
    }
}
