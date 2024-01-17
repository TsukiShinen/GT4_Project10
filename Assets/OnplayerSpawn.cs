using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class OnplayerSpawn : NetworkBehaviour
{
    [Header("If Not Owner")]
    [SerializeField] private List<Component> m_Components = new List<Component>();
    [SerializeField] private List<GameObject> m_GameObjects = new List<GameObject>();

    [Header("If Owner")]
    [SerializeField] private List<GameObject> m_GameObjectsOwner = new List<GameObject>();

    private void Start()
    {
        if (!IsOwner)
        {
            //foreach(var component in m_Components)
            //{
            //    Destroy(component);
            //}

            foreach (var gameObject in m_GameObjects)
            {
                Destroy(gameObject);
            }
        }
        else
        {
            foreach (var gameObject in m_GameObjectsOwner)
            {
                Destroy(gameObject);
            }
        }
    }
}
