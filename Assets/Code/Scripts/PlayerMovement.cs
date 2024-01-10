using System.Collections;
using System.Collections.Generic;
using System.Threading;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerMovement : NetworkBehaviour
{
    [SerializeField] private Rigidbody m_rigidbody;
    [SerializeField] private float m_speed;

    private Vector3 m_inputDirection;

    private void FixedUpdate()
    {
        if (!IsOwner) return;
        m_rigidbody.velocity = m_inputDirection * m_speed;
        //transform.position += m_inputDirection * m_speed * Time.fixedDeltaTime;
    }

    public void MoveAction(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        Vector2 direction = context.ReadValue<Vector2>();
        m_inputDirection = new Vector3(direction.x, 0, direction.y);
    }
}
