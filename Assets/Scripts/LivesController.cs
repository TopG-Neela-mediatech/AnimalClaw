using DG.Tweening;
using TMPro;
using UnityEngine;

namespace tmkoc.claw
{
    public class LivesController : MonoBehaviour
    {
        [SerializeField] private RectTransform livesParent;
        [SerializeField] private TextMeshProUGUI livesText;

        [SerializeField] private float shakeDuration = 0.3f;
        [SerializeField] private float shakeStrength = 20f;
        [SerializeField] private int shakeVibrato = 10;

        private int currentLives;

        private void Start()
        {
            currentLives = GameManager.Instance.LevelManager.CurrentLevelData.Lives;
            UpdateLivesText();
        }

        public void OnIncorrectAttempt()
        {
            livesParent.DOKill();
            livesParent.DOShakeAnchorPos(shakeDuration, shakeStrength, shakeVibrato);

            currentLives--;
            UpdateLivesText();

            if (currentLives <= 0)
            {
                EndPanelScript.Instance.ShowLose();
            }
        }

        private void UpdateLivesText()
        {
            livesText.text = currentLives.ToString();
        }
    }
}
