using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class UpgradeSystem : ScriptableObject
{
    public enum UpgradeType { None, Damage, MoveRange, ShootRange, Accost, TotalMana, ManaGain, Capacity}

    [Header("Infos")]
    public string upName;
    [TextArea]
    public string upDescription;

    [Header("Upgrade")]
    public UpgradeType upgradeType;
    public int value;
    public int goldCost;
}
