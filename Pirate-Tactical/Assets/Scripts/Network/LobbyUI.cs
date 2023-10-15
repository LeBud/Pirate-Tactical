using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [Header("Create Lobby")]
    [SerializeField] Button createLobbyBtt;
    [SerializeField] TMP_InputField lobbyName;
    [SerializeField] Toggle toggleLobby;

    [Header("Search")]
    [SerializeField] Button refreshLobbyBtt;
    [SerializeField] Button lobbyPrefabsBtt;

    [Header("Current Lobby")]
    [SerializeField] TextMeshProUGUI joinedLobbyNameTxt;
    [SerializeField] TextMeshProUGUI gameModeLobbyTxt;
    [SerializeField] GameObject playerConnectedPrefabs;


    private void Awake()
    {
        createLobbyBtt.onClick.AddListener(() =>
        {
            LobbyScript.Instance.CreateLobby(lobbyName.text, toggleLobby);
        });

        refreshLobbyBtt.onClick.AddListener(() => 
        { 
            LobbyScript.Instance.SearchLobbies(); 
        });
    }

}
