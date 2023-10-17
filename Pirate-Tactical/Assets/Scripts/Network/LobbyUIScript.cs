using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using static LobbyManager;

public class LobbyUIScript : MonoBehaviour
{
    public static LobbyUIScript Instance { get; private set; }

    [Header("CreateLobby")]
    [SerializeField] Button createLobbyBtt;
    [SerializeField] TMP_InputField lobbyNameIF;
    [SerializeField] Toggle publicLobby;

    [Header("SearchLobby")]
    [SerializeField] Button connectBtt;
    [SerializeField] Transform searchLobbiesContainer;
    [SerializeField] Button searchLobbyPrefab;

    [Header("JoinedLobby")]
    [SerializeField] TextMeshProUGUI joinedLobbyNameTxt;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }

    private void Start()
    {
        createLobbyBtt.onClick.AddListener(() => { CreateLobby(); });
        connectBtt.onClick.AddListener(() => { LobbyScript.Instance.SearchLobbies(); });
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
            btt.onClick.AddListener(() => { JoinLobby(lobby.LobbyCode); });
        }
    }


    public void CreateLobby()
    {
        LobbyScript.Instance.CreateLobby(lobbyNameIF.text, publicLobby);

        UpdateTextUI();
    }

    public void JoinLobby(string code)
    {
        LobbyScript.Instance.JoinLobbyByCode(code);
    }

    private void UpdateTextUI()
    {
        joinedLobbyNameTxt.text = lobbyNameIF.text;
        //publicPrivateText.text = publicLobby ? "Private" : "Public";
        //maxPlayersText.text = maxPlayers.ToString();
        //gameModeText.text = gameMode.ToString();
    }

}
