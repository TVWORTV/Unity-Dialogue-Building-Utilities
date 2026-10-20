using System;
using UnityEngine;

[Serializable]
public class EntryPart : TextPart
{
    [SerializeReference]
    public TextPart next;

    public override TextPart GetNext()
    {
        return next;
    }
}
