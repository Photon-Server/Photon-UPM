using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class NamedActionManager : MonoBehaviour
{
    public static NamedActionManager SharedInstance;
    [Serializable]
    public struct NamedActionDescriptor
    {
        public string actionName;
        public UnityEvent onActionEvent;
    }

    public List<NamedActionDescriptor> namedActionDescriptors = new List<NamedActionDescriptor>();


    private void Awake()
    {
        if(SharedInstance == null)
            SharedInstance = this;
    }

    private void OnDestroy()
    {
        if (SharedInstance == this)
            SharedInstance = null;
    }

    public static void Invoke(string actionName)
    {
        if (SharedInstance == null)
        {
            Debug.LogError("No NamedActionManager: impossible to invoce named action " + actionName);
            return;
        }
        foreach (var descriptor in SharedInstance.namedActionDescriptors)
        {
            if (descriptor.actionName == actionName && descriptor.onActionEvent != null)
            {
                descriptor.onActionEvent.Invoke();
            }
        }
    }
}
