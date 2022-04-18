using Gismo.Networking.Core;
using UnityEngine;

namespace Gismo.Quip.Minerals
{
    public class Mineral : TrackedScript, IInteractable
    {
        private Vector2 spawnPoint;

        private MineralDeposit owningDeposit;
        private uint depositID;

        private Vector3 startScale;

        private MineralType mineralType;

        [SerializeField] private GameObject visual;
        bool firstPick;

        public override void Start()
        {
            base.Start();
            startScale = transform.localScale;
        }

        public uint InitalizeWithNewID(Vector2 spawn, MineralType type, uint depositID)
        {
            GenerateID();
            Initalize(spawn, depositID, type);

            return ID;
        }

        public uint InitalizeWithNewID(Vector2 spawn, MineralType type, uint depositID, uint newID)
        {
            ID = newID;

            Initalize(spawn, depositID, type);

            return ID;
        }

        private void Initalize(Vector2 spawn, uint depositID, MineralType type)
        {
            spawnPoint = spawn;
            transform.position = spawnPoint;
            this.depositID = depositID;
            mineralType = type;

            base.OnRegisterReady();

#if UNITY_EDITOR
            owningDeposit = GetOwningDeposit();
#else
            owningDeposit = (MineralDeposit)NetGameController.Instance.GetTrackedScript(depositID);
#endif
        }

        public MineralType GetMineralType()
        {
            return mineralType;
        }

        public void PickupFromDeposit()
        {
            owningDeposit.OnDepositLoseMineral();

            ChangeState(false, transform.position.ToVector2(), true);
        }
        
        public void ChangeState(bool state, Vector2 pos, bool origin)
        {
            firstPick = false;
            visual.SetActive(state);

            transform.position = pos.ToVector3(transform.position.z);

            DL.Log($"{state} for {ID} and orgin {origin}");

            if (origin)
            {
                if (NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
                {
                    Packet p = GetPacketServer(Networking.NetworkPackets.ServerSentPackets.MineralStateChange);
                    p.WriteBoolean(state);
                    p.WriteVector2(pos);

                    NetGameController.Instance.SendDataToAll_S(p);
                }
                else
                {
                    Packet p = GetPacketClient(Networking.NetworkPackets.ClientSentPackets.MineralStateChange);
                    p.ReadBoolean(state);
                    p.WriteVector2(pos);

                    NetGameController.Instance.SendData_C(p);
                }
            }
        }

#if UNITY_EDITOR
        public MineralDeposit GetOwningDeposit()
        {
            foreach (MineralDeposit d in FindObjectsOfType<MineralDeposit>())
            {
                if (d.ID == depositID)
                    return d;
            }
            return null;
        }
#endif

        public void Drop(Vector2 position)
        {
            transform.position = position.ToVector3(transform.position.z);
            ChangeState(true, transform.position.ToVector2(), true);
        }

        string IInteractable.GetInteractType()
        {
            return "Mineral";
        }

        void IInteractable.OnSelected()
        {
            if (firstPick)
            {
                PickupFromDeposit();
            }
            else
            {
                ChangeState(false, transform.position.ToVector2(), true);
            }

            PlayerCentralization.Instance.PickupMineral(this);
        }

        bool IInteractable.CanInteract()
        {
            return PlayerCentralization.Instance.CanPickup(false) && visual.activeInHierarchy;
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
            return $"{ID}, Mineral - depsoit:{depositID}, type - {mineralType}";
        }
    }
}
