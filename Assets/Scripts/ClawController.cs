using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace TMKOC.SafetySquad
{
    public class ClawController : MonoBehaviour
    {
        [SerializeField] private RectTransform clawImage;
        [SerializeField] private RectTransform machineImage;
        [SerializeField] private ObjectController[] objectControllers;
        [SerializeField] private RectTransform aimMarkerImage;
        [SerializeField] private Button stopButton;

        [SerializeField] private float clawDropStartOffsetY = 300f;
        [SerializeField] private float clawDropDuration = 1f;
        [SerializeField] private float markerMoveDuration = 0.5f;

        private Sequence markerSequence;
        private ObjectController currentTargetObject;

        private void Awake()
        {
            stopButton.onClick.AddListener(OnStopButtonClicked);
        }

        private void Start()
        {
            AnimateClawIntoMachine();
        }

        private void AnimateClawIntoMachine()
        {
            Vector2 targetPosition = clawImage.anchoredPosition;
            Vector2 startPosition = targetPosition + new Vector2(0f, clawDropStartOffsetY);
            clawImage.anchoredPosition = startPosition;
            clawImage.DOAnchorPos(targetPosition, clawDropDuration)
                .SetEase(Ease.OutBounce)
                .OnComplete(StartMarkerCycle);
        }

        private void StartMarkerCycle()
        {
            if (objectControllers == null || objectControllers.Length == 0) return;

            aimMarkerImage.position = objectControllers[0].transform.position;
            currentTargetObject = objectControllers[0];

            markerSequence = DOTween.Sequence();
            for (int i = 1; i < objectControllers.Length; i++)
            {
                markerSequence.Append(aimMarkerImage.DOMove(objectControllers[i].transform.position, markerMoveDuration).SetEase(Ease.Linear));
            }
            markerSequence.OnUpdate(UpdateCurrentTargetObject);
            markerSequence.SetLoops(-1, LoopType.Yoyo);
        }

        private void UpdateCurrentTargetObject()
        {
            ObjectController closest = objectControllers[0];
            float closestDistance = Vector2.Distance(aimMarkerImage.position, closest.transform.position);
            for (int i = 1; i < objectControllers.Length; i++)
            {
                float distance = Vector2.Distance(aimMarkerImage.position, objectControllers[i].transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closest = objectControllers[i];
                }
            }
            currentTargetObject = closest;
        }

        private void OnStopButtonClicked()
        {
            markerSequence?.Kill();

            bool isCorrect = currentTargetObject != null && currentTargetObject.ObjectType == GameManager.Instance.LevelManager.CorrectObject;
            Debug.Log(isCorrect ? "Correct" : "Incorrect");
        }

        private void OnDestroy()
        {
            markerSequence?.Kill();
        }
    }
}
