using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    public static GameManager Instance { get; private set; }
    public int player1unitLeft = 5;
    public int player2unitLeft = 5;

    public NetworkVariable<bool> gametesting = new NetworkVariable<bool>();

    public NetworkVariable<int> currentRound = new NetworkVariable<int>();

    public NetworkVariable<Vector3> cameraPos = new NetworkVariable<Vector3>();

    public int startRoundCombatZone = 4;

    public NetworkVariable<FixedString128Bytes> player1 = new NetworkVariable<FixedString128Bytes>();
    public NetworkVariable<FixedString128Bytes> player2 = new NetworkVariable<FixedString128Bytes>();

    public enum GameState
    {
        GameStarting,
        Player1Turn,
        Player2Turn,
        GameFinish,
        GameTesting
    }

    public GameState gameState;

    private void Awake()
    {
        if (Instance == null)
            Instance = this;

        if (gameState != GameState.GameTesting)
            gameState = GameState.GameStarting;
    }


    private void Update()
    {
        if (!IsServer) return;

        if (gameState == GameState.GameStarting && NetworkManager.ConnectedClients.Count >= 2)
        {
            UpdateGameStateServerRpc();
            StartCoroutine(StartGame());
        }

        if (player1unitLeft == 0 || player2unitLeft == 0 && gameState != GameState.GameFinish)
        {
            gameState = GameState.GameFinish;
            UpdateGameStateServerRpc();
        }

        if(gameState == GameState.GameTesting && NetworkManager.ConnectedClients.Count >= 1)
        {
            UpdateGameStateServerRpc();
        }
    }

    IEnumerator StartGame()
    {
        Cursor[] players = FindObjectsOfType<Cursor>();
        foreach (Cursor player in players)
        {
            player.CalculateHealthClientRpc();
        }

        yield return new WaitForSeconds(.5f);

        SetUpGameBaseInfoServerRpc();
    }

    [ServerRpc]
    void SetUpGameBaseInfoServerRpc()
    {
        foreach (ulong id in NetworkManager.ConnectedClientsIds)
        {
            HUD.Instance.SetUIClientRpc(id);
        }

    }

    #region SetGameState

    [ServerRpc(RequireOwnership = false)]
    public void UpdateGameStateServerRpc()
    {
        if(!IsServer) return;

        if (gameState == GameState.GameTesting)
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

        if(gameState == GameState.GameFinish)
        {
            Debug.Log("Game Finish");
        }
        else if (gameState == GameState.GameStarting)
            gameState = GameState.Player1Turn;
        else if(gameState == GameState.Player1Turn)
            gameState = GameState.Player2Turn;
        else if(gameState == GameState.Player2Turn)
            gameState = GameState.Player1Turn;

        GivePlayerActionServerRpc();
        HUD.Instance.SetGameStateClientRpc(SetGameStateString(gameState), currentRound.Value);
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
        if (gameState == GameState.Player1Turn)
        {
            Cursor currentP = NetworkManager.ConnectedClients[0].PlayerObject.GetComponent<Cursor>();
            currentP.canPlay.Value = true;

            if (!currentP.unitManager.allShipSpawned.Value) return;
            currentP.ResetShipsActionClientRpc();
        }
        else if (gameState == GameState.Player2Turn)
        {
            Cursor currentP = NetworkManager.ConnectedClients[1].PlayerObject.GetComponent<Cursor>();
            currentP.canPlay.Value = true;

            if (!currentP.unitManager.allShipSpawned.Value) return;
            currentP.ResetShipsActionClientRpc();
        }

        if (gameState == GameState.Player1Turn)
        {
            currentRound.Value++;
            Cursor[] players = FindObjectsOfType<Cursor>();
            foreach(Cursor player in players)
            {
                player.RechargeSpecialClientRpc();
                player.CalculateHealthClientRpc();
            }

            ShipUnit[] ships = FindObjectsOfType<ShipUnit>();
            if(ships.Length > 0)
            {
                foreach (ShipUnit s in ships) 
                { 
                    s.UpdateUnitClientRpc();
                }
            }

            if (currentRound.Value >= startRoundCombatZone && currentRound.Value % 2 != 1 && GridManager.Instance.combatZoneSize.Value > 4)
                GridManager.Instance.combatZoneSize.Value--;
        }
    }

    #endregion

    //Initialise Player
    [ServerRpc(RequireOwnership = false)]
    public void JoinServerServerRpc()
    {
        if (!IsOwner) return;

        int width = GridManager.Instance._width;
        int height = GridManager.Instance._height;

        //Camera.main.transform.position = new Vector3((float)width / 2 - 0.5f, (float)height / 2 - 0.5f, -10);
        Camera.main.transform.position = cameraPos.Value;

        HUD.Instance.SetGameStateClientRpc(SetGameStateString(gameState), currentRound.Value);
    }

    [ServerRpc(RequireOwnership = false)]
    public void SendGameManagerNameServerRpc(FixedString128Bytes name, ulong id)
    {
        if (id == 0)
            player1.Value = name.Value;
        else
            player2.Value = name.Value;
    }

}
