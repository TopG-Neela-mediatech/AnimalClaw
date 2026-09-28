using UnityEngine;

namespace tmkoc.claw
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "LevelDataSO")]
    public class LevelData : ScriptableObject
    {
        [SerializeField] private Objects[] options;
        [SerializeField] private SpriteDataSO spriteData;
        [SerializeField] private Objects correctObject;
        [SerializeField] private int lives = 3;
        [SerializeField] private float timeInSeconds; // reserved for a future timer feature

        public Objects[] Options => options;
        public SpriteDataSO SpriteData => spriteData;
        public Objects CorrectObject => correctObject;
        public int Lives => lives;
        public float TimeInSeconds => timeInSeconds;
    }
}
