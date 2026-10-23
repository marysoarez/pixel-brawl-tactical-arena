using System;
using PixelBrawl.Abilities;
using PixelBrawl.Combat;
using PixelBrawl.Economy;
using PixelBrawl.Inventory;
using PixelBrawl.Player;
using PixelBrawl.Weapons;
using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelBrawl.Match
{
    public sealed class RoundManager : MonoBehaviour
    {
        [Header("Timing")]
        [SerializeField, Min(0f)] private float buyPhaseDuration = 20f;
        [SerializeField, Min(0f)] private float combatDuration = 90f;
        [SerializeField, Min(0f)] private float roundEndDuration = 5f;
        [SerializeField, Min(1)] private int roundsToWin = 7;
        [SerializeField] private bool autoStartMatch = true;

        [Header("References")]
        [SerializeField] private EconomyManager economy;
        [SerializeField] private InventoryController inventory;
        [SerializeField] private PlayerActionGate actionGate;
        [SerializeField] private Health playerHealth;
        [SerializeField] private EquipmentShield equipmentShield;
        [SerializeField] private ShieldAbility shieldAbility;
        [SerializeField] private DashAbility dashAbility;
        [SerializeField] private HitscanWeapon weapon;
        [SerializeField] private Transform playerTransform;
        [SerializeField] private Transform playerSpawn;
        [SerializeField] private CharacterController characterController;
        [SerializeField] private Health[] combatHealth;
        [SerializeField] private TrainingTarget[] trainingTargets;

        public event Action<MatchState> StateChanged;
        public event Action<float> TimerChanged;
        public event Action<int, int, int> ScoreChanged;
        public event Action<bool> RoundFinished;
        public event Action RoundPreparing;

        public MatchState State { get; private set; } = MatchState.Waiting;
        public float TimeRemaining { get; private set; }
        public int CurrentRound { get; private set; }
        public int PlayerScore { get; private set; }
        public int OpponentScore { get; private set; }

        private void OnEnable()
        {
            if (weapon != null)
            {
                weapon.HitConfirmed += HandleHitConfirmed;
            }
        }

        private void Start()
        {
            if (autoStartMatch)
            {
                StartMatch();
            }
            else
            {
                EnterState(MatchState.Waiting, 0f);
            }
        }

        private void Update()
        {
            HandleDebugControls();

            if (State is MatchState.Waiting or MatchState.MatchEnd)
            {
                return;
            }

            TimeRemaining = Mathf.Max(0f, TimeRemaining - Time.deltaTime);
            TimerChanged?.Invoke(TimeRemaining);
            if (TimeRemaining > 0f)
            {
                return;
            }

            switch (State)
            {
                case MatchState.BuyPhase:
                    EnterState(MatchState.Combat, combatDuration);
                    break;
                case MatchState.Combat:
                    EndRound(false);
                    break;
                case MatchState.RoundEnd:
                    StartNextRoundOrFinishMatch();
                    break;
            }
        }

        private void OnDisable()
        {
            if (weapon != null)
            {
                weapon.HitConfirmed -= HandleHitConfirmed;
            }
        }

        public void StartMatch()
        {
            PlayerScore = 0;
            OpponentScore = 0;
            CurrentRound = 1;
            economy.ResetForNewMatch();
            inventory.ResetForNewMatch();
            equipmentShield.ResetForNewMatch();
            shieldAbility.ResetForNewMatch();
            dashAbility.ResetForNewMatch();
            ScoreChanged?.Invoke(CurrentRound, PlayerScore, OpponentScore);
            PrepareRound();
            EnterState(MatchState.BuyPhase, buyPhaseDuration);
        }

        public void EndRound(bool playerWon)
        {
            if (State != MatchState.Combat)
            {
                return;
            }

            if (playerWon)
            {
                PlayerScore++;
                economy.AddCredits(economy.VictoryReward);
            }
            else
            {
                OpponentScore++;
                economy.AddCredits(economy.DefeatReward);
            }

            RoundFinished?.Invoke(playerWon);
            ScoreChanged?.Invoke(CurrentRound, PlayerScore, OpponentScore);
            EnterState(MatchState.RoundEnd, roundEndDuration);
        }

        public void RestartMatch()
        {
            StartMatch();
        }

        public void ForceStartCombat()
        {
            if (State == MatchState.BuyPhase || State == MatchState.Waiting)
            {
                EnterState(MatchState.Combat, combatDuration);
            }
        }

        public void RestartCurrentRound()
        {
            PrepareRound();
            EnterState(MatchState.BuyPhase, buyPhaseDuration);
        }

        private void StartNextRoundOrFinishMatch()
        {
            if (PlayerScore >= roundsToWin || OpponentScore >= roundsToWin)
            {
                EnterState(MatchState.MatchEnd, 0f);
                return;
            }

            CurrentRound++;
            ScoreChanged?.Invoke(CurrentRound, PlayerScore, OpponentScore);
            PrepareRound();
            EnterState(MatchState.BuyPhase, buyPhaseDuration);
        }

        private void PrepareRound()
        {
            playerHealth.RestoreToFull();
            shieldAbility.ResetTemporaryState();
            dashAbility.ResetTemporaryState();

            if (characterController != null)
            {
                characterController.enabled = false;
            }
            playerTransform.SetPositionAndRotation(playerSpawn.position, playerSpawn.rotation);
            if (characterController != null)
            {
                characterController.enabled = true;
            }

            foreach (TrainingTarget target in trainingTargets)
            {
                if (target != null)
                {
                    target.ResetTarget();
                }
            }

            RoundPreparing?.Invoke();
        }

        private void EnterState(MatchState newState, float duration)
        {
            State = newState;
            TimeRemaining = duration;
            bool combatEnabled = newState == MatchState.Combat;
            actionGate.SetCombatAllowed(combatEnabled);
            foreach (Health health in combatHealth)
            {
                health?.SetDamageEnabled(combatEnabled);
            }
            StateChanged?.Invoke(State);
            TimerChanged?.Invoke(TimeRemaining);
        }

        private void HandleHitConfirmed(bool eliminated)
        {
            if (eliminated && State == MatchState.Combat)
            {
                economy.AddCredits(economy.EliminationReward);
            }
        }

        private void HandleDebugControls()
        {
#if UNITY_EDITOR
            bool debugControlsEnabled = true;
#else
            bool debugControlsEnabled = Debug.isDebugBuild;
#endif
            if (!debugControlsEnabled)
            {
                return;
            }

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            if (keyboard.f5Key.wasPressedThisFrame) StartMatch();
            if (keyboard.f6Key.wasPressedThisFrame) EndRound(true);
            if (keyboard.f7Key.wasPressedThisFrame) EndRound(false);
            if (keyboard.f8Key.wasPressedThisFrame) economy.AddCredits(1000);
            if (keyboard.f9Key.wasPressedThisFrame) RestartMatch();
        }
    }
}
