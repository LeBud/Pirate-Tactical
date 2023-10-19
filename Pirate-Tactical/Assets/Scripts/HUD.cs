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

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }

    private void LateUpdate()
    {

    }

    [ClientRpc]
    public void SetGameStateClientRpc(string gameState)
    {
        gameStateTxt.text = gameState;
    }

}
