using System;
using PixelBrawl.AI;
using PixelBrawl.Combat;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelBrawl.Match
{
    public sealed class RoundObjectiveController : MonoBehaviour
    {
        [SerializeField] private RoundManager roundManager;
        [SerializeField] private Health playerHealth;
        [SerializeField] private EnemyBotController[] bots;
        [SerializeField] private Transform[] botSpawns;
        [SerializeField] private BotDifficulty difficulty = BotDifficulty.Normal;

        public event Action<int, int> AliveEnemiesChanged;
        public event Action<BotDifficulty> DifficultyChanged;
        public event Action EnemyEliminated;

        public int AliveEnemies { get; private set; }
        public int TotalEnemies => bots?.Length ?? 0;
        public BotDifficulty Difficulty => difficulty;

        private bool resultSubmitted;

        private void OnEnable()
        {
            roundManager.StateChanged += HandleMatchStateChanged;
            roundManager.RoundPreparing += PrepareRound;
            playerHealth.Died += HandlePlayerDeath;
            foreach (EnemyBotController bot in bots)
            {
                if (bot != null) bot.Eliminated += HandleBotEliminated;
            }
        }

        private void Start()
        {
            ConfigureBots();
            PrepareRound();
            HandleMatchStateChanged(roundManager.State);
        }

        private void OnDisable()
        {
            roundManager.StateChanged -= HandleMatchStateChanged;
            roundManager.RoundPreparing -= PrepareRound;
            playerHealth.Died -= HandlePlayerDeath;
            foreach (EnemyBotController bot in bots)
            {
                if (bot != null) bot.Eliminated -= HandleBotEliminated;
            }
        }

        private void Update()
        {
            HandleDebugControls();
        }

        public void SetDifficulty(BotDifficulty value)
        {
            difficulty = value;
            foreach (EnemyBotController bot in bots) bot?.SetDifficulty(difficulty);
            DifficultyChanged?.Invoke(difficulty);
        }

        public void PrepareRound()
        {
            resultSubmitted = false;
            ConfigureBots();
            for (int index = 0; index < bots.Length; index++)
            {
                if (bots[index] == null) continue;
                Transform spawn = botSpawns != null && botSpawns.Length > 0 ? botSpawns[index % botSpawns.Length] : bots[index].transform;
                bots[index].ResetBot(spawn.position, spawn.rotation);
            }
            RefreshAliveCount();
        }

        private void ConfigureBots()
        {
            foreach (EnemyBotController bot in bots)
            {
                if (bot == null) continue;
                bot.Configure(playerHealth.transform, playerHealth, roundManager, Array.Empty<Transform>());
                bot.SetDifficulty(difficulty);
            }
        }

        private void HandleMatchStateChanged(MatchState state)
        {
            bool combat = state == MatchState.Combat;
            foreach (EnemyBotController bot in bots) bot?.SetCombatEnabled(combat);
        }

        private void HandleBotEliminated(EnemyBotController bot)
        {
            if (roundManager.State != MatchState.Combat || resultSubmitted) return;
            EnemyEliminated?.Invoke();
            RefreshAliveCount();
            if (AliveEnemies == 0)
            {
                resultSubmitted = true;
                roundManager.EndRound(true);
            }
        }

        private void HandlePlayerDeath()
        {
            if (roundManager.State != MatchState.Combat || resultSubmitted) return;
            resultSubmitted = true;
            roundManager.EndRound(false);
        }

        private void RefreshAliveCount()
        {
            AliveEnemies = 0;
            foreach (EnemyBotController bot in bots)
            {
                if (bot != null && bot.IsAlive) AliveEnemies++;
            }
            AliveEnemiesChanged?.Invoke(AliveEnemies, TotalEnemies);
        }

        private void HandleDebugControls()
        {
#if UNITY_EDITOR
            bool enabled = true;
#else
            bool enabled = Debug.isDebugBuild;
#endif
            Keyboard keyboard = Keyboard.current;
            if (!enabled || keyboard == null) return;
            if (keyboard.f10Key.wasPressedThisFrame) roundManager.ForceStartCombat();
            if (keyboard.f11Key.wasPressedThisFrame)
            {
                foreach (EnemyBotController bot in bots) bot?.EliminateForDebug();
            }
            if (keyboard.f12Key.wasPressedThisFrame) roundManager.RestartCurrentRound();
            if (keyboard.pageUpKey.wasPressedThisFrame)
            {
                SetDifficulty((BotDifficulty)(((int)difficulty + 1) % Enum.GetValues(typeof(BotDifficulty)).Length));
            }
        }
    }
}
