using System.Collections;
using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;

public class Notification : Singleton<Notification> 
{
    [SerializeField] private TextMeshProUGUI messageCenter;

    [SerializeField] private ScrollRect rect;
    [SerializeField] private ContentSizeFitter fitter;

    public override void Awake()
    {
        base.Awake();

        messageCenter.text = "";
        Push("Welcome");
    }

    public static void PushNotification(string msg)
    { 
        Instance.Push(msg);
    }

    public void Push(string msg)
    {
        messageCenter.text += $"{msg}\n";
        fitter.SetLayoutVertical();
        rect.verticalNormalizedPosition = 0f;
    }
}
