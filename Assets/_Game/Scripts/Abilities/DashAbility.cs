using System;
using UnityEngine;

namespace PixelBrawl.Abilities
{
    public sealed class DashAbility : MonoBehaviour, ICooldownAbility
    {
        [SerializeField, Min(0f)] private float dashDistance = 5f;
        [SerializeField, Min(0.01f)] private float dashDuration = 0.2f;
        [SerializeField, Min(0f)] private float cooldownDuration = 5f;
        [SerializeField, Min(0)] private int startingCharges = 1;
        [SerializeField, Min(1)] private int maximumCharges = 3;

        public event Action<bool> ActiveStateChanged;

        public bool IsActive { get; private set; }
        public float CooldownDuration => cooldownDuration;
        public float CooldownRemaining => Mathf.Max(0f, nextAvailableTime - Time.time);
        public Vector3 Velocity => IsActive ? dashDirection * (dashDistance / dashDuration) : Vector3.zero;
        public int Charges { get; private set; }
        public int MaximumCharges => maximumCharges;

        private Vector3 dashDirection;
        private float dashEndTime;
        private float nextAvailableTime;

        private void Awake()
        {
            Charges = Mathf.Clamp(startingCharges, 0, maximumCharges);
        }

        public bool TryActivate(Vector3 direction)
        {
            if (IsActive)
            {
                return false;
            }

            bool isOnCooldown = CooldownRemaining > 0f;
            if (isOnCooldown && Charges <= 0)
            {
                return false;
            }

            direction.y = 0f;
            if (direction.sqrMagnitude < 0.001f)
            {
                return false;
            }

            dashDirection = direction.normalized;
            dashEndTime = Time.time + dashDuration;
            nextAvailableTime = Time.time + cooldownDuration;
            if (isOnCooldown)
            {
                Charges--;
            }
            IsActive = true;
            ActiveStateChanged?.Invoke(true);
            return true;
        }

        public void Tick()
        {
            if (IsActive && Time.time >= dashEndTime)
            {
                StopDash();
            }
        }

        public void StopDash()
        {
            if (!IsActive)
            {
                return;
            }

            IsActive = false;
            ActiveStateChanged?.Invoke(false);
        }

        public bool CanAddCharges(int amount)
        {
            return amount > 0 && Charges + amount <= maximumCharges;
        }

        public bool AddCharges(int amount)
        {
            if (!CanAddCharges(amount))
            {
                return false;
            }

            Charges += amount;
            return true;
        }

        public void ResetForNewMatch()
        {
            StopDash();
            nextAvailableTime = 0f;
            Charges = Mathf.Clamp(startingCharges, 0, maximumCharges);
        }

        public void ResetTemporaryState()
        {
            StopDash();
            nextAvailableTime = 0f;
        }
    }
}
