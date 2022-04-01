using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

using UnityEngine.AI;

namespace Gismo.Quip
{
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

        private CameraController cameraController;

        private float zPos;

#if UNITY_EDITOR
        private void Start()
        {
            Initalize(byte.MinValue, NetGameController.Instance.GetWaitingRoomSpawnpoint().position);
        }
#endif

        private void Awake()
        {
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

            SetInteractionCircle(false);

            if (localPlayer)
            {
                GetComponent<SpriteRenderer>().color = Color.red;
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

        public void Update()
        {
            if (!localPlayer)
            {
                if (Vector3.Distance(transform.position, lastKnown.ToVector3(zPos)) > minMoveDistance)
                {
                    agent.SetDestination(lastKnown.ToVector3(zPos));


                    debugText.text = $"{lastKnown.ToVector3(zPos)}";
                    //transform.position = Vector2.Lerp(transform.position, lastKnown, moveSpeed * Time.deltaTime);
                }
            }
            else
            {
                if (Vector3.Distance(transform.position, moveFlag.transform.position) > minMoveDistance)
                {
                    agent.SetDestination(moveFlag.transform.position);
                    debugText.text = $"{moveFlag.transform.position}";
                    //transform.position = Vector2.Lerp(transform.position, moveFlag.transform.position, moveSpeed * Time.deltaTime);
                }
            }
        }
    }
}
