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
            Debug.Log("Hosting");
            NetworkManager.Singleton.StartHost();
            HideNetCodeUI();
            MapManager.Instance.InitialiseServerRpc();
        });

        startClientBtt.onClick.AddListener(() =>
        {
            Debug.Log("Hosting");
            NetworkManager.Singleton.StartClient();
            HideNetCodeUI();
            MapManager.Instance.SetClientInstance();
        });
    }

    void HideNetCodeUI()
    {
        gameObject.SetActive(false);
    }

}
