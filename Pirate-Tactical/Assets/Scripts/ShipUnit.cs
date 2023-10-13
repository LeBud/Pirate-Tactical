using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShipUnit : NetworkBehaviour
{
    public float shipSpeed = .0015f;

    public NetworkVariable<int> unitLife = new NetworkVariable<int>(10);
    public NetworkVariable<Vector2> unitPos = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);


    private void Update()
    {
        unitPos.OnValueChanged += (Vector2 previousPos, Vector2 newPos) => { StartCoroutine(MoveShip()); };
    }

    IEnumerator MoveShip()
    {
        transform.position = new Vector3(unitPos.Value.x, unitPos.Value.y, -1);
        yield return null;
    }

    public void TakeDamage(int dmg)
    {
        unitLife.Value -= dmg;
    }
}

