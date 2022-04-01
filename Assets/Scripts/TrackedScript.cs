using UnityEngine;

using Gismo.Networking.Core;

namespace Gismo
{
    public class TrackedScript : MonoBehaviour
    {
        [ReadOnly.ReadOnly]
        [SerializeField] public uint ID;

        private static uint currentID;

        [SerializeField] private bool autoRegisterID;

        public virtual void Start()
        {
            if (autoRegisterID)
            {
#if UNITY_EDITOR
                OnRegisterReady();
#else
            Quip.NetGameController.Instance.onControllerIsReady += OnRegisterReady;
#endif
            }
        }

        public virtual void OnRegisterReady()
        {
            Quip.NetGameController.Instance.RegisterTrackedScript(ID, this);
        }

        public virtual Packet OnNewClient()
        {
            return Networking.NetworkStatics.EMPTY;
        }
        
        [ContextMenu("Generate ID")]
        public void GenerateID_EDITOR()
        {
            ID = currentID;
            currentID++;
        }

        public void GenerateID()
        {
            ID = Quip.NetGameController.Instance.GetNextTrackedID();
        }

        [ContextMenu("Reset ID")]
        void ResetID()
        {
            currentID = 0;
        }

        public Packet GetPacketServer(Networking.NetworkPackets.ServerSentPackets s)
        {
            Packet p = new Packet(s);
            p.WriteUint(ID);

            return p;
        }

        public Packet GetPacketClient(Networking.NetworkPackets.ClientSentPackets s)
        {
            return GetPacketClient(s, Quip.NetGameController.Instance.GetUserID());
        }

        public Packet GetPacketClient(Networking.NetworkPackets.ClientSentPackets s, byte playerID)
        {
            Packet p = new Packet(s,playerID);
            p.WriteUint(ID);

            return p;
        }
    }
}
