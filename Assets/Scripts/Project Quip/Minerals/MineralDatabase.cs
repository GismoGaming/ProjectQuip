using UnityEngine;


namespace Gismo.Quip.Minerals
{
    [System.Serializable]
    public struct MineralPrefabSpawn
    {
        public GameObject prefab;

        public float spawnCheckRadius;
    }
    
    [System.Serializable]
    public struct MineralCostDetails
    {
        public string name;
        public float price;
    }

    [System.Serializable]
    public struct MineralFullDetails
    {
        public MineralPrefabSpawn prefabSpawn;
        public MineralCostDetails costDetails;
    }

    public class MineralDatabase : Singleton<MineralDatabase>
    {
        [SerializeField] private Tools.VisualDictionary<MineralType, MineralFullDetails> mineralInfo;

        public int maxRetryCount = 5;

        public GameObject GetMineralPrefab(MineralType type)
        {
            return mineralInfo[type].prefabSpawn.prefab;
        }

        public float GetSpawnCheckRadius(MineralType type)
        {
            return mineralInfo[type].prefabSpawn.spawnCheckRadius;
        }

        public MineralCostDetails GetCostDetails(MineralType type)
        {
            return mineralInfo[type].costDetails;
        }
    }
}
