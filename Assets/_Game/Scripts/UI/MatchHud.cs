using PixelBrawl.Abilities;
using PixelBrawl.Combat;
using PixelBrawl.Economy;
using PixelBrawl.Inventory;
using PixelBrawl.Match;
using PixelBrawl.Weapons;
using UnityEngine;
using UnityEngine.UI;

namespace PixelBrawl.UI
{
    public sealed class MatchHud : MonoBehaviour
    {
        [SerializeField] private RoundManager roundManager;
        [SerializeField] private EconomyManager economy;
        [SerializeField] private InventoryController inventory;
        [SerializeField] private EquipmentShield equipmentShield;
        [SerializeField] private DashAbility dashAbility;
        [SerializeField] private ShieldAbility shieldAbility;
        [SerializeField] private Text creditsText;
        [SerializeField] private Text roundText;
        [SerializeField] private Text scoreText;
        [SerializeField] private Text stateText;
        [SerializeField] private Text timerText;
        [SerializeField] private Text weaponText;
        [SerializeField] private Text equipmentShieldText;
        [SerializeField] private Text chargesText;
        [SerializeField] private Text roundResultText;
        [SerializeField] private GameObject debugHint;

        private void OnEnable()
        {
            roundManager.StateChanged += UpdateState;
            roundManager.TimerChanged += UpdateTimer;
            roundManager.ScoreChanged += UpdateScore;
            roundManager.RoundFinished += ShowRoundResult;
            economy.CreditsChanged += UpdateCredits;
            inventory.EquippedWeaponChanged += UpdateWeapon;
            equipmentShield.ShieldChanged += UpdateEquipmentShield;
        }

        private void Start()
        {
            UpdateState(roundManager.State);
            UpdateTimer(roundManager.TimeRemaining);
            UpdateScore(roundManager.CurrentRound, roundManager.PlayerScore, roundManager.OpponentScore);
            UpdateCredits(economy.Credits);
            UpdateWeapon(inventory.CurrentSlot, inventory.EquippedWeapon);
            UpdateEquipmentShield(equipmentShield.CurrentShield, equipmentShield.MaximumShield);
            roundResultText.gameObject.SetActive(false);
            if (debugHint != null)
            {
                debugHint.SetActive(Debug.isDebugBuild || Application.isEditor);
            }
        }

        private void Update()
        {
            chargesText.text = $"BONUS CHARGES  DASH {dashAbility.Charges}/{dashAbility.MaximumCharges}  |  SHIELD {shieldAbility.Charges}/{shieldAbility.MaximumCharges}";
        }

        private void OnDisable()
        {
            roundManager.StateChanged -= UpdateState;
            roundManager.TimerChanged -= UpdateTimer;
            roundManager.ScoreChanged -= UpdateScore;
            roundManager.RoundFinished -= ShowRoundResult;
            economy.CreditsChanged -= UpdateCredits;
            inventory.EquippedWeaponChanged -= UpdateWeapon;
            equipmentShield.ShieldChanged -= UpdateEquipmentShield;
        }

        private void UpdateCredits(int credits)
        {
            creditsText.text = $"{credits} CR";
        }

        private void UpdateScore(int round, int playerScore, int opponentScore)
        {
            roundText.text = $"ROUND {round}";
            scoreText.text = $"{playerScore}  -  {opponentScore}";
        }

        private void UpdateState(MatchState state)
        {
            stateText.text = state.ToString().ToUpperInvariant();
            if (state != MatchState.RoundEnd)
            {
                roundResultText.gameObject.SetActive(false);
            }
        }

        private void UpdateTimer(float seconds)
        {
            int totalSeconds = Mathf.CeilToInt(seconds);
            timerText.text = $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        private void UpdateWeapon(WeaponSlot slot, WeaponData data)
        {
            weaponText.text = slot == WeaponSlot.Melee
                ? "MELEE"
                : data != null ? data.DisplayName.ToUpperInvariant() : "EMPTY SLOT";
        }

        private void UpdateEquipmentShield(float current, float maximum)
        {
            equipmentShieldText.text = $"ARMOR  {Mathf.CeilToInt(current)} / {Mathf.CeilToInt(maximum)}";
        }

        private void ShowRoundResult(bool playerWon)
        {
            roundResultText.text = playerWon ? "ROUND WON" : "ROUND LOST";
            roundResultText.gameObject.SetActive(true);
        }
    }
}
