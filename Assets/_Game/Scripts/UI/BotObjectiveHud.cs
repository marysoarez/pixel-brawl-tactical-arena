using System.Collections;
using PixelBrawl.AI;
using PixelBrawl.Match;
using UnityEngine;
using UnityEngine.UI;

namespace PixelBrawl.UI
{
    public sealed class BotObjectiveHud : MonoBehaviour
    {
        [SerializeField] private RoundObjectiveController objectives;
        [SerializeField] private Text enemiesText;
        [SerializeField] private Text difficultyText;
        [SerializeField] private Text eliminationText;

        private Coroutine feedbackRoutine;

        private void OnEnable()
        {
            objectives.AliveEnemiesChanged += UpdateEnemies;
            objectives.DifficultyChanged += UpdateDifficulty;
            objectives.EnemyEliminated += ShowElimination;
        }

        private void Start()
        {
            UpdateEnemies(objectives.AliveEnemies, objectives.TotalEnemies);
            UpdateDifficulty(objectives.Difficulty);
            eliminationText.gameObject.SetActive(false);
        }

        private void OnDisable()
        {
            objectives.AliveEnemiesChanged -= UpdateEnemies;
            objectives.DifficultyChanged -= UpdateDifficulty;
            objectives.EnemyEliminated -= ShowElimination;
        }

        private void UpdateEnemies(int alive, int total)
        {
            enemiesText.text = $"ENEMIES  {alive} / {total}";
        }

        private void UpdateDifficulty(BotDifficulty difficulty)
        {
            difficultyText.text = $"DIFFICULTY  {difficulty.ToString().ToUpperInvariant()}";
        }

        private void ShowElimination()
        {
            if (feedbackRoutine != null) StopCoroutine(feedbackRoutine);
            feedbackRoutine = StartCoroutine(ShowFeedback());
        }

        private IEnumerator ShowFeedback()
        {
            eliminationText.gameObject.SetActive(true);
            yield return new WaitForSeconds(1.2f);
            eliminationText.gameObject.SetActive(false);
            feedbackRoutine = null;
        }
    }
}
