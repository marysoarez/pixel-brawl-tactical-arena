using System;
using PixelBrawl.Combat;
using PixelBrawl.Match;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

namespace PixelBrawl.AI
{
    [RequireComponent(typeof(Health), typeof(NavMeshAgent), typeof(Collider))]
    public sealed class EnemyBotController : MonoBehaviour
    {
        [Header("Perception")]
        [SerializeField, Range(1f, 180f)] private float fieldOfView = 100f;
        [SerializeField, Min(1f)] private float detectionRange = 22f;
        [SerializeField, Min(0f)] private float targetMemoryDuration = 4f;
        [SerializeField] private LayerMask sightMask = ~0;

        [Header("Combat")]
        [SerializeField, Min(0f)] private float damagePerShot = 10f;
        [SerializeField, Min(0.1f)] private float shotsPerSecond = 2f;
        [SerializeField, Min(1f)] private float attackRange = 25f;
        [SerializeField, Min(0f)] private float stoppingDistance = 7f;
        [SerializeField, Range(0f, 1f)] private float accuracy = 0.65f;
        [SerializeField, Min(0f)] private float reactionTime = 0.5f;
        [SerializeField, Range(0f, 1f)] private float retreatHealthRatio = 0.3f;

        [Header("Navigation")]
        [SerializeField] private Transform[] patrolPoints;
        [SerializeField, Min(0.05f)] private float repathInterval = 0.25f;
        [SerializeField, Min(0.1f)] private float patrolWait = 1.25f;

        public event Action<EnemyBotController> Eliminated;
        public event Action<BotState> StateChanged;

        public BotState State { get; private set; } = BotState.Idle;
        public bool IsAlive => health != null && health.IsAlive;
        public BotDifficulty Difficulty { get; private set; } = BotDifficulty.Normal;

        private Health health;
        private NavMeshAgent agent;
        private Transform player;
        private Health playerHealth;
        private RoundManager roundManager;
        private Renderer[] renderers;
        private Collider[] colliders;
        private Vector3 spawnPosition;
        private Quaternion spawnRotation;
        private Vector3 lastKnownPosition;
        private float lastSeenTime = float.NegativeInfinity;
        private float firstSeenTime = float.PositiveInfinity;
        private float nextShotTime;
        private float nextRepathTime;
        private float patrolWaitUntil;
        private int patrolIndex;

        private bool CombatActive => roundManager != null && roundManager.State == MatchState.Combat;

        private void Awake()
        {
            health = GetComponent<Health>();
            agent = GetComponent<NavMeshAgent>();
            renderers = GetComponentsInChildren<Renderer>(true);
            colliders = GetComponentsInChildren<Collider>(true);
            spawnPosition = transform.position;
            spawnRotation = transform.rotation;
            agent.stoppingDistance = stoppingDistance;
        }

        private void OnEnable()
        {
            health.Died += HandleDeath;
        }

        private void OnDisable()
        {
            health.Died -= HandleDeath;
        }

        private void Update()
        {
            if (!IsAlive || !CombatActive || player == null || playerHealth == null || !playerHealth.IsAlive)
            {
                StopMoving();
                if (IsAlive) SetState(BotState.Idle);
                return;
            }

            bool canSeePlayer = CanSeePlayer();
            if (canSeePlayer)
            {
                lastKnownPosition = player.position;
                lastSeenTime = Time.time;
                if (float.IsPositiveInfinity(firstSeenTime)) firstSeenTime = Time.time;
            }
            else
            {
                firstSeenTime = float.PositiveInfinity;
            }

            float distance = Vector3.Distance(transform.position, player.position);
            if (ShouldRetreat(distance))
            {
                Retreat();
            }
            else if (canSeePlayer && distance <= attackRange && distance <= stoppingDistance + 1.5f)
            {
                Attack();
            }
            else if (canSeePlayer)
            {
                Chase();
            }
            else if (Time.time - lastSeenTime <= targetMemoryDuration)
            {
                Investigate();
            }
            else
            {
                Patrol();
            }
        }

        public void Configure(Transform target, Health targetHealth, RoundManager match, Transform[] points)
        {
            player = target;
            playerHealth = targetHealth;
            roundManager = match;
            if (points != null && points.Length > 0)
            {
                patrolPoints = points;
            }
        }

        public void SetDifficulty(BotDifficulty difficulty)
        {
            Difficulty = difficulty;
            switch (difficulty)
            {
                case BotDifficulty.Easy:
                    reactionTime = 0.9f;
                    accuracy = 0.35f;
                    agent.speed = 2.4f;
                    break;
                case BotDifficulty.Hard:
                    reactionTime = 0.25f;
                    accuracy = 0.82f;
                    agent.speed = 4.2f;
                    break;
                default:
                    reactionTime = 0.5f;
                    accuracy = 0.62f;
                    agent.speed = 3.4f;
                    break;
            }
        }

        public void ResetBot(Vector3 position, Quaternion rotation)
        {
            StopAllCoroutines();
            spawnPosition = position;
            spawnRotation = rotation;
            SetVisible(true);
            health.RestoreToFull();
            health.SetDamageEnabled(false);
            lastSeenTime = float.NegativeInfinity;
            firstSeenTime = float.PositiveInfinity;
            nextShotTime = 0f;
            patrolIndex = 0;
            if (agent.enabled && agent.isOnNavMesh) agent.Warp(position);
            else transform.SetPositionAndRotation(position, rotation);
            SetState(BotState.Idle);
            StopMoving();
        }

        public void SetCombatEnabled(bool enabled)
        {
            health.SetDamageEnabled(enabled);
            if (!enabled)
            {
                StopMoving();
                if (IsAlive) SetState(BotState.Idle);
            }
        }

        public void EliminateForDebug()
        {
            if (IsAlive)
            {
                health.SetDamageEnabled(true);
                health.TakeDamage(health.CurrentHealth);
            }
        }

        private bool CanSeePlayer()
        {
            Vector3 eye = transform.position + Vector3.up * 1.45f;
            Vector3 target = player.position + Vector3.up * 1.2f;
            Vector3 toTarget = target - eye;
            if (toTarget.sqrMagnitude > detectionRange * detectionRange) return false;
            if (Vector3.Angle(transform.forward, toTarget) > fieldOfView * 0.5f) return false;
            if (!Physics.Raycast(eye, toTarget.normalized, out RaycastHit hit, toTarget.magnitude, sightMask, QueryTriggerInteraction.Ignore)) return false;
            return hit.collider.transform == player || hit.collider.transform.IsChildOf(player);
        }

        private bool ShouldRetreat(float distance)
        {
            return Difficulty != BotDifficulty.Easy && health.CurrentHealth / health.MaximumHealth <= retreatHealthRatio && distance < stoppingDistance;
        }

        private void Patrol()
        {
            SetState(BotState.Patrol);
            if (patrolPoints == null || patrolPoints.Length == 0 || !CanNavigate()) return;
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
            {
                if (Time.time < patrolWaitUntil) return;
                patrolIndex = (patrolIndex + 1) % patrolPoints.Length;
                patrolWaitUntil = Time.time + patrolWait;
            }
            SetDestination(patrolPoints[patrolIndex].position);
        }

        private void Investigate()
        {
            SetState(BotState.Investigate);
            SetDestination(lastKnownPosition);
        }

        private void Chase()
        {
            SetState(BotState.Chase);
            SetDestination(player.position);
        }

        private void Retreat()
        {
            SetState(BotState.Retreat);
            Vector3 away = (transform.position - player.position).normalized;
            Vector3 side = Vector3.Cross(Vector3.up, away) * (Difficulty == BotDifficulty.Hard ? 3f : 0f);
            SetDestination(transform.position + away * 6f + side);
        }

        private void Attack()
        {
            SetState(BotState.Attack);
            StopMoving();
            FacePlayer();
            if (Time.time < firstSeenTime + reactionTime || Time.time < nextShotTime) return;
            nextShotTime = Time.time + 1f / shotsPerSecond;

            Vector3 origin = transform.position + Vector3.up * 1.45f;
            Vector3 target = player.position + Vector3.up * 1.2f;
            float missRadius = Mathf.Lerp(1.6f, 0.08f, accuracy);
            target += transform.right * Random.Range(-missRadius, missRadius) + Vector3.up * Random.Range(-missRadius, missRadius);
            Vector3 direction = (target - origin).normalized;
            if (Physics.Raycast(origin, direction, out RaycastHit hit, attackRange, sightMask, QueryTriggerInteraction.Ignore) &&
                (hit.collider.transform == player || hit.collider.transform.IsChildOf(player)))
            {
                playerHealth.TakeDamage(damagePerShot);
            }
        }

        private void FacePlayer()
        {
            Vector3 direction = player.position - transform.position;
            direction.y = 0f;
            if (direction.sqrMagnitude > 0.001f)
            {
                transform.rotation = Quaternion.RotateTowards(transform.rotation, Quaternion.LookRotation(direction), 360f * Time.deltaTime);
            }
        }

        private bool CanNavigate()
        {
            return agent != null && agent.enabled && agent.isOnNavMesh;
        }

        private void SetDestination(Vector3 destination)
        {
            if (!CanNavigate() || Time.time < nextRepathTime) return;
            agent.isStopped = false;
            agent.SetDestination(destination);
            nextRepathTime = Time.time + repathInterval;
        }

        private void StopMoving()
        {
            if (CanNavigate())
            {
                agent.isStopped = true;
                agent.ResetPath();
            }
        }

        private void HandleDeath()
        {
            SetState(BotState.Dead);
            StopMoving();
            SetVisible(false);
            Eliminated?.Invoke(this);
        }

        private void SetVisible(bool visible)
        {
            foreach (Renderer item in renderers) item.enabled = visible;
            foreach (Collider item in colliders) item.enabled = visible;
            if (agent != null) agent.enabled = visible;
        }

        private void SetState(BotState newState)
        {
            if (State == newState) return;
            State = newState;
            StateChanged?.Invoke(State);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            Vector3 left = Quaternion.Euler(0f, -fieldOfView * 0.5f, 0f) * transform.forward;
            Vector3 right = Quaternion.Euler(0f, fieldOfView * 0.5f, 0f) * transform.forward;
            Gizmos.color = Color.cyan;
            Gizmos.DrawRay(transform.position + Vector3.up * 1.45f, left * detectionRange);
            Gizmos.DrawRay(transform.position + Vector3.up * 1.45f, right * detectionRange);
            if (agent != null && agent.hasPath)
            {
                Gizmos.color = Color.magenta;
                Vector3[] corners = agent.path.corners;
                for (int index = 1; index < corners.Length; index++) Gizmos.DrawLine(corners[index - 1], corners[index]);
            }
        }
    }
}
