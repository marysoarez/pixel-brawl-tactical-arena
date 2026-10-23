using System;
using UnityEngine;

namespace PixelBrawl.Economy
{
    public sealed class EconomyManager : MonoBehaviour
    {
        [SerializeField, Min(0)] private int startingCredits = 800;
        [SerializeField, Min(0)] private int maximumCredits = 9000;
        [SerializeField, Min(0)] private int victoryReward = 3000;
        [SerializeField, Min(0)] private int defeatReward = 1900;
        [SerializeField, Min(0)] private int eliminationReward = 200;

        public event Action<int> CreditsChanged;

        public int Credits { get; private set; }
        public int VictoryReward => victoryReward;
        public int DefeatReward => defeatReward;
        public int EliminationReward => eliminationReward;

        public void ResetForNewMatch()
        {
            Credits = Mathf.Clamp(startingCredits, 0, maximumCredits);
            CreditsChanged?.Invoke(Credits);
        }

        public int AddCredits(int amount)
        {
            if (amount <= 0)
            {
                return Credits;
            }

            Credits = Mathf.Min(maximumCredits, Credits + amount);
            CreditsChanged?.Invoke(Credits);
            return Credits;
        }

        public bool CanAfford(int amount)
        {
            return amount >= 0 && Credits >= amount;
        }

        public bool TrySpend(int amount)
        {
            if (amount < 0 || !CanAfford(amount))
            {
                return false;
            }

            Credits -= amount;
            CreditsChanged?.Invoke(Credits);
            return true;
        }
    }
}
