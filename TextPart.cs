using System;
using UnityEngine;
using UnityEngine.Events;

[Serializable]
public abstract class TextPart
{
    public string partName = string.Empty;
    public Speaker speaker; 

    [HideInInspector]
    public Vector2 graphPosition;

    public abstract TextPart GetNext();
    public UnityEvent onEnter = new();
    public UnityEvent onExit = new();

    public virtual void OnEnter()
    {
        onEnter?.Invoke();
    }

    public virtual void OnExit()
    {
        onExit?.Invoke();
    }
}
