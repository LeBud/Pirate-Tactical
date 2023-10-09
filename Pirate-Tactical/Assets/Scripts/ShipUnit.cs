using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ShipUnit : NetworkBehaviour
{
    public float shipSpeed = .0015f;

    public NetworkVariable<Vector2> unitPos = new NetworkVariable<Vector2>(Vector2.zero, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    private void Update()
    {
        unitPos.OnValueChanged += (Vector2 previousPos, Vector2 newPos) => { StartCoroutine(MoveShip()); };
    }

    IEnumerator MoveShip()
    {
        /*while(Mathf.Abs(Vector2.Distance(unitPos.Value, transform.position)) > .1f)
        {
            transform.position = Vector3.MoveTowards(transform.position, new Vector3(unitPos.Value.x, unitPos.Value.y, -1), shipSpeed * Time.deltaTime);
            yield return null;
        }*/
        transform.position = new Vector3(unitPos.Value.x, unitPos.Value.y, -1);
        yield return null;
    }

}

