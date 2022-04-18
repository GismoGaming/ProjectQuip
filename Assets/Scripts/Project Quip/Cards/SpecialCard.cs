using UnityEngine;
using System.Collections;
using UnityEngine.UI;

namespace Gismo.Quip.Cards
{
    public class SpecialCard : CardHandler
    {
        public static SpecialCard Instance;

        private int currentOut;
        private SpecialCardElements element;
        private bool canUse;

        private Color defaultColor;
        private Color cooldownColor;

        private Image cardBackground;

        private Role role;

        [SerializeField] private PlayerController localPlayer;

        [SerializeField] private float playerInteractionRange;

        public override void Awake()
        {
            base.Awake();
            cardBackground = GetComponent<Image>();
            if (Instance != null)
            {
                Destroy(this.gameObject);
            }
            else
            {
                Instance = this;
            }

            defaultColor = cardBackground.color;
            cooldownColor = Color.Lerp(defaultColor, Color.black, .9f);

            OnCardDroped += TryPlace;
            UpdateUI();
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
        public bool IsPick()
        {
            return canUse && currentOut < element.maxOut;
        }

        public override bool CanPickup()
        {
            return base.CanPickup() && IsPick();
        }

        public void UpdateElement(Role newRole)
        {
            role = newRole;
            element = PlayerCentralization.Instance.roleSpecialsTable[role];

            canUse = true;
            UpdateUI();
        }

        public void TryPlace(Vector3 position)
        {
            localPlayer.SetInteractionCircle(false);

            if (element == null)
                return;

            if (Vector3.Distance(localPlayer.transform.position, position) >= playerInteractionRange)
                return;

            if(currentOut < element.maxOut && canUse)
            {
                GameObject newObject = Instantiate(element.prefab, position, Quaternion.identity);
                newObject.GetComponent<TrackedGameObject>().LocalPlace();
                currentOut++;

                StartCoroutine(DoCooldown());
            }
        }

        public void UpdateUI()
        {
            if (IsPick())
            {
                cardBackground.color = defaultColor;
            }
            else
            {
                cardBackground.color = cooldownColor;
            }
        }

        IEnumerator DoCooldown()
        {
            canUse = false;
            UpdateUI();
            yield return new WaitForSeconds(element.cooldown);
            canUse = true;

            UpdateUI();
        }
    }
}
