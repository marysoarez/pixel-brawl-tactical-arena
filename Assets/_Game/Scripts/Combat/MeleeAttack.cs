using System;
using System.Collections;
using System.Collections.Generic;
using PixelBrawl.Weapons;
using PixelBrawl.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelBrawl.Combat
{
    public sealed class MeleeAttack : MonoBehaviour
    {
        [Header("Attack")]
        [SerializeField, Min(0f)] private float damage = 40f;
        [SerializeField, Min(0f)] private float range = 2f;
        [SerializeField, Min(0f)] private float sphereRadius = 0.45f;
        [SerializeField, Min(0f)] private float cooldown = 0.8f;
        [SerializeField] private LayerMask hitMask = ~0;

        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private HitscanWeapon weapon;
        [SerializeField] private Transform visualArm;

        [Header("Animation")]
        [SerializeField, Min(0.01f)] private float attackDuration = 0.28f;
        [SerializeField, Min(0f)] private float punchDistance = 0.7f;

        public event Action<bool> AttackCompleted;

        public bool IsAttacking { get; private set; }
        public float CooldownRemaining => Mathf.Max(0f, nextAttackTime - Time.time);

        private float nextAttackTime;
        private Vector3 armRestPosition;
        private readonly HashSet<IDamageable> damagedTargets = new HashSet<IDamageable>();
        private PlayerActionGate actionGate;

        private void Awake()
        {
            actionGate = GetComponent<PlayerActionGate>();
            if (playerCamera == null)
            {
                playerCamera = GetComponentInChildren<Camera>();
            }

            if (visualArm != null)
            {
                armRestPosition = visualArm.localPosition;
                visualArm.gameObject.SetActive(false);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.vKey.wasPressedThisFrame)
            {
                TryAttack();
            }
        }

        public bool TryAttack()
        {
            if (IsAttacking || CooldownRemaining > 0f || playerCamera == null ||
                (actionGate != null && !actionGate.CanUseCombat))
            {
                return false;
            }

            weapon?.CancelReload();

            nextAttackTime = Time.time + cooldown;
            StartCoroutine(PerformAttack());
            return true;
        }

        private IEnumerator PerformAttack()
        {
            IsAttacking = true;
            damagedTargets.Clear();

            bool hitDamageable = DetectHits();
            if (visualArm != null)
            {
                visualArm.gameObject.SetActive(true);
            }

            float elapsed = 0f;
            while (elapsed < attackDuration)
            {
                elapsed += Time.deltaTime;
                float normalizedTime = Mathf.Clamp01(elapsed / attackDuration);
                float extension = Mathf.Sin(normalizedTime * Mathf.PI) * punchDistance;
                if (visualArm != null)
                {
                    visualArm.localPosition = armRestPosition + Vector3.forward * extension;
                }

                yield return null;
            }

            if (visualArm != null)
            {
                visualArm.localPosition = armRestPosition;
                visualArm.gameObject.SetActive(false);
            }

            IsAttacking = false;
            AttackCompleted?.Invoke(hitDamageable);
        }

        private void OnDisable()
        {
            StopAllCoroutines();
            IsAttacking = false;
            damagedTargets.Clear();

            if (visualArm != null)
            {
                visualArm.localPosition = armRestPosition;
                visualArm.gameObject.SetActive(false);
            }
        }

        private bool DetectHits()
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            RaycastHit[] hits = Physics.SphereCastAll(ray, sphereRadius, range, hitMask, QueryTriggerInteraction.Ignore);
            Array.Sort(hits, (left, right) => left.distance.CompareTo(right.distance));

            bool hitDamageable = false;
            foreach (RaycastHit hit in hits)
            {
                if (hit.collider.transform.IsChildOf(transform) || hit.collider.transform == transform)
                {
                    continue;
                }

                IDamageable damageable = FindDamageable(hit.collider);
                if (damageable == null)
                {
                    break;
                }

                if (damagedTargets.Add(damageable))
                {
                    damageable.TakeDamage(damage);
                    hitDamageable = true;
                }
            }

            return hitDamageable;
        }

        private static IDamageable FindDamageable(Collider hitCollider)
        {
            foreach (MonoBehaviour behaviour in hitCollider.GetComponentsInParent<MonoBehaviour>())
            {
                if (behaviour is IDamageable damageable)
                {
                    return damageable;
                }
            }

            return null;
        }
    }
}
