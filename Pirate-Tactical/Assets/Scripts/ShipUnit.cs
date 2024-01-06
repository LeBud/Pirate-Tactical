using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ShipUnit : NetworkBehaviour
{

    public enum UnitSpecialShot { None, PushUnit, TShot, FireShot, TirBrochette, VentContraire, Grappin, ReloadUnit }
    public enum UnitSpecialTile { None, Mine, BlockTile, Teleport, FouilleOr, CanonSurIle, Barque, ExplodeBarque}
    public enum UnitType { Galion, Brigantin, Sloop}

    [Header("NetworkVariables")]
    public NetworkVariable<int> unitLife = new NetworkVariable<int>(10);
    public NetworkVariable<Vector2> unitPos = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<bool> canMove = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> canShoot = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> canBeSelected = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> canMoveAgain = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> canShootAgain = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<bool> canOnlyMove = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

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
    public NetworkVariable<UnitSpecialShot> unitSpecialShot = new NetworkVariable<UnitSpecialShot>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public NetworkVariable<UnitSpecialTile> unitSpecialTile = new NetworkVariable<UnitSpecialTile>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    public CapacitiesSO shotCapacity;
    public CapacitiesSO tileCapacity;
    public bool upgradedCapacity = false;

    [Header("Barque Parameters")]
    public bool barqueSpawn = false;
    public int barqueIndex;

    [Header("Parameter for barque")]
    public bool isBark;
    public NetworkVariable<int> shipIndexFrom = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    [Header("Upgrade")]
    public bool canBeUpgrade = true;
    //public bool capacitiesUpgraded = false;
    public UpgradeSystem.UpgradeType upgrade;

    [Header("Colors")]
    public Color player1Color;
    public Color player2Color;
    public bool SpriteMode;
    public Sprite Player1Sprite;
    public Sprite Player2Sprite;

    [Header("Others")]
    public SpriteRenderer unitSprite;
    public TMP_Text health;
    public GameObject highlight;
    public GameObject usedSprite;

    [HideInInspector]
    public int index;
    [HideInInspector]
    public ulong clientIdOwner;

    //Point d'actions
    public int movePoint = 1;
    public int attackPoint = 1;

    int roundToStopFireEffect;
    int roundToStopWindEffect;
    int roundToApplyEffect = -1;
    int baseMoveRange;

    int passiveDmg;

    [SerializeField] Transform healthDisplay;

    float healthPercent;
    bool isMoving = false;

    public float moveSpeed = 3;

    private void Start()
    {
        if (IsOwner)
        {
            if(shotCapacity == null)
                unitSpecialShot.Value = UnitSpecialShot.None;
            else
                unitSpecialShot.Value = shotCapacity.shootCapacity;

            if(tileCapacity == null)
                unitSpecialTile.Value = UnitSpecialTile.None;
            else
                unitSpecialTile.Value = tileCapacity.tileCapacity;

            baseMoveRange = unitMoveRange;
        }

        if (!IsServer) return;

        healthPercent = (float)unitLife.Value / maxHealth;
        healthDisplay.localScale = new Vector3(healthPercent, 1, 1);
        SetHealthBarClientRpc(healthPercent, unitLife.Value);
    }

    private void Update()
    {
        if (!IsOwner) return;

        if (transform.position != new Vector3(unitPos.Value.x, unitPos.Value.y, -1) && !isMoving)
            StartCoroutine(MoveShip());

        if (canOnlyMove.Value)
            canShoot.Value = false;

        if (GameManager.Instance.gametesting.Value)
        {
            canMove.Value = true;
            canShoot.Value = true;
            canBeSelected.Value = true;
        }

        if (canBeSelected.Value)
        {
            if (canMoveAgain.Value || canShootAgain.Value)
                return;

            if(!canMove.Value && !canShoot.Value)
                canBeSelected.Value = false;

            if(usedSprite.activeSelf)
                usedSprite.SetActive(false);
        }
        else if(!canBeSelected.Value)
            usedSprite.SetActive(true);

        if (roundToApplyEffect == GameManager.Instance.currentRound.Value)
            canOnlyMove.Value = true;
        else
            canOnlyMove.Value = false;
    }

    [ClientRpc]
    public void UpdateUnitClientRpc()
    {
        //if (!IsOwner) return;

        if(roundToStopFireEffect > GameManager.Instance.currentRound.Value)
        {
            TakeDamageServerRpc(passiveDmg, unitPos.Value, false, 0, false);
            GridManager.Instance.DisplayDamageClientRpc("Brulure", new Vector2(unitPos.Value.x, unitPos.Value.y + 0.5f));
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
        if (!SpriteMode)
        {
            if(id == 0)
                unitSprite.color = player1Color;
            else 
                unitSprite.color = player2Color;
        }
        else
        {
            if (id == 0)
                unitSprite.sprite = Player1Sprite;
            else
                unitSprite.sprite = Player2Sprite;
        }
    }

    [ClientRpc]
    public void UpdateCurrentTileClientRpc(Vector2 pos)
    {
        currentTile = GridManager.Instance.GetTileAtPosition(pos);
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
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(int dmg, Vector2 pos, bool passiveAttack, int effectDuration, bool hasGoneThroughWater)
    {
        //int randomDmg = Random.Range(dmg - 1, dmg + 2);
        int randomDmg = dmg;
        //Crit chance
        int CritChance = Random.Range(0, 101);
        if (CritChance <= 10)
            randomDmg++;

        if (hasGoneThroughWater) randomDmg /= 2;

        GridManager.Instance.DisplayDamageClientRpc(randomDmg.ToString(), pos);

        unitLife.Value -= randomDmg;

        float percent = (float)unitLife.Value / maxHealth;
        SetHealthBarClientRpc(percent, unitLife.Value);

        Cursor[] p = FindObjectsOfType<Cursor>();
        foreach (Cursor c in p)
            c.CalculateHealthClientRpc();

        HUD.Instance.UpdateHealthBarClientRpc();
        
        //Only set to true when an enemy unit attack this one with his special and has a passive effect
        if (passiveAttack && !hasGoneThroughWater)
            GivePassiveFireToUnitClientRpc(effectDuration, dmg);

        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.takeDamage.id);

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

            if(upgrade != UpgradeSystem.UpgradeType.None)
            {
                GridManager.Instance.SpawnShipwrekServerRpc(upgrade, unitPos.Value);
            }

            SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.shipDestroyed.id);
            GridManager.Instance.SetShipOnTileServerRpc(pos, false);
            GetComponent<NetworkObject>().Despawn();
        }

    }

    [ClientRpc]
    public void GivePassiveFireToUnitClientRpc(int roundDuration, int _passiveDmg)
    {
        roundToStopFireEffect = roundDuration + GameManager.Instance.currentRound.Value;
        passiveDmg = _passiveDmg;
        SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.fireDamage.clip);
    }

    [ClientRpc]
    public void GiveWindEffectClientRpc(int roundDuration, bool upgraded)
    {
        roundToStopWindEffect = roundDuration + GameManager.Instance.currentRound.Value;
        if (!upgraded)
            unitMoveRange -= 2;
        else if (upgraded)
            unitMoveRange = 0;

        if(unitMoveRange < 0) unitMoveRange = 0;
    }

    [ClientRpc]
    public void GiveReloadEffectClientRpc(int round, bool upgraded)
    {
        if(!IsOwner) return;

        if(!upgraded)
        {
            if (!canMove.Value)
            {
                if(!canShoot.Value)
                    canShoot.Value = true;

                canMove.Value = true;
                return;
            }
            else if (!canShoot.Value)
            {
                if (!canMove.Value)
                    canMove.Value = true;

                canShoot.Value = true;
                return;
            }

            canMoveAgain.Value = true;
            canShootAgain.Value = true;
        }
        else if (upgraded)
        {
            roundToApplyEffect = round;
        }
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

