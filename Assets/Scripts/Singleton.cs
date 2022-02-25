using UnityEngine;
public class Singleton<T> : MonoBehaviour where T : Component
{
    public static T Instance;

    public virtual void Awake()
    {
        if (Instance != null)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this as T;
        }
    }
}