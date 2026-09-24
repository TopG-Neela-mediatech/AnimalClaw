using System.Collections.Generic;
using System.Linq;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.claw
{
    public class ClawController : MonoBehaviour
    {
        [SerializeField] private RectTransform clawImage;
        [SerializeField] private RectTransform machineImage;
        [SerializeField] private Transform objectParent;
        [SerializeField] private RectTransform aimMarkerImage;
        [SerializeField] private Button stopButton;
        [SerializeField] private LivesController livesController;

        [SerializeField] private float clawDropStartOffsetY = 300f;
        [SerializeField] private float clawDropDuration = 1f;
        [SerializeField] private float markerMoveDuration = 0.5f;

        private readonly List<ObjectController> objectControllers = new List<ObjectController>();
        private Sequence markerSequence;
        private ObjectController currentTargetObject;

        private void Awake()
        {
            stopButton.onClick.AddListener(OnStopButtonClicked);
        }

        private void Start()
        {
            SpawnObjectControllers();
            AnimateClawIntoMachine();
        }

        private void SpawnObjectControllers()
        {
            objectControllers.Clear();

            LevelManager levelManager = GameManager.Instance.LevelManager;
            LevelData levelData = levelManager.CurrentLevelData;

            foreach (ObjectController option in levelData.Options)
            {
                ObjectController instance = Instantiate(levelManager.ObjectControllerPrefab, objectParent);
                SpriteDataSO spriteData = levelData.SpriteDataList.FirstOrDefault(data => data.ObjectType == option.ObjectType);
                Sprite sprite = spriteData != null && spriteData.Sprites.Length > 0 ? spriteData.Sprites[0] : null;
                instance.Initialize(option.ObjectType, sprite);
                objectControllers.Add(instance);
            }
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
            if (objectControllers.Count == 0) return;

            aimMarkerImage.position = objectControllers[0].transform.position;
            currentTargetObject = objectControllers[0];

            markerSequence = DOTween.Sequence();
            for (int i = 1; i < objectControllers.Count; i++)
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
            for (int i = 1; i < objectControllers.Count; i++)
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

            if (isCorrect)
            {
                GameManager.Instance.InvokeLevelWin();
            }
            else
            {
                livesController.OnIncorrectAttempt();
            }
        }

        private void OnDestroy()
        {
            markerSequence?.Kill();
        }
    }
}
