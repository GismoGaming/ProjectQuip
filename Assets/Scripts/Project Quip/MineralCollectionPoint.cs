using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gismo.Networking.Core;

namespace Gismo.Quip
{

    public class MineralCollectionPoint : MonoBehaviour, IInteractable
    {
        Vector3 startScale;

        void Awake()
        {
            startScale = transform.localScale;
        }

        bool IInteractable.CanInteract()
        {
            return PlayerCentralization.Instance.pickedUpMineral.Count > 0;
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
            if (NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
            {
                List<Minerals.MineralCostDetails> costDetails = NetGameController.Instance.MineralCollectAction(GetListOfPickupMinerals());
                foreach (Minerals.MineralCostDetails costDetail in costDetails)
                {
                    NetGameController.Instance.CollectMineral(costDetail, NetGameController.Instance.GetUserID());
                }

                Packet packet = new Packet(Networking.NetworkPackets.ServerSentPackets.MineralCollected);
                packet.WriteByte(NetGameController.Instance.GetUserID());
                packet.WriteList(GetListOfPickupMinerals());
                packet.WriteList(costDetails);
                NetGameController.Instance.SendDataToAll_S(packet);
            }
            else
            {
                Packet packet = new Packet(Networking.NetworkPackets.ClientSentPackets.MineralCollected, NetGameController.Instance.GetUserID());
                packet.WriteList(GetListOfPickupMinerals());

                NetGameController.Instance.SendData_C(packet);
            }

            PlayerCentralization.Instance.pickedUpMineral.Clear();

            PlayerCentralization.Instance.UpdateUI();
        }

        List<uint> GetListOfPickupMinerals()
        {
            List<uint> returnable = new List<uint>();
            foreach(Minerals.Mineral m in PlayerCentralization.Instance.pickedUpMineral)
            {
                returnable.Add(m.ID);
            }
            return returnable;
        }
    }
}