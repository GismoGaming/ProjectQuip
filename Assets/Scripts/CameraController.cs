using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Gismo.Quip
{
    public class CameraController : MonoBehaviour
    {
        Vector2 lastDragPosition;

        [SerializeField] private float dragSensitivity;
        [SerializeField] private float zoomSensitivity;
        [SerializeField] private float zoomSmooth;

        [SerializeField] private Bounds cameraBounds;

        private Camera mainCamera;

        [SerializeField] private Vector2 zoomBounds;
        [SerializeField] private float startingZoom;

        private float zoomLevel;

        void Start()
        {
            mainCamera = GetComponent<Camera>();

            zoomLevel = startingZoom;
        }

        private void Update()
        {
            if(Mouse.current.rightButton.wasPressedThisFrame)
            {
                lastDragPosition = Mouse.current.position.ReadValue();
            }

            if (Mouse.current.scroll.y.ReadValue() != 0.0f)
            {
                zoomLevel -= Mouse.current.scroll.y.ReadValue() * zoomSensitivity;
                zoomLevel = Mathf.Clamp(zoomLevel, zoomBounds.x, zoomBounds.y);
            }

            mainCamera.orthographicSize = Mathf.Lerp(mainCamera.orthographicSize, zoomLevel, zoomSmooth * Time.deltaTime);

            if (Mouse.current.rightButton.IsPressed())
            {
                if (cameraBounds.Contains(transform.position.ToVector2() + (lastDragPosition - Mouse.current.position.ReadValue()) * Time.deltaTime * dragSensitivity))
                {
                    transform.Translate((lastDragPosition - Mouse.current.position.ReadValue()) * Time.deltaTime * dragSensitivity);
                    lastDragPosition = Mouse.current.position.ReadValue();
                }
            }
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireCube(cameraBounds.center, cameraBounds.size);

            Gizmos.color = Color.blue;

            Bounds temp = new Bounds(cameraBounds.center, cameraBounds.size);

            temp.Expand(zoomBounds.y*3.6f);

            Gizmos.DrawWireCube(temp.center, temp.size);
        }
    }
}
