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

    public enum GameState
    {
        GameStarting,
        Player1Turn,
        Player2Turn,
        GameFinish,
        GameTesting
    }
    public GameState state;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (state != GameState.GameTesting)
            state = GameState.GameStarting;
    }

    private void Start()
    {
        
    }

    private void Update()
    {
        if (!IsServer) return;

        if (state == GameState.GameStarting && NetworkManager.ConnectedClients.Count >= 2)
        {
            UpdateGameStateServerRpc();
        }

        if (player1unitLeft == 0 || player2unitLeft == 0 && state != GameState.GameFinish)
        {
            state = GameState.GameFinish;
            UpdateGameStateServerRpc();
        }

        if(state == GameState.GameTesting && NetworkManager.ConnectedClients.Count >= 1)
        {
            UpdateGameStateServerRpc();
        }
    }

    #region SetGameState

    [ServerRpc(RequireOwnership = false)]
    public void UpdateGameStateServerRpc()
    {
        if(state == GameState.GameTesting)
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

        if(state == GameState.GameFinish)
        {
            Debug.Log("Game Finish");
        }
        else if (state == GameState.GameStarting)
            state = GameState.Player1Turn;
        else if(state == GameState.Player1Turn)
            state = GameState.Player2Turn;
        else if(state == GameState.Player2Turn)
            state = GameState.Player1Turn;

        GivePlayerActionServerRpc();
        HUD.Instance.SetGameStateClientRpc(SetGameStateString(state), currentRound.Value);
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
        if (state == GameState.Player1Turn)
        {
            Cursor currentP = NetworkManager.ConnectedClients[0].PlayerObject.GetComponent<Cursor>();
            currentP.canPlay.Value = true;

            if (!currentP.unitManager.allShipSpawned.Value) return;
            currentP.ResetShipsActionClientRpc();
            currentP.TotalActionPoint();
            currentRound.Value++;
        }
        else if (state == GameState.Player2Turn)
        {
            Cursor currentP = NetworkManager.ConnectedClients[1].PlayerObject.GetComponent<Cursor>();
            currentP.canPlay.Value = true;

            if (!currentP.unitManager.allShipSpawned.Value) return;
            currentP.ResetShipsActionClientRpc();
            currentP.TotalActionPoint();
        }
    }

    #endregion

    //Initialise Player
    [ServerRpc(RequireOwnership = false)]
    public void JoinServerServerRpc()
    {
        if (!IsOwner) return;
        Camera.main.transform.position = new Vector3((float)19 / 2 - 0.5f, (float)9 / 2 - 0.5f, -10);

        HUD.Instance.SetGameStateClientRpc(SetGameStateString(state), currentRound.Value);
    }

}
