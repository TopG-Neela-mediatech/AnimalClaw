using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.claw
{
    public class FerrisWheelController : MonoBehaviour
    {
        [SerializeField] private Image ferrisWheelCircle;
        [SerializeField] private Image[] ferrisCabins;
        [SerializeField] private float rotationSpeed = 20f; // degrees per second, clockwise

        private Quaternion[] cabinStartRotations;

        private void Start()
        {
            cabinStartRotations = new Quaternion[ferrisCabins.Length];
            for (int i = 0; i < ferrisCabins.Length; i++)
            {
                cabinStartRotations[i] = ferrisCabins[i].rectTransform.rotation;
            }

            float duration = 360f / Mathf.Max(0.01f, rotationSpeed);
            ferrisWheelCircle.rectTransform
                .DORotate(new Vector3(0f, 0f, -360f), duration, RotateMode.FastBeyond360)
                .SetRelative(true)
                .SetEase(Ease.Linear)
                .SetLoops(-1, LoopType.Incremental)
                .SetLink(gameObject);
        }

        // Cancel the parent's rotation so the cabins stay upright.
        private void LateUpdate()
        {
            for (int i = 0; i < ferrisCabins.Length; i++)
            {
                ferrisCabins[i].rectTransform.rotation = cabinStartRotations[i];
            }
        }
    }
}
