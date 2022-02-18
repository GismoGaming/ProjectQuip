using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gismo.Generic
{
    public class SimpleFollow : MonoBehaviour
    {
        [SerializeField] private Transform target;
        [SerializeField] private Vector3 offset;
        [SerializeField] private float speed = 10f;

        void Start()
        {
            transform.position = target.position + offset;
        }

        void LateUpdate()
        {
            Vector3 finalPosition = target.position + offset;
            Vector3 lerpPosition = Vector3.Lerp(transform.position, finalPosition, speed * Time.deltaTime);
            transform.position = lerpPosition;
        }

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
        }
    }
}
