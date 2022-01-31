using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gismo.Quip.Cards
{
    public class CardHandler : MonoBehaviour
    {
        RectTransform rect;

        bool isDragging;

        Vector2 lastPosition;

        Vector2 homePosition;

        [SerializeField] private RectTransform homeDeckRect;

        [SerializeField] Transform outsideRect;

        void Start()
        {
            rect = GetComponent<RectTransform>();

            homePosition = rect.position;
        }

        void Update()
        {
            DoMouseDrag();
        }

        void DoMouseDrag()
        {
            if(rect.IsInsideRect(Input.mousePosition) && Input.GetMouseButtonDown(0) && !isDragging)
            {
                isDragging = true;
                lastPosition = Input.mousePosition;

                rect.SetParent(outsideRect);
            }

            if (isDragging)
            {
                if (Input.GetMouseButton(0))
                {
                    lastPosition = rect.position;

                    rect.position = Input.mousePosition;

                    if (!StaticFunctions.IsRectWithinScreen(rect))
                    {
                        rect.position = lastPosition;
                    }
                }

                if (Input.GetMouseButtonUp(0))
                {
                    isDragging = false;

                    if(homeDeckRect.IsRectWithin(rect))
                    {
                        LeanTween.move(rect.gameObject, homePosition, .5f).setEaseOutBounce();

                        rect.SetParent(homeDeckRect);
                    }
                    else
                    {
                        rect.SetParent(outsideRect);
                    }
                }

                if (Input.GetMouseButtonUp(1))
                {
                    isDragging = false;
                   
                    LeanTween.move(rect.gameObject, homePosition, .5f).setEaseOutBounce();

                    rect.SetParent(homeDeckRect);
                }
            }
        }
    }
}

