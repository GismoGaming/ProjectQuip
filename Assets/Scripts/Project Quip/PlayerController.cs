using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using Gismo.Networking.Core;
using Gismo.Networking;

using UnityEngine.AI;

namespace Gismo.Quip
{
    public class PlayerController : MonoBehaviour
    {
        private int controllingUser;

        [HideInInspector]
        public bool localPlayer;

        [SerializeField] TMPro.TextMeshProUGUI text;

        private Vector3 lastKnown;

        [SerializeField] private float minMoveDistance = .1f;

        [SerializeField] private GameObject moveFlag;

        private NavMeshAgent agent;

#if UNITY_EDITOR
        private void Awake()
        {
            Initalize(-1, Vector3.zero);
        }
#endif

        public void Initalize(int playerID,Vector3 startPos)
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
            
            text.text = $"{controllingUser} - {localPlayer}";

            GetComponentInChildren<Canvas>().sortingOrder += controllingUser;
            GetComponent<SpriteRenderer>().sortingOrder += controllingUser;

            transform.position = startPos;

            if (localPlayer)
            {
                GetComponent<SpriteRenderer>().color = Color.red;
                moveFlag = GameObject.Find("Move Flag");

                transform.position = moveFlag.transform.position;
            }
            lastKnown = startPos;
        }

        public void UpdatePosition(Vector3 position)
        {
            lastKnown = position;
        }

        public void Update()
        {
            if (Vector3.Distance(transform.position, lastKnown) > minMoveDistance)
            {
                if (!localPlayer)
                {
                    agent.SetDestination(lastKnown);
                    //transform.position = Vector2.Lerp(transform.position, lastKnown, moveSpeed * Time.deltaTime);
                }
                else
                {
                    agent.SetDestination(moveFlag.transform.position);
                    //transform.position = Vector2.Lerp(transform.position, moveFlag.transform.position, moveSpeed * Time.deltaTime);
                }

            }
        }
    }
}
