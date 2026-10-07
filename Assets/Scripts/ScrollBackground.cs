using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.claw
{
    public class ScrollBackground : MonoBehaviour
    {
        [SerializeField] private RawImage background;
        [SerializeField] private float scrollSpeed = 1f;

        private void Start()
        {
            if (background == null)
            {
                Debug.LogError("No background image assigned!");
                return;
            }
        }
        private void Update()
        {
            ScrollBG();
        }
        private void ScrollBG()
        {
            if (background != null)
            {
                Rect uv = background.uvRect;
                uv.x = Time.time * scrollSpeed;
                background.uvRect = uv;
            }            
        }
    }
}
