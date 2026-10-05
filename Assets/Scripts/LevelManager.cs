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

        private const string StorySeenKey = "AnimalClawStorySeen";

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

            bool isFirstTime = currentLevelIndex == 0 && PlayerPrefs.GetInt(StorySeenKey, 0) == 0;
            if (isFirstTime)
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
            PlayerPrefs.SetInt(StorySeenKey, 1);
            StartLevel();
        }
        private void OnLevelStart()
        {
         
        }
        private void OnLevelWin()
        {
            EndPanelScript.Instance.ShowWin();
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
            ReloadScene();
        }
        public void RetryLevel()
        {
            ReloadScene();
        }
        private void ReloadScene()
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
        private void OnDestroy()
        {
            GameManager.Instance.OnLevelStart -= OnLevelStart;
            GameManager.Instance.OnLevelWin -= OnLevelWin;
            storyController.OnStoryFinished -= OnStoryFinished;
        }
    }
}
