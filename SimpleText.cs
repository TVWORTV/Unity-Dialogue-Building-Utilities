using System;
using UnityEngine;

[Serializable]
public class SimpleText : TextPart
{
    public Speaker speaker; 
    [TextArea]
    public string line;

    [SerializeReference]
    public TextPart next;

    public override TextPart GetNext()
    {
        return next;
    }
}
