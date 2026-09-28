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
        private LevelData currentLevelData;
        public int currentLevelIndex { get; private set; }
        public Objects CorrectObject => currentLevelData.CorrectObject;
        public LevelData CurrentLevelData => currentLevelData;
        public ObjectController ObjectControllerPrefab => objectControllerPrefab;
        private void StartLevel() => GameManager.Instance.InvokeLevelStart();


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
            HelperGameCategoryDataSaver.LevelCompleted(currentLevelIndex);
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

        }
    }
}
