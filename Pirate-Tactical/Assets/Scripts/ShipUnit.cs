using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class ShipUnit : NetworkBehaviour
{

    public enum UnitSpecialShot { None, PushUnit, TShot, FireShot}
    public enum UnitSpecialTile { None, Mine, BlockTile}

    [Header("NetworkVariables")]
    public NetworkVariable<int> unitLife = new NetworkVariable<int>(10);
    public NetworkVariable<Vector2> unitPos = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> canShoot = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> canBeSelected = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [HideInInspector]
    public TileScript currentTile;

    public string unitName;

    [Header("Unit Base stats")]
    public int maxHealth;
    public int unitMoveRange = 4;
    public int unitShootRange = 4;
    public int damage = 4;

    [Header("Unit Special stats")]
    public UnitSpecialShot unitSpecialShot;
    public UnitSpecialTile unitSpecialTile;
    public int specialAbilityCost;
    public int specialAbilityPassiveDuration;

    [Header("Colors")]
    public Color player1Color;
    public Color player2Color;

    [Header("Others")]
    public SpriteRenderer unitSprite;
    public TMP_Text health;

    [HideInInspector]
    public int index;
    [HideInInspector]
    public ulong clientIdOwner;

    //Point d'actions
    public int movePoint = 1;
    public int attackPoint = 1;

    int roundToStopEffect;

    [SerializeField] Transform healthDisplay;

    float healthPercent;

    private void Start()
    {
        healthPercent = (float)unitLife.Value / maxHealth;
        healthDisplay.localScale = new Vector3(healthPercent, 1, 1);
        SetHealthBarClientRpc(healthPercent);
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
                TakeDamageServerRpc(GridManager.Instance.combatZoneDamage, unitPos.Value, false, 0);

        if(roundToStopEffect > GameManager.Instance.currentRound.Value)
        {
            TakeDamageServerRpc(6, unitPos.Value, false, 0);
        }
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
    public void TakeDamageServerRpc(int dmg, Vector2 pos, bool passiveAttack, int effectDuration)
    {
        int randomDmg = Random.Range(dmg - 1, dmg + 2);

        unitLife.Value -= randomDmg;

        float percent = (float)unitLife.Value / maxHealth;
        SetHealthBarClientRpc(percent);

        Cursor[] p = FindObjectsOfType<Cursor>();
        foreach (Cursor c in p)
            c.CalculateHealthClientRpc();

        //Only set to true when an enemy unit attack this one with his special and has a passive effect
        if (passiveAttack)
            GivePassiveEffectToUnitClientRpc(effectDuration);

        if (unitLife.Value <= 0)
        {
            GridManager.Instance.SetShipOnTileServerRpc(pos, false);
            GetComponent<NetworkObject>().Despawn();
        }

    }

    [ClientRpc]
    public void GivePassiveEffectToUnitClientRpc(int roundDuration)
    {
        roundToStopEffect = roundDuration + GameManager.Instance.currentRound.Value;
    }

    [ClientRpc]
    public void SetHealthBarClientRpc(float percent)
    {
        health.text = unitLife.Value.ToString() + " / " + maxHealth;
        healthDisplay.localScale = new Vector3(percent, 1, 1);
    }
}

