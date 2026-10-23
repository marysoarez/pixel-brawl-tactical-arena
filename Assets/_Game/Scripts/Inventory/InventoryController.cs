using System;
using System.Collections;
using PixelBrawl.Player;
using PixelBrawl.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelBrawl.Inventory
{
    public sealed class InventoryController : MonoBehaviour
    {
        [Serializable]
        private sealed class WeaponSlotState
        {
            public WeaponData Data;
            public int Magazine;
            public int Reserve;

            public void SetNewWeapon(WeaponData data)
            {
                Data = data;
                Magazine = data != null ? data.MagazineSize : 0;
                Reserve = data != null ? data.StartingReserveAmmo : 0;
            }
        }

        [SerializeField] private HitscanWeapon weapon;
        [SerializeField] private WeaponData startingSecondaryWeapon;
        [SerializeField] private PlayerActionGate actionGate;
        [SerializeField, Min(0f)] private float switchDuration = 0.35f;

        public event Action<WeaponSlot, WeaponData> EquippedWeaponChanged;
        public event Action InventoryChanged;

        public WeaponSlot CurrentSlot { get; private set; } = WeaponSlot.Secondary;
        public WeaponData EquippedWeapon => GetState(CurrentSlot)?.Data;

        private readonly WeaponSlotState primary = new WeaponSlotState();
        private readonly WeaponSlotState secondary = new WeaponSlotState();
        private bool isSwitching;

        private void Awake()
        {
            if (weapon == null)
            {
                weapon = GetComponentInChildren<HitscanWeapon>();
            }

            if (actionGate == null)
            {
                actionGate = GetComponent<PlayerActionGate>();
            }
        }

        private void OnEnable()
        {
            if (weapon != null)
            {
                weapon.AmmoChanged += StoreCurrentAmmo;
            }
        }

        private void Start()
        {
            ResetForNewMatch();
        }

        private void Update()
        {
            if (isSwitching || weapon == null || weapon.IsReloading || (actionGate != null && actionGate.ShopOpen))
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.digit1Key.wasPressedThisFrame)
            {
                TrySwitchTo(WeaponSlot.Primary);
            }
            else if (keyboard.digit2Key.wasPressedThisFrame)
            {
                TrySwitchTo(WeaponSlot.Secondary);
            }
            else if (keyboard.digit3Key.wasPressedThisFrame)
            {
                TrySwitchTo(WeaponSlot.Melee);
            }
        }

        private void OnDisable()
        {
            if (weapon != null)
            {
                weapon.AmmoChanged -= StoreCurrentAmmo;
            }
        }

        public void ResetForNewMatch()
        {
            primary.SetNewWeapon(null);
            secondary.SetNewWeapon(startingSecondaryWeapon);
            CurrentSlot = WeaponSlot.Secondary;
            EquipCurrentState();
            InventoryChanged?.Invoke();
        }

        public bool CanPurchaseWeapon(WeaponData data, WeaponSlot slot)
        {
            if (data == null || slot == WeaponSlot.Melee)
            {
                return false;
            }

            return GetState(slot).Data != data;
        }

        public bool TryPurchaseWeapon(WeaponData data, WeaponSlot slot)
        {
            if (!CanPurchaseWeapon(data, slot))
            {
                return false;
            }

            GetState(slot).SetNewWeapon(data);
            InventoryChanged?.Invoke();
            return true;
        }

        public bool OwnsWeapon(WeaponData data)
        {
            return data != null && (primary.Data == data || secondary.Data == data);
        }

        public bool TrySwitchTo(WeaponSlot slot)
        {
            if (isSwitching || slot == CurrentSlot || (slot != WeaponSlot.Melee && GetState(slot).Data == null))
            {
                return false;
            }

            StartCoroutine(SwitchWeapon(slot));
            return true;
        }

        private IEnumerator SwitchWeapon(WeaponSlot slot)
        {
            isSwitching = true;
            actionGate?.SetWeaponSwitching(true);
            weapon.SetVisualsVisible(false);
            yield return new WaitForSeconds(switchDuration);

            CurrentSlot = slot;
            EquipCurrentState();
            isSwitching = false;
            actionGate?.SetWeaponSwitching(false);
        }

        private void EquipCurrentState()
        {
            bool rangedWeaponEquipped = CurrentSlot != WeaponSlot.Melee;
            actionGate?.SetRangedWeaponEquipped(rangedWeaponEquipped);
            weapon.SetVisualsVisible(rangedWeaponEquipped);

            WeaponSlotState state = GetState(CurrentSlot);
            if (state != null && state.Data != null)
            {
                weapon.Equip(state.Data, state.Magazine, state.Reserve);
            }

            EquippedWeaponChanged?.Invoke(CurrentSlot, state?.Data);
        }

        private void StoreCurrentAmmo(int magazine, int reserve)
        {
            WeaponSlotState state = GetState(CurrentSlot);
            if (state == null)
            {
                return;
            }

            state.Magazine = magazine;
            state.Reserve = reserve;
        }

        private WeaponSlotState GetState(WeaponSlot slot)
        {
            return slot switch
            {
                WeaponSlot.Primary => primary,
                WeaponSlot.Secondary => secondary,
                _ => null
            };
        }
    }
}
