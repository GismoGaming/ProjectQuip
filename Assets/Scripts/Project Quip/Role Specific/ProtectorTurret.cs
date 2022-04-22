using Gismo.Networking.Core;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Gismo.Networking;

namespace Gismo.Quip.RoleSpecific
{
    public class ProtectorTurret : TrackedGameObject, IInteractable
    {
        Vector3 startScale;

        [SerializeField] private float attackRange;
        [SerializeField] private float attackRate;

        void Awake()
        {
            startScale = transform.localScale;

            if(NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
            {
                StartCoroutine(FireUpdate());
            }
        }

        IEnumerator FireUpdate()
        {
            yield return new WaitForSeconds(attackRate);

            DoAttack();

            StartCoroutine(FireUpdate());
        }

        void DoAttack()
        {
            foreach (Enemies.Enemy enemy in FindObjectsOfType<Enemies.Enemy>())
            {
                if (Vector3.Distance(transform.position, enemy.transform.position) <= attackRange)
                {
                    enemy.DoDamage();
                    Packet p = new Packet(NetworkPackets.ServerSentPackets.EnemyDamaged);
                    List<uint> s = new List<uint>();
                    s.Add(enemy.ID);
                    p.WriteList(s);

                    NetGameController.Instance.SendDataToAll_S(p);

                    return;
                }
            }
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
