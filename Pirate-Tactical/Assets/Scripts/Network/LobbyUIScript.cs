using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUIScript : MonoBehaviour
{
    public static LobbyUIScript Instance { get; private set; }

    [Header("CreateLobby")]
    [SerializeField] Button createLobbyBtt;
    [SerializeField] TMP_InputField lobbyNameIF;
    [SerializeField] Toggle publicLobby;

    [Header("SearchLobby")]
    [SerializeField] Transform searchLobbiesContainer;
    [SerializeField] Button searchLobbyPrefab;

    [Header("JoinedLobby")]
    [SerializeField] TextMeshProUGUI joinedLobbyNameTxt;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }

    public void AddSearchLobbies(QueryResponse query)
    {
        foreach(Transform t in searchLobbiesContainer)
        {
            Destroy(t.gameObject);
        }

        foreach(var lobby in query.Results)
        {
            Button btt = Instantiate(searchLobbyPrefab);
            btt.onClick.AddListener(() => { JoinLobby(lobby.LobbyCode); });
        }
    }


    public void CreateLobby()
    {
        LobbyScript.Instance.CreateLobby(lobbyNameIF.text, publicLobby);
        joinedLobbyNameTxt.text = LobbyScript.Instance.joinedLobby.Name;
    }

    public void JoinLobby(string code)
    {
        LobbyScript.Instance.JoinLobbyByCode(code);
    }
}
