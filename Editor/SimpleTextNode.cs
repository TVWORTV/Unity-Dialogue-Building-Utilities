using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class SimpleTextNode : DialogueNode
{
    Port output;

    public SimpleTextNode(DialogueGraphView graph)
        : base(graph, "Text", new Color(0.2f, 0.35f, 0.6f), true, 340)
    {
    }

    public override IEnumerable<Port> OutputPorts
    {
        get { yield return output; }
    }

    public override TextPart GetLink(Port port)
    {
        return ((SimpleText)Part).next;
    }

    public override void SetLink(Port port, TextPart target)
    {
        ((SimpleText)Part).next = target;
    }

    protected override void Build()
    {
        output = CreateOutputPort("Next", 0);
        outputContainer.Add(output);

        TextField field = new() { multiline = true, value = ((SimpleText)Part).line };
        field.style.minHeight = 70;
        field.RegisterValueChangedCallback(evt =>
        {
            ((SimpleText)Part).line = evt.newValue;
            graph.MarkDirty();
        });
        extensionContainer.Add(field);
    }
}
