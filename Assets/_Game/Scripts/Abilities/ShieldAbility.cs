using System;
using PixelBrawl.Combat;
using PixelBrawl.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelBrawl.Abilities
{
    [RequireComponent(typeof(Health))]
    public sealed class ShieldAbility : MonoBehaviour, ICooldownAbility, IDamageAbsorber
    {
        [SerializeField, Min(0f)] private float shieldAmount = 50f;
        [SerializeField, Min(0f)] private float maximumDuration = 5f;
        [SerializeField, Min(0f)] private float cooldownDuration = 15f;
        [SerializeField] private GameObject shieldVisual;
        [SerializeField, Min(0)] private int startingCharges = 1;
        [SerializeField, Min(1)] private int maximumCharges = 3;

        public event Action<float, float> ShieldChanged;
        public event Action<bool> ActiveStateChanged;

        public bool IsActive => CurrentShield > 0f && Time.time < shieldEndTime;
        public int Priority => 0;
        public float CurrentShield { get; private set; }
        public float MaximumShield => shieldAmount;
        public float CooldownDuration => cooldownDuration;
        public float CooldownRemaining => Mathf.Max(0f, nextAvailableTime - Time.time);
        public int Charges { get; private set; }
        public int MaximumCharges => maximumCharges;

        private float shieldEndTime;
        private float nextAvailableTime;
        private PlayerActionGate actionGate;

        private void Awake()
        {
            actionGate = GetComponent<PlayerActionGate>();
            Charges = Mathf.Clamp(startingCharges, 0, maximumCharges);
            if (shieldVisual != null)
            {
                shieldVisual.SetActive(false);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.qKey.wasPressedThisFrame && (actionGate == null || actionGate.CanUseCombat))
            {
                TryActivate();
            }

            if (CurrentShield > 0f && Time.time >= shieldEndTime)
            {
                DeactivateShield();
            }
        }

        public bool TryActivate()
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

            CurrentShield = shieldAmount;
            shieldEndTime = Time.time + maximumDuration;
            nextAvailableTime = Time.time + cooldownDuration;
            if (isOnCooldown)
            {
                Charges--;
            }
            if (shieldVisual != null)
            {
                shieldVisual.SetActive(true);
            }
            ShieldChanged?.Invoke(CurrentShield, shieldAmount);
            ActiveStateChanged?.Invoke(true);
            return true;
        }

        public float AbsorbDamage(float incomingDamage)
        {
            if (!IsActive || incomingDamage <= 0f)
            {
                return incomingDamage;
            }

            float absorbed = Mathf.Min(CurrentShield, incomingDamage);
            CurrentShield -= absorbed;
            ShieldChanged?.Invoke(CurrentShield, shieldAmount);

            if (CurrentShield <= 0f)
            {
                DeactivateShield();
            }

            return incomingDamage - absorbed;
        }

        private void DeactivateShield()
        {
            bool wasActive = CurrentShield > 0f || Time.time < shieldEndTime;
            CurrentShield = 0f;
            shieldEndTime = 0f;
            if (shieldVisual != null)
            {
                shieldVisual.SetActive(false);
            }
            ShieldChanged?.Invoke(CurrentShield, shieldAmount);
            if (wasActive)
            {
                ActiveStateChanged?.Invoke(false);
            }
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
            DeactivateShield();
            nextAvailableTime = 0f;
            Charges = Mathf.Clamp(startingCharges, 0, maximumCharges);
        }

        public void ResetTemporaryState()
        {
            DeactivateShield();
            nextAvailableTime = 0f;
        }
    }
}
