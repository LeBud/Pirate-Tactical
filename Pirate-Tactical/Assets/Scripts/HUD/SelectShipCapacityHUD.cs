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

    [Header("Capacities")]
    public CapacitiesSO[] galionShotCapacities;
    public CapacitiesSO[] galionTileCapacities;
    public CapacitiesSO[] brigantinShotCapacities;
    public CapacitiesSO[] brigantinTileCapacities;
    public CapacitiesSO[] sloopShotCapacities;
    public CapacitiesSO[] sloopTileCapacities;

    public Button readyBtt;

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
        RefreshOptionsDropDown();
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
                    txts[i].text = galionShotCapacities[galionShot.value].capacityDescription;
                    break;
                case 1:
                    txts[i].text = galionTileCapacities[galionTile.value].capacityDescription;
                    break;
                case 2:
                    txts[i].text = brigantinShotCapacities[brigantinShot.value].capacityDescription;
                    break;
                case 3:
                    txts[i].text = brigantinTileCapacities[brigantinTile.value].capacityDescription;
                    break;
                case 4:
                    txts[i].text = sloopShotCapacities[sloopShot.value].capacityDescription;
                    break;
                case 5:
                    txts[i].text = sloopTileCapacities[sloopTile.value].capacityDescription;
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
                break;
            case 1:
                player.unitManager.ships[0].tileCapacity = galionTileCapacities[galionShot.value];
                txts[capacityIndex].text = galionTileCapacities[galionTile.value].capacityDescription;
                break;
            case 2:
                player.unitManager.ships[1].shotCapacity = brigantinShotCapacities[brigantinShot.value];
                player.unitManager.ships[2].shotCapacity = brigantinShotCapacities[brigantinShot.value];
                txts[capacityIndex].text = brigantinShotCapacities[brigantinShot.value].capacityDescription;
                break;
            case 3:
                player.unitManager.ships[1].tileCapacity = brigantinTileCapacities[brigantinTile.value];
                player.unitManager.ships[2].tileCapacity = brigantinTileCapacities[brigantinTile.value];
                txts[capacityIndex].text = brigantinTileCapacities[brigantinTile.value].capacityDescription;
                break;
            case 4:
                player.unitManager.ships[3].shotCapacity = sloopShotCapacities[sloopShot.value];
                player.unitManager.ships[4].shotCapacity = sloopShotCapacities[sloopShot.value];
                txts[capacityIndex].text = sloopShotCapacities[sloopShot.value].capacityDescription;
                break;
            case 5:
                player.unitManager.ships[3].tileCapacity = sloopTileCapacities[sloopTile.value];
                player.unitManager.ships[4].tileCapacity = sloopTileCapacities[sloopTile.value];
                txts[capacityIndex].text = sloopTileCapacities[sloopTile.value].capacityDescription;
                break;
        }
    }

    public void GetReady()
    {
        isReady = !isReady;

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
    }

    [ClientRpc]
    public void CloseWindowClientRpc()
    {
        selectShipHUD.SetActive(false);
    }

}
