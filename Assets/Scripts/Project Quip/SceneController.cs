using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gismo.Quip.Scene
{
    public enum MineralType { Gold, Mercury, Diamond}

    [System.Serializable]
    public struct MineralDetails
    {
        public GameObject prefab;

        public Vector2 amount;
        public Vector4 position;
    }

    [System.Serializable]
    public struct MineralNode
    {
        public Vector2 worldPosition;
        public Vector3 worldRotation;
        public MineralType type;

        public float mineralAmount;
    }

    [System.Serializable]
    public class WorldData
    {
        public bool generated;
        public List<MineralNode> mineralNodes;
    }

    public class SceneController : MonoBehaviour
    {
        [SerializeField] private WorldData worldInformation;

        [SerializeField] private Vector2Int nodeCountBounds;

        [SerializeField] private Tools.VisualDictionary<MineralType, MineralDetails> mineralDetails;

        private void Awake()
        {
            //if (NetGameController.instance.IsConnectedAs(ConnectionType.Server))
            //{
                if (worldInformation.generated)
                {
                    LoadWorldData(worldInformation);
                }
                else
                {
                    RandomizeWorldData();
                    LoadWorldData(worldInformation);
                }
            //}
        }

        public void RandomizeWorldData()
        {
            worldInformation = new WorldData();
            int numberOfNodes = Random.Range(nodeCountBounds.x, nodeCountBounds.y);

            worldInformation.mineralNodes = new List<MineralNode>();

            for (int nodeCount = 1; nodeCount < numberOfNodes; nodeCount++)
            {
                MineralType type = (MineralType)System.Enum.GetValues(typeof(MineralType)).GetValue(Random.Range(0, System.Enum.GetValues(typeof(MineralType)).Length));
                MineralDetails detail = mineralDetails[type];
                
                MineralNode newMineralNode = new MineralNode
                {
                    worldPosition = new Vector2(Random.Range(detail.position.x, detail.position.y), Random.Range(detail.position.z, detail.position.w)),
                    type = type,
                    mineralAmount = Random.Range(detail.amount.x, detail.amount.y),
                    worldRotation = new Vector3(0f, Random.Range(0, 180f), Random.Range(0, 180f)),
                };

                // Need to prevent overlap

                worldInformation.mineralNodes.Add(newMineralNode);
            }
        }

        public void LoadWorldData(WorldData worldData)
        {
            for(int nodeCount = 1; nodeCount < worldData.mineralNodes.Count; nodeCount++)
            {
                //Instantiate(nodePrefabs.GetItem(worldData.mineralNodes[nodeCount].type), worldData.mineralNodes[nodeCount].worldPosition, Quaternion.Euler(worldData.mineralNodes[nodeCount].worldRotation));
            }
        }
    }
}
