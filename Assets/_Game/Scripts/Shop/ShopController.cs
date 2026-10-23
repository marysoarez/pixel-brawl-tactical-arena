using System;
using PixelBrawl.Abilities;
using PixelBrawl.Combat;
using PixelBrawl.Economy;
using PixelBrawl.Inventory;
using PixelBrawl.Match;
using PixelBrawl.Player;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace PixelBrawl.Shop
{
    public sealed class ShopController : MonoBehaviour
    {
        [SerializeField] private RoundManager roundManager;
        [SerializeField] private EconomyManager economy;
        [SerializeField] private InventoryController inventory;
        [SerializeField] private EquipmentShield equipmentShield;
        [SerializeField] private DashAbility dashAbility;
        [SerializeField] private ShieldAbility shieldAbility;
        [SerializeField] private PlayerActionGate actionGate;
        [SerializeField] private GameObject shopPanel;
        [SerializeField] private Text balanceText;
        [SerializeField] private Text feedbackText;
        [SerializeField] private ShopItemData[] items;

        public event Action<bool> VisibilityChanged;
        public event Action ItemsChanged;

        public bool IsOpen { get; private set; }
        public ShopItemData[] Items => items;

        private void OnEnable()
        {
            roundManager.StateChanged += HandleMatchStateChanged;
            economy.CreditsChanged += HandleCreditsChanged;
        }

        private void Start()
        {
            SetOpen(false);
            HandleCreditsChanged(economy.Credits);
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard != null && keyboard.bKey.wasPressedThisFrame && roundManager.State == MatchState.BuyPhase)
            {
                SetOpen(!IsOpen);
            }
        }

        private void OnDisable()
        {
            roundManager.StateChanged -= HandleMatchStateChanged;
            economy.CreditsChanged -= HandleCreditsChanged;
        }

        public bool TryPurchase(ShopItemData item)
        {
            if (!IsOpen || roundManager.State != MatchState.BuyPhase || item == null ||
                !item.Available || !item.HasValidConfiguration)
            {
                SetFeedback("ITEM UNAVAILABLE");
                return false;
            }

            if (!CanAcquire(item, out string reason))
            {
                SetFeedback(reason);
                return false;
            }

            if (!economy.TrySpend(item.Price))
            {
                SetFeedback("INSUFFICIENT CREDITS");
                return false;
            }

            bool acquired = Acquire(item);
            if (!acquired)
            {
                economy.AddCredits(item.Price);
                SetFeedback("PURCHASE FAILED");
                return false;
            }

            SetFeedback($"PURCHASED: {item.DisplayName}");
            ItemsChanged?.Invoke();
            return true;
        }

        public string GetStatus(ShopItemData item)
        {
            if (item == null || !item.Available || !item.HasValidConfiguration) return "UNAVAILABLE";
            if (!CanAcquire(item, out string reason)) return reason;
            return item.Price == 0 ? "FREE" : $"{item.Price} CR";
        }

        public bool CanAttemptPurchase(ShopItemData item)
        {
            return IsOpen && roundManager.State == MatchState.BuyPhase && item != null &&
                   item.Available && item.HasValidConfiguration && CanAcquire(item, out _);
        }

        private bool CanAcquire(ShopItemData item, out string reason)
        {
            bool canAcquire = item.PurchaseType switch
            {
                ShopPurchaseType.Weapon => inventory.CanPurchaseWeapon(item.WeaponData, item.WeaponSlot),
                ShopPurchaseType.EquipmentShield => equipmentShield.CanEquip(item.ShieldAmount),
                ShopPurchaseType.DashCharge => dashAbility.CanAddCharges(item.AbilityCharges),
                ShopPurchaseType.SpecialShieldCharge => shieldAbility.CanAddCharges(item.AbilityCharges),
                _ => false
            };

            reason = item.PurchaseType switch
            {
                ShopPurchaseType.Weapon when inventory.OwnsWeapon(item.WeaponData) => "OWNED",
                ShopPurchaseType.EquipmentShield when !canAcquire => "EQUIPPED / LOWER TIER",
                ShopPurchaseType.DashCharge when !canAcquire => "MAX CHARGES",
                ShopPurchaseType.SpecialShieldCharge when !canAcquire => "MAX CHARGES",
                _ when !canAcquire => "UNAVAILABLE",
                _ => string.Empty
            };
            return canAcquire;
        }

        private bool Acquire(ShopItemData item)
        {
            return item.PurchaseType switch
            {
                ShopPurchaseType.Weapon => inventory.TryPurchaseWeapon(item.WeaponData, item.WeaponSlot),
                ShopPurchaseType.EquipmentShield => equipmentShield.TryEquip(item.ShieldAmount),
                ShopPurchaseType.DashCharge => dashAbility.AddCharges(item.AbilityCharges),
                ShopPurchaseType.SpecialShieldCharge => shieldAbility.AddCharges(item.AbilityCharges),
                _ => false
            };
        }

        private void SetOpen(bool isOpen)
        {
            IsOpen = isOpen && roundManager.State == MatchState.BuyPhase;
            shopPanel.SetActive(IsOpen);
            actionGate.SetShopOpen(IsOpen);
            Cursor.lockState = IsOpen ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible = IsOpen;
            if (feedbackText != null) feedbackText.text = string.Empty;
            VisibilityChanged?.Invoke(IsOpen);
            ItemsChanged?.Invoke();
        }

        private void HandleMatchStateChanged(MatchState state)
        {
            if (state != MatchState.BuyPhase && IsOpen)
            {
                SetOpen(false);
            }
        }

        private void HandleCreditsChanged(int credits)
        {
            if (balanceText != null)
            {
                balanceText.text = $"BALANCE: {credits} CR";
            }
            ItemsChanged?.Invoke();
        }

        private void SetFeedback(string message)
        {
            if (feedbackText != null)
            {
                feedbackText.text = message;
            }
        }
    }
}
