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
        [SerializeField] private UnityEvent OnCardResetHome;
        [SerializeField] private UnityEvent OnCardDropped;

        void Start()
        {
            rect = GetComponent<RectTransform>();

            startSize = rect.sizeDelta;
            changedSize = startSize * sizeChange;

            homePosition = rect.position;
        }

        void Update()
        {
            if (!isDragging)
            {
                if (rect.IsInsideRect(Mouse.current.position.ReadValue()) && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    isDragging = true;
                    lastPosition = Mouse.current.position.ReadValue();

                    rect.SetParent(outsideRect);

                    rect.LeanSize(changedSize, generalMovement.timing).setEase(generalMovement.type);
                }       
            }

            if (isDragging)
            {
                lastPosition = rect.position;

                rect.position = Mouse.current.position.ReadValue();

                if (!StaticFunctions.IsRectWithinScreen(rect))
                {
                    rect.position = lastPosition;
                }

                if (Mouse.current.leftButton.wasReleasedThisFrame)
                {
                    isDragging = false;

                    if (homeDeckRect.IsRectWithin(rect))
                    {
                        ResetCardToHome();
                        OnCardResetHome?.Invoke();
                    }
                    else
                    {
                        OnCardDropped?.Invoke();
                        LTSeq seq = LeanTween.sequence();
                        seq.append(.5f);
                        seq.append(() => ResetCardToHome());
                    }
                }

                if (Mouse.current.rightButton.wasPressedThisFrame)
                {
                    isDragging = false;

                    ResetCardToHome();
                    OnCardResetHome?.Invoke();
                }
            }
        }

        void ResetCardToHome()
        {
            rect.LeanSize(startSize, generalMovement.timing).setEase(generalMovement.type);
            LeanTween.move(rect.gameObject,homePosition,  generalMovement.timing).setEase(generalMovement.type);

            rect.SetParent(homeDeckRect);
        }
    }
}

