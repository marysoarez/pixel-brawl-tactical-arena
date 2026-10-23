using System;
using System.Collections;
using PixelBrawl.Combat;
using PixelBrawl.Player;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelBrawl.Weapons
{
    public sealed class HitscanWeapon : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform weaponVisual;
        [SerializeField] private Transform muzzle;
        [SerializeField] private GameObject muzzleFlash;
        [SerializeField] private Material impactMaterial;
        [SerializeField] private FpsLook fpsLook;

        [Header("Aiming")]
        [SerializeField] private Vector3 hipPosition = new Vector3(0.32f, -0.28f, 0.65f);
        [SerializeField] private Vector3 aimedPosition = new Vector3(0f, -0.18f, 0.55f);

        [Header("Raycast")]
        [SerializeField] private LayerMask hitMask = ~0;
        [SerializeField, Min(0f)] private float muzzleFlashDuration = 0.05f;
        [SerializeField, Min(0f)] private float impactDuration = 0.12f;

        public event Action<int, int> AmmoChanged;
        public event Action<bool> ReloadStateChanged;
        public event Action<bool> HitConfirmed;
        public event Action<WeaponData> WeaponChanged;

        public int MagazineAmmo { get; private set; }
        public int ReserveAmmo { get; private set; }
        public bool IsReloading { get; private set; }
        public bool IsAiming { get; private set; }
        public WeaponData CurrentWeaponData => weaponData;

        private float nextShotTime;
        private Coroutine muzzleFlashRoutine;
        private Coroutine reloadRoutine;
        private MeleeAttack meleeAttack;
        private PlayerActionGate actionGate;

        private void Awake()
        {
            if (playerCamera == null)
            {
                playerCamera = GetComponentInParent<Camera>();
            }

            if (fpsLook == null)
            {
                fpsLook = GetComponentInParent<FpsLook>();
            }

            meleeAttack = GetComponentInParent<MeleeAttack>();
            actionGate = GetComponentInParent<PlayerActionGate>();

            if (weaponData != null)
            {
                MagazineAmmo = weaponData.MagazineSize;
                ReserveAmmo = weaponData.StartingReserveAmmo;
            }

            if (muzzleFlash != null)
            {
                muzzleFlash.SetActive(false);
            }
        }

        private void Start()
        {
            AmmoChanged?.Invoke(MagazineAmmo, ReserveAmmo);
        }

        private void Update()
        {
            if (weaponData == null || playerCamera == null)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            bool cursorIsLocked = Cursor.lockState == CursorLockMode.Locked;
            bool meleeIsActive = meleeAttack != null && meleeAttack.IsAttacking;
            bool combatInputAllowed = actionGate == null || actionGate.CanUseWeapon;
            IsAiming = cursorIsLocked && combatInputAllowed && !meleeIsActive && mouse != null &&
                       mouse.rightButton.isPressed && !IsReloading;
            UpdateAiming();

            if (!cursorIsLocked || meleeIsActive || (actionGate != null && !actionGate.CanUseWeapon))
            {
                return;
            }

            if (keyboard != null && keyboard.rKey.wasPressedThisFrame)
            {
                TryStartReload();
            }

            if (!IsReloading && mouse != null && mouse.leftButton.isPressed && Time.time >= nextShotTime)
            {
                TryFire();
            }
        }

        private void TryFire()
        {
            if (MagazineAmmo <= 0)
            {
                TryStartReload();
                return;
            }

            nextShotTime = Time.time + 60f / weaponData.RoundsPerMinute;
            MagazineAmmo--;
            AmmoChanged?.Invoke(MagazineAmmo, ReserveAmmo);

            fpsLook?.AddRecoil(weaponData.RecoilKick);
            PlayMuzzleFlash();

            bool hitDamageable = false;
            bool eliminatedTarget = false;
            for (int pellet = 0; pellet < weaponData.PelletsPerShot; pellet++)
            {
                Ray ray = CreateShotRay();
                if (!Physics.Raycast(ray, out RaycastHit hit, weaponData.Range, hitMask, QueryTriggerInteraction.Ignore))
                {
                    continue;
                }

                SpawnImpact(hit.point, hit.normal);

                IDamageable damageable = FindDamageable(hit.collider);
                if (damageable != null)
                {
                    hitDamageable = true;
                    eliminatedTarget |= damageable.TakeDamage(weaponData.BaseDamage);
                }
            }

            if (hitDamageable)
            {
                HitConfirmed?.Invoke(eliminatedTarget);
            }
        }

        public void Equip(WeaponData data, int magazineAmmo, int reserveAmmo)
        {
            if (data == null || IsReloading)
            {
                return;
            }

            weaponData = data;
            MagazineAmmo = Mathf.Clamp(magazineAmmo, 0, data.MagazineSize);
            ReserveAmmo = Mathf.Max(0, reserveAmmo);
            playerCamera.fieldOfView = data.HipFieldOfView;
            AmmoChanged?.Invoke(MagazineAmmo, ReserveAmmo);
            WeaponChanged?.Invoke(weaponData);
        }

        public void SetVisualsVisible(bool isVisible)
        {
            if (weaponVisual == null)
            {
                return;
            }

            foreach (Renderer weaponRenderer in weaponVisual.GetComponentsInChildren<Renderer>(true))
            {
                if (muzzleFlash == null || !weaponRenderer.transform.IsChildOf(muzzleFlash.transform))
                {
                    weaponRenderer.enabled = isVisible;
                }
            }
        }

        private void TryStartReload()
        {
            if (IsReloading || MagazineAmmo >= weaponData.MagazineSize || ReserveAmmo <= 0)
            {
                return;
            }

            reloadRoutine = StartCoroutine(Reload());
        }

        private IEnumerator Reload()
        {
            IsReloading = true;
            ReloadStateChanged?.Invoke(true);
            yield return new WaitForSeconds(weaponData.ReloadDuration);

            int neededAmmo = weaponData.MagazineSize - MagazineAmmo;
            int ammoToLoad = Mathf.Min(neededAmmo, ReserveAmmo);
            MagazineAmmo += ammoToLoad;
            ReserveAmmo -= ammoToLoad;

            IsReloading = false;
            reloadRoutine = null;
            ReloadStateChanged?.Invoke(false);
            AmmoChanged?.Invoke(MagazineAmmo, ReserveAmmo);
        }

        public void CancelReload()
        {
            if (!IsReloading)
            {
                return;
            }

            if (reloadRoutine != null)
            {
                StopCoroutine(reloadRoutine);
                reloadRoutine = null;
            }

            IsReloading = false;
            ReloadStateChanged?.Invoke(false);
        }

        private void OnDisable()
        {
            CancelReload();
            IsAiming = false;
            fpsLook?.SetSensitivityMultiplier(1f);
        }

        private void UpdateAiming()
        {
            float targetFov = IsAiming ? weaponData.AimedFieldOfView : weaponData.HipFieldOfView;
            float interpolation = 1f - Mathf.Exp(-weaponData.AimTransitionSpeed * Time.deltaTime);
            playerCamera.fieldOfView = Mathf.Lerp(playerCamera.fieldOfView, targetFov, interpolation);

            if (weaponVisual != null)
            {
                Vector3 targetPosition = IsAiming ? aimedPosition : hipPosition;
                weaponVisual.localPosition = Vector3.Lerp(weaponVisual.localPosition, targetPosition, interpolation);
            }

            fpsLook?.SetSensitivityMultiplier(IsAiming ? weaponData.AimedSensitivityMultiplier : 1f);
        }

        private void PlayMuzzleFlash()
        {
            if (muzzleFlash == null)
            {
                return;
            }

            if (muzzleFlashRoutine != null)
            {
                StopCoroutine(muzzleFlashRoutine);
            }

            muzzleFlashRoutine = StartCoroutine(ShowMuzzleFlash());
        }

        private IEnumerator ShowMuzzleFlash()
        {
            muzzleFlash.SetActive(true);
            yield return new WaitForSeconds(muzzleFlashDuration);
            muzzleFlash.SetActive(false);
            muzzleFlashRoutine = null;
        }

        private void SpawnImpact(Vector3 point, Vector3 normal)
        {
            GameObject impact = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            impact.name = "BulletImpact";
            impact.transform.position = point + normal * 0.01f;
            impact.transform.localScale = Vector3.one * 0.08f;

            Collider impactCollider = impact.GetComponent<Collider>();
            if (impactCollider != null)
            {
                impactCollider.enabled = false;
                Destroy(impactCollider);
            }

            if (impactMaterial != null)
            {
                impact.GetComponent<Renderer>().sharedMaterial = impactMaterial;
            }

            Destroy(impact, impactDuration);
        }

        private Ray CreateShotRay()
        {
            Ray centerRay = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f));
            if (weaponData.SpreadAngle <= 0f)
            {
                return centerRay;
            }

            Vector2 spread = UnityEngine.Random.insideUnitCircle * Mathf.Tan(weaponData.SpreadAngle * Mathf.Deg2Rad);
            Vector3 direction = (centerRay.direction + playerCamera.transform.right * spread.x +
                                 playerCamera.transform.up * spread.y).normalized;
            return new Ray(centerRay.origin, direction);
        }

        private static IDamageable FindDamageable(Collider hitCollider)
        {
            MonoBehaviour[] behaviours = hitCollider.GetComponentsInParent<MonoBehaviour>();
            foreach (MonoBehaviour behaviour in behaviours)
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
