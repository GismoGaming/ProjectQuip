using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gismo.Networking.Core;

namespace Gismo.Quip
{

    public class MineralCollectionPoint : Singleton<MineralCollectionPoint>, IInteractable
    {
        Vector3 startScale;

        public override void Awake()
        {
            base.Awake();
            startScale = transform.localScale;
        }

        bool IInteractable.CanInteract()
        {
            return PlayerCentralization.Instance.HasMineralsInHand();
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
            return "Mineral Collection Zone";
        }

        void IInteractable.OnSelected()
        {
            DropOffMinerals();
        }

        public void DropOffMinerals()
        {
            if (NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
            {
                List<Minerals.MineralCostDetails> costDetails = PlayerCentralization.Instance.GetListOfPickedUpMineralsCostDetails();
                foreach (Minerals.MineralCostDetails costDetail in costDetails)
                {
                    NetGameController.Instance.CollectMineral(costDetail, NetGameController.Instance.GetUserID());
                }

                Packet packet = new Packet(Networking.NetworkPackets.ServerSentPackets.MineralCollected);
                packet.WriteByte(NetGameController.Instance.GetUserID());
                packet.WriteList(PlayerCentralization.Instance.GetListOfPickedUpMineralIDs());
                packet.WriteList(costDetails);
                NetGameController.Instance.SendDataToAll_S(packet);
            }
            else
            {
                Packet packet = new Packet(Networking.NetworkPackets.ClientSentPackets.MineralCollected, NetGameController.Instance.GetUserID());
                packet.WriteList(PlayerCentralization.Instance.GetListOfPickedUpMineralIDs());

                NetGameController.Instance.SendData_C(packet);
            }

            PlayerCentralization.Instance.ClearPickedUpMinerals();
        }
    }
}