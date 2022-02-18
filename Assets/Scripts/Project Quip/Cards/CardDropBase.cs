using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gismo.Quip.Cards
{
    public class CardDropBase : MonoBehaviour
    {
        private Camera mainCamera;

        public delegate void CardEvent(Vector3 position);
        public CardEvent OnCardDown;
        public CardEvent OnCardAwake;

        private float zOffset;

        public virtual void Awake()
        {
            mainCamera = Camera.main;

            OnCardAwake?.Invoke(GetMousePosition());
        }

        public void OnCardDropped()
        {
            OnCardDown?.Invoke(GetMousePosition());
        }

        Vector3 GetMousePosition()
        {
            return mainCamera.ScreenToWorldPoint(transform.position).ChangeZ(zOffset);
        }
    }
}