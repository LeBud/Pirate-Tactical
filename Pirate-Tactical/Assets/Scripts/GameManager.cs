using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public bool combatZoneShrinkEveryRound = false;
    bool poolingStarted = false;

    public bool spawnShipAnyWhere = false;
    public bool canStartGame = false;

    public Cursor[] players = new Cursor[2];

    public GameObject endGameAnnounce;
    public TextMeshProUGUI endGameTxt;

    public enum GameState
    {
        GameStarting,
        selectingShips,
        Player1Turn,
        Player2Turn,
        GameFinish,
        GameTesting
    }

    public GameState gameState;

    public GameObject leavePanel;

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

        if (canStartGame && gameState == GameState.selectingShips)
        {
            if (players[0].isReady.Value && players[1].isReady.Value)
            {
                canStartGame = false;
                StartGameWhenReady();
            }
        }

        if (gameState == GameState.GameStarting && NetworkManager.ConnectedClients.Count >= 2)
        {
            SelectShipCapacityHUD.Instance.SetPlayerToClientRpc();
            players[0] = NetworkManager.ConnectedClients[0].PlayerObject.GetComponent<Cursor>();
            players[1] = NetworkManager.ConnectedClients[1].PlayerObject.GetComponent<Cursor>();
            canStartGame = true;
            gameState = GameState.selectingShips;
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

    public void StartGameWhenReady()
    {
        SelectShipCapacityHUD.Instance.CloseWindowClientRpc();
        UpdateGameStateServerRpc();
        StartCoroutine(StartGame());
    }

    IEnumerator StartGame()
    {
        Cursor[] players = FindObjectsOfType<Cursor>();
        foreach (Cursor player in players)
        {
            player.CalculateHealthClientRpc();
        }

        if (currentRound.Value > 0)
            HUD.Instance.UpdateHealthBarClientRpc();

        yield return new WaitForSeconds(.5f);

        SetUpGameBaseInfoServerRpc();
        HandleUpgradeSystem.Instance.GenerateUpgradeOnServerRpc();
        SoundManager.Instance.PlaySoundOnClients(SoundManager.Instance.gameStarting.id);
    }

    IEnumerator PoolingInfosToUpdate()
    {
        if (!IsServer) yield break;

        poolingStarted = true;
        Cursor[] players = FindObjectsOfType<Cursor>();

        while (true)
        {
            for (int i = 0; i < 2; i++)
            {
                for (int x = 0; x < players[i].unitManager.ships.Length; x++)
                {
                    if (players[i].unitManager.ships[x] != null)
                    {
                        int life = players[i].unitManager.ships[x].unitLife.Value;
                        float percent = (float)life / players[i].unitManager.ships[x].maxHealth;
                        players[i].unitManager.ships[x].SetHealthBarClientRpc(percent, life);
                    }
                }
                players[i].CalculateHealthClientRpc();
            }

            HUD.Instance.UpdateHealthBarClientRpc();

            yield return new WaitForSeconds(1);
            if(gameState == GameState.GameFinish)
                yield break;
        }
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

        players[0].canPlay.Value = false;
        players[1].canPlay.Value = false;

        switch (gameState)
        {
            case GameState.GameFinish:
                FinishGameOnServerRpc();
                return;
            case GameState.GameStarting:
                gameState = GameState.selectingShips;
                break;
            case GameState.Player1Turn:
                gameState = GameState.Player2Turn;
                break;
            case GameState.Player2Turn:
                gameState = GameState.Player1Turn;
                break;
            case GameState.selectingShips:
                gameState = GameState.Player1Turn;
                break;
        }

        GivePlayerActionServerRpc();
        HUD.Instance.SetGameStateClientRpc(SetGameStateString(gameState), currentRound.Value);
        GridManager.Instance.UpdateTilesServerRpc();
    }

    string SetGameStateString(GameState newState)
    {
        string returnState = "";

        switch (newState)
        {
            case GameState.GameFinish:
                returnState = "Partie fini";
                break;
            case GameState.GameStarting:
                returnState = "La partie commence";
                break;
            case GameState.Player1Turn:
                returnState = "Au tour de : " + player1.Value;
                break;
            case GameState.Player2Turn:
                returnState = "Au tour de : " + player2.Value;
                break;
            case GameState.GameTesting:
                returnState = "Game Testing";
                break;
            case GameState.selectingShips:
                returnState = "S�lectionnez capacit�s";
                break;
        }

        return returnState;
    }

    [ServerRpc(RequireOwnership = false)]
    void GivePlayerActionServerRpc()
    {
        if(!IsServer) return;

        if (gameState == GameState.Player1Turn || gameState == GameState.Player2Turn)
        {
            ShipUnit[] ships = FindObjectsOfType<ShipUnit>();
            if (ships.Length > 0)
            {
                foreach (ShipUnit s in ships)
                {
                    s.ZoneDamageClientRpc();
                }
            }
        }

        if (gameState == GameState.Player1Turn)
        {
            currentRound.Value++;
            if(currentRound.Value == 3)
                HUD.Instance.DisplayStormClientRpc();

            if (currentRound.Value > 1)
                HUD.Instance.DisplayGoldManaGainClientRpc();

            for(int i = 0; i < 2; i++)
            {
                players[i].RechargeSpecialClientRpc();
                players[i].CalculateHealthClientRpc();
                if(currentRound.Value > 1)
                    players[i].GoldGainClientRpc();

                if (players[i].unitManager.allShipSpawned.Value)
                {
                    for(int x = 0; x < players[i].unitManager.ships.Length; x++)
                    {
                        if(players[i].unitManager.ships[x] != null)
                            players[i].unitManager.ships[x].UpdateUnitClientRpc();
                    }
                }
            }

            if(currentRound.Value > 0)
                HUD.Instance.UpdateHealthBarClientRpc();

            if (currentRound.Value >= startRoundCombatZone && currentRound.Value % 2 != 1 && GridManager.Instance.combatZoneSize.Value > 4 && !combatZoneShrinkEveryRound)
                GridManager.Instance.combatZoneSize.Value--;
            else if (currentRound.Value >= startRoundCombatZone && GridManager.Instance.combatZoneSize.Value > 4 && combatZoneShrinkEveryRound)
                GridManager.Instance.combatZoneSize.Value--;

            Shipwrek[] wreck = FindObjectsOfType<Shipwrek>();
            if(wreck.Length > 0)
            {
                foreach (var w in wreck)
                    w.CheckForRoundToDisappearServerRpc();
            }

            if (currentRound.Value > 0 && !poolingStarted)
                PoolingInfosToUpdate();
        }

        GridManager.Instance.CheckAllShip();

        //Setup pour que seulement le joueur puisse spawn ses unit�s puis l'autre joueur eznsuite
        if (gameState == GameState.Player1Turn)
        {
            players[0].canPlay.Value = true;

            if (currentRound.Value == 0)
                players[0].SetSpawnableTileClientRpc(0);

            if (players[0].unitManager.allShipSpawned.Value)
                players[0].ResetShipsActionClientRpc();

            HUD.Instance.PlayerTurnClientRpc(player1.Value);
        }
        else if (gameState == GameState.Player2Turn)
        {
            players[1].canPlay.Value = true;

            if (currentRound.Value == 0)
                players[1].SetSpawnableTileClientRpc(1);

            if (players[1].unitManager.allShipSpawned.Value)
                players[1].ResetShipsActionClientRpc();

            HUD.Instance.PlayerTurnClientRpc(player2.Value);
        }
    }

    #endregion

    [ServerRpc]
    public void FinishGameOnServerRpc()
    {

        for (int i = 0; i < 2; i++)
        {
            players[i].RechargeSpecialClientRpc();
            players[i].CalculateHealthClientRpc();
            if (currentRound.Value > 1)
                players[i].GoldGainClientRpc();

            for (int x = 0; x < players[i].unitManager.ships.Length; x++)
            {
                if (players[i].unitManager.ships[x] != null)
                    players[i].unitManager.ships[x].UpdateUnitClientRpc();
            }
        }

        if (currentRound.Value > 0)
            HUD.Instance.UpdateHealthBarClientRpc();

        string winner = "";

        if (player1unitLeft == 0 && player2unitLeft == 0)
            winner = "�galit�";
        else if (player1unitLeft == 0 && player2unitLeft > 0)
            winner = player2.Value.ToString() + " � gagner";
        else if (player2unitLeft == 0 && player1unitLeft > 0)
            winner = player1.Value.ToString() + " � gagner";

        HUD.Instance.SetGameStateClientRpc(winner, currentRound.Value);
        EndGameOnClientRpc(winner);
    }

    [ClientRpc]
    void EndGameOnClientRpc(FixedString128Bytes p)
    {
        StartCoroutine(EndGame(p.ToString()));
    }

    IEnumerator EndGame(string p)
    {
        endGameAnnounce.SetActive(true);
        endGameTxt.text = p;
        SoundManager.Instance.PlaySoundLocally(SoundManager.Instance.endGame);
        yield return new WaitForSeconds(10);
        NetworkManager.Shutdown();
        StartCoroutine(ResetHUD());
    }

    //Initialise Player
    [ServerRpc(RequireOwnership = false)]
    public void JoinServerServerRpc()
    {
        if (!IsOwner) return;

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

    public void QuitGame()
    {
        if (IsServer)
            StartCoroutine(DisconnectPlayer(false));

        if (IsClient)
        {
            StartCoroutine(DisconnectPlayer(true));
            /*NetworkManager.Shutdown();
            StartCoroutine(ResetHUD());*/
        }
    }

    [ServerRpc(RequireOwnership = false)]
    void LeaveOnServerRpc()
    {
        StartCoroutine(DisconnectPlayer(false));
    }

    IEnumerator DisconnectPlayer(bool client)
    {
        if (client)
            LeaveOnServerRpc();

        if(IsServer)
            SendLeaveToClientRpc();

        StartCoroutine(ResetHUD());
        NetworkManager.DisconnectClient(1);
        
        yield return new WaitForSeconds(3);

        NetworkManager.Shutdown();
    }

    [ClientRpc]
    public void SendLeaveToClientRpc()
    {
        leavePanel.SetActive(true);
        StartCoroutine(ResetHUD());
    }

    IEnumerator ResetHUD()
    {
        gameState = GameState.GameStarting;
        foreach (var t in HUD.Instance.lobbyScreen)
            t.SetActive(false);

        SelectShipCapacityHUD.Instance.lobbyHUD.SetActive(true);
        HUD.Instance.inGameHUD.SetActive(false);
        HUD.Instance.pausePanel.SetActive(false);
        HUD.Instance.inPauseMenu = false;
        HUD.Instance.lobbyMenu.SetActive(true);
        HUD.Instance.startBtt.interactable = true;
        HUD.Instance.startBtt.gameObject.SetActive(false);

        yield return new WaitForSeconds(3);
        
        leavePanel.SetActive(false);
    }

    public void CloseGame()
    {
        Application.Quit();
    }
}
