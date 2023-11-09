using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
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

    Cursor player;
    Cursor enemyPlayer;

    int playerMaxHealth;
    int enemyMaxHealth;

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
        }
        else
        {
            currentShipInfo.text = "ship selected : none ";
        }

        specialSlider.value = player.currentSpecialCharge;
        specialTxt.text = player.currentSpecialCharge.ToString() + " / " + player.maxSpecialCharge.ToString();

        if (player.shipSelected)
        {
            shipHighlight.gameObject.SetActive(true);
            shipHighlight.transform.position = shipsDisplay.GetChild(player.currentShipIndex).transform.position;
            currentMode.text = GetCurrentMode(player.currentModeIndex);
        }
        else
        {
            currentMode.text = "none";
            shipHighlight.gameObject.SetActive(false);
        }

    }

    void SetShipOnHUD()
    {
        if (player == null) return;
        for(int i = 0; i<player.unitManager.ships.Length; i++)
        {
            shipsDisplay.GetChild(i).GetChild(3).GetComponent<TextMeshProUGUI>().text = player.unitManager.ships[i].damage.ToString();
            shipsDisplay.GetChild(i).GetChild(4).GetComponent<TextMeshProUGUI>().text = player.unitManager.ships[i].specialAbilityCost.ToString();
        }
    }

    string GetCurrentMode(int i)
    {
        if (i == 0)
            return "Interact";
        else if (i == 1)
            return "Move unit";
        else if (i == 2)
            return "attack enemy";
        else if (i == 3)
            return "special unit tile";
        else if (i == 4)
            return "special unit attack";

        return null;
    }

    [ClientRpc]
    public void SetGameStateClientRpc(string gameState, int round)
    {
        gameStateTxt.text = gameState + "\nround " + round;
    }

    public void UpdateGameMode()
    {
        if(player != null && player.canPlay.Value)
            GameManager.Instance.UpdateGameStateServerRpc();
    }

    [ClientRpc]
    public void UpdateHealthBarClientRpc()
    {
        playerSlider.value = player.totalPlayerHealth.Value;
        enemyPlayerSlider.value = enemyPlayer.totalPlayerHealth.Value;

        playerHealthTxt.text = player.totalPlayerHealth.Value.ToString() + " / " + playerMaxHealth;
        enemyHealthTxt.text = enemyPlayer.totalPlayerHealth.Value.ToString() + " / " + enemyMaxHealth;

        SetShipOnHUD();
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

        SetShipOnHUD();
        
    }

}
