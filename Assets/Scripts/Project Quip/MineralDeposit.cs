using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Gismo.Networking.Core;

namespace Gismo.Quip.Minerals
{
    public enum MineralType { Gold, Mercury, Diamond}

    public struct MineralWorldSpawnInfos
    {
        public Vector2 startPos;
        public Vector2 endPos;
    }

    [System.Serializable]
    public struct MineralNode
    {
        public MineralType type;

        public int maxOut;

        public int amountPerTick;
        public float tickRate;
    }

    public class MineralDeposit : TrackedScript, IInteractable
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

        [SerializeField] private GameObject miningRigVisual;

        private bool canPlaceMiner;
        Vector3 startScale;

        public override void Start()
        {
            base.Start();
            startScale = transform.localScale;
            SetMinerStatus(true);
        }

        public override void OnRegisterReady()
        {
            base.OnRegisterReady();
            
            prefab = MineralDatabase.Instance.GetMineralPrefab(nodeDetails.type);
            spawnCheckRadius = MineralDatabase.Instance.GetSpawnCheckRadius(nodeDetails.type);
        }

        public void Initalize(Packet packet)
        {
            bool t = packet.ReadBoolean();
            DL.Log($"<b>Initalzing {ID}</b>{t}",Dev.DebugLogType.error);
            SetMinerStatus(t);
        }

        public bool CanPlaceMiner()
        {
            return canPlaceMiner;
        }

        public void StartDepositOutput()
        {
            SetMinerStatus(false);
            doSpawn = true;
            StartCoroutine(DepositSpawnLoop());
        }

        private void PlaceMinerOnDeposit()
        {
#if UNITY_EDITOR
            StartDepositOutput();
#else
                    if (NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
                    {
                        StartDepositOutput();
                    }
                    else
                    {
                        SetMinerStatus(false);
                        Networking.Core.Packet newPacket = new Networking.Core.Packet(Networking.NetworkPackets.ClientSentPackets.MineralMineBegin,NetGameController.Instance.GetUserID());

                        newPacket.WriteUint(ID);

                        NetGameController.Instance.SendData_C(newPacket);
                    }
#endif
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

                Packet clientPacket = GetPacketServer(Networking.NetworkPackets.ServerSentPackets.MineralSpawn);
                clientPacket.WriteInt(amountToDeploy);
#endif
                amountCurrentlyOut += amountToDeploy;

                float currentSpawnOffset = mineralEndSpawnOffset;

                for(int i = 0; i < amountToDeploy; i++)
                {
                    currentSpawnOffset = GenerateSpawnDetails(currentSpawnOffset, out Vector2 spawnPosition);
                    uint newID = SpawnMineral(spawnPosition).GetComponent<Mineral>().InitalizeWithNewID(spawnPosition, nodeDetails.type, ID);
#if !UNITY_EDITOR
                    clientPacket.WriteUint(newID);
                    clientPacket.WriteVector2(spawnPosition);
#endif
                }
#if !UNITY_EDITOR
                NetGameController.Instance.SendDataToAll_S(clientPacket);
#endif
            }
            yield return new WaitForSeconds(nodeDetails.tickRate);
            StartCoroutine(DepositSpawnLoop());
        }

        public void SetMinerStatus(bool value)
        {
            canPlaceMiner = value;
            miningRigVisual.SetActive(!value);
        }

        public void OnDepositLoseMineral()
        {
            if(NetGameController.Instance.IsConnectedAs(ConnectionType.Client))
            {
                // send to server
                NetGameController.Instance.SendData_C(GetPacketClient(Networking.NetworkPackets.ClientSentPackets.DepositLoseMineral));
            }
            else
            {
                amountCurrentlyOut--;
            }
        }

        public void SpawnMineral(List<Tracked2DPositionUint> details)
        {
            if(canPlaceMiner)
                SetMinerStatus(false);

            foreach (Tracked2DPositionUint pos in details)
            {
                SpawnMineral(pos.position).GetComponent<Mineral>().InitalizeWithNewID(pos.position, nodeDetails.type, ID,pos.id);
            }    
        }

        GameObject SpawnMineral(Vector2 startPosition)
        {
            return Instantiate(prefab, startPosition, Quaternion.identity);
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.blue;

            Gizmos.DrawWireSphere(mineralSpawnPoint.position, mineralStartSpawnOffset);

            Gizmos.color = Color.green;

            Gizmos.DrawWireSphere(transform.position, mineralEndSpawnOffset);
        }

        public override Packet OnNewClient()
        {
            DL.Log($"Sending new client packet to {canPlaceMiner} for me {ID}");
            Packet clientPacket = GetPacketServer(Networking.NetworkPackets.ServerSentPackets.MineralDepositInit);

            clientPacket.WriteBoolean(canPlaceMiner);

            return clientPacket;
        }

        private float GenerateSpawnDetails(float currentSpawnOffset, out Vector2 startPosition)
        {
            startPosition = transform.position.AddRandomOnUnitCircle(currentSpawnOffset);
            for (int attempts = 0; attempts < MineralDatabase.Instance.maxRetryCount; attempts++)
            {
                startPosition = transform.position.AddRandomOnUnitCircle(currentSpawnOffset);

                if (Physics2D.OverlapCircle(startPosition, spawnCheckRadius, LayerMask.GetMask("Mineral")) == null)
                {
                    break;
                }
            }

            if (Physics2D.OverlapCircle(startPosition, spawnCheckRadius, LayerMask.GetMask("Mineral")) == null)
            {
                currentSpawnOffset += mineralEndSpawnOffset / 2;
                startPosition = transform.position.AddRandomOnUnitCircle(currentSpawnOffset);

                for (int attempts = 0; attempts < MineralDatabase.Instance.maxRetryCount; attempts++)
                {
                    startPosition = transform.position.AddRandomOnUnitCircle(currentSpawnOffset);

                    if (Physics2D.OverlapCircle(startPosition, spawnCheckRadius, LayerMask.GetMask("Mineral")) == null)
                    {
                        break;
                    }
                }
            }

            return currentSpawnOffset;
        }


        string IInteractable.GetInteractType()
        {
            return "Miner";
        }

        void IInteractable.OnSelected()
        {
            PlaceMinerOnDeposit();
        }

        bool IInteractable.CanInteract()
        {
            return CanPlaceMiner();
        }

        void IInteractable.DoHighlight(bool status)
        {
            LeanTween.cancel(gameObject);
            if (status)
            {
                transform.LeanScale(startScale * 1.25f, .25f);
            }
            else
            {
                transform.LeanScale(startScale, .25f);
            }
        }
    }
}
