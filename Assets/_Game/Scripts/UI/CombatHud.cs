using System.Collections;
using PixelBrawl.Abilities;
using PixelBrawl.Combat;
using PixelBrawl.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace PixelBrawl.UI
{
    public sealed class CombatHud : MonoBehaviour
    {
        [SerializeField] private HitscanWeapon weapon;
        [SerializeField] private Health playerHealth;
        [SerializeField] private Text healthText;
        [SerializeField] private Text ammoText;
        [SerializeField] private Text reloadText;
        [SerializeField] private Text hitmarkerText;
        [SerializeField] private Text eliminationText;
        [SerializeField] private DashAbility dashAbility;
        [SerializeField] private ShieldAbility shieldAbility;
        [SerializeField] private MeleeAttack meleeAttack;
        [SerializeField] private Text shieldText;
        [SerializeField] private Text dashStatusText;
        [SerializeField] private Text shieldStatusText;
        [SerializeField] private Text meleeFeedbackText;
        [SerializeField, Min(0f)] private float hitmarkerDuration = 0.12f;
        [SerializeField, Min(0f)] private float eliminationDuration = 1.2f;

        private Coroutine hitmarkerRoutine;
        private Coroutine eliminationRoutine;

        private void OnEnable()
        {
            if (weapon != null)
            {
                weapon.AmmoChanged += UpdateAmmo;
                weapon.ReloadStateChanged += UpdateReloadState;
                weapon.HitConfirmed += ShowHitConfirmation;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged += UpdateHealth;
            }

            if (shieldAbility != null)
            {
                shieldAbility.ShieldChanged += UpdateShield;
            }

            if (meleeAttack != null)
            {
                meleeAttack.AttackCompleted += ShowMeleeFeedback;
            }
        }

        private void Start()
        {
            if (weapon != null)
            {
                UpdateAmmo(weapon.MagazineAmmo, weapon.ReserveAmmo);
                UpdateReloadState(weapon.IsReloading);
            }

            if (playerHealth != null)
            {
                UpdateHealth(playerHealth.CurrentHealth, playerHealth.MaximumHealth);
            }

            hitmarkerText.gameObject.SetActive(false);
            eliminationText.gameObject.SetActive(false);
            if (meleeFeedbackText != null)
            {
                meleeFeedbackText.gameObject.SetActive(false);
            }

            if (shieldAbility != null)
            {
                UpdateShield(shieldAbility.CurrentShield, shieldAbility.MaximumShield);
            }
        }

        private void Update()
        {
            if (dashAbility != null && dashStatusText != null)
            {
                dashStatusText.text = dashAbility.IsActive
                    ? "DASH ACTIVE"
                    : dashAbility.CooldownRemaining <= 0f
                        ? "SHIFT  DASH READY"
                        : $"DASH  {dashAbility.CooldownRemaining:0.0}s";
            }

            if (shieldAbility != null && shieldStatusText != null)
            {
                shieldStatusText.text = shieldAbility.IsActive
                    ? "SHIELD ACTIVE"
                    : shieldAbility.CooldownRemaining <= 0f
                        ? "Q  SHIELD READY"
                        : $"SHIELD  {shieldAbility.CooldownRemaining:0.0}s";
            }
        }

        private void OnDisable()
        {
            if (weapon != null)
            {
                weapon.AmmoChanged -= UpdateAmmo;
                weapon.ReloadStateChanged -= UpdateReloadState;
                weapon.HitConfirmed -= ShowHitConfirmation;
            }

            if (playerHealth != null)
            {
                playerHealth.HealthChanged -= UpdateHealth;
            }

            if (shieldAbility != null)
            {
                shieldAbility.ShieldChanged -= UpdateShield;
            }

            if (meleeAttack != null)
            {
                meleeAttack.AttackCompleted -= ShowMeleeFeedback;
            }
        }

        private void UpdateHealth(float current, float maximum)
        {
            healthText.text = $"HP  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
        }

        private void UpdateAmmo(int magazine, int reserve)
        {
            ammoText.text = $"{magazine:00} / {reserve:000}";
        }

        private void UpdateShield(float current, float maximum)
        {
            if (shieldText != null)
            {
                shieldText.text = $"SHIELD  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
            }
        }

        private void ShowMeleeFeedback(bool hit)
        {
            if (meleeFeedbackText == null)
            {
                return;
            }

            meleeFeedbackText.text = hit ? "PUNCH HIT" : "PUNCH MISS";
            StartCoroutine(ShowTemporarily(meleeFeedbackText, 0.45f));
        }

        private void UpdateReloadState(bool isReloading)
        {
            reloadText.gameObject.SetActive(isReloading);
            reloadText.text = isReloading ? "RELOADING..." : string.Empty;
        }

        private void ShowHitConfirmation(bool eliminated)
        {
            if (hitmarkerRoutine != null)
            {
                StopCoroutine(hitmarkerRoutine);
            }

            hitmarkerRoutine = StartCoroutine(ShowTemporarily(hitmarkerText, hitmarkerDuration));

            if (!eliminated)
            {
                return;
            }

            if (eliminationRoutine != null)
            {
                StopCoroutine(eliminationRoutine);
            }

            eliminationRoutine = StartCoroutine(ShowTemporarily(eliminationText, eliminationDuration));
        }

        private static IEnumerator ShowTemporarily(Text text, float duration)
        {
            text.gameObject.SetActive(true);
            yield return new WaitForSeconds(duration);
            text.gameObject.SetActive(false);
        }
    }
}
