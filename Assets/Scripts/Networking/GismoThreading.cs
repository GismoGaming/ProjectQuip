using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Gismo.Networking.Core
{
    public class GismoThreading : MonoBehaviour
    {
        private static GismoThreading instance = null;

        private class ActionQueue
        {
            public List<Action> actionQueuesFunctions = new List<Action>();
            public List<Action> actionCopiedQueueFunctions = new List<Action>();
            public bool noActionQueueToUpdateFunctions = true;

            public void AddAction(Action action)
            {
                if (action == null)
                {
                    throw new ArgumentNullException("action");
                }

                lock (actionQueuesFunctions)
                {
                    actionQueuesFunctions.Add(action);
                    noActionQueueToUpdateFunctions = false;
                }
            }

            public void ExecuteQueue()
            {
                if (noActionQueueToUpdateFunctions)
                {
                    return;
                }

                //Clear the old actions from the actionCopiedQueueUpdateFunc queue
                actionCopiedQueueFunctions.Clear();
                lock (actionQueuesFunctions)
                {
                    //Copy actionQueuesUpdateFunc to the actionCopiedQueueUpdateFunc variable
                    actionCopiedQueueFunctions.AddRange(actionQueuesFunctions);
                    //Now clear the actionQueuesUpdateFunc since we've done copying it
                    actionQueuesFunctions.Clear();
                    noActionQueueToUpdateFunctions = true;
                }

                // Loop and execute the functions from the actionCopiedQueueUpdateFunc
                for (int i = 0; i < actionCopiedQueueFunctions.Count; i++)
                {
                    actionCopiedQueueFunctions[i].Invoke();
                }
            }
        }

        private static ActionQueue normalUpdate;
        private static ActionQueue lateUpdate;
        private static ActionQueue fixedUpdate;

        public static void InitalizeThreading(bool visible = false)
        {
            if (instance != null)
            {
                return;
            }

            if (Application.isPlaying)
            {
                // add an invisible game object to the scene
                GameObject obj = new GameObject("Main Thread Executor");
                if (!visible)
                {
                    obj.hideFlags = HideFlags.HideAndDontSave;
                }

                DontDestroyOnLoad(obj);
                instance = obj.AddComponent<GismoThreading>();

                instance.Init();
            }
        }

        public void Init()
        {
            normalUpdate = new ActionQueue();
            lateUpdate = new ActionQueue();
            fixedUpdate = new ActionQueue();
        }

        public void Awake()
        {
            DontDestroyOnLoad(gameObject);
        }
        
        #region Normal Update
        public static void ExecuteInNormalUpdate(Action action)
        {
            InitalizeThreading();
            normalUpdate.AddAction(action);
        }
        void Update()
        {
            InitalizeThreading();
            normalUpdate.ExecuteQueue();    
        }
        #endregion

        #region Late Update
        public static void ExecuteInLateUpdate(Action action)
        {
            InitalizeThreading();
            lateUpdate.AddAction(action);
        }

        void LateUpdate()
        {
            InitalizeThreading();
            lateUpdate.ExecuteQueue();
        }
#endregion

        #region Fixed Update
        public static void ExecuteInFixedUpdate(Action action)
        {
            InitalizeThreading();
            fixedUpdate.AddAction(action);
        }

        void FixedUpdate()
        {
            InitalizeThreading();
            lateUpdate.ExecuteQueue();
        }

        #endregion

        public void OnDisable()
        {
            if (instance == this)
            {
                instance = null;
            }
        }
    }
}
