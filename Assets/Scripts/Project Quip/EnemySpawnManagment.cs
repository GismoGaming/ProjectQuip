using Gismo.Tools;
using UnityEngine;
using System.Collections;
using Gismo.Networking.Core;

namespace Gismo.Quip.Enemies
{
    public class EnemySpawnManagment : Singleton<EnemySpawnManagment>
    {
        [SerializeField] VisualDictionary<byte, GameObject> prefabDictionary;

        [SerializeField] Transform[] spawnPoints;

        private float spawnDelayLength = 5f;

        int spawnedCount;
        [SerializeField] private int maxSpawn;

        public override void Awake()
        {
            base.Awake();
            NetGameController.Instance.onGameStart += ()=>
            {
                if(NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
                {
                    StartCoroutine(SpawnCycle());
                }
            };
        }

        public GameObject SpawnEnemy(byte prefabID, Vector2 spawnPoint)
        {
            DL.Log($"Spawing enemy {prefabID} - {spawnPoint}");
            return Instantiate(prefabDictionary[prefabID], spawnPoint, Quaternion.identity);
        }

        public void SpawnEnemy(uint id, byte prefabID, Vector2 spawnPoint)
        {
            DL.Log($"Spawing enemy {id}: {prefabID} - {spawnPoint}");
            Instantiate(prefabDictionary[prefabID], spawnPoint, Quaternion.identity).GetComponent<Enemy>().GlobalPlace(id, spawnPoint);
        }

        public void RemoveEnemy()
        {
            spawnedCount--;
        }

        IEnumerator SpawnCycle()
        {
            if (spawnedCount <= maxSpawn)
            {
                byte spawnID = prefabDictionary.GetRandomKey();

                Vector2 spawn = spawnPoints.GetRandomItem().position.ToVector2();

                GameObject newEnemy = SpawnEnemy(spawnID, spawn);

                uint id = newEnemy.GetComponent<Enemy>().LocalPlace(spawn);

                Packet toClients = new Packet(Networking.NetworkPackets.ServerSentPackets.EnemySpawn);
                toClients.WriteUint(id);
                toClients.WriteByte(spawnID);
                toClients.WriteVector2(spawn);

                NetGameController.Instance.SendDataToAll_S(toClients);

                spawnedCount++;
            }
            yield return new WaitForSeconds(spawnDelayLength);
            StartCoroutine(SpawnCycle());
        }
    }
}
