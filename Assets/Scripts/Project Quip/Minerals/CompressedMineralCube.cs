using Gismo.Networking.Core;
using UnityEngine;

namespace Gismo.Quip.Minerals
{
    public class CompressedMineralCube : TrackedScript, IInteractable
    {
        private MineralCostDetails details;
        private Vector3 startScale;

        [SerializeField] private GameObject visual;

        public override void Start()
        {
            startScale = transform.localScale;
        }

        public MineralCostDetails GetMineralCosts()
        {
            return details;
        }

        public void InitalizeAndGenerateID(Vector2 position, MineralCostDetails details)
        {
            GenerateID();

            transform.position = position;
            this.details = details;

            base.OnRegisterReady();
        }

        public void InitalizeWithID(Vector2 position, MineralCostDetails details, uint id)
        {
            transform.position = position;
            this.details = details;
            ID = id;

            base.OnRegisterReady();
        }

        public void ChangeState(bool state, Vector2 pos, bool origin)
        {
            visual.SetActive(state);

            transform.position = pos.ToVector3(transform.position.z);

            DL.Log($"{state} for {ID} and orgin {origin}");

            if (origin)
            {
                if (NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
                {
                    Packet p = GetPacketServer(Networking.NetworkPackets.ServerSentPackets.CompressedMineralStateChange);
                    p.WriteBoolean(state);
                    p.WriteVector2(pos);

                    NetGameController.Instance.SendDataToAll_S(p);
                }
                else
                {
                    Packet p = GetPacketClient(Networking.NetworkPackets.ClientSentPackets.CompressedMineralStateChange);
                    p.ReadBoolean(state);
                    p.WriteVector2(pos);

                    NetGameController.Instance.SendData_C(p);
                }
            }
        }

        string IInteractable.GetInteractType()
        {
            return "Compressed Mineral Cube";
        }

        void IInteractable.OnSelected()
        {
            ChangeState(false, transform.position.ToVector2(), true);

            PlayerCentralization.Instance.PickupCompressedCube(this);
        }

        bool IInteractable.CanInteract()
        {
            return PlayerCentralization.Instance.playerRole == Role.Hauler && PlayerCentralization.Instance.CanPickup(true) && visual.activeInHierarchy;
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
            return $"{ID}, Compresed Mineral Cube";
        }
    }
}