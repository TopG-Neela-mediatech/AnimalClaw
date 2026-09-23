using UnityEngine;
using UnityEngine.UI;

namespace TMKOC.SafetySquad
{
    public class ObjectController : MonoBehaviour
    {
        [SerializeField] private Image objectImage;
        [SerializeField] private Objects objectType;
        public Image ObjectImage => objectImage;
        public Objects ObjectType => objectType;
    }
}
