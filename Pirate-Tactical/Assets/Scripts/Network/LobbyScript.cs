using System.Collections;
using System.Collections.Generic;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbyScript : MonoBehaviour
{

    Lobby hostLobby;
    Lobby joinedLobby;
    float heartbeatTimer;
    float lobbyUpdateTimer;
    string playerName = "Player 1";
    private async void Start()
    {
        await UnityServices.InitializeAsync(); //Permet d'envoyer aux services unity la requête d'un serveur

        AuthenticationService.Instance.SignedIn += () =>
        {
            Debug.Log("Signed in " + AuthenticationService.Instance.PlayerId);
        };

        await AuthenticationService.Instance.SignInAnonymouslyAsync(); //Permet aux joueur de jouer de façon anonyme sans compte
    }

    private void Update()
    {
        HandleLobbyHeartBeat();
        HandleLobbyCallForUpdate();
    }

    async void HandleLobbyHeartBeat()
    {
        if(hostLobby != null)
        {
            heartbeatTimer -= Time.deltaTime;
            if (heartbeatTimer < 0)
            {
                float maxHeartbeatTimer = 15;
                heartbeatTimer = maxHeartbeatTimer;

                await LobbyService.Instance.SendHeartbeatPingAsync(hostLobby.Id);
            }
        }
    }

    async void HandleLobbyCallForUpdate()
    {
        if (joinedLobby != null)
        {
            lobbyUpdateTimer -= Time.deltaTime;
            if (lobbyUpdateTimer < 0)
            {
                float lobbyTimer = 1.1f;
                lobbyUpdateTimer = lobbyTimer;

                Lobby lobby = await LobbyService.Instance.GetLobbyAsync(joinedLobby.Id);
                joinedLobby = lobby;
            }
        }

    }

    #region CreateLobby
    private async void CreateLobby()
    {
        //Actuellement toutes les valeurs du lobby (options/gamemode) sont attribuer lorsque le lobby est créer
        try
        {
            string lobbyNamme = "MyLobby";
            int maxPlayers = 2;
            //Set parameters to the lobby like set it private
            CreateLobbyOptions createOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Player = GetPlayer(),
                Data = new Dictionary<string, DataObject>
                {
                    {"GameMode", new DataObject(DataObject.VisibilityOptions.Public, "TestMode") },
                    {"Map", new DataObject(DataObject.VisibilityOptions.Public, "testMap") }
                }
            };

            Lobby lobby = await LobbyService.Instance.CreateLobbyAsync(lobbyNamme, maxPlayers, createOptions);

            hostLobby = lobby;
            joinedLobby = hostLobby;

            Debug.Log("Created lobby ! " + lobby.Name + " | Max players " + lobby.MaxPlayers + " | ID " + lobby.Id + " | Lobby code" + lobby.LobbyCode);

            PrintPlayers(hostLobby);
        }
        catch(LobbyServiceException e)
        {
            Debug.Log(e);
        }
    }

    #endregion

    #region SearchLobby
    private async void SearchLobbies()
    {
        try
        {
            //Search lobby with filters
            QueryLobbiesOptions searchOptions = new QueryLobbiesOptions
            {
                Count = 25,
                Filters = new List<QueryFilter>
                {
                    new QueryFilter(QueryFilter.FieldOptions.AvailableSlots, "0", QueryFilter.OpOptions.GT),
                    //new QueryFilter(QueryFilter.FieldOptions.S1, "TestMode", QueryFilter.OpOptions.EQ) // Search party via gamemode
                } ,
                Order = new List<QueryOrder>
                {
                    new QueryOrder(false, QueryOrder.FieldOptions.Created)
                }
            };

            QueryResponse query = await Lobbies.Instance.QueryLobbiesAsync(searchOptions);

            Debug.Log("Lobbies found : " + query.Results.Count);
            foreach (var result in query.Results) 
            {
                Debug.Log(result.Name + " " + result.MaxPlayers + " " + result.Data["GameMode"].Value);
            }
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    #endregion

    #region JoinLobby

    async void JoinLobbyByCode(string lobbyCode)
    {
        try
        {
            JoinLobbyByCodeOptions joinOption = new JoinLobbyByCodeOptions
            {
                Player = GetPlayer()
            };
            Lobby lobby = await Lobbies.Instance.JoinLobbyByCodeAsync(lobbyCode, joinOption);
            joinedLobby = lobby;

            Debug.Log("joined lobby with code " + lobbyCode);

            PrintPlayers(lobby);

        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

    async void QuickJoinLobby()
    {
        //Join a lobby public lobby without entering lobby code or id
        try
        {
            await LobbyService.Instance.QuickJoinLobbyAsync();
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }

    #endregion

    #region UpdateLobby
    async void UpdateLobby(string gameMode)
    {
        try
        {
            hostLobby = await Lobbies.Instance.UpdateLobbyAsync(hostLobby.Id, new UpdateLobbyOptions
            {
                Data = new Dictionary<string, DataObject>
                {
                    {"GameMode", new DataObject(DataObject.VisibilityOptions.Public, gameMode) }
                }
            });
            joinedLobby = hostLobby;
            PrintPlayers(hostLobby);
        }
        catch (LobbyServiceException e)
        {
            Debug.Log(e);
        }

    }
    #endregion

    void PrintPlayers()
    {
        PrintPlayers(joinedLobby);
    }

    void PrintPlayers(Lobby lobby)
    {
        Debug.Log("Players in lobby : " + lobby.Name + " " + lobby.Data["GameMode"].Value + " " + lobby.Data["Map"].Value);
        foreach(Player player in lobby.Players)
        {
            Debug.Log(player.Id + " " + player.Data["PlayerName"].Value);
        }
    }

    Player GetPlayer()
    {
        return new Player
        {
            Data = new Dictionary<string, PlayerDataObject>
                    {
                        { "PlayerName", new PlayerDataObject(PlayerDataObject.VisibilityOptions.Public, playerName) }
                    }
        };
    }

}
