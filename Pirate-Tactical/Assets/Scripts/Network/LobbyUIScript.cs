using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIScript : MonoBehaviour
{
    public static LobbyUIScript Instance { get; private set; }

    [SerializeField] TMP_InputField playerName;
    [SerializeField] GameObject lobbyUI;

    [Header("CreateLobby")]
    [SerializeField] Button createLobbyBtt;
    [SerializeField] TMP_InputField lobbyNameIF;
    //[SerializeField] Toggle privateLobby;

    [Header("SearchLobby")]
    [SerializeField] Button connectBtt;
    [SerializeField] Button refreshBtt;
    [SerializeField] Transform searchLobbiesContainer;
    [SerializeField] Button searchLobbyPrefab;
    [SerializeField] GameObject searchLobbyObject;

    [Header("JoinedLobby")]
    [SerializeField] TextMeshProUGUI joinedLobbyNameTxt;
    [SerializeField] GameObject joinedLobbyObject;
    [SerializeField] GameObject playersInLobbyPref;
    [SerializeField] Transform playersInLobbyContainer;
    [SerializeField] Button leaveBtt;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }

    private void Start()
    {
        createLobbyBtt.onClick.AddListener(() => { CreateLobby(); });
        connectBtt.onClick.AddListener(() => { LobbyScript.Instance.SearchLobbies(); });
        refreshBtt.onClick.AddListener(() => { LobbyScript.Instance.SearchLobbies(); });
        leaveBtt.onClick.AddListener(() => { LobbyScript.Instance.LeaveLobby(); });
        playerName.onValueChanged.AddListener(delegate { LobbyScript.Instance.playerName = playerName.text; });
    }

    void Authenticate()
    {
        LobbyScript.Instance.UpdatePlayerName(playerName.text);
    }

    public void AddSearchLobbies(QueryResponse query)
    {
        foreach(Transform t in searchLobbiesContainer)
        {
            Destroy(t.gameObject);
        }

        foreach(var lobby in query.Results)
        {
            Button btt = Instantiate(searchLobbyPrefab, searchLobbiesContainer);
            btt.GetComponentInChildren<TextMeshProUGUI>().text = lobby.Name + " | " + lobby.Players.Count + " joueur(s) connecté(s)";
            btt.onClick.AddListener(() => { JoinLobby(lobby.Id); });
        }
    }


    public void CreateLobby()
    {
        LobbyScript.Instance.CreateLobby(lobbyNameIF.text, false);
    }

    public void JoinLobby(string code)
    {
        LobbyScript.Instance.JoinLobbyById(code);
        searchLobbyObject.SetActive(false);
        joinedLobbyObject.SetActive(true);
    }

    public void UpdateTextUI()
    {
        joinedLobbyNameTxt.text = LobbyScript.Instance.joinedLobby.Name;
        //publicPrivateText.text = publicLobby ? "Private" : "Public";
        //maxPlayersText.text = maxPlayers.ToString();
        //gameModeText.text = gameMode.ToString();

        foreach (Transform t in playersInLobbyContainer)
            Destroy(t.gameObject);

        foreach(var player in LobbyScript.Instance.joinedLobby.Players)
        {
            var p = Instantiate(playersInLobbyPref, playersInLobbyContainer);
            p.GetComponentInChildren<TextMeshProUGUI>().text = player.Data["PlayerName"].Value.ToString();
        }
    }

    public void HideUI()
    {
        lobbyUI.SetActive(false);
    }
}
