using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShipUnit : NetworkBehaviour
{

    public enum UnitSpecialShot { None, PushUnit, TShot, FireShot, TirBrochette, VentContraire, Grappin}
    public enum UnitSpecialTile { None, Mine, BlockTile, Teleport, FouilleOr, CanonSurIle, Barque}
    public enum UnitType { Galion, Brigantin, Sloop}

    [Header("NetworkVariables")]
    public NetworkVariable<int> unitLife = new NetworkVariable<int>(10);
    public NetworkVariable<Vector2> unitPos = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> canShoot = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> canBeSelected = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [HideInInspector]
    public TileScript currentTile;

    public UnitType unitName;

    [Header("Unit Base stats")]
    public int maxHealth;
    public int unitDamage;
    public int unitAccostDamage;
    public int unitMoveRange = 4;
    public int unitShootRange = 4;
    public NetworkVariable<int> damage = new NetworkVariable<int>(4, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<int> accostDmgBoost = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Unit Special stats")]
    public UnitSpecialShot unitSpecialShot;
    public UnitSpecialTile unitSpecialTile;
    public CapacitiesSO shotCapacity;
    public CapacitiesSO tileCapacity;

    [Header("Barque Parameters")]
    public bool barqueSpawn = false;
    public int barqueIndex;

    [Header("Parameter for barque")]
    public bool isBark;
    public NetworkVariable<int> shipIndexFrom = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Upgrade")]
    public bool canBeUpgrade = true;

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

    int roundToStopFireEffect;
    int roundToStopWindEffect;
    int baseMoveRange;

    int passiveDmg;

    [SerializeField] Transform healthDisplay;

    float healthPercent;
    bool isMoving = false;

    public float moveSpeed = 3;

    private void Start()
    {
        healthPercent = (float)unitLife.Value / maxHealth;
        healthDisplay.localScale = new Vector3(healthPercent, 1, 1);
        SetHealthBarClientRpc(healthPercent, unitLife.Value);
        baseMoveRange = unitMoveRange;

        if (isBark) return;
        unitSpecialShot = shotCapacity.shootCapacity;
        unitSpecialTile = tileCapacity.tileCapacity;
    }

    private void Update()
    {
        if (transform.position != new Vector3(unitPos.Value.x, unitPos.Value.y, -1) && !isMoving)
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

        if(roundToStopFireEffect > GameManager.Instance.currentRound.Value)
        {
            TakeDamageServerRpc(passiveDmg, unitPos.Value, false, 0, false);
        }

        if (roundToStopWindEffect < GameManager.Instance.currentRound.Value)
        {
            unitMoveRange = baseMoveRange;
        }
    }

    [ClientRpc]
    public void ZoneDamageClientRpc()
    {
        if (!IsOwner) return;
        if (currentTile.tileOutOfCombatZone.Value)
            TakeDamageServerRpc(GridManager.Instance.combatZoneDamage, unitPos.Value, false, 0, false);
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
        isMoving = true;

        float t = 0;
        Vector3 startPos = transform.position;
        Vector3 endPos = new Vector3(unitPos.Value.x, unitPos.Value.y, -1);
        while (t < moveSpeed)
        {
            transform.position = Vector3.Lerp(startPos, endPos, t / moveSpeed);
            t += Time.deltaTime;
            yield return null;
        }

        transform.position = endPos;

        isMoving = false;
        /*transform.position = new Vector3(unitPos.Value.x, unitPos.Value.y, -1);
        yield return null;*/
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int dmg, Vector2 pos, bool passiveAttack, int effectDuration, bool hasGoneThroughWater)
    {
        int randomDmg = Random.Range(dmg - 1, dmg + 2);
        //Crit chance
        int CritChance = Random.Range(0, 101);
        if (CritChance <= 10)
            randomDmg++;

        if (hasGoneThroughWater) randomDmg /= 2;

        unitLife.Value -= randomDmg;

        float percent = (float)unitLife.Value / maxHealth;
        SetHealthBarClientRpc(percent, unitLife.Value);

        Cursor[] p = FindObjectsOfType<Cursor>();
        foreach (Cursor c in p)
            c.CalculateHealthClientRpc();

        //Only set to true when an enemy unit attack this one with his special and has a passive effect
        if (passiveAttack && !hasGoneThroughWater)
            GivePassiveFireToUnitClientRpc(effectDuration, dmg);

        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.takeDamage);

        if (unitLife.Value <= 0)
        {
            if (isBark)
                NetworkManager.ConnectedClients[GetComponent<NetworkObject>().OwnerClientId].PlayerObject.GetComponent<Cursor>().ResetShipBarque(shipIndexFrom.Value);

            if (!isBark)
            {
                if (GetComponent<NetworkObject>().OwnerClientId == 0)
                    GameManager.Instance.player1unitLeft--;
                else if(GetComponent<NetworkObject>().OwnerClientId == 1)
                    GameManager.Instance.player2unitLeft--;
            }

            SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.shipDestroyed);
            GridManager.Instance.SetShipOnTileServerRpc(pos, false);
            GetComponent<NetworkObject>().Despawn();
        }

    }

    [ClientRpc]
    public void GivePassiveFireToUnitClientRpc(int roundDuration, int _passiveDmg)
    {
        roundToStopFireEffect = roundDuration + GameManager.Instance.currentRound.Value;
        passiveDmg = _passiveDmg;
        SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.fireDamage);
    }

    [ClientRpc]
    public void GiveWindEffectClientRpc(int roundDuration)
    {
        roundToStopWindEffect = roundDuration + GameManager.Instance.currentRound.Value;
        unitMoveRange -= 2;
        if(unitMoveRange < 0) unitMoveRange = 0;
    }

    [ClientRpc]
    public void SetHealthBarClientRpc(float percent, int life)
    {
        health.text = life + " / " + maxHealth;
        healthDisplay.localScale = new Vector3(percent, 1, 1);
    }

    [ClientRpc]
    public void SetNewTileClientRpc(Vector2 newTilePos)
    {
        currentTile = GridManager.Instance.GetTileAtPosition(newTilePos);
    }
}

