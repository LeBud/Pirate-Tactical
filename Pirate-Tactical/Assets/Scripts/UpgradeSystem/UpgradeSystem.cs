using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class UpgradeSystem : ScriptableObject
{
    public enum UpgradeType { None, Damage, MoveRange, ShootRange, Accost, TotalMana, ManaGain, ShootCapacity, TileCapacity}

    [Header("Infos")]
    public string upName;
    public string upDescription;

    [Header("Upgrade")]
    public UpgradeType upgradeType;
    public int value;
    public int goldCost;

    [Header("New Capacity")]
    public ShipUnit.UnitSpecialShot newShootCapacity;
    public ShipUnit.UnitSpecialTile newTileCapacity;
}
