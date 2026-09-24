using UnityEngine;

namespace tmkoc.claw
{
    [CreateAssetMenu(fileName = "SpriteData", menuName = "SpriteDataSO")]
    public class SpriteDataSO : ScriptableObject
    {
        [SerializeField] private Objects objectType;
        [SerializeField] private Sprite[] sprites;

        public Objects ObjectType => objectType;
        public Sprite[] Sprites => sprites;
    }
}
