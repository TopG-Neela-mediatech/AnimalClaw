using System;
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
        [SerializeField] private RectTransform clawHandAttachPoint;
        [SerializeField] private RectTransform machineImage;
        [SerializeField] private Transform objectParent;
        [SerializeField] private Button stopButton;
        [SerializeField] private LivesController livesController;

        [Header("Chain Link")]
        [SerializeField] private RectTransform linkPrefab;
        [SerializeField] private RectTransform linkParent;
        [SerializeField] private float linkSpacing = 50f;

        [Header("Claw Movement")]
        [SerializeField] private float clawDropStartOffsetY = 300f;
        [SerializeField] private float entryDropDuration = 1f;
        [SerializeField] private float horizontalMoveDuration = 0.5f;
        [SerializeField] private float grabDropDuration = 0.6f;
        [SerializeField] private float grabLiftDuration = 0.6f;
        [SerializeField] private float shakeDuration = 0.3f;
        [SerializeField] private float shakeStrength = 30f;

        [Header("Win Animation")]
        [SerializeField] private float winScale = 1.3f;
        [SerializeField] private float winScaleDuration = 0.6f;
        [SerializeField] private float winSpinDuration = 0.8f;

        private readonly List<ObjectController> objectControllers = new List<ObjectController>();
        private readonly List<RectTransform> activeLinks = new List<RectTransform>();

        private Sequence horizontalSequence;
        private ObjectController currentTargetObject;
        private float topAnchorWorldY;
        private float restWorldY;

        private void Awake()
        {
            stopButton.onClick.AddListener(OnStopButtonClicked);
        }

        private void Start()
        {
            stopButton.interactable = true;
            SpawnObjectControllers();
            AnimateClawIntoMachine();
        }

        private void SpawnObjectControllers()
        {
            objectControllers.Clear();

            LevelManager levelManager = GameManager.Instance.LevelManager;
            LevelData levelData = levelManager.CurrentLevelData;

            foreach (Objects optionType in levelData.Options)
            {
                ObjectController instance = Instantiate(levelManager.ObjectControllerPrefab, objectParent);
                ObjectSpriteData spriteData = levelData.SpriteData.ObjectSprites.FirstOrDefault(data => data.ObjectType == optionType);
                instance.Initialize(optionType, spriteData?.Sprite);
                objectControllers.Add(instance);
            }
        }

        private void AnimateClawIntoMachine()
        {
            Vector2 targetPosition = clawImage.anchoredPosition;
            Vector2 startPosition = targetPosition + new Vector2(0f, clawDropStartOffsetY);
            clawImage.anchoredPosition = startPosition;

            activeLinks.Clear();
            topAnchorWorldY = clawImage.position.y;

            clawImage.DOAnchorPos(targetPosition, entryDropDuration)
                .SetEase(Ease.OutBounce)
                .OnUpdate(UpdateChainLinks)
                .OnComplete(() =>
                {
                    restWorldY = clawImage.position.y;
                    StartHorizontalCycle();
                });
        }

        private void StartHorizontalCycle()
        {
            if (objectControllers.Count == 0) return;

            Vector3 pos = clawImage.position;
            pos.x = objectControllers[0].transform.position.x;
            pos.y = restWorldY;
            clawImage.position = pos;
            currentTargetObject = objectControllers[0];

            horizontalSequence = DOTween.Sequence();
            for (int i = 1; i < objectControllers.Count; i++)
            {
                Vector3 targetPos = objectControllers[i].transform.position;
                targetPos.y = restWorldY;
                horizontalSequence.Append(clawImage.DOMove(targetPos, horizontalMoveDuration).SetEase(Ease.Linear));
            }
            horizontalSequence.OnUpdate(UpdateCurrentTargetObject);
            horizontalSequence.SetLoops(-1, LoopType.Yoyo);
        }

        private void UpdateCurrentTargetObject()
        {
            ObjectController closest = objectControllers[0];
            float closestDistance = Mathf.Abs(clawImage.position.x - closest.transform.position.x);
            for (int i = 1; i < objectControllers.Count; i++)
            {
                float distance = Mathf.Abs(clawImage.position.x - objectControllers[i].transform.position.x);
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
            stopButton.interactable = false;
            horizontalSequence?.Kill();

            ObjectController target = currentTargetObject;
            bool isCorrect = target != null && target.ObjectType == GameManager.Instance.LevelManager.CorrectObject;
            Debug.Log(isCorrect ? "Correct" : "Incorrect");

            clawImage.DOMove(target.transform.position, grabDropDuration)
                .SetEase(Ease.InQuad)
                .OnUpdate(UpdateChainLinks)
                .OnComplete(() =>
                {
                    if (isCorrect)
                    {
                        HandleCorrectGrab(target);
                    }
                    else
                    {
                        HandleIncorrectGrab();
                    }
                });
        }

        private void HandleCorrectGrab(ObjectController target)
        {
            target.transform.SetParent(clawHandAttachPoint, true);

            RiseToRest(() =>
            {
                PlayWinAnimation(target);
                GameManager.Instance.InvokeLevelWin();
            });
        }

        private void HandleIncorrectGrab()
        {
            clawImage.DOShakeAnchorPos(shakeDuration, shakeStrength)
                .OnComplete(() =>
                {
                    RiseToRest(() => livesController.OnIncorrectAttempt(ResumeHorizontalCycle));
                });
        }

        private void RiseToRest(Action onComplete)
        {
            Vector3 restPosition = clawImage.position;
            restPosition.y = restWorldY;
            clawImage.DOMove(restPosition, grabLiftDuration)
                .SetEase(Ease.InOutQuad)
                .OnUpdate(UpdateChainLinks)
                .OnComplete(() => onComplete?.Invoke());
        }

        private void ResumeHorizontalCycle()
        {
            stopButton.interactable = true;
            StartHorizontalCycle();
        }

        private void PlayWinAnimation(ObjectController target)
        {
            Transform targetTransform = target.transform;
            targetTransform.DOScale(winScale, winScaleDuration).SetEase(Ease.OutBack);
            targetTransform.DORotate(new Vector3(0f, 360f, 0f), winSpinDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear);
        }

        private void UpdateChainLinks()
        {
            float distance = Mathf.Max(0f, topAnchorWorldY - clawImage.position.y);
            int required = Mathf.FloorToInt(distance / linkSpacing);

            while (activeLinks.Count < required)
            {
                RectTransform link = Instantiate(linkPrefab, linkParent);
                activeLinks.Add(link);
            }
            while (activeLinks.Count > required)
            {
                RectTransform link = activeLinks[activeLinks.Count - 1];
                activeLinks.RemoveAt(activeLinks.Count - 1);
                Destroy(link.gameObject);
            }

            Vector3 clawPos = clawImage.position;
            for (int i = 0; i < activeLinks.Count; i++)
            {
                Vector3 linkPos = clawPos;
                linkPos.y = topAnchorWorldY - (i + 1) * linkSpacing;
                activeLinks[i].position = linkPos;
            }
        }

        private void OnDestroy()
        {
            horizontalSequence?.Kill();
            clawImage.DOKill();
        }
    }
}
