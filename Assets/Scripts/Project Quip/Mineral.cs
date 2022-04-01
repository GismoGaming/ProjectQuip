using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Gismo.Networking.Core;

namespace Gismo.Quip.Minerals
{
    public class Mineral : TrackedScript, IInteractable
    {
        private Vector2 spawnPoint;
        
        private MineralDeposit owningDeposit;
        private uint depositID;

        private Vector3 startScale;

        private MineralType mineralType;

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

            Initalize(spawn, depositID,type);

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

        public void PickupMineral()
        {
            owningDeposit.OnDepositLoseMineral();

            if(NetGameController.Instance.IsConnectedAs(ConnectionType.Client))
            {
                NetGameController.Instance.SendData_C(GetPacketClient(Networking.NetworkPackets.ClientSentPackets.PickupMineral));
            }
            else
            {
                NetGameController.Instance.SendDataToAll_S(GetPacketServer(Networking.NetworkPackets.ServerSentPackets.PickupMineral));
            }

            OnPickup();            
        }
        public void OnPickup()
        {
            gameObject.SetActive(false);
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

        string IInteractable.GetInteractType()
        {
            return "Mineral";
        }

        void IInteractable.OnSelected()
        {
            PickupMineral();

            PlayerCentralization.Instance.PickupMineral(this);
        }

        bool IInteractable.CanInteract()
        {
            return PlayerCentralization.Instance.gameObject.activeInHierarchy;
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
