using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TileScript : NetworkBehaviour
{
    [Header("Tile Graph")]
    [SerializeField] SpriteRenderer _renderer;
    public GameObject _highlight;
    public GameObject _highlightRange;
    public GameObject _highlightBlocked;
    public GameObject _highlightOutOfCombatZone;
    public GameObject _highlightMine;
    public GameObject _highlightSpawn;
    public GameObject _blockedTileTemp;

    [Header("Normals Colors")]
    public Color normalColor;
    public Color offsetNormalColor;

    [Header("Pathfind Colors")]
    public Color pathColor;
    public Color openColor;
    public Color closeColor;

    public bool Walkable = true;
    public bool Mountain = true;
    public bool ShopTile = true;
    public bool canAccessShop = true;
    public bool canSpawnShip = false;

    public NetworkVariable<Vector2> pos = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> shipOnTile = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> offsetTile = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> blockedTile = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> tileOutOfCombatZone = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> mineInTile = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> upgradedMine = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<int> shopIndex = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> cannonInTile = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    public NetworkVariable<bool> shipwrek = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone);
    
    #region PathFinding

    public static event Action<TileScript> OnHoverTile;
    void OnOnHoverTile(TileScript _selected) => selected = _selected == this;
    public List<TileScript> Neighbors;
    public TileScript Connection { get; private set; }
    public float G { get; private set; }
    public float H { get; private set; }
    public float F => G + H;
    bool selected;

    public void SetG(float g) => G = g;

    public void SetH(float h) => H = h;

    public void SetConnection(TileScript tileBase) => Connection = tileBase;

    #endregion

    int roundToUnblock;
    public int roundTillUnblock = 2;

    #region enable/disable/start
    void OnEnable()
    {
        OnHoverTile += OnOnHoverTile;
    }


    void OnDisable()
    {
        OnHoverTile -= OnOnHoverTile;
    }

    private void Start()
    {
        #region SetNeighbors
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
        #endregion

        OnHoverTile += OnOnHoverTile;
    }
    #endregion

    [ClientRpc]
    public void InitTilesClientRpc()
    {
        SetColor(3);
    }

    void OnMouseEnter()
    {
        _highlight.SetActive(true);
    }

    void OnMouseExit()
    {
        _highlight.SetActive(false);
    }

    private void OnMouseDown()
    {
        if(!Walkable || shipOnTile.Value) return;
        OnHoverTile?.Invoke(this);  //OnHoverTile s'invoque ici, lorsque le joueur appuie sur la tile
    }

    public void HighLightRange(bool active)
    {

        _highlightRange.SetActive(active);
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
                _renderer.color = offsetTile.Value ? normalColor : offsetNormalColor;
                break;
                    
        }
    }

    [ClientRpc]
    public void SetTileToBlockTileClientRpc(bool blocked, int roundDuration)
    {
        if (blocked)
            roundToUnblock = GameManager.Instance.currentRound.Value + roundDuration;
        _highlightBlocked.SetActive(blocked);
    }

    [ServerRpc]
    public void UnblockTileServerRpc()
    {
        if(GameManager.Instance.currentRound.Value >= roundToUnblock)
        {
            blockedTile.Value = false;
            SetTileToBlockTileClientRpc(false,0);
        }
    }

    [ClientRpc]
    public void SetMineTileToClientRpc(ulong id, bool active)
    {
        if(NetworkManager.LocalClientId == id && active)
            _highlightMine.SetActive(true);
        else if(!active)
            _highlightMine.SetActive(false);
    }


    [ClientRpc]
    public void SetTileToOutOfZoneClientRpc()
    {
        _highlightOutOfCombatZone.SetActive(true);
    }

    #region HighlightTiles

    [ClientRpc]
    void HighlightClientRpc(bool active)
    {
        _highlight.SetActive(active);
    }

    [ServerRpc(RequireOwnership = false)]
    void HighlightServerRpc(bool active)
    {
        _highlight.SetActive(active);
    }

    #endregion

    public float GetTileDistance(Vector2 other)
    {
        var dist = new Vector2Int(Mathf.Abs((int)pos.Value.x - (int)other.x), Mathf.Abs((int)pos.Value.y - (int)other.y));

        var lowest = Mathf.Min(dist.x, dist.y);
        var highest = Mathf.Max(dist.x, dist.y);

        var horizontalMovesRequired = highest - lowest;

        return lowest * 14 + horizontalMovesRequired * 10;
    }
}