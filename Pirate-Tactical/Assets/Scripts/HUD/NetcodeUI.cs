using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class NetcodeUI : MonoBehaviour
{

    [SerializeField] Button startHostBtt;
    [SerializeField] Button startClientBtt;

    private void Awake()
    {
        startHostBtt.onClick.AddListener(() =>
        {
            Debug.Log("Hosting...");
            NetworkManager.Singleton.StartHost();
            GridManager.Instance.GenerateGridOnTileMapServerRpc();
            HUD.Instance.inGameHUD.SetActive(true);
            HideNetCodeUI();
        });

        startClientBtt.onClick.AddListener(() =>
        {
            Debug.Log("Connecting...");
            NetworkManager.Singleton.StartClient();
            HUD.Instance.inGameHUD.SetActive(true);
            HideNetCodeUI();
        });
    }

    void HideNetCodeUI()
    {
        gameObject.SetActive(false);
    }

}
