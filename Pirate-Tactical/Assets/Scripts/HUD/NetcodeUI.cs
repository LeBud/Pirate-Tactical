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
            //MapManager.Instance.InitialiseServerRpc();
            GridManager.Instance.GenerateGridServerRpc();
            HideNetCodeUI();
        });

        startClientBtt.onClick.AddListener(() =>
        {
            Debug.Log("Connecting...");
            NetworkManager.Singleton.StartClient();
            //MapManager.Instance.SetClientInstanceClientRpc();
            GridManager.Instance.JoinServerServerRpc();
            HideNetCodeUI();
        });
    }

    void HideNetCodeUI()
    {
        gameObject.SetActive(false);
    }

}
