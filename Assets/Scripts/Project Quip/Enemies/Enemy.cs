using System.Collections;
using System.Collections.Generic;
using Gismo.Networking;
using Gismo.Networking.Core;

using UnityEngine;
using UnityEngine.AI;

namespace Gismo.Quip.Enemies
{
    public class Enemy : TrackedScript
    {
        NavMeshAgent agent;

        private Vector2 gotoPoint;

        [SerializeField]private float tickRate;

        [Header("AI Behavior")]
        [SerializeField] private float playerSeeRange;
        [SerializeField] private float looseInterestRange;
        [SerializeField] private float attackRange;

        //[SerializeField] private TMPro.TextMeshProUGUI text;

        bool canAttack;

        bool isServer;

        enum EnemyState { RandomWander, PlayerChase, Attack}
        EnemyState currentState;

        PlayerController currentFollow;

        void Awake()
        {
            //text.text = "";
            agent = GetComponent<NavMeshAgent>();
            canAttack = true;
            agent.updateRotation = false;
            agent.updateUpAxis = false;
        }

        void Update()
        {
            if (!isServer)
                return;

            if (currentState == EnemyState.RandomWander)
            {
                if (GetNearbyPlayer(out currentFollow))
                {
                    //text.text = "Switch to player follow";
                    currentState = EnemyState.PlayerChase;
                    GoToPoint(currentFollow.transform.position);
                }
            }
            else if(currentState == EnemyState.PlayerChase)
            {
                if (Vector2.Distance(transform.position, currentFollow.transform.position) >= looseInterestRange)
                {
                    //text.text = "HasLost Interst";
                    currentState = EnemyState.RandomWander;
                    GenerateRandomMovePoint();
                }
                else if (Vector2.Distance(transform.position, currentFollow.transform.position) <= attackRange)
                {
                    currentState = EnemyState.Attack;
                    DoAttack();
                }
            }
            else if(currentState == EnemyState.Attack)
            {
                if (Vector2.Distance(transform.position, currentFollow.transform.position) < attackRange)
                {
                    DoAttack();
                }
                else
                {
                    currentState = EnemyState.PlayerChase;
                }
            }
        }

        private void DoAttack()
        {
            if (canAttack)
            {
                canAttack = false;
                agent.isStopped = true;
                DoAttackAnimation();
            }
        }

        IEnumerator Tick()
        {
            yield return new WaitForSeconds(tickRate);
            if (currentState == EnemyState.RandomWander)
            {
                //text.text = "Random Wander";
                GenerateRandomMovePoint();
            }
            else if (currentState == EnemyState.PlayerChase)
            {

                //text.text = "Following player";
                GoToPoint(currentFollow.transform.position);
            }
            else if(currentState == EnemyState.Attack)
            {
                //text.text = "Attacking";
            }

            StartCoroutine(Tick());
        }

        private void GenerateRandomMovePoint()
        {
            GoToPoint(transform.position.AddRandomOnUnitCircle(5f));
        }

        public void DoDamage()
        {
            LTSeq seq = LeanTween.sequence();
            seq.append(transform.LeanScale(transform.localScale * 1.1f, .2f));
            seq.append(transform.LeanScale(transform.localScale * .2f, .2f));
            seq.append(() => 
            {
                if(isServer)
                    EnemySpawnManagment.Instance.RemoveEnemy();
                Destroy(gameObject);
            });
        }

        public void DoAttackAnimation()
        {
            transform.LeanRotateAround(Vector3.back, 360f, 1f).setDelay(.5f).setOnComplete(() =>
            {
                transform.rotation = Quaternion.identity;
                if (isServer)
                {
                    canAttack = true;
                    agent.isStopped = false;
                }
            });

            if(isServer)
            {
                NetGameController.Instance.SendDataToAll_S(GetPacketServer(NetworkPackets.ServerSentPackets.EnemyAttack));

                List<byte> playersHit = new List<byte>();

                foreach (PlayerController player in NetGameController.Instance.GetPlayers().Values)
                {
                    if (Vector3.Distance(player.transform.position, transform.position) <= attackRange && player.canTakeDamage)
                    {
                        DL.Log($"Player {player.GetControllingPlayer()} has been hit!");

                        playersHit.Add(player.GetControllingPlayer());

                        player.DoDamage();
                    }
                }

                if (playersHit.Count != 0)
                {
                    Packet p = new Packet(NetworkPackets.ServerSentPackets.PlayerHurt);
                    p.WriteList(playersHit);

                    NetGameController.Instance.SendDataToAll_S(p);
                }
                
            }
        }

        public uint LocalPlace(Vector2 spawn)
        {
            GenerateID();
            
            base.OnRegisterReady();

            if(NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
            {
                isServer = true;
                gotoPoint = spawn;
                agent.SetDestination(gotoPoint);

                StartCoroutine(Tick());
            }

            return ID;
        }

        public void GlobalPlace(uint id, Vector2 position)
        {
            transform.position = position.ToVector3();

            ID = id;

            gotoPoint = position;

            base.OnRegisterReady();
        }


        void GoToMovePoint()
        {
            if (NavMesh.SamplePosition(gotoPoint, out NavMeshHit hit, 10f, NavMesh.AllAreas))
            {
                gotoPoint = hit.position;

                agent.SetDestination(gotoPoint);
            }

            if (isServer)
            {
                Packet toClients = GetPacketServer(NetworkPackets.ServerSentPackets.EnemyMove);
                toClients.WriteVector2(gotoPoint);
                NetGameController.Instance.SendDataToAll_S(toClients);
            }
        }

        public void GoToPoint(Vector2 position)
        {
            gotoPoint = position;

            GoToMovePoint();
        }

        bool GetNearbyPlayer(out PlayerController playerC)
        {
            foreach(PlayerController player in NetGameController.Instance.GetPlayers().Values)
            {
                if(Vector3.Distance(player.transform.position,transform.position) <= playerSeeRange)
                {
                    playerC = player;
                    return true;
                }
            }

            playerC = null;
            return false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.Lerp(Color.red, Color.clear, .5f);
            Gizmos.DrawSphere(transform.position, playerSeeRange);

            Gizmos.color = Color.Lerp(Color.green, Color.clear, .5f);
            Gizmos.DrawSphere(transform.position, looseInterestRange);

            Gizmos.color = Color.Lerp(Color.blue, Color.clear, .5f);
            Gizmos.DrawSphere(transform.position, attackRange);

        }
    }
}
