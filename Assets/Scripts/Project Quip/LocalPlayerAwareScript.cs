using UnityEngine;

namespace Gismo.Quip
{ 
    public class LocalPlayerAwareScript : MonoBehaviour
    {
        [HideInInspector]
        public GameObject localPlayer;
        public virtual void Awake()
        {
#if UNITY_EDITOR
            localPlayer = FindObjectOfType<PlayerController>().gameObject;
#else
            NetGameController.Instance.onControllerIsReady += GetPlayer;
#endif
        }

#if !UNITY_EDITOR
        void GetPlayer()
        {
            localPlayer = NetGameController.Instance.GetLocalPlayer().gameObject;
        }
#endif
    }
}
