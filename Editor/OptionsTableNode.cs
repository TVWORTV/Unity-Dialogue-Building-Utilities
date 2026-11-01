using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class OptionsTableNode : DialogueNode
{
    readonly VisualElement optionsContainer = new();
    readonly List<Port> outputPorts = new();

    public OptionsTableNode(DialogueGraphView graph)
        : base(graph, "Options", new Color(0.45f, 0.25f, 0.55f), true, 440)
    {
    }

    OptionsTable Table => (OptionsTable)Part;

    public override IEnumerable<Port> OutputPorts => outputPorts;

    public override TextPart GetLink(Port port)
    {
        return Table.options[(int)port.userData].leadsTo;
    }

    public override void SetLink(Port port, TextPart target)
    {
        Table.options[(int)port.userData].leadsTo = target;
    }

    protected override void Build()
    {
        extensionContainer.Add(optionsContainer);
        extensionContainer.Add(new Button(AddOption) { text = "Add Option" });
        Rebuild(false);
    }

    void AddOption()
    {
        Table.options.Add(new Option());
        graph.MarkDirty();
        Rebuild(true);
    }

    void RemoveOption(int index)
    {
        Table.options.RemoveAt(index);
        graph.MarkDirty();
        Rebuild(true);
    }

    void MoveOption(int index, int direction)
    {
        int target = index + direction;
        if (target < 0 || target >= Table.options.Count) return;

        (Table.options[index], Table.options[target]) = (Table.options[target], Table.options[index]);
        graph.MarkDirty();
        Rebuild(true);
    }

    void AddCondition(int optionIndex)
    {
        Table.options[optionIndex].conditions.Add(null);
        graph.MarkDirty();
        Rebuild(true);
    }

    void RemoveCondition(int optionIndex, int conditionIndex)
    {
        Table.options[optionIndex].conditions.RemoveAt(conditionIndex);
        graph.MarkDirty();
        Rebuild(true);
    }

    void Rebuild(bool reconnect)
    {
        foreach (Port oldPort in outputPorts) graph.DisconnectEdges(oldPort);
        outputPorts.Clear();
        optionsContainer.Clear();

        List<Option> options = Table.options;
        for (int i = 0; i < options.Count; i++)
        {
            VisualElement box = CreateOptionBox(i, out Port newPort);
            optionsContainer.Add(box);
            outputPorts.Add(newPort);
            if (reconnect) graph.ConnectPort(newPort, options[i].leadsTo);
        }

        RefreshPorts();
    }

    VisualElement CreateOptionBox(int index, out Port output)
    {
        Option option = Table.options[index];

        VisualElement box = new();
        box.style.backgroundColor = new Color(0f, 0f, 0f, 0.25f);
        box.style.marginTop = 4;
        box.style.marginBottom = 4;
        box.style.marginLeft = 4;
        box.style.marginRight = 4;
        box.style.paddingTop = 4;
        box.style.paddingBottom = 4;
        box.style.paddingLeft = 6;
        box.style.paddingRight = 6;
        box.style.borderTopLeftRadius = 4;
        box.style.borderTopRightRadius = 4;
        box.style.borderBottomLeftRadius = 4;
        box.style.borderBottomRightRadius = 4;

        VisualElement header = new();
        header.style.flexDirection = FlexDirection.Row;
        header.style.alignItems = Align.Center;

        Label optionLabel = new($"Option {index + 1}");
        optionLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        optionLabel.style.flexGrow = 1;
        header.Add(optionLabel);

        header.Add(new Button(() => MoveOption(index, -1)) { text = "\u25B2" });
        header.Add(new Button(() => MoveOption(index, 1)) { text = "\u25BC" });
        header.Add(new Button(() => RemoveOption(index)) { text = "Remove" });

        output = CreateOutputPort("Leads to", index);
        header.Add(output);
        box.Add(header);

        TextField textField = new("Text") { value = option.text ?? string.Empty };
        textField.labelElement.style.minWidth = 50;
        textField.RegisterValueChangedCallback(evt =>
        {
            Table.options[index].text = evt.newValue;
            graph.MarkDirty();
        });
        box.Add(textField);

        VisualElement conditionsHeader = new();
        conditionsHeader.style.flexDirection = FlexDirection.Row;
        conditionsHeader.style.alignItems = Align.Center;
        Label conditionsLabel = new("Conditions");
        conditionsLabel.style.flexGrow = 1;
        conditionsHeader.Add(conditionsLabel);
        conditionsHeader.Add(new Button(() => AddCondition(index)) { text = "+" });
        box.Add(conditionsHeader);

        for (int j = 0; j < option.conditions.Count; j++)
        {
            box.Add(CreateConditionRow(index, j, option.conditions[j]));
        }

        return box;
    }

    VisualElement CreateConditionRow(int optionIndex, int conditionIndex, Condition current)
    {
        List<Condition> choices = new() { null };
        choices.AddRange(Resources.LoadAll<Condition>("Conditions"));
        if (current != null && !choices.Contains(current)) choices.Add(current);

        PopupField<Condition> popup = new(null, choices, choices.IndexOf(current), FormatCondition, FormatCondition);
        popup.style.flexGrow = 1;
        popup.RegisterValueChangedCallback(evt =>
        {
            Table.options[optionIndex].conditions[conditionIndex] = evt.newValue;
            graph.MarkDirty();
        });

        VisualElement row = new();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.Add(popup);
        row.Add(new Button(() => RemoveCondition(optionIndex, conditionIndex)) { text = "-" });
        return row;
    }

    static string FormatCondition(Condition condition)
    {
        return condition != null ? condition.name : "None";
    }
}
