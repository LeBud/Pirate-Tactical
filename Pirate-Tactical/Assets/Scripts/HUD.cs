using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class HUD : NetworkBehaviour
{
    public static HUD Instance { get; private set; }
    [SerializeField] TextMeshProUGUI gameStateTxt;
    [SerializeField] TextMeshProUGUI currentShipInfo;

    Cursor player;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }

    private void LateUpdate()
    {
        if (player == null) return;
        if (player.shipSelected)
        {
            currentShipInfo.text = "ship selected : " + player.shipSelected + "\nCan move : " + player.unitManager.ships[player.currentShipIndex].canMove.Value 
            + "\nCan shoot : " + player.unitManager.ships[player.currentShipIndex].canShoot.Value;
        }
        else
        {
            currentShipInfo.text = "ship selected : " + player.shipSelected;
        }
    }

    [ClientRpc]
    public void SetGameStateClientRpc(string gameState)
    {
        gameStateTxt.text = gameState;
        if(gameState == "Player 1 Turn" && player == null)
            player = NetworkManager.LocalClient.PlayerObject.GetComponent<Cursor>();
    }

}
