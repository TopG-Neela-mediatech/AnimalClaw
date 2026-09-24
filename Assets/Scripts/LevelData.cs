using UnityEngine;

namespace tmkoc.claw
{
    [CreateAssetMenu(fileName = "LevelData", menuName = "LevelDataSO")]
    public class LevelData : ScriptableObject
    {
        [SerializeField] private ObjectController[] options;
        [SerializeField] private SpriteDataSO[] spriteDataList;
        [SerializeField] private Objects correctObject;
        [SerializeField] private int lives = 3;
        [SerializeField] private float timeInSeconds; // reserved for a future timer feature

        public ObjectController[] Options => options;
        public SpriteDataSO[] SpriteDataList => spriteDataList;
        public Objects CorrectObject => correctObject;
        public int Lives => lives;
        public float TimeInSeconds => timeInSeconds;
    }
}
