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

    public GameObject inGameHUD;

    Cursor player;

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
            + "\nMode Index : " + GetCurrentMode(player.currentModeIndex);
        }
        else
        {
            currentShipInfo.text = "ship selected : none ";
        }
    }

    string GetCurrentMode(int i)
    {
        if (i == 0)
            return "Move unit";
        else if (i == 1)
            return "attack enemy";
        else if (i == 2)
            return "block a tile";

        return null;
    }

    [ClientRpc]
    public void SetGameStateClientRpc(string gameState, int round)
    {
        gameStateTxt.text = gameState + "\nround " + round;
        if(gameState == "Player 1 Turn" && player == null)
            player = NetworkManager.LocalClient.PlayerObject.GetComponent<Cursor>();
    }

    public void UpdateGameMode()
    {
        if(player != null && player.canPlay.Value)
            GameManager.Instance.UpdateGameStateServerRpc();
    }

}
