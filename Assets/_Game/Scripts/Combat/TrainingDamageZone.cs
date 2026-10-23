using UnityEngine;

namespace PixelBrawl.Combat
{
    public sealed class TrainingDamageZone : MonoBehaviour
    {
        [SerializeField, Min(0f)] private float damagePerTick = 25f;
        [SerializeField, Min(0.05f)] private float tickInterval = 1f;

        private float nextDamageTime;

        private void OnTriggerStay(Collider other)
        {
            if (Time.time < nextDamageTime)
            {
                return;
            }

            Health health = other.GetComponentInParent<Health>();
            if (health == null || !other.CompareTag("Player"))
            {
                return;
            }

            health.TakeDamage(damagePerTick);
            nextDamageTime = Time.time + tickInterval;
        }
    }
}
