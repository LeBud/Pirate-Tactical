using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class HUD : MonoBehaviour
{
    public static HUD Instance { get; private set; }
    [SerializeField] TextMeshProUGUI gameStateTxt;

    private void Awake()
    {
        if(Instance == null)
            Instance = this;
    }

    private void LateUpdate()
    {
    }

    public void SetGameState()
    {
        gameStateTxt.text = GameManager.Instance.state.ToString();
    }

}
