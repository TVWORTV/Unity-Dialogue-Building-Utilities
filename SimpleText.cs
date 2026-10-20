using System;
using UnityEngine;

[Serializable]
public class SimpleText : TextPart
{
    [TextArea]
    public string line;

    [SerializeReference]
    public TextPart next;

    public override TextPart GetNext()
    {
        return next;
    }
}
