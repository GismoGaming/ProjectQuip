using Gismo.Networking.Core;
using Gismo.Quip.Minerals;
using UnityEngine;

namespace Gismo.Quip.RoleSpecific
{
    public class HaulerContainmentCube : TrackedGameObject, IInteractable
    {
        [SerializeField] private GameObject prefab;
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

                newPacket.WriteInt((int)Role.Hauler);

                NetGameController.Instance.SendDataToAll_S(newPacket);
            }
            else
            {
                Packet newPacket = GetPositionPacketClient(Networking.NetworkPackets.ClientSentPackets.SpecialAbiltyPlace);

                newPacket.WriteInt((int)Role.Hauler);

                NetGameController.Instance.SendData_C(newPacket);
            }
        }

        bool IInteractable.CanInteract()
        {
            return PlayerCentralization.Instance.CanDropOffToCompressed();
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
            return "Hauler Containment Cube";
        }

        public GameObject SpawnNewCubeWithID(MineralCostDetails details,Vector2 p,uint id)
        {
            GameObject newGameObject = Instantiate(prefab, p, Quaternion.identity);
            newGameObject.GetComponent<CompressedMineralCube>().InitalizeWithID(p,details,id);
            return newGameObject;
        }

        public GameObject SpawnNewCube(MineralCostDetails details)
        {
            Vector2 p = transform.position.AddRandomOnUnitCircle(1f);
            GameObject newGameObject = Instantiate(prefab, p, Quaternion.identity);
            newGameObject.GetComponent<CompressedMineralCube>().InitalizeAndGenerateID(p, details);
            return newGameObject;
        }

        void IInteractable.OnSelected()
        {
            MineralCostDetails details = new MineralCostDetails();
            foreach(MineralCostDetails costs in PlayerCentralization.Instance.GetListOfPickedUpMineralsCostDetails())
            {
                details.price += costs.price;
            }

            details.price *= 2f;

            details.name = "Containment Cube";

            if(NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
            {
                GameObject g = SpawnNewCube(details);
                Packet newPacket = GetPacketServer(Networking.NetworkPackets.ServerSentPackets.CompressedSpawn);
                newPacket.WriteUint(g.GetComponent<CompressedMineralCube>().ID);
                newPacket.WriteCostDetails(details);
                newPacket.WriteVector2(g.transform.position.ToVector2());

                NetGameController.Instance.SendDataToAll_S(newPacket);
            }
            else
            {
                Packet newPacket = GetPacketClient(Networking.NetworkPackets.ClientSentPackets.CompressedSpawn);

                newPacket.WriteCostDetails(details);

                NetGameController.Instance.SendData_C(newPacket);
            }

            PlayerCentralization.Instance.ClearPickedUpMinerals();
        }

        public override string ToString()
        {
            return $"{ID} - HCC";
        }
    }
}
