using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

namespace Gismo.Quip.Cards
{
    public class CardHandler : MonoBehaviour
    {
        RectTransform rect;

        bool isDragging;

        Vector2 lastPosition;

        Vector2 homePosition;

        [SerializeField] private RectTransform homeDeckRect;

        [SerializeField] RectTransform outsideRect;

        private Vector2 startSize;

        [SerializeField] private float sizeChange;
        private Vector2 changedSize;

        [SerializeField] private LeanTweenMovement generalMovement;

        [Header("Card Events")]
        private Camera mainCamera;

        public delegate void CardEvent();
        public delegate void CardEventPositon(Vector3 position);

        public CardEventPositon OnCardDroped;
        public CardEvent OnCardStartDrag;
        public CardEvent OnCardResetHome;

        public CardEventPositon OnCardMoving;

        private float zOffset;

        private InputActionMap action;

        public virtual void Awake()
        {
            action = PlayerInput.GetPlayerByIndex(0).currentActionMap;
            rect = GetComponent<RectTransform>();

            startSize = rect.sizeDelta;
            changedSize = startSize * sizeChange;

            homePosition = rect.position;
            mainCamera = Camera.main;
        }

        Vector3 GetMousePosition()
        {
            return mainCamera.ScreenToWorldPoint(transform.position).ChangeZ(zOffset);
        }

        public virtual bool CanPickup()
        {
            return rect.IsInsideRect(action["Card Position"].ReadValue<Vector2>());
        }

        void Update()
        {
            if (!isDragging)
            {
                if (CanPickup() && action["Card Pickup"].WasPressedThisFrame())
                {
                    isDragging = true;
                    lastPosition = action["Card Position"].ReadValue<Vector2>();

                    rect.SetParent(outsideRect);

                    rect.LeanSize(changedSize, generalMovement.timing).setEase(generalMovement.type);

                    OnCardStartDrag?.Invoke();
                }
            }

            if (isDragging)
            {
                lastPosition = rect.position;

                rect.position = action["Card Position"].ReadValue<Vector2>();

                if (!StaticFunctions.IsRectWithinScreen(rect))
                {
                    rect.position = lastPosition;
                }

                if (action["Card Pickup"].WasReleasedThisFrame())
                {
                    isDragging = false;

                    if (homeDeckRect.IsRectWithin(rect))
                    {
                        ResetCardToHome();
                        OnCardResetHome?.Invoke();
                    }
                    else
                    {
                        OnCardDroped?.Invoke(GetMousePosition());
                        LTSeq seq = LeanTween.sequence();
                        seq.append(.5f);
                        seq.append(() => ResetCardToHome());
                    }
                }

                if (action["Card Cancel"].WasReleasedThisFrame())
                {
                    isDragging = false;

                    ResetCardToHome();
                    OnCardResetHome?.Invoke();
                }
            }
        }

        void LateUpdate()
        {
            OnCardMoving?.Invoke(GetMousePosition());
        }

        void ResetCardToHome()
        {
            rect.LeanSize(startSize, generalMovement.timing).setEase(generalMovement.type);
            LeanTween.move(rect.gameObject, homePosition, generalMovement.timing).setEase(generalMovement.type);

            rect.SetParent(homeDeckRect);
        }


    }
}

