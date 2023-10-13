using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }

    public enum GameState
    {
        GameStarting,
        player1Turn,
        player2Turn,
        GameFinish
    }
    public GameState state;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        state = GameState.GameStarting;
    }

    private void Update()
    {
        if (!IsServer) return;

        if (state == GameState.GameStarting && NetworkManager.ConnectedClients.Count >= 2)
        {
            UpdateGameStateServerRpc();
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void UpdateGameStateServerRpc()
    {
        NetworkManager.ConnectedClients[0].PlayerObject.GetComponent<Cursor>().canPlay.Value = false;
        NetworkManager.ConnectedClients[1].PlayerObject.GetComponent<Cursor>().canPlay.Value = false;

        if (state == GameState.GameStarting)
            state = GameState.player1Turn;
        else if(state == GameState.player1Turn)
            state = GameState.player2Turn;
        else if(state == GameState.player2Turn)
            state = GameState.player1Turn;

        GivePlayerAction();
    }

    void GivePlayerAction()
    {
        if (state == GameState.player1Turn)
            NetworkManager.ConnectedClients[0].PlayerObject.GetComponent<Cursor>().canPlay.Value = true;
        else if (state == GameState.player2Turn)
            NetworkManager.ConnectedClients[1].PlayerObject.GetComponent<Cursor>().canPlay.Value = true;
    }

}
