using System;
using System.Collections.Generic;
using UnityEngine;

namespace PixelBrawl.Combat
{
    public sealed class Health : MonoBehaviour, IDamageable
    {
        [SerializeField, Min(1f)] private float maximumHealth = 100f;

        public event Action<float, float> HealthChanged;
        public event Action<float> Damaged;
        public event Action Died;

        public float MaximumHealth => maximumHealth;
        public float CurrentHealth { get; private set; }
        public bool IsAlive => CurrentHealth > 0f;
        public bool DamageEnabled { get; private set; } = true;

        private void Awake()
        {
            CurrentHealth = maximumHealth;
        }

        private void Start()
        {
            HealthChanged?.Invoke(CurrentHealth, maximumHealth);
        }

        public bool TakeDamage(float amount)
        {
            if (!DamageEnabled || !IsAlive || amount <= 0f)
            {
                return false;
            }

            float remainingDamage = amount;
            List<IDamageAbsorber> absorbers = new List<IDamageAbsorber>();
            foreach (MonoBehaviour behaviour in GetComponents<MonoBehaviour>())
            {
                if (behaviour is IDamageAbsorber absorber)
                {
                    absorbers.Add(absorber);
                }
            }

            absorbers.Sort((left, right) => left.Priority.CompareTo(right.Priority));
            foreach (IDamageAbsorber absorber in absorbers)
            {
                remainingDamage = absorber.AbsorbDamage(remainingDamage);
                if (remainingDamage <= 0f)
                {
                    return false;
                }
            }

            float previousHealth = CurrentHealth;
            CurrentHealth = Mathf.Max(0f, CurrentHealth - remainingDamage);
            float appliedDamage = previousHealth - CurrentHealth;

            Damaged?.Invoke(appliedDamage);
            HealthChanged?.Invoke(CurrentHealth, maximumHealth);

            bool wasEliminated = CurrentHealth <= 0f;
            if (wasEliminated)
            {
                Died?.Invoke();
            }

            return wasEliminated;
        }

        public void RestoreToFull()
        {
            CurrentHealth = maximumHealth;
            HealthChanged?.Invoke(CurrentHealth, maximumHealth);
        }

        public void SetDamageEnabled(bool enabled)
        {
            DamageEnabled = enabled;
        }

        private void OnValidate()
        {
            maximumHealth = Mathf.Max(1f, maximumHealth);
        }
    }
}
