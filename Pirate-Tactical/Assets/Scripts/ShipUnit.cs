using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ShipUnit : NetworkBehaviour
{
    public TileScript currentTile;

    //public float shipSpeed = .0015f;
    public float maxHealth;
    public NetworkVariable<int> unitLife = new NetworkVariable<int>(10);
    public NetworkVariable<Vector2> unitPos = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public Color player1Color;
    public Color player2Color;

    public SpriteRenderer unitSprite;

    public int unitMoveRange = 4;
    public int unitShootRange = 4;
    public int damage = 4;
    public int index;

    public ulong clientIdOwner;

    //Point d'actions
    public int movePoint = 1;
    public int attackPoint = 1;

    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> canShoot = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> canBeSelected = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [SerializeField] Transform healthDisplay;

    float healthPercent;

    private void Start()
    {
        healthPercent = (float)unitLife.Value / maxHealth;
        healthDisplay.localScale = new Vector3(healthPercent, 1, 1);
    }

    private void Update()
    {
        if (transform.position != new Vector3(unitPos.Value.x, unitPos.Value.y, -1))
            StartCoroutine(MoveShip());

        if (GameManager.Instance.gametesting.Value)
        {
            canMove.Value = true;
            canShoot.Value = true;
            canBeSelected.Value = true;
        }

        if (canBeSelected.Value)
        {
            if(!canMove.Value && !canShoot.Value)
            {
                canBeSelected.Value = false;
            }
        }
    }

    [ClientRpc]
    public void UpdateUnitClientRpc()
    {
        if (!IsOwner) return;
        if(GameManager.Instance.currentRound.Value >= GameManager.Instance.startRoundCombatZone)
            if (currentTile.tileOutOfCombatZone.Value)
                TakeDamageServerRpc(GridManager.Instance.combatZoneDamage, unitPos.Value);
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

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int dmg, Vector2 pos)
    {
        unitLife.Value -= dmg;

        float percent = (float)unitLife.Value / maxHealth;
        SetHealthBarClientRpc(percent);

        if (unitLife.Value <= 0)
        {
            GridManager.Instance.SetShipOnTileServerRpc(pos, false);
            GetComponent<NetworkObject>().Despawn();
        }

    }

    [ClientRpc]
    public void SetHealthBarClientRpc(float percent)
    {
        healthDisplay.localScale = new Vector3(percent, 1, 1);
    }
}

