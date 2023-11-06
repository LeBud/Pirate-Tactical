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
    [SerializeField] TextMeshProUGUI gameStateTxt;
    [SerializeField] TextMeshProUGUI currentShipInfo;
    [SerializeField] Button endTurnBtt;
    [SerializeField] Slider specialSlider;
    public Slider playerSlider;
    public Slider enemyPlayerSlider;

    public TextMeshProUGUI playerName;
    public TextMeshProUGUI enemyPlayerName;
    public TextMeshProUGUI currentMode;

    public GameObject inGameHUD;

    Cursor player;
    Cursor enemyPlayer;

    public Transform shipsDisplay;
    public Transform shipHighlight;

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
            + "\nCan shoot : " + player.unitManager.ships[player.currentShipIndex].canShoot.Value
        }
        else
        {
            currentShipInfo.text = "ship selected : none ";
        }

        specialSlider.value = player.currentSpecialCharge;

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
        for(int i = 0; i<player.unitManager.ships.Length; i++)
        {
            shipsDisplay.GetChild(i).GetChild(3).GetComponent<TextMeshProUGUI>().text = player.unitManager.ships[i].unitLife.Value.ToString();
            shipsDisplay.GetChild(i).GetChild(4).GetComponent<TextMeshProUGUI>().text = player.unitManager.ships[i].damage.ToString();
        }
    }

    string GetCurrentMode(int i)
    {
        if (i == 0)
            return "Move unit";
        else if (i == 1)
            return "attack enemy";
        else if (i == 2)
            return "special unit tile";
        else if (i == 3)
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
            if (id == 0) playerName.text = GameManager.Instance.player1.Value.ToString();
            else if (id == 1) playerName.text = GameManager.Instance.player2.Value.ToString();
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
            if (id == 0) enemyPlayerName.text = GameManager.Instance.player1.Value.ToString();
            else if (id == 1) enemyPlayerName.text = GameManager.Instance.player2.Value.ToString();
        }

        SetShipOnHUD();
        
    }

}
