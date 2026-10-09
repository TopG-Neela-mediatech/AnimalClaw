using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace tmkoc.claw
{
    public class LevelManager : MonoBehaviour
    {
        [SerializeField] private Button playSchoolBackButton;
        [SerializeField] private LevelData[] levels;
        [SerializeField] private ObjectController objectControllerPrefab;
        [SerializeField] private StoryController storyController;
        private LevelData currentLevelData;
        public int currentLevelIndex { get; private set; }
        public Objects CorrectObject => currentLevelData.CorrectObject;
        public LevelData CurrentLevelData => currentLevelData;
        public ObjectController ObjectControllerPrefab => objectControllerPrefab;
        public bool HasLevelStarted { get; private set; }

        private void StartLevel()
        {
            HasLevelStarted = true;
            GameManager.Instance.InvokeLevelStart();
        }


        private void Awake()
        {
            SetDataSaver();
         //   PlayschoolCommon.Instance.SpawnplayschoolWinLosePanel();
            playSchoolBackButton.onClick.AddListener(() => SceneManager.LoadScene(TMKOCPlaySchoolConstants.TMKOCPlayMainMenu));
        }
        private void Start()
        {
            GameManager.Instance.OnLevelStart += OnLevelStart;
            GameManager.Instance.OnLevelWin += OnLevelWin;

            // Progress comes from the Playschool level category: no saved progress means a first launch, so play the story.
            if (currentLevelIndex == 0)
            {
                storyController.OnStoryFinished += OnStoryFinished;
                storyController.gameObject.SetActive(true);
            }
            else
            {
                StartLevel();
            }
        }
        private void OnStoryFinished()
        {
            storyController.OnStoryFinished -= OnStoryFinished;
            storyController.gameObject.SetActive(false);
            StartLevel();
        }
        private void OnLevelStart()
        {
            GameManager.Instance.SoundManager.PlayLevelStartSequence(currentLevelIndex == 0);
        }
        private void OnLevelWin()
        {
            EndPanelScript.Instance.ShowWin();
            if (currentLevelIndex == levels.Length - 1)
            {
                GameManager.Instance.SoundManager.PlayFinalOutro();
            }
        }
        private IEnumerator LoadWinPanelWithDelay(float delay)
        {
            yield return new WaitForSeconds(delay);          
            WinLosePanelScript.Instance.ShowNextLevelPopUp(LoadNextLevel);
        }
        private void SetDataSaver()
        {
            HelperGameCategoryDataSaver.Init(levels.Length);
            currentLevelIndex = HelperGameCategoryDataSaver.GetStartLevel();
            currentLevelData = levels[currentLevelIndex];
        }
        private void SaveLevel()
        {
            currentLevelIndex++;
            // Saved even past the last level; GetStartLevel() wraps a saved index >= level count back to 0.
            HelperGameCategoryDataSaver.LevelCompleted(currentLevelIndex);
            if (currentLevelIndex >= levels.Length)
            {
#if PLAYSCHOOL_MAIN
                    EffectParticleControll.Instance.SpawnGameEndPanel();
                   //GameManager.Instance.SoundManager.PlayFinalOutro();
                    GameOverEndPanel.Instance.AddTheListnerRetryGame();
                    return;
#endif
                currentLevelIndex = 0;
                return;
            }
        }
        public void LoadNextLevel()
        {
            SaveLevel();
            RestartLevel();
        }
        public void RetryLevel()
        {
            RestartLevel();
        }
        // Restarts the current level index in place (index 0 after the last level) without reloading the scene.
        private void RestartLevel()
        {
            if (currentLevelIndex >= levels.Length)
            {
                currentLevelIndex = 0;
            }
            currentLevelData = levels[currentLevelIndex];
            HasLevelStarted = false;
            GameManager.Instance.SoundManager.StopVoiceAndAnimal();
            GameManager.Instance.InvokeLevelReset();
            StartLevel();
        }
        private void OnDestroy()
        {
            GameManager.Instance.OnLevelStart -= OnLevelStart;
            GameManager.Instance.OnLevelWin -= OnLevelWin;
            storyController.OnStoryFinished -= OnStoryFinished;
        }
    }
}
