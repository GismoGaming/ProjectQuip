using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using Gismo.Networking.Core;

namespace Gismo.Quip.Minerals
{
    public enum MineralType { Aqua, Crystal, Earth, Void, Magic, Thunder, Evil}

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
        private bool doSpawn;

        [SerializeField] private MineralNode nodeDetails;

        private int amountCurrentlyOut;

        private GameObject prefab;
        private float spawnCheckRadius;

        [SerializeField] private Transform mineralSpawnPoint;
        [SerializeField] private float mineralStartSpawnOffset;

        [SerializeField] private float mineralEndSpawnOffset;

        [SerializeField] private LeanTweenMovement moveType;

        [SerializeField] private GameObject miningRigVisual;

        private bool hasBeenFound;
        private SpriteRenderer sr;

        [SerializeField] private float inRangeCheckDistance;

        [SerializeField] private bool startVisible;

        private bool canPlaceMiner;
        Vector3 startScale;

        byte foundUser;

        public override void Start()
        {
            base.Start();
            startScale = transform.localScale;
            SetMinerStatus(true);

            sr = GetComponent<SpriteRenderer>();

            if(startVisible)
            {
                HasBeenFound();
            }
            else
            {

                UpdateFoundVisual();
            }
        }

        public override void OnRegisterReady()
        {
            base.OnRegisterReady();
            
            prefab = MineralDatabase.Instance.GetMineralPrefab(nodeDetails.type);
            spawnCheckRadius = MineralDatabase.Instance.GetSpawnCheckRadius(nodeDetails.type);
        }

        void UpdateFoundVisual()
        {
            if(hasBeenFound)
            {
                sr.color = Color.white;
            }
            else
            {
                sr.color = Color.Lerp(Color.white, Color.clear, .9f);
            }
        }

        public void Initalize(Packet packet)
        {
            bool mineralStatus = packet.ReadBoolean();
            hasBeenFound = packet.ReadBoolean();

            if (hasBeenFound)
            {
                HasBeenFound(packet.ReadByte());
            }

            SetMinerStatus(mineralStatus);
        }

        public bool CanPlaceMiner()
        {
            return hasBeenFound && canPlaceMiner;
        }

        public void StartDepositOutput()
        {
            SetMinerStatus(false);
            doSpawn = true;
            StartCoroutine(UpdateLoop());
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

        void LateUpdate()
        {
            CheckVisual();
        }

        private void CheckVisual()
        {
            if(hasBeenFound)
                return;
            foreach(KeyValuePair<byte,PlayerController> player in  NetGameController.Instance.GetPlayers())
            {
                if(Vector3.Distance(transform.position,player.Value.gameObject.transform.position) <= inRangeCheckDistance)
                {
                    foundUser = player.Key;
                    HasBeenFound(foundUser);
                    Packet p = GetPacketServer(Networking.NetworkPackets.ServerSentPackets.MineralNodeFound);
                    p.WriteByte(foundUser);
                    NetGameController.Instance.SendDataToAll_S(p);
                    return;
                }
            }
        }

        public void HasBeenFound()
        {
            hasBeenFound = true;
            UpdateFoundVisual();
        }

        public void HasBeenFound(byte b)
        {
            if(!startVisible)
                Notification.PushNotification($"A new mineral node has been found by {NetGameController.Instance.GetPlayer(b).GetUserName()}");
            HasBeenFound();
        }

        IEnumerator UpdateLoop()
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
            StartCoroutine(UpdateLoop());
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
            GameObject G = Instantiate(prefab, mineralSpawnPoint.position, Quaternion.identity);

            G.LeanMove(startPosition, moveType.timing).setEase(moveType.type);
            return G;
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
            Packet clientPacket = GetPacketServer(Networking.NetworkPackets.ServerSentPackets.MineralDepositInit);

            clientPacket.WriteBoolean(canPlaceMiner);
            clientPacket.WriteBoolean(hasBeenFound);
            
            if(hasBeenFound)
            {
                clientPacket.WriteByte(foundUser);
            }

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

        public override string ToString()
        {
            return $"{ID}: Mineral Deposit ,type {nodeDetails.type}, miner status {canPlaceMiner}";
        }
    }
}
