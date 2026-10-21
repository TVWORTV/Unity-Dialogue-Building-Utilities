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
    readonly string defaultTitle;

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
        defaultTitle = nodeTitle;
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
        BuildNameField();
        Build();
        extensionContainer.Add(eventsContainer);
        RefreshTitle();
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

    void BuildNameField()
    {
        TextField nameField = new("Name") { value = Part.partName ?? string.Empty };
        nameField.labelElement.style.minWidth = 50;
        nameField.style.marginTop = 4;
        nameField.style.marginBottom = 4;
        nameField.RegisterValueChangedCallback(evt =>
        {
            Part.partName = evt.newValue;
            RefreshTitle();
            graph.MarkDirty();
        });
        extensionContainer.Add(nameField);
    }

    void RefreshTitle()
    {
        string partName = Part.partName;
        title = string.IsNullOrWhiteSpace(partName) ? defaultTitle : $"{partName.Trim()} ({defaultTitle})";
    }

    void AddEventField(SerializedObject serializedObject, SerializedProperty property)
    {
        PropertyField field = new(property);
        field.Bind(serializedObject);
        eventsContainer.Add(field);
    }
}
