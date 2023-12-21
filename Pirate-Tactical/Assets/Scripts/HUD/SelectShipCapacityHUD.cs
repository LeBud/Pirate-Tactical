using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class SelectShipCapacityHUD : NetworkBehaviour
{

    public static SelectShipCapacityHUD Instance;

    public GameObject selectShipHUD;
    public GameObject lobbyHUD;

    [Header("Capacities")]
    public CapacitiesSO[] galionShotCapacities;
    public CapacitiesSO[] galionTileCapacities;
    public CapacitiesSO[] brigantinShotCapacities;
    public CapacitiesSO[] brigantinTileCapacities;
    public CapacitiesSO[] sloopShotCapacities;
    public CapacitiesSO[] sloopTileCapacities;

    public Button readyBtt;
    public string bttReadyTxt;
    public string bttNotReadyTxt;

    [Header("DropDowns")]
    public TMP_Dropdown galionShot;
    public TMP_Dropdown galionTile;
    public TMP_Dropdown brigantinShot;
    public TMP_Dropdown brigantinTile;
    public TMP_Dropdown sloopShot;
    public TMP_Dropdown sloopTile;

    [Header("Capacity descriptions")]
    public TextMeshProUGUI[] txts;

    bool isReady;

    public Cursor player;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }

    private void Start()
    {
        readyBtt.onClick.AddListener(() => { GetReady(); });
        readyBtt.GetComponentInChildren<TextMeshProUGUI>().text = bttNotReadyTxt;
    }

    void RefreshOptionsDropDown()
    {
        ClearOptions();

        foreach (var c in galionShotCapacities)
            galionShot.options.Add(new TMP_Dropdown.OptionData(c.capacityName));
        foreach (var c in galionTileCapacities)
            galionTile.options.Add(new TMP_Dropdown.OptionData(c.capacityName));
        foreach (var c in brigantinShotCapacities)
            brigantinShot.options.Add(new TMP_Dropdown.OptionData(c.capacityName));
        foreach (var c in brigantinTileCapacities)
            brigantinTile.options.Add(new TMP_Dropdown.OptionData(c.capacityName));
        foreach (var c in sloopShotCapacities)
            sloopShot.options.Add(new TMP_Dropdown.OptionData(c.capacityName));
        foreach (var c in sloopTileCapacities)
            sloopTile.options.Add(new TMP_Dropdown.OptionData(c.capacityName));

        RefreshOptions();

        for(int i = 0; i < txts.Length; i++)
        {
            switch (i)
            {
                case 0:
                    player.unitManager.ships[0].shotCapacity = galionShotCapacities[galionShot.value];
                    player.unitManager.ships[0].unitSpecialShot.Value = galionShotCapacities[galionShot.value].shootCapacity;
                    txts[i].text = galionShotCapacities[galionShot.value].capacityDescription;
                    HUD.Instance.shipsDisplay.GetChild(0).transform.GetChild(5).GetComponent<Image>().sprite = galionShotCapacities[galionShot.value].icon;
                    break;
                case 1:
                    player.unitManager.ships[0].tileCapacity = galionTileCapacities[galionTile.value];
                    player.unitManager.ships[0].unitSpecialTile.Value = galionTileCapacities[galionTile.value].tileCapacity;
                    txts[i].text = galionTileCapacities[galionTile.value].capacityDescription;
                    HUD.Instance.shipsDisplay.GetChild(0).transform.GetChild(6).GetComponent<Image>().sprite = galionTileCapacities[galionTile.value].icon;
                    break;
                case 2:
                    player.unitManager.ships[1].shotCapacity = brigantinShotCapacities[brigantinShot.value];
                    player.unitManager.ships[2].shotCapacity = brigantinShotCapacities[brigantinShot.value];
                    player.unitManager.ships[1].unitSpecialShot.Value = brigantinShotCapacities[brigantinShot.value].shootCapacity;
                    player.unitManager.ships[2].unitSpecialShot.Value = brigantinShotCapacities[brigantinShot.value].shootCapacity;
                    txts[i].text = brigantinShotCapacities[brigantinShot.value].capacityDescription;
                    HUD.Instance.shipsDisplay.GetChild(1).transform.GetChild(5).GetComponent<Image>().sprite = brigantinShotCapacities[brigantinShot.value].icon;
                    HUD.Instance.shipsDisplay.GetChild(2).transform.GetChild(5).GetComponent<Image>().sprite = brigantinShotCapacities[brigantinShot.value].icon;
                    break;
                case 3:
                    player.unitManager.ships[1].tileCapacity = brigantinTileCapacities[brigantinTile.value];
                    player.unitManager.ships[2].tileCapacity = brigantinTileCapacities[brigantinTile.value];
                    player.unitManager.ships[1].unitSpecialTile.Value = brigantinTileCapacities[brigantinTile.value].tileCapacity;
                    player.unitManager.ships[2].unitSpecialTile.Value = brigantinTileCapacities[brigantinTile.value].tileCapacity;
                    txts[i].text = brigantinTileCapacities[brigantinTile.value].capacityDescription;
                    HUD.Instance.shipsDisplay.GetChild(1).transform.GetChild(6).GetComponent<Image>().sprite = brigantinTileCapacities[brigantinTile.value].icon;
                    HUD.Instance.shipsDisplay.GetChild(2).transform.GetChild(6).GetComponent<Image>().sprite = brigantinTileCapacities[brigantinTile.value].icon;
                    break;
                case 4:
                    player.unitManager.ships[3].shotCapacity = sloopShotCapacities[sloopShot.value];
                    player.unitManager.ships[4].shotCapacity = sloopShotCapacities[sloopShot.value];
                    player.unitManager.ships[3].unitSpecialShot.Value = sloopShotCapacities[sloopShot.value].shootCapacity;
                    player.unitManager.ships[4].unitSpecialShot.Value = sloopShotCapacities[sloopShot.value].shootCapacity;
                    txts[i].text = sloopShotCapacities[sloopShot.value].capacityDescription;
                    HUD.Instance.shipsDisplay.GetChild(3).transform.GetChild(5).GetComponent<Image>().sprite = sloopShotCapacities[sloopShot.value].icon;
                    HUD.Instance.shipsDisplay.GetChild(4).transform.GetChild(5).GetComponent<Image>().sprite = sloopShotCapacities[sloopShot.value].icon;
                    break;
                case 5:
                    player.unitManager.ships[3].tileCapacity = sloopTileCapacities[sloopTile.value];
                    player.unitManager.ships[4].tileCapacity = sloopTileCapacities[sloopTile.value];
                    player.unitManager.ships[3].unitSpecialTile.Value = sloopTileCapacities[sloopTile.value].tileCapacity;
                    player.unitManager.ships[4].unitSpecialTile.Value = sloopTileCapacities[sloopTile.value].tileCapacity;
                    txts[i].text = sloopTileCapacities[sloopTile.value].capacityDescription;
                    HUD.Instance.shipsDisplay.GetChild(3).transform.GetChild(6).GetComponent<Image>().sprite = sloopTileCapacities[sloopTile.value].icon;
                    HUD.Instance.shipsDisplay.GetChild(4).transform.GetChild(6).GetComponent<Image>().sprite = sloopTileCapacities[sloopTile.value].icon;
                    break;
            }
        }
        
    }

    void ClearOptions()
    {
        galionShot.ClearOptions();
        galionTile.ClearOptions();
        brigantinShot.ClearOptions();
        brigantinTile.ClearOptions();
        sloopShot.ClearOptions();
        sloopTile.ClearOptions();
}

    void RefreshOptions()
    {
        galionShot.RefreshShownValue();
        galionTile.RefreshShownValue();
        brigantinShot.RefreshShownValue();
        brigantinTile.RefreshShownValue();
        sloopShot.RefreshShownValue();
        sloopTile.RefreshShownValue();
    }

    public void SelectCapacity(int capacityIndex)
    {
        switch (capacityIndex)
        {
            case 0:
                player.unitManager.ships[0].shotCapacity = galionShotCapacities[galionShot.value];
                txts[capacityIndex].text = galionShotCapacities[galionShot.value].capacityDescription;
                player.unitManager.ships[0].unitSpecialShot.Value = galionShotCapacities[galionShot.value].shootCapacity;
                HUD.Instance.shipsDisplay.GetChild(0).transform.GetChild(5).GetComponent<Image>().sprite = galionShotCapacities[galionShot.value].icon;
                break;
            case 1:
                player.unitManager.ships[0].tileCapacity = galionTileCapacities[galionShot.value];
                txts[capacityIndex].text = galionTileCapacities[galionTile.value].capacityDescription;
                player.unitManager.ships[0].unitSpecialTile.Value = galionTileCapacities[galionTile.value].tileCapacity;
                HUD.Instance.shipsDisplay.GetChild(0).transform.GetChild(6).GetComponent<Image>().sprite = galionTileCapacities[galionTile.value].icon;
                break;
            case 2:
                player.unitManager.ships[1].shotCapacity = brigantinShotCapacities[brigantinShot.value];
                player.unitManager.ships[2].shotCapacity = brigantinShotCapacities[brigantinShot.value];
                txts[capacityIndex].text = brigantinShotCapacities[brigantinShot.value].capacityDescription;
                player.unitManager.ships[1].unitSpecialShot.Value = brigantinShotCapacities[brigantinShot.value].shootCapacity;
                player.unitManager.ships[2].unitSpecialShot.Value = brigantinShotCapacities[brigantinShot.value].shootCapacity;
                HUD.Instance.shipsDisplay.GetChild(1).transform.GetChild(5).GetComponent<Image>().sprite = brigantinShotCapacities[brigantinShot.value].icon;
                HUD.Instance.shipsDisplay.GetChild(2).transform.GetChild(5).GetComponent<Image>().sprite = brigantinShotCapacities[brigantinShot.value].icon;
                break;
            case 3:
                player.unitManager.ships[1].tileCapacity = brigantinTileCapacities[brigantinTile.value];
                player.unitManager.ships[2].tileCapacity = brigantinTileCapacities[brigantinTile.value];
                txts[capacityIndex].text = brigantinTileCapacities[brigantinTile.value].capacityDescription;
                player.unitManager.ships[1].unitSpecialTile.Value = brigantinTileCapacities[brigantinTile.value].tileCapacity;
                player.unitManager.ships[2].unitSpecialTile.Value = brigantinTileCapacities[brigantinTile.value].tileCapacity;
                HUD.Instance.shipsDisplay.GetChild(1).transform.GetChild(6).GetComponent<Image>().sprite = brigantinTileCapacities[brigantinTile.value].icon;
                HUD.Instance.shipsDisplay.GetChild(2).transform.GetChild(6).GetComponent<Image>().sprite = brigantinTileCapacities[brigantinTile.value].icon;
                break;
            case 4:
                player.unitManager.ships[3].shotCapacity = sloopShotCapacities[sloopShot.value];
                player.unitManager.ships[4].shotCapacity = sloopShotCapacities[sloopShot.value];
                txts[capacityIndex].text = sloopShotCapacities[sloopShot.value].capacityDescription;
                player.unitManager.ships[3].unitSpecialShot.Value = sloopShotCapacities[sloopShot.value].shootCapacity;
                player.unitManager.ships[4].unitSpecialShot.Value = sloopShotCapacities[sloopShot.value].shootCapacity;
                HUD.Instance.shipsDisplay.GetChild(3).transform.GetChild(5).GetComponent<Image>().sprite = sloopShotCapacities[sloopShot.value].icon;
                HUD.Instance.shipsDisplay.GetChild(4).transform.GetChild(5).GetComponent<Image>().sprite = sloopShotCapacities[sloopShot.value].icon;
                break;
            case 5:
                player.unitManager.ships[3].tileCapacity = sloopTileCapacities[sloopTile.value];
                player.unitManager.ships[4].tileCapacity = sloopTileCapacities[sloopTile.value];
                txts[capacityIndex].text = sloopTileCapacities[sloopTile.value].capacityDescription;
                player.unitManager.ships[3].unitSpecialTile.Value = sloopTileCapacities[sloopTile.value].tileCapacity;
                player.unitManager.ships[4].unitSpecialTile.Value = sloopTileCapacities[sloopTile.value].tileCapacity;
                HUD.Instance.shipsDisplay.GetChild(3).transform.GetChild(6).GetComponent<Image>().sprite = sloopTileCapacities[sloopTile.value].icon;
                HUD.Instance.shipsDisplay.GetChild(4).transform.GetChild(6).GetComponent<Image>().sprite = sloopTileCapacities[sloopTile.value].icon;
                break;
        }
    }

    public void GetReady()
    {
        isReady = !isReady;
        if (isReady)
            readyBtt.GetComponentInChildren<TextMeshProUGUI>().text = bttReadyTxt;
        else
            readyBtt.GetComponentInChildren<TextMeshProUGUI>().text = bttNotReadyTxt;

        player.isReady.Value = isReady;
    }

    [ClientRpc]
    public void SetPlayerToClientRpc()
    {
        Cursor[] c = FindObjectsOfType<Cursor>();
        foreach (Cursor cursor in c)
            if (cursor.GetComponent<NetworkObject>().OwnerClientId == NetworkManager.LocalClientId)
                player = cursor;

        selectShipHUD.SetActive(true);
        lobbyHUD.SetActive(false);

        RefreshOptionsDropDown();
    }

    [ClientRpc]
    public void CloseWindowClientRpc()
    {
        selectShipHUD.SetActive(false);
    }

}
