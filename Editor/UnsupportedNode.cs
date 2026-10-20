using System;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class UnsupportedNode : DialogueNode
{
    public UnsupportedNode(DialogueGraphView graph, string typeName)
        : base(graph, "Unsupported: " + typeName, new Color(0.5f, 0.15f, 0.15f), true, 340)
    {
    }

    public override IEnumerable<Port> OutputPorts => Array.Empty<Port>();

    public override TextPart GetLink(Port port)
    {
        return null;
    }

    public override void SetLink(Port port, TextPart target)
    {
    }

    protected override void Build()
    {
    }
}
