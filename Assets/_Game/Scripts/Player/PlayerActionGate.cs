using System;
using UnityEngine;

namespace PixelBrawl.Player
{
    public sealed class PlayerActionGate : MonoBehaviour
    {
        public event Action StateChanged;

        public bool CombatAllowed { get; private set; }
        public bool ShopOpen { get; private set; }
        public bool WeaponSwitching { get; private set; }
        public bool RangedWeaponEquipped { get; private set; } = true;

        public bool CanUseCombat => CombatAllowed && !ShopOpen;
        public bool CanUseWeapon => CanUseCombat && !WeaponSwitching && RangedWeaponEquipped;

        public void SetCombatAllowed(bool allowed)
        {
            CombatAllowed = allowed;
            StateChanged?.Invoke();
        }

        public void SetShopOpen(bool isOpen)
        {
            ShopOpen = isOpen;
            StateChanged?.Invoke();
        }

        public void SetWeaponSwitching(bool isSwitching)
        {
            WeaponSwitching = isSwitching;
            StateChanged?.Invoke();
        }

        public void SetRangedWeaponEquipped(bool isEquipped)
        {
            RangedWeaponEquipped = isEquipped;
            StateChanged?.Invoke();
        }
    }
}
