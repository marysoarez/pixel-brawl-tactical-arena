using System.Collections;
using UnityEngine;

namespace PixelBrawl.Combat
{
    [RequireComponent(typeof(Health))]
    public sealed class TrainingTarget : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float respawnDelay;
        [SerializeField] private Color fullHealthColor = new Color(0.15f, 0.8f, 0.3f);
        [SerializeField] private Color lowHealthColor = new Color(0.95f, 0.15f, 0.1f);
        [SerializeField, Min(0f)] private float hitFlashDuration = 0.08f;

        private Health health;
        private Renderer[] targetRenderers;
        private Collider[] targetColliders;
        private Coroutine flashRoutine;
        private MaterialPropertyBlock propertyBlock;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            EnsureInitialized();
            health.HealthChanged += HandleHealthChanged;
            health.Damaged += HandleDamaged;
            health.Died += HandleDeath;
        }

        private void Start()
        {
            UpdateHealthColor(health.CurrentHealth, health.MaximumHealth);
        }

        private void OnDisable()
        {
            health.HealthChanged -= HandleHealthChanged;
            health.Damaged -= HandleDamaged;
            health.Died -= HandleDeath;
        }

        private void HandleHealthChanged(float current, float maximum)
        {
            UpdateHealthColor(current, maximum);
        }

        private void HandleDamaged(float amount)
        {
            if (flashRoutine != null)
            {
                StopCoroutine(flashRoutine);
            }

            flashRoutine = StartCoroutine(FlashWhite());
        }

        private void HandleDeath()
        {
            if (respawnDelay > 0f)
            {
                StartCoroutine(RespawnAfterDelay());
            }
            else
            {
                SetTargetVisible(false);
            }
        }

        private IEnumerator FlashWhite()
        {
            SetColor(Color.white);
            yield return new WaitForSeconds(hitFlashDuration);
            UpdateHealthColor(health.CurrentHealth, health.MaximumHealth);
            flashRoutine = null;
        }

        private IEnumerator RespawnAfterDelay()
        {
            SetTargetVisible(false);
            yield return new WaitForSeconds(respawnDelay);
            health.RestoreToFull();
            SetTargetVisible(true);
        }

        private void UpdateHealthColor(float current, float maximum)
        {
            float healthRatio = maximum > 0f ? current / maximum : 0f;
            SetColor(Color.Lerp(lowHealthColor, fullHealthColor, healthRatio));
        }

        private void SetColor(Color color)
        {
            EnsureInitialized();
            foreach (Renderer targetRenderer in targetRenderers)
            {
                if (targetRenderer == null)
                {
                    continue;
                }

                targetRenderer.GetPropertyBlock(propertyBlock);
                propertyBlock.SetColor(BaseColorId, color);
                propertyBlock.SetColor(ColorId, color);
                targetRenderer.SetPropertyBlock(propertyBlock);
            }
        }

        private void SetTargetVisible(bool isVisible)
        {
            foreach (Renderer targetRenderer in targetRenderers)
            {
                targetRenderer.enabled = isVisible;
            }

            foreach (Collider targetCollider in targetColliders)
            {
                targetCollider.enabled = isVisible;
            }
        }

        public void ResetTarget()
        {
            EnsureInitialized();
            StopAllCoroutines();
            flashRoutine = null;
            health.RestoreToFull();
            SetTargetVisible(true);
            UpdateHealthColor(health.CurrentHealth, health.MaximumHealth);
        }

        private void EnsureInitialized()
        {
            if (health == null)
            {
                health = GetComponent<Health>();
            }

            if (targetRenderers == null)
            {
                targetRenderers = GetComponentsInChildren<Renderer>(true);
            }

            if (targetColliders == null)
            {
                targetColliders = GetComponentsInChildren<Collider>(true);
            }

            if (propertyBlock == null)
            {
                propertyBlock = new MaterialPropertyBlock();
            }
        }
    }
}
