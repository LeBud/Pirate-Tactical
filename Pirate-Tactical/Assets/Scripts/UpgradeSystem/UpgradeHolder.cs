using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeHolder : MonoBehaviour
{
    public UpgradeSystem currentUpgrade;

    public TextMeshProUGUI uPName;
    public TextMeshProUGUI uPDescription;
    public Button upBuyBtt;

    int shop;
    int num;
    int ID;
    int gold;

    public void SetUpgrade(int shopID, int i, int playerID, int playerGold)
    {
        currentUpgrade = HandleUpgradeSystem.Instance.upgrades[shopID,i];
        uPName.text = currentUpgrade.upName;
        uPDescription.text = currentUpgrade.upDescription;

        shop = shopID;
        num = i;
        ID = playerID;
        gold = playerGold;

        upBuyBtt.onClick.AddListener(() => BuyUpgrade());
    }

    public void BuyUpgrade()
    {
        HandleUpgradeSystem.Instance.UpgradeUnitServerRpc(shop, num, (ulong)ID, gold);
    }
}
