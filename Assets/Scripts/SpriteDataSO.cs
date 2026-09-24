using UnityEngine;

namespace tmkoc.claw
{
    [CreateAssetMenu(fileName = "SpriteDataSO", menuName = "TMKOC/SafetySquad/Sprite Data")]
    public class SpriteDataSO : ScriptableObject
    {
        [SerializeField] private Objects objectType;
        [SerializeField] private Sprite[] sprites;

        public Objects ObjectType => objectType;
        public Sprite[] Sprites => sprites;
    }
}
