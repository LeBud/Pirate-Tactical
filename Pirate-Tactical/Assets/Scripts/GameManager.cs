using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public int player1unitLeft = 5;
    public int player2unitLeft = 5;

    public NetworkVariable<bool> gametesting = new NetworkVariable<bool>();

    public NetworkVariable<int> currentRound = new NetworkVariable<int>();

    public int startRoundCombatZone = 4;

    public enum GameState
    {
        GameStarting,
        Player1Turn,
        Player2Turn,
        GameFinish,
        GameTesting
    }
    //public GameState state;

    public NetworkVariable<GameState> gameState = new NetworkVariable<GameState>(0);

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (gameState.Value != GameState.GameTesting)
            gameState.Value = GameState.GameStarting;
    }


    private void Update()
    {
        if (!IsServer) return;

        if (gameState.Value == GameState.GameStarting && NetworkManager.ConnectedClients.Count >= 2)
        {
            UpdateGameStateServerRpc();
        }

        if (player1unitLeft == 0 || player2unitLeft == 0 && gameState.Value != GameState.GameFinish)
        {
            gameState.Value = GameState.GameFinish;
            UpdateGameStateServerRpc();
        }

        if(gameState.Value == GameState.GameTesting && NetworkManager.ConnectedClients.Count >= 1)
        {
            UpdateGameStateServerRpc();
        }
    }

    #region SetGameState

    [ServerRpc(RequireOwnership = false)]
    public void UpdateGameStateServerRpc()
    {
        if(gameState.Value == GameState.GameTesting)
        {
            gametesting.Value = true;
            foreach(var client in NetworkManager.ConnectedClients)
            {
                NetworkManager.ConnectedClients[client.Key].PlayerObject.GetComponent<Cursor>().canPlay.Value = true;
            }
            return;
        }

        NetworkManager.ConnectedClients[0].PlayerObject.GetComponent<Cursor>().canPlay.Value = false;
        NetworkManager.ConnectedClients[1].PlayerObject.GetComponent<Cursor>().canPlay.Value = false;

        if(gameState.Value == GameState.GameFinish)
        {
            Debug.Log("Game Finish");
        }
        else if (gameState.Value == GameState.GameStarting)
            gameState.Value = GameState.Player1Turn;
        else if(gameState.Value == GameState.Player1Turn)
            gameState.Value = GameState.Player2Turn;
        else if(gameState.Value == GameState.Player2Turn)
            gameState.Value = GameState.Player1Turn;

        GivePlayerActionServerRpc();
        HUD.Instance.SetGameStateClientRpc(SetGameStateString(gameState.Value), currentRound.Value);
        GridManager.Instance.UpdateTilesServerRpc();
    }

    string SetGameStateString(GameState newState)
    {
        string returnState = "";

        if (newState == GameState.GameTesting)
            returnState = "Game Testing";
        else if (newState == GameState.Player1Turn)
            returnState = "Player 1 Turn";
        else if (newState == GameState.Player2Turn)
            returnState = "Player 2 Turn";
        else if (newState == GameState.GameStarting)
            returnState = "Game is Starting";
        else if (newState == GameState.GameFinish)
            returnState = "Game is Finish";

        return returnState;
    }

    [ServerRpc(RequireOwnership = false)]
    void GivePlayerActionServerRpc()
    {
        if(!IsServer) return;

        //Setup pour que seulement le joueur puisse spawn ses unités puis l'autre joueur eznsuite
        if (gameState.Value == GameState.Player1Turn)
        {
            Cursor currentP = NetworkManager.ConnectedClients[0].PlayerObject.GetComponent<Cursor>();
            currentP.canPlay.Value = true;

            if (!currentP.unitManager.allShipSpawned.Value) return;
            currentP.ResetShipsActionClientRpc();
        }
        else if (gameState.Value == GameState.Player2Turn)
        {
            Cursor currentP = NetworkManager.ConnectedClients[1].PlayerObject.GetComponent<Cursor>();
            currentP.canPlay.Value = true;

            if (!currentP.unitManager.allShipSpawned.Value) return;
            currentP.ResetShipsActionClientRpc();
        }

        if(gameState.Value == GameState.Player1Turn)
            currentRound.Value++;
    }

    #endregion

    //Initialise Player
    [ServerRpc(RequireOwnership = false)]
    public void JoinServerServerRpc()
    {
        if (!IsOwner) return;
        Camera.main.transform.position = new Vector3((float)19 / 2 - 0.5f, (float)9 / 2 - 0.5f, -10);

        HUD.Instance.SetGameStateClientRpc(SetGameStateString(gameState.Value), currentRound.Value);
    }

}
