using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;

using UnityEngine.AI;
using Gismo.Networking.Core;
using Gismo.Networking;

namespace Gismo.Quip
{
    public enum Role {NA, Protector, Hauler, Logistics};
    public class PlayerController : MonoBehaviour
    {
        private byte controllingUser;

        [HideInInspector]
        public bool localPlayer;

        [SerializeField] TextMeshProUGUI debugText;

        private Vector2 lastKnown;

        [SerializeField] private float minMoveDistance = .1f;

        [SerializeField] private GameObject moveFlag;

        private NavMeshAgent agent;

        [SerializeField] private GameObject selectionCircle;
        [SerializeField] private float attackRange;

        private CameraController cameraController;

        [SerializeField] private GameObject localPlayerIndicator;

        private float zPos;

        private string username;

        private bool canAttack = true;

        private Role playerRole;

        private InputActionMap action;

        public bool canTakeDamage;

        public string GetUserName()
        {
            return username;
        }
        public void SetUserName(string s)
        {
            if (string.IsNullOrEmpty(s) || username == s)
                return;

            if (string.IsNullOrEmpty(username))
            {
                Notification.PushNotification($"{s} joins the fray!");
            }
            else
            {
                Notification.PushNotification($"{username} has changed their name to {s}!");
            }    

            username = s;

            UpdateDebug();
        }
        public byte GetControllingPlayer()
        {
            return controllingUser;
        }
        public void UpdateRole(Role newRole)
        {
            if (localPlayer && newRole != Role.NA)
            {
                Cards.SpecialCard.Instance.UpdateElement(newRole);
            }
            playerRole = newRole;
            UpdateDebug();
        }

        public void UpdateDebug()
        {
            debugText.text = $"{username}: {playerRole}";
        }

#if UNITY_EDITOR
        private void Start()
        {
            Initalize(byte.MinValue, NetGameController.Instance.GetWaitingRoomSpawnpoint().position);

            SetUserName(NetGameController.Instance.GetRandomUserName());
        }
#endif

        private void Awake()
        {
#if UNITY_EDITOR
            StaticFunctions.MoveToMainScene(gameObject);
            Cards.OmniInteractiveCard.Instance.SetPlayer(this);
            Cards.SpecialCard.Instance.SetPlayer(this);
#endif
            canTakeDamage = true;
            zPos = transform.position.z;
        }

        public void SetPosition(Vector2 position)
        {
            agent.Warp(position.ToVector3(zPos));

            UpdatePosition(position);

            if (localPlayer)
            {
                moveFlag.transform.position = position.ToVector3(zPos);

                cameraController.SetPosition(transform.position.ToVector2(), cameraController.GetDefaultZoom());
            }
        }

        public void SetPosition(SerializableVector2 position)
        {
            SetPosition(new Vector3(position.x, position.y, zPos));
        }

        public void SetInteractionCircle(bool status)
        {
            selectionCircle.SetActive(status);
        }

        public void Initalize(byte playerID, Vector3 startPos)
        {
            agent = GetComponent<NavMeshAgent>();
            agent.updateRotation = false;
            agent.updateUpAxis = false;

            controllingUser = playerID;

#if UNITY_EDITOR
            localPlayer = true;
#else
            localPlayer = controllingUser == NetGameController.Instance.GetUserID();
#endif
            localPlayerIndicator.SetActive(localPlayer);
            SetInteractionCircle(false);

            if (localPlayer)
            {
                action = PlayerInput.GetPlayerByIndex(0).currentActionMap;
                localPlayerIndicator.SetActive(true);
                moveFlag = GameObject.Find("Move Flag");

                cameraController = FindObjectOfType<CameraController>();

                PlayerCentralization.Instance.UpdateUI();
            }

            SetPosition(startPos);
        }

        public void UpdatePosition(Vector2 position)
        {
            lastKnown = position;
        }

        public void DoAttack()
        {
            transform.LeanRotateAround(Vector3.back, 360f, 1f).setOnComplete(() => 
            { 
                transform.rotation = Quaternion.identity; 
                if(localPlayer)
                    canAttack = true; 

                if(NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
                {
                    List<uint> ids = new List<uint>();
                    foreach(Enemies.Enemy enemy in FindObjectsOfType<Enemies.Enemy>())
                    {
                        if(Vector3.Distance(transform.position, enemy.transform.position) <= attackRange)
                        {
                            enemy.DoDamage();
                            ids.Add(enemy.ID);
                        }
                    }

                    if(ids.Count !=0)
                    {
                        Packet p = new Packet(NetworkPackets.ServerSentPackets.EnemyDamaged);
                        p.WriteList(ids);

                        NetGameController.Instance.SendDataToAll_S(p);
                    }
                }
            });
        }

        public void DoDamage()
        {
            canTakeDamage = false;
            LTSeq seq = LeanTween.sequence();
            seq.append(transform.LeanRotateAround(Vector3.back, -15f, .1f));
            seq.append(transform.LeanRotateAround(Vector3.back, 30f, .1f));
            seq.append(transform.LeanRotateAround(Vector3.back, -5f, .1f));
            seq.append(transform.LeanRotateAround(Vector3.back, 10f, .1f));

            seq.append(() =>
            {
                transform.rotation = Quaternion.identity;
                canTakeDamage = true;
            });
        }

        public Role GetRole()
        {
            return playerRole;
        }

        public void Update()
        {
            if (localPlayer)
            {
                if (Vector3.Distance(transform.position, moveFlag.transform.position) > minMoveDistance)
                {
                    agent.SetDestination(moveFlag.transform.position);
                    //transform.position = Vector2.Lerp(transform.position, moveFlag.transform.position, moveSpeed * Time.deltaTime);
                }

                if(canAttack && action["Attack"].WasPressedThisFrame() && !DL.DLUp)
                {
                    TryAttacking();
                }
            }
            else
            {
                if (Vector3.Distance(transform.position, lastKnown.ToVector3(zPos)) > minMoveDistance)
                {
                    agent.SetDestination(lastKnown.ToVector3(zPos));
                    //transform.position = Vector2.Lerp(transform.position, lastKnown, moveSpeed * Time.deltaTime);
                }
            }
        }

        private void TryAttacking()
        {
            if (PlayerCentralization.Instance.HasMineralsInHand())
            {
                foreach (uint id in PlayerCentralization.Instance.GetListOfPickedUpMineralIDs())
                {
                    ((Minerals.Mineral)NetGameController.Instance.GetTrackedScript(id)).Drop(transform.position.ToVector2() + StaticFunctions.RandomOnUnitCircle(1f));
                }
                PlayerCentralization.Instance.ClearPickedUpMinerals();
                PlayerCentralization.Instance.UpdateUI();
            }
            canAttack = false;
            DoAttack();

            if (NetGameController.Instance.IsConnectedAs(ConnectionType.Server))
            {
                Packet p = new Packet(NetworkPackets.ServerSentPackets.ClientAttack);

                p.WriteByte(NetworkStatics.ServerID);

                NetGameController.Instance.SendDataToAll_S(p);
            }
            else
            {
                NetGameController.Instance.SendData_C(new Packet(NetworkPackets.ClientSentPackets.ClientAttack, NetGameController.Instance.GetUserID()));
            }
        }
    }
}
