using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TileScript : NetworkBehaviour
{
    [SerializeField] SpriteRenderer _renderer;
    public GameObject _highlight;
    bool selected;

    public Color pathColor;
    public Color openColor;
    public Color closeColor;
    public Color normalColor;

    public NetworkVariable<Vector2> pos = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone);

    public bool Walkable = true;

    public static event Action<TileScript> OnHoverTile;
    void OnEnable() => OnHoverTile += OnOnHoverTile;
    void OnDisable() => OnHoverTile -= OnOnHoverTile;
    void OnOnHoverTile(TileScript _selected) => selected = _selected == this;

    #region PathFinding

    public ICoords Coords;
    public float GetDistance(TileScript other) => Coords.GetDistance(other.Coords);
    public List<TileScript> Neighbors;
    public TileScript Connection { get; private set; }
    public float G { get; private set; }
    public float H { get; private set; }
    public float F => G + H;

    public void SetG(float g)
    {
        G = g;
    }

    public void SetH(float h)
    {
        H = h;
    }

    public void SetConnection(TileScript tileBase)
    {
        Connection = tileBase;
    }

    #endregion

    private void Start()
    {
        TileScript[] neighbors = FindObjectsOfType<TileScript>();

        Vector2 neighborPos = new Vector2(pos.Value.x + 1, pos.Value.y);
        if (GridManager.Instance.dictionnary.Contains(neighborPos))
        {
            foreach(var n in neighbors)
            {
                if(n.pos.Value == neighborPos) Neighbors.Add(n);
            }
        }
        neighborPos = new Vector2(pos.Value.x - 1, pos.Value.y);
        if (GridManager.Instance.dictionnary.Contains(neighborPos))
        {
            foreach (var n in neighbors)
            {
                if (n.pos.Value == neighborPos) Neighbors.Add(n);
            }
        }
        neighborPos = new Vector2(pos.Value.x, pos.Value.y + 1);
        if (GridManager.Instance.dictionnary.Contains(neighborPos))
        {
            foreach (var n in neighbors)
            {
                if (n.pos.Value == neighborPos) Neighbors.Add(n);
            }
        }
        neighborPos = new Vector2(pos.Value.x, pos.Value.y - 1);
        if (GridManager.Instance.dictionnary.Contains(neighborPos))
        {
            foreach (var n in neighbors)
            {
                if (n.pos.Value == neighborPos) Neighbors.Add(n);
            }
        }

        OnHoverTile += OnOnHoverTile;
    }

    void OnMouseEnter()
    {
        HighlightClientRpc();
        HighlightServerRpc();
        _highlight.SetActive(true);
    }

    void OnMouseExit()
    {
        DeHighlightClientRpc();
        DeHighlightServerRpc();
        _highlight.SetActive(false);
    }

    private void OnMouseDown()
    {
        if(!Walkable) return;
        OnHoverTile?.Invoke(this);
    }

    public void SetColor(int colorI)
    {
        switch (colorI)
        {
            case 0:
                _renderer.color = pathColor;
                break; 
            case 1:
                _renderer.color = openColor;
                break; 
            case 2:
                _renderer.color = closeColor;
                break; 
            case 3:
                _renderer.color = normalColor;
                break;
                    
        }
    }

    #region HighlightTiles

    [ClientRpc]
    void HighlightClientRpc()
    {
        _highlight.SetActive(true);
    }

    [ClientRpc]
    void DeHighlightClientRpc()
    {
        _highlight.SetActive(false);
    }

    [ServerRpc(RequireOwnership = false)]
    void HighlightServerRpc()
    {
        _highlight.SetActive(true);
    }

    [ServerRpc(RequireOwnership = false)]
    void DeHighlightServerRpc()
    {
        _highlight.SetActive(false);
    }

    #endregion
}

public struct SquareCoord : ICoords
{
    public float GetDistance(ICoords other)
    {
        var dist = new Vector2Int(Mathf.Abs((int)Pos.x - (int)other.Pos.x), Mathf.Abs((int)Pos.y - (int)other.Pos.y));

        var lowest = Mathf.Min(dist.x, dist.y);
        var highest = Mathf.Max(dist.x, dist.y);

        var horizontalMovesRequired = highest - lowest;

        return lowest * 14 + horizontalMovesRequired * 10;
    }
    public Vector2 Pos { get; set; }
}

public interface ICoords
{
    public float GetDistance(ICoords other);
    public Vector2 Pos { get; set; }
}