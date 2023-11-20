using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HandleUpgradeSystem : MonoBehaviour
{
    
    public static HandleUpgradeSystem Instance { get; private set; }


    private void Awake()
    {
        if(Instance == null) 
            Instance = this;
    }


    public void UpgradeUnit(UpgradeSystem upgrade)
    {

    }

}
