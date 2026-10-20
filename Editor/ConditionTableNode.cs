using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class ConditionTableNode : DialogueNode
{
    readonly VisualElement entriesContainer = new();
    readonly List<Port> outputPorts = new();

    public ConditionTableNode(DialogueGraphView graph)
        : base(graph, "Condition Table", new Color(0.6f, 0.4f, 0.1f), true, 440)
    {
    }

    ConditionTable Table => (ConditionTable)Part;

    public override IEnumerable<Port> OutputPorts => outputPorts;

    public override TextPart GetLink(Port port)
    {
        return Table.entries[(int)port.userData].leadsTo;
    }

    public override void SetLink(Port port, TextPart target)
    {
        Table.entries[(int)port.userData].leadsTo = target;
    }

    protected override void Build()
    {
        extensionContainer.Add(entriesContainer);
        extensionContainer.Add(new Button(AddEntry) { text = "Add Entry" });
        Rebuild(false);
    }

    void AddEntry()
    {
        Table.entries.Add(new ConditionEntry());
        graph.MarkDirty();
        Rebuild(true);
    }

    void RemoveEntry(int index)
    {
        Table.entries.RemoveAt(index);
        graph.MarkDirty();
        Rebuild(true);
    }

    void AddCondition(int entryIndex)
    {
        Table.entries[entryIndex].conditions.Add(null);
        graph.MarkDirty();
        Rebuild(true);
    }

    void RemoveCondition(int entryIndex, int conditionIndex)
    {
        Table.entries[entryIndex].conditions.RemoveAt(conditionIndex);
        graph.MarkDirty();
        Rebuild(true);
    }

    void Rebuild(bool reconnect)
    {
        foreach (Port oldPort in outputPorts) graph.DisconnectEdges(oldPort);
        outputPorts.Clear();
        entriesContainer.Clear();

        List<ConditionEntry> entries = Table.entries;
        for (int i = 0; i < entries.Count; i++)
        {
            VisualElement box = CreateEntryBox(i, out Port newPort);
            entriesContainer.Add(box);
            outputPorts.Add(newPort);
            if (reconnect) graph.ConnectPort(newPort, entries[i].leadsTo);
        }

        RefreshPorts();
    }

    VisualElement CreateEntryBox(int index, out Port output)
    {
        ConditionEntry entry = Table.entries[index];

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

        Label entryLabel = new($"Entry {index + 1}");
        entryLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        entryLabel.style.flexGrow = 1;
        header.Add(entryLabel);

        IntegerField priorityField = new("Priority") { value = entry.priority };
        priorityField.style.width = 150;
        priorityField.RegisterValueChangedCallback(evt =>
        {
            Table.entries[index].priority = evt.newValue;
            graph.MarkDirty();
        });
        header.Add(priorityField);

        header.Add(new Button(() => RemoveEntry(index)) { text = "Remove" });

        output = CreateOutputPort("Leads to", index);
        header.Add(output);
        box.Add(header);

        VisualElement conditionsHeader = new();
        conditionsHeader.style.flexDirection = FlexDirection.Row;
        conditionsHeader.style.alignItems = Align.Center;
        Label conditionsLabel = new("Conditions");
        conditionsLabel.style.flexGrow = 1;
        conditionsHeader.Add(conditionsLabel);
        conditionsHeader.Add(new Button(() => AddCondition(index)) { text = "+" });
        box.Add(conditionsHeader);

        for (int j = 0; j < entry.conditions.Count; j++)
        {
            box.Add(CreateConditionRow(index, j, entry.conditions[j]));
        }

        return box;
    }

    VisualElement CreateConditionRow(int entryIndex, int conditionIndex, Condition current)
    {
        List<Condition> choices = new() { null };
        choices.AddRange(Resources.LoadAll<Condition>("Conditions"));
        if (current != null && !choices.Contains(current)) choices.Add(current);

        PopupField<Condition> popup = new(null, choices, choices.IndexOf(current), FormatCondition, FormatCondition);
        popup.style.flexGrow = 1;
        popup.RegisterValueChangedCallback(evt =>
        {
            Table.entries[entryIndex].conditions[conditionIndex] = evt.newValue;
            graph.MarkDirty();
        });

        VisualElement row = new();
        row.style.flexDirection = FlexDirection.Row;
        row.style.alignItems = Align.Center;
        row.Add(popup);
        row.Add(new Button(() => RemoveCondition(entryIndex, conditionIndex)) { text = "-" });
        return row;
    }

    static string FormatCondition(Condition condition)
    {
        return condition != null ? condition.name : "None";
    }
}
