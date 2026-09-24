using UnityEngine;
using UnityEngine.UI;

namespace tmkoc.claw
{
    public class ObjectController : MonoBehaviour
    {
        [SerializeField] private Image objectImage;
        [SerializeField] private Objects objectType;
        public Image ObjectImage => objectImage;
        public Objects ObjectType => objectType;

        public void Initialize(Objects type, Sprite sprite)
        {
            objectType = type;
            if (sprite != null)
            {
                objectImage.sprite = sprite;
            }
        }
    }
}
