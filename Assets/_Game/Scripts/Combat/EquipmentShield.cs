using System;
using UnityEngine;

namespace PixelBrawl.Combat
{
    public sealed class EquipmentShield : MonoBehaviour, IDamageAbsorber
    {
        public event Action<float, float> ShieldChanged;

        public int Priority => 10;
        public float CurrentShield { get; private set; }
        public float MaximumShield { get; private set; }

        public bool CanEquip(float amount)
        {
            return amount > MaximumShield;
        }

        public bool TryEquip(float amount)
        {
            if (!CanEquip(amount))
            {
                return false;
            }

            MaximumShield = amount;
            CurrentShield = amount;
            ShieldChanged?.Invoke(CurrentShield, MaximumShield);
            return true;
        }

        public float AbsorbDamage(float incomingDamage)
        {
            if (incomingDamage <= 0f || CurrentShield <= 0f)
            {
                return incomingDamage;
            }

            float absorbed = Mathf.Min(CurrentShield, incomingDamage);
            CurrentShield -= absorbed;
            ShieldChanged?.Invoke(CurrentShield, MaximumShield);
            return incomingDamage - absorbed;
        }

        public void ResetForNewMatch()
        {
            CurrentShield = 0f;
            MaximumShield = 0f;
            ShieldChanged?.Invoke(0f, 0f);
        }
    }
}
