using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class EntryNode : DialogueNode
{
    Port output;

    public EntryNode(DialogueGraphView graph)
        : base(graph, "Entry", new Color(0.15f, 0.45f, 0.2f), false, 340)
    {
        capabilities &= ~Capabilities.Deletable;
        capabilities &= ~Capabilities.Copiable;
    }

    public override IEnumerable<Port> OutputPorts
    {
        get { yield return output; }
    }

    public override TextPart GetLink(Port port)
    {
        return ((EntryPart)Part).next;
    }

    public override void SetLink(Port port, TextPart target)
    {
        ((EntryPart)Part).next = target;
    }

    protected override void Build()
    {
        output = CreateOutputPort("Next", 0);
        outputContainer.Add(output);
    }
}
