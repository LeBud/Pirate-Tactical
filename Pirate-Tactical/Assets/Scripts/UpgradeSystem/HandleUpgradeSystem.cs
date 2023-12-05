using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class HandleUpgradeSystem : NetworkBehaviour
{
    
    public static HandleUpgradeSystem Instance { get; private set; }

    public UpgradeSystem[,] upgrades = new UpgradeSystem[3, 3];
    public List<UpgradeSystem> allUpgrades = new List<UpgradeSystem>();
    public UpgradeSystem CapacityUpgrade;

    List<UpgradeSystem> tempUpgrades = new List<UpgradeSystem>();

    public NetworkList<int> upgradesInt;
    public NetworkList<int> capacityInt;

    private void Awake()
    {
        if(Instance == null) 
            Instance = this;

        upgradesInt = new NetworkList<int>();
        capacityInt = new NetworkList<int>();
    }

    [ServerRpc]
    public void GenerateUpgradeOnServerRpc()
    {
        if(!IsServer) return;

        while(tempUpgrades.Count < 6) 
        {
            tempUpgrades.Clear();
            upgradesInt.Clear();

            for(int i = 0; i < 6; i++)
            {
                int index = Random.Range(0, allUpgrades.Count);
                tempUpgrades.Add(allUpgrades[index]);
                upgradesInt.Add(index);
            }
            
            tempUpgrades = tempUpgrades.Distinct().ToList();
        }

        SetUpgradeOnClientRpc();
    }

    [ClientRpc]
    public void SetUpgradeOnClientRpc()
    {
        StartCoroutine(AttributeUpgrades());
    }

    IEnumerator AttributeUpgrades()
    {
        yield return new WaitForSeconds(1);

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < 3; j++)
            {
                switch (i)
                {
                    case 0:
                        if (j == 2)
                            upgrades[i, j] = CapacityUpgrade;
                        else
                            upgrades[i, j] = allUpgrades[upgradesInt[j]];
                        break;
                    case 1:
                        if (j == 2)
                            upgrades[i, j] = CapacityUpgrade;
                        else
                            upgrades[i, j] = allUpgrades[upgradesInt[j + 2]];
                        break;
                    case 2:
                        if (j == 2)
                            upgrades[i, j] = CapacityUpgrade;
                        else
                            upgrades[i, j] = allUpgrades[upgradesInt[j + 4]];
                        break;

                }
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpgradeUnitServerRpc(int shopIndex,int i, ulong id, int playerGold)
    {
        if(!IsServer) return;

        if (playerGold >= upgrades[shopIndex, i].goldCost)
            PutUpgradeOnClientRpc(shopIndex, i, id);       
    }

    [ClientRpc]
    public void PutUpgradeOnClientRpc(int shopIndex, int i, ulong id)
    {
        if (NetworkManager.LocalClientId != id) return;

        Cursor p = HUD.Instance.player;

        switch (upgrades[shopIndex, i].upgradeType)
        {
            case UpgradeSystem.UpgradeType.Accost:
                p.unitManager.ships[p.currentShipIndex].accostDmgBoost.Value += upgrades[shopIndex, i].value;
                p.unitManager.ships[p.currentShipIndex].upgrade = upgrades[shopIndex, i].upgradeType;
                break;
            case UpgradeSystem.UpgradeType.Damage:
                p.unitManager.ships[p.currentShipIndex].damage.Value += upgrades[shopIndex, i].value;
                p.unitManager.ships[p.currentShipIndex].upgrade = upgrades[shopIndex, i].upgradeType;
                break;
            case UpgradeSystem.UpgradeType.ManaGain:
                p.specialGainPerRound += upgrades[shopIndex, i].value;
                p.unitManager.ships[p.currentShipIndex].upgrade = upgrades[shopIndex, i].upgradeType;
                break;
            case UpgradeSystem.UpgradeType.MoveRange:
                p.unitManager.ships[p.currentShipIndex].unitMoveRange += upgrades[shopIndex, i].value;
                p.unitManager.ships[p.currentShipIndex].upgrade = upgrades[shopIndex, i].upgradeType;
                break;
            case UpgradeSystem.UpgradeType.TotalMana:
                p.maxSpecialCharge += upgrades[shopIndex, i].value;
                p.unitManager.ships[p.currentShipIndex].upgrade = upgrades[shopIndex, i].upgradeType;
                break;
            case UpgradeSystem.UpgradeType.ShootRange:
                p.unitManager.ships[p.currentShipIndex].unitShootRange += upgrades[shopIndex, i].value;
                p.unitManager.ships[p.currentShipIndex].upgrade = upgrades[shopIndex, i].upgradeType;
                break;
            case UpgradeSystem.UpgradeType.Capacity:
                p.unitManager.ships[p.currentShipIndex].capacitiesUpgraded = true;
                p.unitManager.ships[p.currentShipIndex].upgrade = upgrades[shopIndex, i].upgradeType;
                break;
        }

        p.SetShopToInactive(shopIndex);

        p.playerGold -= upgrades[shopIndex, i].goldCost;
        p.unitManager.ships[p.currentShipIndex].canBeUpgrade = false;
        p.HasDidAnActionClientRpc();

        SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.buyUpgrade);

        HUD.Instance.UpgradeWindow(false, 0);
    }

    [ServerRpc(RequireOwnership = false)]
    public void GetUpgradeFromShipwreckServerRpc(Vector2 pos, ulong id)
    {
        Shipwrek[] shipwreks = FindObjectsOfType<Shipwrek>();
        foreach(var s in shipwreks)
        {
            if(s.pos == pos)
            {
                foreach (var u in allUpgrades)
                {
                    if(u.upgradeType == s.upgradeType.Value)
                    {
                        PutUpgradeFromShipwreckClientRpc(s.upgradeType.Value, u.value, id);
                        GridManager.Instance.SetShipwreckOnMapServerRpc(pos, false);
                        s.GetComponent<NetworkObject>().Despawn();
                        break;
                    }
                }
                break;
                
            }
        }
    }

    [ClientRpc]
    public void PutUpgradeFromShipwreckClientRpc(UpgradeSystem.UpgradeType type, int value, ulong id)
    {
        if (NetworkManager.LocalClientId != id) return;

        Cursor p = HUD.Instance.player;

        switch (type)
        {
            case UpgradeSystem.UpgradeType.Accost:
                p.unitManager.ships[p.currentShipIndex].accostDmgBoost.Value += value;
                p.unitManager.ships[p.currentShipIndex].upgrade = type;
                break;
            case UpgradeSystem.UpgradeType.Damage:
                p.unitManager.ships[p.currentShipIndex].damage.Value += value;
                p.unitManager.ships[p.currentShipIndex].upgrade = type;
                break;
            case UpgradeSystem.UpgradeType.ManaGain:
                p.specialGainPerRound += value;
                p.unitManager.ships[p.currentShipIndex].upgrade = type;
                break;
            case UpgradeSystem.UpgradeType.MoveRange:
                p.unitManager.ships[p.currentShipIndex].unitMoveRange += value;
                p.unitManager.ships[p.currentShipIndex].upgrade = type;
                break;
            case UpgradeSystem.UpgradeType.TotalMana:
                p.maxSpecialCharge += value;
                p.unitManager.ships[p.currentShipIndex].upgrade = type;
                break;
            case UpgradeSystem.UpgradeType.ShootRange:
                p.unitManager.ships[p.currentShipIndex].unitShootRange += value;
                p.unitManager.ships[p.currentShipIndex].upgrade = type;
                break;
            case UpgradeSystem.UpgradeType.Capacity:
                p.unitManager.ships[p.currentShipIndex].upgradedCapacity = true;
                p.unitManager.ships[p.currentShipIndex].upgrade = type;
                break;
        }

        p.unitManager.ships[p.currentShipIndex].canBeUpgrade = false;
        p.HasDidAnActionClientRpc();

        SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.buyUpgrade);
    }
}
