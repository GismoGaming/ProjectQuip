using UnityEngine;

using Gismo.Networking.Core;

namespace Gismo
{
    public class TrackedScript : MonoBehaviour
    {
        [ReadOnly.ReadOnly]
        [SerializeField] public int ID;

        private static int currentID;

        public virtual void Start()
        {
#if UNITY_EDITOR
            OnRegisterReady();
#else
            Quip.NetGameController.Instance.onControllerIsReady += OnRegisterReady;
#endif
        }

        public virtual void OnRegisterReady()
        {
#if !UNITY_EDITOR
            Quip.NetGameController.Instance.RegisterTrackedScript(ID, this);
#endif
        }

        public virtual Packet OnNewClient()
        {
            return new Packet();
        }
        
        [ContextMenu("Generate ID")]
        public void GenerateID()
        {
            ID = currentID;
            currentID++;
        }

        [ContextMenu("Reset ID")]
        void ResetID()
        {
            currentID = 0;
        }

        public Packet GetPacketServer(Networking.NetworkPackets.ServerSentPackets s)
        {
            Packet p = new Packet(s);
            p.WriteInt(ID);

            return p;
        }

        public Packet GetPacketServer(Networking.NetworkPackets.ClientSentPackets s,int playerID)
        {
            Packet p = new Packet(s,playerID);
            p.WriteInt(ID);

            return p;
        }
    }
}
