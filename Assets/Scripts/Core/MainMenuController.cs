using System.Collections.Generic;
using GridShift.Data;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace GridShift.Core
{
    public class MainMenuController : MonoBehaviour
    {
        [Header("Levels")]
        [SerializeField] private List<LevelData> availableLevels = new List<LevelData>();
        [SerializeField] private string playSceneName = "PlayScene";

        [Header("UI")]
        [SerializeField] private Transform levelButtonRoot;
        [SerializeField] private Button levelButtonPrefab;
        [SerializeField] private Button playAllButton;
        [SerializeField] private Text statusText;

        private void Awake()
        {
            BuildLevelButtons();

            if (playAllButton != null)
            {
                playAllButton.onClick.RemoveListener(PlayAllLevels);
                playAllButton.onClick.AddListener(PlayAllLevels);
            }
        }

        private void BuildLevelButtons()
        {
            if (levelButtonRoot == null || levelButtonPrefab == null)
            {
                SetStatus("Level button root or prefab is not assigned.");
                return;
            }

            for (int i = levelButtonRoot.childCount - 1; i >= 0; i--)
            {
                Transform child = levelButtonRoot.GetChild(i);
                if (levelButtonPrefab != null && child == levelButtonPrefab.transform)
                {
                    child.gameObject.SetActive(false);
                    continue;
                }

                Destroy(child.gameObject);
            }

            int createdCount = 0;
            for (int i = 0; i < availableLevels.Count; i++)
            {
                LevelData level = availableLevels[i];
                if (level == null)
                {
                    continue;
                }

                Button button = Instantiate(levelButtonPrefab, levelButtonRoot);
                button.gameObject.SetActive(true);
                SetButtonLabel(button, BuildLevelButtonLabel(i, level));

                LevelData capturedLevel = level;
                button.onClick.AddListener(() => PlayLevel(capturedLevel));
                createdCount++;
            }

            SetStatus(createdCount > 0 ? "Select a level." : "No levels are assigned.");
        }

        private void PlayLevel(LevelData level)
        {
            if (level == null)
            {
                SetStatus("Selected level is missing.");
                return;
            }

            GameSession.PlaySingleLevel(level);
            SceneManager.LoadScene(playSceneName);
        }

        private void PlayAllLevels()
        {
            List<LevelData> validLevels = availableLevels.FindAll(level => level != null);
            if (validLevels.Count == 0)
            {
                SetStatus("No levels are assigned.");
                return;
            }

            GameSession.PlayStageSequence(validLevels);
            SceneManager.LoadScene(playSceneName);
        }

        private void SetButtonLabel(Button button, string label)
        {
            Text text = button.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = label;
            }
        }

        private string BuildLevelButtonLabel(int index, LevelData level)
        {
            int bestStars = LevelProgressStore.GetBestStars(level);
            if (bestStars <= 0)
            {
                return $"{index + 1}. {level.name}";
            }

            int bestMoves = LevelProgressStore.GetBestMoves(level);
            return bestMoves >= 0
                ? $"{index + 1}. {level.name}  Stars {bestStars}/3  Best {bestMoves}"
                : $"{index + 1}. {level.name}  Stars {bestStars}/3";
        }

        private void SetStatus(string message)
        {
            if (statusText != null)
            {
                statusText.text = message;
            }
        }
    }
}
