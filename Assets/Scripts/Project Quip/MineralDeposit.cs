using System.Collections;
using UnityEngine;
using System.Collections.Generic;

namespace Gismo.Quip.Mineral
{
    public enum MineralType { Gold, Mercury, Diamond}

    [System.Serializable]
    public struct MineralNode
    {
        public MineralType type;

        public int maxOut;

        public int amountPerTick;
        public float tickRate;
    }

    public class MineralDeposit : TrackedScript
    {
        [SerializeField] private bool doSpawn;

        [SerializeField] private MineralNode nodeDetails;

        [SerializeField] private int amountCurrentlyOut;

        private GameObject prefab;
        private float spawnCheckRadius;

        [SerializeField] private Transform mineralSpawnPoint;
        [SerializeField] private float mineralStartSpawnOffset;

        [SerializeField] private float mineralEndSpawnOffset;

        [SerializeField] private LeanTweenMovement moveType;

        private List<Vector2> mineralSpawnedPoints;

        [SerializeField] private GameObject miningRigVisual;

        public override void OnRegisterReady()
        {
            base.OnRegisterReady();
            miningRigVisual.SetActive(false);
            prefab = MineralDatabase.Instance.GetMineralPrefab(nodeDetails.type);
            spawnCheckRadius = MineralDatabase.Instance.GetSpawnCheckRadius(nodeDetails.type);
        }

        public override Networking.Core.Packet OnNewClient()
        {
            Networking.Core.Packet packet = new Networking.Core.Packet(Networking.NetworkPackets.ServerSentPackets.MineralInit);

            packet.WriteInt(ID);

            packet.WriteBoolean(miningRigVisual.activeInHierarchy);

            DL.Log(mineralSpawnedPoints.Count.ToString());

            packet.WriteInt(mineralSpawnedPoints.Count);

            foreach (Vector2 v in mineralSpawnedPoints)
                packet.WriteVector2(v);

            return packet;
        }

        public bool CanPlaceMiner()
        {
            return !doSpawn;
        }

        public void StartDepositOutput()
        {
            miningRigVisual.SetActive(true);
            doSpawn = true;
            mineralSpawnedPoints = new List<Vector2>();
            StartCoroutine(DepositSpawnLoop());
        }

        IEnumerator DepositSpawnLoop()
        {
            if (doSpawn && amountCurrentlyOut < nodeDetails.maxOut)
            {
                int amountToDeploy = nodeDetails.amountPerTick;
                if(amountCurrentlyOut + nodeDetails.amountPerTick > nodeDetails.maxOut)
                {
                    amountToDeploy = nodeDetails.maxOut - amountCurrentlyOut;
                }
#if !UNITY_EDITOR

                Networking.Core.Packet clientPacket = GetPacketServer(Networking.NetworkPackets.ServerSentPackets.MineralSpawn);
                clientPacket.WriteInt(amountToDeploy);
#endif
                amountCurrentlyOut += amountToDeploy;

                float currentSpawnOffset = mineralEndSpawnOffset;

                for(int i = 0; i < amountToDeploy; i++)
                {
                    Vector3 startPosition = mineralSpawnPoint.position.AddRandomOnUnitCircle(mineralStartSpawnOffset);

                    Vector3 movePosition = transform.position.AddRandomOnUnitCircle(currentSpawnOffset);

                    for (int attempts = 0; attempts < MineralDatabase.Instance.maxRetryCount; attempts++)
                    {
                        movePosition = transform.position.AddRandomOnUnitCircle(currentSpawnOffset);

                        if (Physics2D.OverlapCircle(movePosition, spawnCheckRadius, LayerMask.GetMask("Mineral")) == null)
                        {
                           break;
                        }
                    }

                    if (Physics2D.OverlapCircle(movePosition, spawnCheckRadius, LayerMask.GetMask("Mineral")) == null)
                    {
                        currentSpawnOffset += mineralEndSpawnOffset / 2;
                        movePosition = transform.position.AddRandomOnUnitCircle(currentSpawnOffset);
                        
                        for (int attempts = 0; attempts < MineralDatabase.Instance.maxRetryCount; attempts++)
                        {
                            movePosition = transform.position.AddRandomOnUnitCircle(currentSpawnOffset);

                            if (Physics2D.OverlapCircle(movePosition, spawnCheckRadius, LayerMask.GetMask("Mineral")) == null)
                            {
                                break;
                            }
                        }
                    }
#if !UNITY_EDITOR
                    clientPacket.WriteVector2(movePosition);
                    clientPacket.WriteVector2(startPosition);
#endif
                        SpawnMineral(movePosition, startPosition);

                    mineralSpawnedPoints.Add(movePosition);
                }
#if !UNITY_EDITOR
                NetGameController.Instance.SendDataToAll_S(clientPacket);
#endif
            }
            yield return new WaitForSeconds(nodeDetails.tickRate);
            StartCoroutine(DepositSpawnLoop());
        }

        void SpawnMineral(Vector2 movePosition, Vector2 startPosition)
        {
            if (!miningRigVisual.activeInHierarchy)
                miningRigVisual.SetActive(true);

            GameObject newSpawn = Instantiate(prefab, startPosition, Quaternion.identity);

            LeanTween.move(newSpawn, movePosition, moveType.timing).setEase(moveType.type);
        }

        public void SpawnMineral(Vector2 position)
        {
            Instantiate(prefab, position, Quaternion.identity);
        }

        public void HandleSpawnMineralPacket(Networking.Core.Packet packet)
        {
            int spawnCount = packet.ReadInt();

            for (int i = 1; i <= spawnCount; i++)
            {
                SpawnMineral(packet.ReadVector2(), packet.ReadVector2());
            }
        }

        public void HandleInitalMineralSpawns(Networking.Core.Packet packet)
        {
            miningRigVisual.SetActive(packet.ReadBoolean());
            int spawnCount = packet.ReadInt();
            for (int i = 1; i <= spawnCount; i++)
            {
                SpawnMineral(packet.ReadVector2());
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(mineralSpawnPoint.position, mineralStartSpawnOffset);

            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(transform.position, mineralEndSpawnOffset);
        }
    }
}
