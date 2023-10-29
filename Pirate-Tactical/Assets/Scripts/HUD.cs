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
            currentShipInfo.text = "ship selected : " + player.shipSelected + "\nCan move : " + player.unitManager.ships[player.currentShipIndex].canMove.Value 
            + "\nCan shoot : " + player.unitManager.ships[player.currentShipIndex].canShoot.Value
            + "\nMode Index : " + player.currentModeIndex;
        }
        else
        {
            currentShipInfo.text = "ship selected : " + player.shipSelected;
        }
    }

    [ClientRpc]
    public void SetGameStateClientRpc(string gameState, int round)
    {
        gameStateTxt.text = gameState + " round : " + round;
        if(gameState == "Player 1 Turn" && player == null)
            player = NetworkManager.LocalClient.PlayerObject.GetComponent<Cursor>();
    }

    public void UpdateGameMode()
    {
        if(player != null && player.canPlay.Value)
            GameManager.Instance.UpdateGameStateServerRpc();
    }

}
