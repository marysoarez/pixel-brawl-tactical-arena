using PixelBrawl.Inventory;
using PixelBrawl.Weapons;
using UnityEngine;

namespace PixelBrawl.Shop
{
    [CreateAssetMenu(fileName = "ShopItem", menuName = "Pixel Brawl/Shop Item")]
    public sealed class ShopItemData : ScriptableObject
    {
        [SerializeField] private string displayName;
        [SerializeField] private ShopCategory category;
        [SerializeField] private ShopPurchaseType purchaseType;
        [SerializeField, Min(0)] private int price;
        [SerializeField] private bool available = true;
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private WeaponSlot weaponSlot;
        [SerializeField, Min(0f)] private float shieldAmount;
        [SerializeField, Min(1)] private int abilityCharges = 1;

        public string DisplayName => displayName;
        public ShopCategory Category => category;
        public ShopPurchaseType PurchaseType => purchaseType;
        public int Price => price;
        public bool Available => available;
        public WeaponData WeaponData => weaponData;
        public WeaponSlot WeaponSlot => weaponSlot;
        public float ShieldAmount => shieldAmount;
        public int AbilityCharges => abilityCharges;

        public bool HasValidConfiguration => purchaseType switch
        {
            ShopPurchaseType.Weapon => weaponData != null && weaponSlot != WeaponSlot.Melee,
            ShopPurchaseType.EquipmentShield => shieldAmount > 0f,
            ShopPurchaseType.DashCharge => abilityCharges > 0,
            ShopPurchaseType.SpecialShieldCharge => abilityCharges > 0,
            _ => false
        };
    }
}
