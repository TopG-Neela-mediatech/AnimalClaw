using System.Collections;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.claw
{
    public class HandTutorialManager : MonoBehaviour
    {
        [SerializeField] private Image handImage;
        [SerializeField] private RectTransform stopButton;
        [SerializeField] private Vector2 handOffset;

        [Header("Tap Animation")]
        [SerializeField] private float tapScale = 0.8f;
        [SerializeField] private Vector2 tapMove = new Vector2(0f, -15f);
        [SerializeField] private float tapDuration = 0.25f;
        [SerializeField] private float tapPause = 0.3f;

        [Header("Idle Hint")]
        [SerializeField] private float idleDelay = 10f;
        [SerializeField] private float hintDuration = 3f;

        private Coroutine idleRoutine;

        // Progress comes from the Playschool level category: the guided tutorial is level index 0.
        public bool IsTutorialLevel => GameManager.Instance.LevelManager.currentLevelIndex == 0;

        private void Awake()
        {
            handImage.gameObject.SetActive(false);
        }

        // First level: show the tap hint until the user taps (StopHint is called from the stop button).
        public void ShowTutorialHint()
        {
            StopHint();
            ShowHand();
        }

        // Waits for idleDelay, shows the hint for hintDuration, then repeats until StopHint.
        public void StartIdleWatch()
        {
            StopHint();
            idleRoutine = StartCoroutine(IdleRoutine());
        }

        public void StopHint()
        {
            if (idleRoutine != null)
            {
                StopCoroutine(idleRoutine);
                idleRoutine = null;
            }
            HideHand();
        }

        private IEnumerator IdleRoutine()
        {
            // The idle timer starts only once the level-start voice-over has finished (one frame lets it begin first).
            yield return null;
            yield return new WaitUntil(() => !GameManager.Instance.SoundManager.IsLevelStartSequenceRunning);

            while (true)
            {
                yield return new WaitForSeconds(idleDelay);
                ShowHand();
                yield return new WaitForSeconds(hintDuration);
                HideHand();
            }
        }

        private void ShowHand()
        {
            RectTransform handRect = handImage.rectTransform;
            handImage.gameObject.SetActive(true);
            handRect.DOKill();
            handRect.localScale = Vector3.one;
            handRect.position = stopButton.position;
            handRect.anchoredPosition += handOffset;

            Vector2 basePosition = handRect.anchoredPosition;
            DOTween.Sequence()
                .Append(handRect.DOScale(tapScale, tapDuration).SetEase(Ease.OutQuad))
                .Join(handRect.DOAnchorPos(basePosition + tapMove, tapDuration).SetEase(Ease.OutQuad))
                .Append(handRect.DOScale(1f, tapDuration).SetEase(Ease.InQuad))
                .Join(handRect.DOAnchorPos(basePosition, tapDuration).SetEase(Ease.InQuad))
                .AppendInterval(tapPause)
                .SetLoops(-1)
                .SetTarget(handRect)
                .SetLink(handImage.gameObject);
        }

        private void HideHand()
        {
            handImage.rectTransform.DOKill();
            handImage.gameObject.SetActive(false);
        }
    }
}
