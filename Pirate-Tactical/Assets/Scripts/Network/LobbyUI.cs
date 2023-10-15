using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LobbyUI : MonoBehaviour
{
    [SerializeField] Button createLobbyBtt;

    [SerializeField] TMP_InputField lobbyName;
    [SerializeField] Toggle toggleLobby;


    private void Awake()
    {
        createLobbyBtt.onClick.AddListener(() =>
        {
            LobbyScript.Instance.CreateLobby(lobbyName.text, toggleLobby);
        });
    }
}
