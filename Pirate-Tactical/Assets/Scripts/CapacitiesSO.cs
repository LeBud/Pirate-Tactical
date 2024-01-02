using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CapacitiesSO : ScriptableObject
{
    public string capacityName;
    [TextArea]
    public string capacityDescription;
    public Sprite icon;

    [Header("Capacity Cost")]
    public int specialAbilityCost;

    [Header("Shoot Capacity")]
    public ShipUnit.UnitSpecialShot shootCapacity;
    public int specialShootRange;
    public int specialAbilityDamage;

    public bool specialPassifDamage;
    public int shootPassiveDuration;

    [Header("Tile Capacity")]
    public ShipUnit.UnitSpecialTile tileCapacity;
    public int specialTileRange;

    public bool specialPassifTile;
    public int tilePassiveDuration;

}
