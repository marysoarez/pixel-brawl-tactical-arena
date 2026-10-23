using UnityEngine;

namespace PixelBrawl.Weapons
{
    [CreateAssetMenu(fileName = "WeaponData", menuName = "Pixel Brawl/Weapon Data")]
    public sealed class WeaponData : ScriptableObject
    {
        [Header("Identity")]
        [SerializeField] private string displayName = "Weapon";

        [Header("Damage and firing")]
        [SerializeField, Min(0f)] private float baseDamage = 25f;
        [SerializeField, Min(1f)] private float roundsPerMinute = 600f;
        [SerializeField, Min(1f)] private float range = 100f;
        [SerializeField, Min(1)] private int pelletsPerShot = 1;
        [SerializeField, Min(0f)] private float spreadAngle;

        [Header("Ammunition")]
        [SerializeField, Min(1)] private int magazineSize = 30;
        [SerializeField, Min(0)] private int startingReserveAmmo = 90;
        [SerializeField, Min(0f)] private float reloadDuration = 2f;

        [Header("Aim down sights")]
        [SerializeField, Range(1f, 179f)] private float hipFieldOfView = 75f;
        [SerializeField, Range(1f, 179f)] private float aimedFieldOfView = 50f;
        [SerializeField, Min(0f)] private float aimTransitionSpeed = 12f;
        [SerializeField, Range(0.05f, 1f)] private float aimedSensitivityMultiplier = 0.65f;

        [Header("Recoil")]
        [SerializeField, Min(0f)] private float recoilKick = 0.75f;

        public string DisplayName => displayName;
        public float BaseDamage => baseDamage;
        public float RoundsPerMinute => roundsPerMinute;
        public float Range => range;
        public int PelletsPerShot => pelletsPerShot;
        public float SpreadAngle => spreadAngle;
        public int MagazineSize => magazineSize;
        public int StartingReserveAmmo => startingReserveAmmo;
        public float ReloadDuration => reloadDuration;
        public float HipFieldOfView => hipFieldOfView;
        public float AimedFieldOfView => aimedFieldOfView;
        public float AimTransitionSpeed => aimTransitionSpeed;
        public float AimedSensitivityMultiplier => aimedSensitivityMultiplier;
        public float RecoilKick => recoilKick;
    }
}
