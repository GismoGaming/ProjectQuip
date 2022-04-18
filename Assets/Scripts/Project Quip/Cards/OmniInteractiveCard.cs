using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gismo.Quip.Cards
{
    public class OmniInteractiveCard : CardHandler
    {
        public static OmniInteractiveCard Instance;

        [SerializeField] private float overlapRange;

        [SerializeField] private TMPro.TextMeshProUGUI contextMessage;

        private PlayerController localPlayer;

        [SerializeField] private float playerInteractionRange;

        [System.Serializable]
        public struct ContextDetails
        {
            public int maxAmount;
            public string contextText;
        }

        [SerializeField] private Tools.VisualDictionary<string, ContextDetails> contextItems;

        List<IInteractable> currentlyInteractable;

        [SerializeField] private string[] layers;

        class IInteractDetails
        {
            public string type;
            public List<IInteractable> interactions;
        }

        public override void Awake()
        {
            base.Awake();
            if (Instance != null)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }

            OnCardDroped += CheckContext;
            OnCardMoving += MessageContext;
            contextMessage.text = "";

#if !UNITY_EDITOR
            NetGameController.Instance.onAssignedID += GetPlayer;
#endif
            OnCardStartDrag += (() =>
            {
                localPlayer.SetInteractionCircle(true);
            });
        }

#if !UNITY_EDITOR
        void GetPlayer()
        {
            localPlayer = NetGameController.Instance.GetLocalPlayer();
        }
#endif
        public void SetPlayer(PlayerController p)
        {
            localPlayer = p;
        }
        private void CheckContext(Vector3 position)
        {
            SetInteractionStuffs(false);

            Collider2D[] results = Physics2D.OverlapCircleAll(position, overlapRange, LayerMask.GetMask(layers));

            if (results.Length != 0)
            {
                IInteractDetails details = GetMostInteractable(results);
                if (details != null)
                {
                    contextMessage.text = contextItems[details.type].contextText;
                    
                    foreach(IInteractable i in details.interactions)
                    {
                        i.OnSelected();
                    }
                }
            }

            localPlayer.SetInteractionCircle(false);
        }

        private void MessageContext(Vector3 position)
        {           
            Collider2D[] results = Physics2D.OverlapCircleAll(position, overlapRange,LayerMask.GetMask(layers));

            SetInteractionStuffs(false);

            if (results.Length != 0)
            {
                IInteractDetails details = GetMostInteractable(results);
                if (details != null)
                {
                    contextMessage.text = contextItems[details.type].contextText;
                    currentlyInteractable = details.interactions;
                    SetInteractionStuffs(true);
                }
            }
            else
            {
                contextMessage.text = "";
            }
        }

        private void SetInteractionStuffs(bool value)
        {
            if (currentlyInteractable == null)
                return;

            foreach (IInteractable interaction in currentlyInteractable)
            {
                interaction.DoHighlight(value);
            }
        }

        IInteractDetails GetMostInteractable(Collider2D[] colliders)
        {
            Dictionary<string, List<IInteractable>> interactables = new Dictionary<string, List<IInteractable>>();
            foreach(Collider2D c in colliders)
            {
                if (Vector3.Distance(localPlayer.transform.position, c.gameObject.transform.position) > playerInteractionRange)
                    continue;

                if(c.TryGetComponent(out IInteractable interaction))
                {
                    if(interaction.CanInteract())
                    {
                        string interactionType = interaction.GetInteractType();
                        if (!interactables.ContainsKey(interactionType))
                        {
                            interactables.Add(interactionType, new List<IInteractable>());
                        }

                        if (contextItems[interactionType].maxAmount != -1 && PlayerCentralization.Instance.GetListOfPickedUpMineralsCostDetails().Count + interactables[interactionType].Count >= contextItems[interactionType].maxAmount)
                            continue;

                        interactables[interactionType].Add(interaction);
                    }
                }
            }

            if (interactables.Count == 0)
                return null;

            KeyValuePair<string, List<IInteractable>> mostInRange = interactables.First();
            foreach (KeyValuePair<string, List<IInteractable>> valuePair in interactables)
            {
                if(mostInRange.Value.Count < valuePair.Value.Count)
                {
                    mostInRange = valuePair;
                }
            }

            return new IInteractDetails
            {
                type = mostInRange.Key,
                interactions = mostInRange.Value
            };
        }
    }
}