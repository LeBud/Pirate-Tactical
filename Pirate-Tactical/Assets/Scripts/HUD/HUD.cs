using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using Unity.VisualScripting;
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
    [SerializeField] Transform selectedBtt;

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

    [Header("Announcer")]
    public GameObject stormGO;
    public GameObject playerTurn;
    public TextMeshProUGUI playerTurnTxt;

    [Header("Round Gain")]
    public TextMeshProUGUI goldGain;
    public TextMeshProUGUI manaGain;

    [Header("Lobby")]
    public GameObject[] lobbyScreen;
    public GameObject lobbyMenu;

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
            currentShipInfo.text = "Navire sélectionner : " + player.unitManager.ships[player.currentShipIndex].unitName + "\nPeut se déplacer : " + NavireMove(player.unitManager.ships[player.currentShipIndex].canMove.Value)
            + "\nPeut tirer : " + NavireMove(player.unitManager.ships[player.currentShipIndex].canShoot.Value) + "\nDéplacement : " + MovemOde(player.pathMode);

            shipHighlight.gameObject.SetActive(true);
            shipHighlight.transform.position = shipsDisplay.GetChild(player.currentShipIndex).transform.position;
            currentMode.text = GetCurrentMode(player.currentModeIndex);

            if(player.unitManager.ships[player.currentShipIndex].shotCapacity != null)
                specialAttackBtt.GetComponentInChildren<TextMeshProUGUI>().text = player.unitManager.ships[player.currentShipIndex].shotCapacity.capacityName;
            if(player.unitManager.ships[player.currentShipIndex].tileCapacity != null)
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

            selectedBtt.gameObject.SetActive(true);
            switch (player.currentModeIndex)
            {
                case 0:
                    selectedBtt.position = interactBtt.transform.position;
                    break;
                case 1:
                    selectedBtt.position = moveBtt.transform.position;
                    break;
                case 2:
                    selectedBtt.position = attackBtt.transform.position;
                    break;
                case 3:
                    selectedBtt.position = specialTileBtt.transform.position;
                    break;
                case 4:
                    selectedBtt.position = specialAttackBtt.transform.position;
                    break;
            }

        }
        else
        {
            currentShipInfo.text = "";
            currentMode.text = "";
            shipHighlight.gameObject.SetActive(false);
            selectedBtt.gameObject.SetActive(false);

            moveBtt.interactable = false;
            attackBtt.interactable = false;
            specialTileBtt.interactable = false;
            specialAttackBtt.interactable = false;
            interactBtt.interactable = false;
        }

        specialSlider.value = player.currentSpecialCharge;
        specialTxt.text = player.currentSpecialCharge.ToString() + "/" + player.maxSpecialCharge.ToString();
        goldTxt.text = "Or : " + player.playerGold;

    }

    string NavireMove(bool yes)
    {
        if (yes) return "Oui";
        else return "Non";
    }
    string MovemOde(bool yes)
    {
        if (yes) return "Manuel";
        else return "Automatique";
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
                shipsDisplay.GetChild(i).transform.GetChild(4).GetComponent<TextMeshProUGUI>().text = player.unitManager.ships[i].tileCapacity.specialAbilityCost.ToString();
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
                return "Aborder/Acheter";
            case 1:
                return "Déplacer navire";
            case 2:
                return "Tirer";
            case 3:
                return "Capacité spéciale de terrain";
            case 4:
                return "Capacité spéciale des canons";
            default:
                return null;
        }
    }

    public void UpdateGameMode()
    {
        if(player != null && player.canPlay.Value)
        {
            player.DeselectShip();
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
        if (round == -1)
            gameStateTxt.text = "Sélectionner capacités";
        else if(round == 0)
            gameStateTxt.text = gameState + "\nPlacer les navires";
        else
            gameStateTxt.text = gameState + "\nTour " + round;
    }

    [ClientRpc]
    public void UpdateHealthBarClientRpc()
    {
        if (player == null || enemyPlayer == null) return;

        playerSlider.value = player.totalPlayerHealth.Value;
        enemyPlayerSlider.value = enemyPlayer.totalPlayerHealth.Value;

        playerHealthTxt.text = player.totalPlayerHealth.Value.ToString() + "/" + playerMaxHealth;
        enemyHealthTxt.text = enemyPlayer.totalPlayerHealth.Value.ToString() + "/" + enemyMaxHealth;

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
            enemyHealthTxt.text = enemyPlayer.totalPlayerHealth.Value.ToString() + "/" + enemyMaxHealth;
            if (id == 0) enemyPlayerName.text = GameManager.Instance.player1.Value.ToString();
            else if (id == 1) enemyPlayerName.text = GameManager.Instance.player2.Value.ToString();
        }


        StartCoroutine(SetShipOnHUD());
        StartCoroutine(SetEnemiesShip());
    }

    #endregion

    [ClientRpc]
    public void DisplayStormClientRpc()
    {
        StartCoroutine(StormDisplay());
    }

    IEnumerator StormDisplay()
    {
        stormGO.SetActive(true);
        yield return new WaitForSeconds(4);
        stormGO.SetActive(false);
    }

    [ClientRpc]
    public void PlayerTurnClientRpc(FixedString128Bytes p)
    {
        StartCoroutine(PlayerTurn(p));
    }

    IEnumerator PlayerTurn(FixedString128Bytes p)
    {
        playerTurn.SetActive(true);
        playerTurnTxt.text = "Au tour de : " + p;
        SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.playerTurn);
        yield return new WaitForSeconds(2);
        playerTurn.SetActive(false);
    }

    [ClientRpc]
    public void DisplayGoldManaGainClientRpc()
    {
        StartCoroutine(ShowGoldManaGain());
    }

    IEnumerator ShowGoldManaGain()
    {
        goldGain.text = "+" + player.playerGoldGainPerRound.ToString();
        manaGain.text = "+" + player.specialGainPerRound.ToString();

        goldGain.gameObject.SetActive(true);
        manaGain.gameObject.SetActive(true);

        yield return new WaitForSeconds(4);

        goldGain.gameObject.SetActive(false);
        manaGain.gameObject.SetActive(false);

    }

}
