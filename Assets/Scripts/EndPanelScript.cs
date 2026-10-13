using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System;


namespace tmkoc.claw
{
    public class EndPanelScript : MonoBehaviour
    {
        public static EndPanelScript Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Image winImage;
        [SerializeField] private Image loseImage;
        [SerializeField] private RectTransform winPanel;
        [SerializeField] private RectTransform losePanel;
        [SerializeField] private Button[] homeButtons;
        [SerializeField] private RectTransform winRaysT;
        [SerializeField] private RectTransform loseRaysT;

        [Header("Slide Anim")]
        [SerializeField] private Vector2 onScreenAnchoredPos = Vector2.zero;
        [SerializeField] private float slideInDuration = 0.9f;
        [SerializeField] private float slideOutDuration = 0.5f;
        [SerializeField] private Ease slideInEase = Ease.OutBack;
        [SerializeField] private Ease slideOutEase = Ease.InBack;

        [Header("Auto Hide")]
        [SerializeField] private float minDisplayTime = 5f;

        public bool IsShowing => (winPanel != null && winPanel.gameObject.activeSelf) || (losePanel != null && losePanel.gameObject.activeSelf);

        private RectTransform activePanel;
        private Action onPanelHidden;
        private Coroutine autoHideRoutine;

        private void Awake()
        {
            Instance = this;
            winPanel.gameObject.SetActive(false);
            losePanel.gameObject.SetActive(false);
        }
        private void Start()
        {
            foreach (var homeButton in homeButtons)
            {
                homeButton.onClick.AddListener(() => SceneManager.LoadScene(TMKOCPlaySchoolConstants.TMKOCPlayMainMenu));
            }
        }
        private void OnDisable()
        {
            winPanel.DOKill();
            losePanel.DOKill();
        }

        // Public API
        public void ShowWin()
        {
            winImage.sprite = GameManager.Instance.LevelManager.CurrentLevelData.WinSprite;
            Show(winPanel, () => GameManager.Instance.LevelManager.LoadNextLevel());
            RotateRaysLoop(winRaysT, 8f, true);
        }

        public void ShowLose()
        {
            loseImage.sprite = GameManager.Instance.LevelManager.CurrentLevelData.LoseSprite;
            Show(losePanel, () => GameManager.Instance.LevelManager.RetryLevel());
            RotateRaysLoop(loseRaysT, 8f, false);
        }

        private void Show(RectTransform panel, Action onHidden)
        {
            activePanel = panel;
            onPanelHidden = onHidden;

            panel.gameObject.SetActive(true);
            panel.DOKill();
            panel.anchoredPosition = OffScreenPos(panel);
            panel.DOAnchorPos(onScreenAnchoredPos, slideInDuration).SetEase(slideInEase);

            if (autoHideRoutine != null)
            {
                StopCoroutine(autoHideRoutine);
            }
            autoHideRoutine = StartCoroutine(AutoHideRoutine());
        }

        // Stay up for minDisplayTime, or until the voice-over that is playing has finished if that takes longer.
        private IEnumerator AutoHideRoutine()
        {
            yield return new WaitForSeconds(minDisplayTime);
            AudioSource voiceSource = RuntimeAudioLoader.Instance._commonAudioSource;
            yield return new WaitUntil(() => !voiceSource.isPlaying);
            autoHideRoutine = null;
            HidePanel();
        }

        // Panel's RectTransform is stretched to the canvas, so its own height is a resolution-independent slide distance.
        private Vector2 OffScreenPos(RectTransform panel)
        {
            return new Vector2(0f, -panel.rect.height);
        }
        private void RotateRaysLoop(RectTransform rays, float duration = 8f, bool clockwise = true)
        {
            rays.DOKill();
            float angle = clockwise ? -360f : 360f;
            rays.DORotate(new Vector3(0f, 0f, angle), duration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetRelative(true);
        }
        // Starts the next level or restarts the current one, then slides the panel away.
        private void HidePanel()
        {
            onPanelHidden?.Invoke();

            activePanel.DOKill();
            activePanel.DOAnchorPos(OffScreenPos(activePanel), slideOutDuration)
                .SetEase(slideOutEase)
                .OnComplete(() => activePanel.gameObject.SetActive(false));
        }
    }
}
