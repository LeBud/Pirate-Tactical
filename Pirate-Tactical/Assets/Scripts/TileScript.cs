using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class TileScript : NetworkBehaviour
{
    [SerializeField] SpriteRenderer _renderer;
    public GameObject _highlight;

    public NetworkVariable<Vector2> pos = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone);


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
