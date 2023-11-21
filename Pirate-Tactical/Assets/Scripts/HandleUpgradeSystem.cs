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
    public List<UpgradeSystem> allCapacityUpgrades = new List<UpgradeSystem>();

    List<UpgradeSystem> tempUpgrades = new List<UpgradeSystem>();
    List<UpgradeSystem> tempCapacityUpgrades = new List<UpgradeSystem>();

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

        Debug.Log("Upgrades Selected");

        while (tempCapacityUpgrades.Count < 3)
        {
            tempCapacityUpgrades.Clear();
            capacityInt.Clear();

            for (int i = 0; i < 3; i++)
            {
                int index = Random.Range(0, allCapacityUpgrades.Count);
                tempCapacityUpgrades.Add(allCapacityUpgrades[index]);
                capacityInt.Add(index);
            }

            tempCapacityUpgrades = tempCapacityUpgrades.Distinct().ToList();
        }

        Debug.Log("New Capacities Selected");

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
                            upgrades[i, j] = allCapacityUpgrades[capacityInt[i]];
                        else
                            upgrades[i, j] = allUpgrades[upgradesInt[j]];
                        break;
                    case 1:
                        if (j == 2)
                            upgrades[i, j] = allCapacityUpgrades[capacityInt[i]];
                        else
                            upgrades[i, j] = allUpgrades[upgradesInt[j + 2]];
                        break;
                    case 2:
                        if (j == 2)
                            upgrades[i, j] = allCapacityUpgrades[capacityInt[i]];
                        else
                            upgrades[i, j] = allUpgrades[upgradesInt[j + 4]];
                        break;

                }
                Debug.Log(upgrades[i, j].upgradeType.ToString());
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpgradeUnitServerRpc(int shopIndex,int i, ulong id)
    {
        if(!IsServer) return;

        PutUpgradeOnClientRpc(shopIndex, i, id);       
    }

    [ClientRpc]
    public void PutUpgradeOnClientRpc(int shopIndex, int i, ulong id)
    {
        if (NetworkManager.LocalClientId != id) return;

        Cursor p = NetworkManager.ConnectedClients[id].PlayerObject.GetComponent<Cursor>();

        switch (upgrades[shopIndex, i].upgradeType)
        {
            case UpgradeSystem.UpgradeType.Accost:
                p.unitManager.ships[p.currentShipIndex].accostDmgBoost.Value += upgrades[shopIndex, i].value;
                break;
            case UpgradeSystem.UpgradeType.Damage:
                p.unitManager.ships[p.currentShipIndex].damage.Value += upgrades[shopIndex, i].value;
                break;
            case UpgradeSystem.UpgradeType.ManaGain:
                p.specialGainPerRound += upgrades[shopIndex, i].value;
                break;
            case UpgradeSystem.UpgradeType.MoveRange:
                p.unitManager.ships[p.currentShipIndex].unitMoveRange += upgrades[shopIndex, i].value;
                break;
            case UpgradeSystem.UpgradeType.TotalMana:
                p.maxSpecialCharge += upgrades[shopIndex, i].value;
                break;
            case UpgradeSystem.UpgradeType.ShootRange:
                p.unitManager.ships[p.currentShipIndex].unitShootRange += upgrades[shopIndex, i].value;
                break;
            case UpgradeSystem.UpgradeType.TileCapacity:
                p.unitManager.ships[p.currentShipIndex].unitSpecialTile = upgrades[shopIndex, i].newTileCapacity;
                break;
            case UpgradeSystem.UpgradeType.ShootCapacity:
                p.unitManager.ships[p.currentShipIndex].unitSpecialShot = upgrades[shopIndex, i].newShootCapacity;
                break;
        }

        p.unitManager.ships[p.currentShipIndex].canBeUpgrade = false;
        p.HasDidAnActionClientRpc();
    }


}
