using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public abstract class DialogueNode : Node
{
    protected readonly DialogueGraphView graph;
    readonly VisualElement eventsContainer = new();

    public Port InputPort { get; private set; }
    public TextPart Part => graph.GetPart(this);

    public abstract IEnumerable<Port> OutputPorts { get; }
    public abstract TextPart GetLink(Port output);
    public abstract void SetLink(Port output, TextPart target);
    protected abstract void Build();

    public IEnumerable<Port> AllPorts
    {
        get
        {
            if (InputPort != null) yield return InputPort;
            foreach (Port port in OutputPorts) yield return port;
        }
    }

    protected DialogueNode(DialogueGraphView graph, string nodeTitle, Color titleColor, bool hasInput, float width)
    {
        this.graph = graph;
        title = nodeTitle;
        style.width = width;
        titleContainer.style.backgroundColor = titleColor;

        if (hasInput)
        {
            InputPort = InstantiatePort(Orientation.Horizontal, Direction.Input, Port.Capacity.Multi, typeof(TextPart));
            InputPort.portName = "In";
            inputContainer.Add(InputPort);
        }
    }

    public void Initialize()
    {
        Build();
        extensionContainer.Add(eventsContainer);
        RefreshExpandedState();
        RefreshPorts();
    }

    public void BindEvents(SerializedObject serializedObject, SerializedProperty partProperty)
    {
        eventsContainer.Clear();
        AddEventField(serializedObject, partProperty.FindPropertyRelative("onEnter"));
        AddEventField(serializedObject, partProperty.FindPropertyRelative("onExit"));
    }

    protected Port CreateOutputPort(string portName, int slot)
    {
        Port port = InstantiatePort(Orientation.Horizontal, Direction.Output, Port.Capacity.Single, typeof(TextPart));
        port.portName = portName;
        port.userData = slot;
        return port;
    }

    void AddEventField(SerializedObject serializedObject, SerializedProperty property)
    {
        PropertyField field = new(property);
        field.Bind(serializedObject);
        eventsContainer.Add(field);
    }
}
