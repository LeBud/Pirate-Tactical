using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

public class HUD : NetworkBehaviour
{
    public static HUD Instance { get; private set; }

    [Header("Texts")]
    [SerializeField] TextMeshProUGUI gameStateTxt;
    [SerializeField] TextMeshProUGUI currentShipInfo;
    public TextMeshProUGUI playerName;
    public TextMeshProUGUI enemyPlayerName;
    public TextMeshProUGUI currentMode;
    public TextMeshProUGUI playerHealthTxt;
    public TextMeshProUGUI enemyHealthTxt;
    public TextMeshProUGUI specialTxt;
    public TextMeshProUGUI goldTxt;

    [Header("Sliders")]
    [SerializeField] Slider specialSlider;
    public Slider playerSlider;
    public Slider enemyPlayerSlider;

    [Header("Buttons")]
    [SerializeField] Button endTurnBtt;
    [SerializeField] Button moveBtt;
    [SerializeField] Button attackBtt;
    [SerializeField] Button specialAttackBtt;
    [SerializeField] Button specialTileBtt;
    [SerializeField] Button interactBtt;

    [Header("Others")]
    public GameObject inGameHUD;
    public Transform shipsDisplay;
    public Transform shipHighlight;
    public Transform enemyShipsDisplay;

    [Header("Upgrade")]
    public GameObject upgradeWindow;
    public GameObject upgradePrefab;
    public Transform upgradesContainer;
    public bool isInUpgradeWindow;

    [Header("Pause Menu")]
    public GameObject pausePanel;
    public bool inPauseMenu;

    [HideInInspector]
    public Cursor player;
    Cursor enemyPlayer;

    int playerMaxHealth;
    int enemyMaxHealth;

    [Header("Icons")]
    public List<CapacitiesSO> capacities;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;

        endTurnBtt.onClick.AddListener(() => { UpdateGameMode(); });
    }

    private void LateUpdate()
    {
        if (player == null) return;

        if (player.shipSelected)
        {
            currentShipInfo.text = "ship selected : " + player.unitManager.ships[player.currentShipIndex].unitName + "\nCan move : " + player.unitManager.ships[player.currentShipIndex].canMove.Value
            + "\nCan shoot : " + player.unitManager.ships[player.currentShipIndex].canShoot.Value;

            shipHighlight.gameObject.SetActive(true);
            shipHighlight.transform.position = shipsDisplay.GetChild(player.currentShipIndex).transform.position;
            currentMode.text = GetCurrentMode(player.currentModeIndex);

            specialAttackBtt.GetComponentInChildren<TextMeshProUGUI>().text = player.unitManager.ships[player.currentShipIndex].shotCapacity.capacityName;
            specialTileBtt.GetComponentInChildren<TextMeshProUGUI>().text = player.unitManager.ships[player.currentShipIndex].tileCapacity.capacityName;

            if (player.unitManager.ships[player.currentShipIndex].canMove.Value)
                moveBtt.interactable = true;
            else if (!player.unitManager.ships[player.currentShipIndex].canMove.Value)
                moveBtt.interactable = false;

            if (player.unitManager.ships[player.currentShipIndex].canShoot.Value)
            {
                attackBtt.interactable = true;
                interactBtt.interactable = true;

                if (player.unitManager.ships[player.currentShipIndex].unitSpecialTile.Value != ShipUnit.UnitSpecialTile.None)
                    specialTileBtt.interactable = true;
                else
                    specialTileBtt.interactable = false;

                if(player.unitManager.ships[player.currentShipIndex].unitSpecialShot.Value != ShipUnit.UnitSpecialShot.None)
                    specialAttackBtt.interactable = true;
                else
                    specialAttackBtt.interactable = false;
            }
            else if (!player.unitManager.ships[player.currentShipIndex].canShoot.Value)
            {
                attackBtt.interactable = false;
                specialTileBtt.interactable = false;
                specialAttackBtt.interactable = false;
                interactBtt.interactable = false;
            }

        }
        else
        {
            currentShipInfo.text = "ship selected : none ";
            currentMode.text = "none";
            shipHighlight.gameObject.SetActive(false);

            moveBtt.interactable = false;
            attackBtt.interactable = false;
            specialTileBtt.interactable = false;
            specialAttackBtt.interactable = false;
            interactBtt.interactable = false;
        }

        specialSlider.value = player.currentSpecialCharge;
        specialTxt.text = player.currentSpecialCharge.ToString() + " / " + player.maxSpecialCharge.ToString();
        goldTxt.text = "Gold : " + player.playerGold;

    }

    public IEnumerator SetShipOnHUD()
    {
        if (player == null) yield break;

        yield return new WaitForSeconds(0.5f);

        for(int i = 0; i<player.unitManager.ships.Length; i++)
        {
            if (player.unitManager.ships[i] != null)
            {
                shipsDisplay.GetChild(i).gameObject.SetActive(true);
                shipsDisplay.GetChild(i).transform.GetChild(3).GetComponent<TextMeshProUGUI>().text = player.unitManager.ships[i].unitLife.Value.ToString();
                shipsDisplay.GetChild(i).transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = player.unitManager.ships[i].shotCapacity.specialAbilityCost.ToString();
                //Mettre des sprites
                //shipsDisplay.GetChild(i).transform.GetChild(5).GetComponent<Image>().sprite = player.unitManager.ships[i].unitSpecialShot.Value.ToString();
                //shipsDisplay.GetChild(i).transform.GetChild(6).GetComponent<Image>().sprite = player.unitManager.ships[i].unitSpecialTile.Value.ToString();
            }
            else
                shipsDisplay.GetChild(i).gameObject.SetActive(false);
        }
    }

    public IEnumerator SetEnemiesShip()
    {
        if (enemyPlayer == null) yield break;

        yield return new WaitForSeconds(0.5f);

        for (int i = 0; i < enemyPlayer.unitManager.ships.Length; i++)
        {
            if (enemyPlayer.unitManager.ships[i] != null)
            {
                enemyShipsDisplay.GetChild(i).gameObject.SetActive(true);
                enemyShipsDisplay.GetChild(i).transform.GetChild(2).GetComponent<TextMeshProUGUI>().text = enemyPlayer.unitManager.ships[i].unitLife.Value.ToString();
                //Mettre des sprites
                enemyShipsDisplay.GetChild(i).transform.GetChild(3).GetComponent<Image>().sprite = GetIconShot(enemyPlayer.unitManager.ships[i].unitSpecialShot.Value);
                enemyShipsDisplay.GetChild(i).transform.GetChild(4).GetComponent<Image>().sprite = GetIconTile(enemyPlayer.unitManager.ships[i].unitSpecialTile.Value);
            }
            else
                enemyShipsDisplay.GetChild(i).gameObject.SetActive(false);
        }
    }

    Sprite GetIconShot(ShipUnit.UnitSpecialShot shot)
    {
        foreach(var c in capacities)
        {
            if (c.shootCapacity == shot)
                return c.icon;
        }

        return null;
    }

    Sprite GetIconTile(ShipUnit.UnitSpecialTile shot)
    {
        foreach (var c in capacities)
        {
            if (c.tileCapacity == shot)
                return c.icon;
        }
        return null;
    }

    string GetCurrentMode(int i)
    {
        switch (i)
        {
            case 0:
                return "Interact";
            case 1:
                return "Move unit";
            case 2:
                return "attack enemy";
            case 3:
                return "special unit tile";
            case 4:
                return "special unit attack";
            default:
                return null;
        }
    }
    public void UpdateGameMode()
    {
        if(player != null && player.canPlay.Value)
        {
            player.shipSelected = false;
            player.HideTiles();
            GameManager.Instance.UpdateGameStateServerRpc();
        }
    }

    public void UpgradeWindow(bool active, int shopID)
    {
        upgradeWindow.SetActive(active);
        isInUpgradeWindow = active;

        if (active)
        {
            for(int i = 0; i < 3; i++)
            {
                GameObject up = Instantiate(upgradePrefab, upgradesContainer);
                up.GetComponent<UpgradeHolder>().SetUpgrade(shopID, i, (int)NetworkManager.LocalClientId, player.playerGold);
            }
        }
        else if (!active)
        {
            foreach(Transform c in upgradesContainer)
                Destroy(c.gameObject);
        }
    }

    public void CloseUpgradeWindowBtt()
    {
        UpgradeWindow(false, 0);
    }

    public void PauseGame()
    {
        inPauseMenu = !inPauseMenu;

        if (inPauseMenu)
            pausePanel.SetActive(true);
        else
            pausePanel.SetActive(false);
    }

    public void SelectShipOnHUD(int i)
    {
        player.shipSelected = false;
        player.SelectShipHUD(i);
    }

    #region ClientRpcMethods

    [ClientRpc]
    public void SetGameStateClientRpc(string gameState, int round)
    {
        if(round == -1)
            gameStateTxt.text = gameState;
        else if(round == 0)
            gameStateTxt.text = gameState + "\nspawn ships";
        else
            gameStateTxt.text = gameState + "\nround " + round;
    }

    [ClientRpc]
    public void UpdateHealthBarClientRpc()
    {
        if (player == null || enemyPlayer == null) return;

        playerSlider.value = player.totalPlayerHealth.Value;
        enemyPlayerSlider.value = enemyPlayer.totalPlayerHealth.Value;

        playerHealthTxt.text = player.totalPlayerHealth.Value.ToString() + " / " + playerMaxHealth;
        enemyHealthTxt.text = enemyPlayer.totalPlayerHealth.Value.ToString() + " / " + enemyMaxHealth;

        StartCoroutine(SetShipOnHUD());
        StartCoroutine(SetEnemiesShip());
    }

    [ClientRpc]
    public void SetUIClientRpc(ulong id)
    {
        Cursor[] c = FindObjectsOfType<Cursor>();
        if(id == NetworkManager.LocalClientId)
        {
            foreach(Cursor cursor in c)
            {
                if(cursor.GetComponent<NetworkObject>().OwnerClientId == id)
                {
                    player = cursor;
                    break;
                }
            }
            playerSlider.maxValue = player.totalPlayerHealth.Value;
            playerSlider.value = player.totalPlayerHealth.Value;
            playerMaxHealth = player.totalPlayerHealth.Value;
            playerHealthTxt.text = player.totalPlayerHealth.Value.ToString() + " / " + playerMaxHealth;
            if (id == 0) playerName.text = GameManager.Instance.player1.Value.ToString();
            else if (id == 1) playerName.text = GameManager.Instance.player2.Value.ToString();

            moveBtt.onClick.AddListener(() => { player.currentModeIndex = 1; });
            attackBtt.onClick.AddListener(() => { player.currentModeIndex = 2; });
            specialTileBtt.onClick.AddListener(() => { player.currentModeIndex = 3; });
            specialAttackBtt.onClick.AddListener(() => { player.currentModeIndex = 4; });
            interactBtt.onClick.AddListener(() => { player.currentModeIndex = 0; });

        }
        else
        {
            foreach (Cursor cursor in c)
            {
                if (cursor.GetComponent<NetworkObject>().OwnerClientId == id)
                {
                    enemyPlayer = cursor;
                    break;
                }
            }
            enemyPlayerSlider.maxValue = enemyPlayer.totalPlayerHealth.Value;
            enemyPlayerSlider.value = enemyPlayer.totalPlayerHealth.Value;
            enemyMaxHealth = enemyPlayer.totalPlayerHealth.Value;
            enemyHealthTxt.text = enemyPlayer.totalPlayerHealth.Value.ToString() + " / " + enemyMaxHealth;
            if (id == 0) enemyPlayerName.text = GameManager.Instance.player1.Value.ToString();
            else if (id == 1) enemyPlayerName.text = GameManager.Instance.player2.Value.ToString();
        }


        StartCoroutine(SetShipOnHUD());
        StartCoroutine(SetEnemiesShip());
    }

#endregion

}
