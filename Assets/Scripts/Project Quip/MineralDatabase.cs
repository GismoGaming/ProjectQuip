using UnityEngine;


namespace Gismo.Quip.Mineral
{
    [System.Serializable]
    public struct MineralInformation
    {
        public GameObject prefab;

        public float spawnCheckRadius;
    }
    public class MineralDatabase : Singleton<MineralDatabase>
    {
        [SerializeField] private Tools.VisualDictionary<MineralType, MineralInformation> mineralInfo;

        public int maxRetryCount = 5;

        public GameObject GetMineralPrefab(MineralType type)
        {
            return mineralInfo[type].prefab;
        }

        public float GetSpawnCheckRadius(MineralType type)
        {
            return mineralInfo[type].spawnCheckRadius;
        }
    }
}
