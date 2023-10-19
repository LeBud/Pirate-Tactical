using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ShipUnit : NetworkBehaviour
{
    public TileScript currentTile;

    public float shipSpeed = .0015f;

    public NetworkVariable<int> unitLife = new NetworkVariable<int>(10);
    public NetworkVariable<Vector2> unitPos = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Color player1Color;
    public Color player2Color;

    public SpriteRenderer unitSprite;

    public int unitRange = 4;
    public int damage = 4;
    public int index;

    public ulong clientIdOwner;

    //Point d'actions
    public int movePoint;
    public int attackPoint;

    private void Update()
    {
        //unitPos.OnValueChanged += (Vector2 previousPos, Vector2 newPos) => { StartCoroutine(MoveShip()); };
        if (transform.position != new Vector3(unitPos.Value.x, unitPos.Value.y, -1))
            StartCoroutine(MoveShip());
    }

    [ClientRpc]
    public void SetShipColorClientRpc(ulong id)
    {
        if(id == 0)
            unitSprite.color = player1Color;
        else 
            unitSprite.color = player2Color;
    }

    IEnumerator MoveShip()
    {
        transform.position = new Vector3(unitPos.Value.x, unitPos.Value.y, -1);
        yield return null;
    }

    public void TakeDamage(int dmg)
    {
        unitLife.Value -= dmg;
        if(unitLife.Value <= 0)
        {
            DestroyUnitOnServerRpc(NetworkManager.LocalClientId);
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void DestroyUnitOnServerRpc(ulong id)
    {
        if (id == 0)
        {
            GameManager.Instance.player1unitLeft--;
            GetComponent<NetworkObject>().Despawn();
        }
        else if (id == 1)
        {
            GameManager.Instance.player2unitLeft--;
            GetComponent<NetworkObject>().Despawn();
        }

    }

}

