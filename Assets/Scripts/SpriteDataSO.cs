using System;
using UnityEngine;

namespace tmkoc.claw
{
    [Serializable]
    public class ObjectSpriteData
    {
        [SerializeField] private Objects objectType;
        [SerializeField] private Sprite sprite;

        public Objects ObjectType => objectType;
        public Sprite Sprite => sprite;
    }

    [CreateAssetMenu(fileName = "SpriteData", menuName = "SpriteDataSO")]
    public class SpriteDataSO : ScriptableObject
    {
        [SerializeField] private ObjectSpriteData[] objectSprites;

        public ObjectSpriteData[] ObjectSprites => objectSprites;
    }
}
