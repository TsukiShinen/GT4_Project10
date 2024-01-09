using System.Collections.Generic;
using Unity.FPS.Game;
using UnityEngine;

namespace Unity.FPS.Gameplay
{
    public class ProjectileHitscan : ProjectileBase
    {
        [Header("General")]
        [Tooltip("Length of hitscan ray")]
        public float Range = 100f;

        [Tooltip("VFX prefab to spawn upon impact")]
        public GameObject ImpactVfx;

        [Tooltip("LifeTime of the VFX before being destroyed")]
        public float ImpactVfxLifetime = 5f;

        [Tooltip("Offset along the hit normal where the VFX will be spawned")]
        public float ImpactVfxSpawnOffset = 0.1f;

        [Tooltip("Clip to play on impact")]
        public AudioClip ImpactSfxClip;

        [Tooltip("Layers this hitscan can collide with")]
        public LayerMask HittableLayers = -1;

        [Header("Damage")]
        [Tooltip("Damage of the projectile")]
        public float Damage = 40f;

        ProjectileBase m_ProjectileBase;
        List<Collider> m_IgnoredColliders;

        const QueryTriggerInteraction k_TriggerInteraction = QueryTriggerInteraction.Collide;

        void OnEnable()
        {
            m_ProjectileBase = GetComponent<ProjectileBase>();
            DebugUtility.HandleErrorIfNullGetComponent<ProjectileBase, ProjectileStandard>(m_ProjectileBase, this,
                gameObject);
            m_ProjectileBase.OnShoot += OnShoot;
            Destroy(gameObject, Range / InheritedMuzzleVelocity.magnitude);
        }

        new void OnShoot()
        {
            m_IgnoredColliders = new List<Collider>();

            // Ignore colliders of owner
            Collider[] ownerColliders = m_ProjectileBase.Owner.GetComponentsInChildren<Collider>();
            m_IgnoredColliders.AddRange(ownerColliders);

            Vector3 rayOrigin = m_ProjectileBase.InitialPosition;
            Vector3 rayDirection = m_ProjectileBase.InitialDirection;

            if (Physics.Raycast(rayOrigin, rayDirection, out RaycastHit hit, Range, HittableLayers, k_TriggerInteraction))
            {
                if (IsHitValid(hit))
                {
                    OnHit(hit.point, hit.normal, hit.collider);
                }
            }
        }

        bool IsHitValid(RaycastHit hit)
        {
            // ignore hits with an ignore component
            if (hit.collider.GetComponent<IgnoreHitDetection>())
            {
                return false;
            }

            // ignore hits with triggers that don't have a Damageable component
            if (hit.collider.isTrigger && hit.collider.GetComponent<Damageable>() == null)
            {
                return false;
            }

            // ignore hits with specific ignored colliders (self colliders, by default)
            if (m_IgnoredColliders != null && m_IgnoredColliders.Contains(hit.collider))
            {
                return false;
            }

            return true;
        }

        void OnHit(Vector3 point, Vector3 normal, Collider collider)
        {
            // damage
            Damageable damageable = collider.GetComponent<Damageable>();
            if (damageable)
            {
                damageable.InflictDamage(Damage, false, Owner);
            }

            // impact vfx
            if (ImpactVfx)
            {
                GameObject impactVfxInstance = Instantiate(ImpactVfx, point + (normal * ImpactVfxSpawnOffset), Quaternion.LookRotation(normal));
                if (ImpactVfxLifetime > 0)
                {
                    Destroy(impactVfxInstance.gameObject, ImpactVfxLifetime);
                }
            }

            // impact sfx
            if (ImpactSfxClip)
            {
                AudioUtility.CreateSFX(ImpactSfxClip, point, AudioUtility.AudioGroups.Impact, 1f, 3f);
            }

            // Self Destruct
            Destroy(gameObject);
        }
    }
}
