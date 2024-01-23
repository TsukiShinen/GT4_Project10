using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace Unity.FPS.Game
{
    public abstract class ProjectileBase : NetworkBehaviour
    {
        public GameObject Owner { get; private set; }
        public Vector3 InitialPosition { get; private set; }
        public Vector3 InitialDirection { get; private set; }
        public Vector3 InheritedMuzzleVelocity { get; private set; }
        public float InitialCharge { get; private set; }
        public ulong OwnerId { get; private set; }

        public UnityAction OnShoot;

        public void Shoot(GameObject owner, ulong ownerId)
        {
            Owner = owner;
            OwnerId = ownerId;
            InitialPosition = transform.position;
            InitialDirection = transform.forward;

            OnShoot?.Invoke();
        }
    }
}