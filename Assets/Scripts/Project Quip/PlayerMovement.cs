using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Gismo.Networking.Core;
using Gismo.Networking;

namespace Gismo.Quip
{
    public enum UpdateType { TimedUpdate, Update, LateUpdate, FixedUpdate };
    public class PlayerMovement : MonoBehaviour
    {
        private int controllingUser;

        [HideInInspector]
        public bool localPlayer;

        [SerializeField] private float moveSpeed;

        [SerializeField] private UpdateType updateType;

        [System.Serializable]
        public class TimedUpdateDetails
        {
            public bool doUpdate = true;
            public float updateTime;
        }

        [SerializeField] private TimedUpdateDetails timedUpdateDetails;

        [SerializeField] TMPro.TextMeshProUGUI text;

        private Vector3 lastKnown;

        [SerializeField] private float minMoveDistance = .1f;

        public void Initalize(int playerID)
        {
            controllingUser = playerID;

            localPlayer = controllingUser == NetGameController.instance.GetUserID();

            if (localPlayer)
            {
                GetComponent<SpriteRenderer>().color = Color.red;
            }

            text.text = $"{controllingUser} - {localPlayer}";

            GetComponentInChildren<Canvas>().sortingOrder += controllingUser;
            GetComponent<SpriteRenderer>().sortingOrder += controllingUser;

            if (updateType == UpdateType.TimedUpdate && localPlayer)
                StartCoroutine(TimedUpdateLoop());

            lastKnown = transform.position;
        }

        public void UpdatePosition(Vector3 position)
        {
            transform.position = position;
        }

        public void PrepairPacket()
        {
            if (!localPlayer)
                return;

            if(Vector3.Distance(transform.position,lastKnown) < minMoveDistance)
            {
                return;
            }

            lastKnown = transform.position;

            switch(NetGameController.instance.GetConnectionType())
            {
                case ConnectionType.Client:
                    Packet clientPacket = new Packet(NetworkPackets.ClientSentPackets.ClientPosition, NetGameController.instance.GetUserID());

                    clientPacket.WriteVector3(transform.position);

                    NetGameController.instance.SendData_C(clientPacket);
                    break;
                case ConnectionType.Server:
                    Packet serverPacket = new Packet(NetworkPackets.ServerSentPackets.ClientPositionShare);

                    serverPacket.WriteInt(-1);
                    serverPacket.WriteVector3(transform.position);

                    NetGameController.instance.SendDataToAll_S(serverPacket);
                    break;
                default:
                    return;
            }
        }

        public void Update()
        {
            if (updateType == UpdateType.Update)
            {
                PrepairPacket();
            }

            if (!localPlayer)
                return;

            transform.Translate(new Vector3(Input.GetAxis("Horizontal"), Input.GetAxis("Vertical"), 0f) * (moveSpeed * Time.deltaTime));
        }

        public void LateUpdate()
        {
            if (updateType == UpdateType.LateUpdate)
            {
                PrepairPacket();
            }
        }

        public void FixedUpdate()
        {
            if (updateType == UpdateType.FixedUpdate)
            {
                PrepairPacket();
            }
        }

        IEnumerator TimedUpdateLoop()
        {
            while (timedUpdateDetails.doUpdate)
            {
                PrepairPacket();
                yield return new WaitForSeconds(timedUpdateDetails.updateTime);
            }
        }
    }
}
