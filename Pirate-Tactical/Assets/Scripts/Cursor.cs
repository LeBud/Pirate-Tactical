using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Cursor : NetworkBehaviour
{
    void Update()
    {
        if(!IsOwner) return;

        Vector3 mousePos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        Vector2 pos = new Vector2(mousePos.x, mousePos.y);
        transform.position = new Vector3(pos.x, pos.y, -5);
    }
}
