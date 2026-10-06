using System;
using System.Collections.Generic;
using System.Linq;
using AssetKits.ParticleImage;
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
        [SerializeField, Range(0f, 1f)] private float maxFillAmount = 0.8f;

        [Header("Stop Button Press")]
        [SerializeField] private float buttonRestScaleY = 1.5f;
        [SerializeField] private float buttonPressedScaleY = 0.8f;
        [SerializeField] private float buttonPressDuration = 0.1f;

        [Header("Claw Movement")]
        [SerializeField] private float clawSpeed = 400f;
        [SerializeField] private Vector2 clawHorizontalRange = new Vector2(-400f, 400f);
        [SerializeField] private float clawDropMaxY = -175f;
        [SerializeField] private float grabDropDuration = 0.6f;
        [SerializeField] private float grabLiftDuration = 0.6f;

        [Header("Claw Hands")]
        [SerializeField, Range(0f, 15f)] private float handGrabAngle = 15f;
        [SerializeField] private float handAnimDuration = 0.25f;
        [SerializeField] private Image linkFillImage;
        [Header("Incorrect Drop")]
        [SerializeField] private float clawShakeDuration = 0.4f;
        [SerializeField] private float clawShakeStrength = 15f;
        [SerializeField] private float dropFallDuration = 0.5f;

        [Header("Win Animation")]
        [SerializeField] private RectTransform clawDropTarget;
        [SerializeField] private float dropTargetExtraY = 30f;
        [SerializeField] private RectTransform winningParent;
        [SerializeField] private Image shineImage;
        [SerializeField] private GridLayoutGroup objectLayoutGroup;
        [SerializeField] private float winScale = 1.3f;
        [SerializeField] private float winScaleDuration = 0.6f;
        [SerializeField] private float winSpinDuration = 0.8f;
        [SerializeField] private float shineSpinDuration = 6f;
        [SerializeField] private float winPanelDelay = 3f;

        [SerializeField] ParticleImage confettiEffect;
        private readonly List<ObjectController> objectControllers = new List<ObjectController>();

        private Tween horizontalTween;
        private float moveDirection = -1f;
        private float lastClawX;
        private float restAnchoredY;
        private float restWorldY;
        private Vector3 centerWorldPosition;
        private Vector3 leftHandStartEuler;
        private Vector3 rightHandStartEuler;

        private void Awake()
        {
            stopButton.onClick.AddListener(OnStopButtonClicked);
        }

        private void Start()
        {
            stopButton.interactable = false;
            leftHandStartEuler = clawLeftHand.localEulerAngles;
            rightHandStartEuler = clawRightHand.localEulerAngles;

            SpawnObjectControllers();
            ApplyCellSize();
            // ObjectParent uses a layout group; positions are only valid after a rebuild.
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)objectParent);
            // Freeze positions so reparenting a toy doesn't make the others shift.
            objectLayoutGroup.enabled = false;
            shineImage.gameObject.SetActive(false);

            centerWorldPosition = clawStick.position;
            restWorldY = centerWorldPosition.y;
            restAnchoredY = clawStick.anchoredPosition.y;
            linkFillImage.fillAmount = 0f;

            // Wait for the level to start (it is delayed while the story plays); handles either Start order.
            if (GameManager.Instance.LevelManager.HasLevelStarted)
            {
                BeginGameplay();
            }
            else
            {
                GameManager.Instance.OnLevelStart += BeginGameplay;
            }
        }

        private void BeginGameplay()
        {
            GameManager.Instance.OnLevelStart -= BeginGameplay;
            stopButton.interactable = true;
            StartHorizontalCycle();
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

        private void ApplyCellSize()
        {
            int count = objectControllers.Count;
            float size = count <= 3 ? 125f : count == 4 ? 90f : 90f;
            objectLayoutGroup.cellSize = new Vector2(size, size);
        }

        private void StartHorizontalCycle()
        {
            if (objectControllers.Count == 0)
            {
                Debug.LogError("ClawController: no ObjectControllers were spawned, so the claw has nothing to cycle across. " +
                    "Check that CurrentLevelData.Options is non-empty and that ObjectControllerPrefab/SpriteData are assigned.", this);
                return;
            }

            lastClawX = clawStick.position.x;

            float leftX = Mathf.Min(clawHorizontalRange.x, clawHorizontalRange.y);
            float rightX = Mathf.Max(clawHorizontalRange.x, clawHorizontalRange.y);

            // Travel to the left end once, then ping-pong between the ends forever (no restart from center).
            horizontalTween = clawStick.DOAnchorPosX(leftX, LocalMoveDuration(clawStick.anchoredPosition.x, leftX))
                .SetEase(Ease.Linear)
                .OnUpdate(UpdateMoveDirection)
                .OnComplete(() =>
                {
                    horizontalTween = clawStick.DOAnchorPosX(rightX, LocalMoveDuration(leftX, rightX))
                        .SetEase(Ease.Linear)
                        .SetLoops(-1, LoopType.Yoyo)
                        .OnUpdate(UpdateMoveDirection);
                });
        }

        private float LocalMoveDuration(float fromX, float toX)
        {
            return Mathf.Max(0.01f, Mathf.Abs(toX - fromX) / Mathf.Max(0.01f, clawSpeed));
        }

        private float MoveDuration(float fromX, float toX)
        {
            float worldSpeed = Mathf.Max(0.01f, clawSpeed) * Mathf.Abs(clawStick.lossyScale.x);
            return Mathf.Max(0.01f, Mathf.Abs(toX - fromX) / worldSpeed);
        }

        private void UpdateMoveDirection()
        {
            float x = clawStick.position.x;
            if (!Mathf.Approximately(x, lastClawX))
            {
                moveDirection = Mathf.Sign(x - lastClawX);
                lastClawX = x;
            }
        }

        private void PlayButtonPress()
        {
            Transform buttonTransform = stopButton.transform;
            buttonTransform.DOKill();
            buttonTransform.DOScaleY(buttonPressedScaleY, buttonPressDuration).SetLink(stopButton.gameObject);
        }

        // The toy the claw is heading toward: nearest one at/ahead of it in the travel direction.
        private ObjectController PickTargetAhead()
        {
            float clawX = clawStick.position.x;
            ObjectController best = null;
            float bestDistance = float.MaxValue;
            foreach (ObjectController controller in objectControllers)
            {
                float offset = (controller.transform.position.x - clawX) * moveDirection;
                if (offset < 0f) continue;
                if (offset < bestDistance)
                {
                    bestDistance = offset;
                    best = controller;
                }
            }

            if (best != null) return best;
            return objectControllers.OrderBy(c => Mathf.Abs(c.transform.position.x - clawX)).First();
        }

        private void OnStopButtonClicked()
        {
            stopButton.interactable = false;
            horizontalTween?.Kill();
            PlayButtonPress();

            ObjectController target = PickTargetAhead();
            bool isCorrect = target.ObjectType == GameManager.Instance.LevelManager.CorrectObject;
            Debug.Log(isCorrect ? "Correct" : "Incorrect");

            float targetX = target.transform.position.x;
            clawStick.DOMoveX(targetX, MoveDuration(clawStick.position.x, targetX))
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    clawStick.DOAnchorPosY(clawDropMaxY, grabDropDuration)
                        .SetEase(Ease.InQuad)
                        .OnUpdate(UpdateChainLinks)
                        .OnComplete(() => CloseHands(() => GrabAndRise(target, isCorrect)));
                });
        }

        private void GrabAndRise(ObjectController target, bool isCorrect)
        {
            Vector3 originalWorldPosition = target.transform.position;
            int originalSiblingIndex = target.transform.GetSiblingIndex();
            target.transform.SetParent(clawStick, true);

            RiseToRest(() =>
            {
                if (isCorrect)
                {
                    DropAtTarget(target);
                }
                else
                {
                    PlayIncorrectDrop(target, originalWorldPosition, originalSiblingIndex);
                }
            });
        }

        private void PlayIncorrectDrop(ObjectController target, Vector3 originalWorldPosition, int originalSiblingIndex)
        {
            clawStick.DOShakeAnchorPos(clawShakeDuration, clawShakeStrength)
                .OnComplete(() =>
                {
                    OpenHands(() =>
                    {
                        target.transform.SetParent(objectParent, true);
                        target.transform.SetSiblingIndex(originalSiblingIndex);
                        target.transform.DOMove(originalWorldPosition, dropFallDuration)
                            .SetEase(Ease.OutBounce)
                            .OnComplete(() => livesController.OnIncorrectAttempt(ResumeHorizontalCycle));
                    });
                });
        }

        private void DropAtTarget(ObjectController target)
        {
            float targetX = clawDropTarget.position.x;
            clawStick.DOMoveX(targetX, MoveDuration(clawStick.position.x, targetX))
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    OpenHands(() =>
                    {
                        // Parent is intentionally unchanged here; it moves to winningParent once the win animation starts.
                        Vector3 dropPosition = clawDropTarget.position + Vector3.down * (dropTargetExtraY * clawDropTarget.lossyScale.y);
                        target.transform.DOMove(dropPosition, dropFallDuration)
                            .SetEase(Ease.InQuad)
                            .OnComplete(() => PlayWinSequence(target));
                    });
                });
        }

        private void MoveToWinningParent(RectTransform targetRect)
        {
            targetRect.SetParent(winningParent, true);

            // The toy came from a layout group, so its anchors/pivot aren't centered; fix them without moving it.
            Vector3 worldPosition = targetRect.position;
            targetRect.anchorMin = targetRect.anchorMax = targetRect.pivot = new Vector2(0.5f, 0.5f);
            targetRect.position = worldPosition;
        }

        private void PlayWinSequence(ObjectController target)
        {
            RectTransform targetRect = (RectTransform)target.transform;
            MoveToWinningParent(targetRect);

            RectTransform shineRect = shineImage.rectTransform;
            shineRect.SetParent(winningParent, false);
            shineRect.anchoredPosition = Vector2.zero;
            shineRect.SetAsFirstSibling();
            shineImage.gameObject.SetActive(true);
            shineRect.DORotate(new Vector3(0f, 0f, -360f), shineSpinDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetLink(shineImage.gameObject);

            targetRect.DOAnchorPos(Vector2.zero, winScaleDuration).SetEase(Ease.OutBack);
            targetRect.DOScale(winScale, winScaleDuration).SetEase(Ease.OutBack);
            confettiEffect.Play();
            targetRect.DORotate(new Vector3(0f, 360f, 0f), winSpinDuration, RotateMode.FastBeyond360).SetEase(Ease.Linear);

            DOVirtual.DelayedCall(winPanelDelay, () =>
            {
                shineRect.DOKill();
                shineImage.gameObject.SetActive(false);
                GameManager.Instance.InvokeLevelWin();
            }).SetLink(gameObject);
        }

        private void RiseToRest(Action onComplete)
        {
            Vector3 restPosition = clawStick.position;
            restPosition.y = restWorldY;
            clawStick.DOMove(restPosition, grabLiftDuration)
                .SetEase(Ease.OutQuad)
                .OnUpdate(UpdateChainLinks)
                .OnComplete(() => onComplete?.Invoke());
        }

        private void ResumeHorizontalCycle()
        {
            Transform buttonTransform = stopButton.transform;
            buttonTransform.DOKill();
            buttonTransform.DOScaleY(buttonRestScaleY, buttonPressDuration)
                .SetLink(stopButton.gameObject)
                .OnComplete(() => stopButton.interactable = true);
            StartHorizontalCycle();
        }

        private void CloseHands(Action onComplete)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Join(clawLeftHand.DOLocalRotate(WithZ(leftHandStartEuler, leftHandStartEuler.z + handGrabAngle), handAnimDuration));
            sequence.Join(clawRightHand.DOLocalRotate(WithZ(rightHandStartEuler, rightHandStartEuler.z + handGrabAngle), handAnimDuration));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        private void OpenHands(Action onComplete)
        {
            Sequence sequence = DOTween.Sequence();
            sequence.Join(clawLeftHand.DOLocalRotate(leftHandStartEuler, handAnimDuration));
            sequence.Join(clawRightHand.DOLocalRotate(rightHandStartEuler, handAnimDuration));
            sequence.OnComplete(() => onComplete?.Invoke());
        }

        private static Vector3 WithZ(Vector3 euler, float z) => new Vector3(euler.x, euler.y, z);

        // The chain fills in proportion to how far the stick has dropped below its rest height.
        private void UpdateChainLinks()
        {
            float dropDistance = Mathf.Max(0f, restAnchoredY - clawStick.anchoredPosition.y);
            float totalDrop = Mathf.Max(0.01f, restAnchoredY - clawDropMaxY);
            linkFillImage.fillAmount = maxFillAmount * Mathf.Clamp01(dropDistance / totalDrop);
        }

        private void OnDestroy()
        {
            GameManager.Instance.OnLevelStart -= BeginGameplay;
            horizontalTween?.Kill();
            clawStick.DOKill();
        }
    }
}
