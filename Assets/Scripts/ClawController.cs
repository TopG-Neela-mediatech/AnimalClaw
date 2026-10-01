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
        [SerializeField] private RectTransform clawStick;
        [SerializeField] private RectTransform clawLeftHand;
        [SerializeField] private RectTransform clawRightHand;
        [SerializeField] private RectTransform machineImage;
        [SerializeField] private Transform objectParent;
        [SerializeField] private Button stopButton;
        [SerializeField] private LivesController livesController;

        [Header("Chain Link")]
        [SerializeField] private RectTransform linkPrefab;
        [SerializeField] private float linkSpacing = 50f;

        [Header("Claw Movement")]
        [SerializeField] private float horizontalMoveDuration = 0.5f;
        [SerializeField] private float grabDropDuration = 0.6f;
        [SerializeField] private float grabLiftDuration = 0.6f;

        [Header("Claw Hands")]
        [SerializeField, Range(0f, 15f)] private float handGrabAngle = 15f;
        [SerializeField] private float handAnimDuration = 0.25f;

        [Header("Incorrect Drop")]
        [SerializeField] private float dropBounceStrength = 20f;
        [SerializeField] private float dropBounceDuration = 0.4f;

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
        private Vector3 centerWorldPosition;
        private float leftHandStartZ;
        private float rightHandStartZ;

        private void Awake()
        {
            stopButton.onClick.AddListener(OnStopButtonClicked);
        }

        private void Start()
        {
            stopButton.interactable = true;
            leftHandStartZ = clawLeftHand.localEulerAngles.z;
            rightHandStartZ = clawRightHand.localEulerAngles.z;

            SpawnObjectControllers();

            centerWorldPosition = clawStick.position;
            restWorldY = centerWorldPosition.y;
            topAnchorWorldY = restWorldY;
            activeLinks.Clear();
            UpdateChainLinks();

            StartHorizontalCycle(centerWorldPosition);
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

        private void StartHorizontalCycle(Vector3? startFrom = null)
        {
            if (objectControllers.Count == 0)
            {
                Debug.LogError("ClawController: no ObjectControllers were spawned, so the claw has nothing to cycle across. " +
                    "Check that CurrentLevelData.Options is non-empty and that ObjectControllerPrefab/SpriteData are assigned.", this);
                return;
            }

            clawStick.position = startFrom ?? clawStick.position;
            currentTargetObject = objectControllers[0];

            horizontalSequence = DOTween.Sequence();
            foreach (ObjectController controller in objectControllers)
            {
                Vector3 targetPos = controller.transform.position;
                targetPos.y = restWorldY;
                horizontalSequence.Append(clawStick.DOMove(targetPos, horizontalMoveDuration).SetEase(Ease.Linear));
            }
            horizontalSequence.OnUpdate(UpdateCurrentTargetObject);
            horizontalSequence.SetLoops(-1, LoopType.Yoyo);
        }

        private void UpdateCurrentTargetObject()
        {
            ObjectController closest = objectControllers[0];
            float closestDistance = Mathf.Abs(clawStick.position.x - closest.transform.position.x);
            for (int i = 1; i < objectControllers.Count; i++)
            {
                float distance = Mathf.Abs(clawStick.position.x - objectControllers[i].transform.position.x);
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

            clawStick.DOMove(target.transform.position, grabDropDuration)
                .SetEase(Ease.InQuad)
                .OnUpdate(UpdateChainLinks)
                .OnComplete(() =>
                {
                    CloseHands(() =>
                    {
                        if (isCorrect)
                        {
                            HandleCorrectGrab(target);
                        }
                        else
                        {
                            HandleIncorrectGrab(target);
                        }
                    });
                });
        }

        private void HandleCorrectGrab(ObjectController target)
        {
            target.transform.SetParent(clawStick, true);

            RiseToRest(() =>
            {
                PlayWinAnimation(target);
                GameManager.Instance.InvokeLevelWin();
            });
        }

        private void HandleIncorrectGrab(ObjectController target)
        {
            OpenHands(() =>
            {
                PlayDropBounce(target);
                RiseToRest(() => livesController.OnIncorrectAttempt(ResumeHorizontalCycle));
            });
        }

        private void RiseToRest(Action onComplete)
        {
            Vector3 restPosition = clawStick.position;
            restPosition.y = restWorldY;
            clawStick.DOMove(restPosition, grabLiftDuration)
                .SetEase(Ease.InOutQuad)
                .OnUpdate(UpdateChainLinks)
                .OnComplete(() => onComplete?.Invoke());
        }

        private void ResumeHorizontalCycle()
        {
            stopButton.interactable = true;
            StartHorizontalCycle();
        }

        private void CloseHands(Action onComplete)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Join(clawLeftHand.DOLocalRotate(new Vector3(0f, 0f, leftHandStartZ + handGrabAngle), handAnimDuration));
            sequence.Join(clawRightHand.DOLocalRotate(new Vector3(0f, 0f, rightHandStartZ + handGrabAngle), handAnimDuration));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        private void OpenHands(Action onComplete)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Join(clawLeftHand.DOLocalRotate(new Vector3(0f, 0f, leftHandStartZ), handAnimDuration));
            sequence.Join(clawRightHand.DOLocalRotate(new Vector3(0f, 0f, rightHandStartZ), handAnimDuration));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        private void PlayDropBounce(ObjectController target)
        {
            RectTransform targetRect = target.transform as RectTransform;
            if (targetRect == null) return;

            targetRect.DOPunchAnchorPos(new Vector2(0f, -dropBounceStrength), dropBounceDuration, 1, 0.5f);
        }

        private void PlayWinAnimation(ObjectController target)
        {
            Transform targetTransform = target.transform;
            targetTransform.DOScale(winScale, winScaleDuration).SetEase(Ease.OutBack);
            targetTransform.DORotate(new Vector3(0f, 360f, 0f), winSpinDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear);
        }

        // Links are children of the claw stick, so they travel with it horizontally for free;
        // only their local Y (distance from the stick) needs updating as the stick moves vertically.
        private void UpdateChainLinks()
        {
            float distance = Mathf.Max(0f, topAnchorWorldY - clawStick.position.y);
            int required = Mathf.FloorToInt(distance / linkSpacing);

            while (activeLinks.Count < required)
            {
                RectTransform link = Instantiate(linkPrefab, clawStick);
                link.SetAsFirstSibling();
                activeLinks.Add(link);
            }
            while (activeLinks.Count > required)
            {
                RectTransform link = activeLinks[activeLinks.Count - 1];
                activeLinks.RemoveAt(activeLinks.Count - 1);
                Destroy(link.gameObject);
            }

            for (int i = 0; i < activeLinks.Count; i++)
            {
                activeLinks[i].anchoredPosition = new Vector2(0f, (i + 1) * linkSpacing);
            }
        }

        private void OnDestroy()
        {
            horizontalSequence?.Kill();
            clawStick.DOKill();
        }
    }
}
