using Gismo.Networking.Core;
using UnityEngine;

namespace Gismo.Quip.RoleSpecific
{
    public class ProtectorTurret : TrackedGameObject, IInteractable
    {
        Vector3 startScale;

        void Awake()
        {
            startScale = transform.localScale;
        }

        public override void OnPlacedDownPacket()
        {
            if (NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
            {
                Packet newPacket = GetPositionPacketServer(Networking.NetworkPackets.ServerSentPackets.SpecialAbiltyPlace);

                newPacket.WriteInt((int)Role.Protector);

                NetGameController.Instance.SendDataToAll_S(newPacket);
            }
            else
            {
                Packet newPacket = GetPositionPacketClient(Networking.NetworkPackets.ClientSentPackets.SpecialAbiltyPlace);

                newPacket.WriteInt((int)Role.Protector);

                NetGameController.Instance.SendData_C(newPacket);
            }
        }

        bool IInteractable.CanInteract()
        {
            return false;
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

        string IInteractable.GetInteractType()
        {
            return "Protector Turret";
        }

        void IInteractable.OnSelected()
        {       }

        public override string ToString()
        {
            return $"{ID} - PT";
        }
    }
}
