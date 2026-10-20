using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueGraphView : GraphView
{
    readonly FullTextAsset asset;
    readonly List<DialogueNode> nodeList = new();
    SerializedObject serializedObject;

    public DialogueGraphView(FullTextAsset asset)
    {
        this.asset = asset;
        DialogueAssetUtility.Sanitize(asset);

        style.flexGrow = 1;
        SetupZoom(ContentZoomer.DefaultMinScale, ContentZoomer.DefaultMaxScale);
        this.AddManipulator(new ContentDragger());
        this.AddManipulator(new SelectionDragger());
        this.AddManipulator(new RectangleSelector());

        GridBackground grid = new();
        Insert(0, grid);
        grid.StretchToParentSize();

        foreach (TextPart part in asset.parts)
        {
            DialogueNode node = CreateNode(part);
            nodeList.Add(node);
            AddElement(node);
            node.SetPosition(new Rect(part.graphPosition, Vector2.zero));
        }

        foreach (DialogueNode node in nodeList) node.Initialize();

        foreach (DialogueNode node in nodeList)
        {
            foreach (Port port in node.OutputPorts)
            {
                ConnectPort(port, node.GetLink(port));
            }
        }

        RebindAll();
        graphViewChanged = OnGraphViewChanged;
    }

    public TextPart GetPart(DialogueNode node)
    {
        return asset.parts[nodeList.IndexOf(node)];
    }

    public void MarkDirty()
    {
        EditorUtility.SetDirty(asset);
        serializedObject?.Update();
    }

    public void Save()
    {
        for (int i = 0; i < nodeList.Count; i++)
        {
            asset.parts[i].graphPosition = nodeList[i].GetPosition().position;
        }
        EditorUtility.SetDirty(asset);
        AssetDatabase.SaveAssets();
    }

    public void ConnectPort(Port output, TextPart target)
    {
        if (target == null) return;

        int index = asset.parts.IndexOf(target);
        if (index < 0) return;

        DialogueNode targetNode = nodeList[index];
        if (targetNode.InputPort == null) return;

        Edge edge = output.ConnectTo(targetNode.InputPort);
        AddElement(edge);
    }

    public void DisconnectEdges(Port port)
    {
        foreach (Edge edge in port.connections.ToList())
        {
            edge.input?.Disconnect(edge);
            edge.output?.Disconnect(edge);
            RemoveElement(edge);
        }
    }

    public override List<Port> GetCompatiblePorts(Port startPort, NodeAdapter nodeAdapter)
    {
        List<Port> compatible = new();
        ports.ForEach(port =>
        {
            if (port != startPort && port.node != startPort.node && port.direction != startPort.direction)
            {
                compatible.Add(port);
            }
        });
        return compatible;
    }

    public override void BuildContextualMenu(ContextualMenuPopulateEvent evt)
    {
        base.BuildContextualMenu(evt);
        if (evt.target is not GraphView) return;

        Vector2 position = contentViewContainer.WorldToLocal(evt.mousePosition);
        evt.menu.AppendAction("Simple Text", _ => AddPart(new SimpleText(), position));
        evt.menu.AppendAction("Condition Table", _ => AddPart(new ConditionTable(), position));
    }

    DialogueNode CreateNode(TextPart part)
    {
        switch (part)
        {
            case EntryPart:
                return new EntryNode(this);
            case SimpleText:
                return new SimpleTextNode(this);
            case ConditionTable:
                return new ConditionTableNode(this);
            default:
                return new UnsupportedNode(this, part.GetType().Name);
        }
    }

    void AddPart(TextPart part, Vector2 position)
    {
        part.graphPosition = position;
        asset.parts.Add(part);

        DialogueNode node = CreateNode(part);
        nodeList.Add(node);
        AddElement(node);
        node.SetPosition(new Rect(position, Vector2.zero));
        node.Initialize();

        MarkDirty();
        RebindAll();
    }

    void RemovePart(DialogueNode node)
    {
        int index = nodeList.IndexOf(node);
        if (index < 0) return;

        TextPart part = asset.parts[index];
        foreach (TextPart other in asset.parts) DialogueAssetUtility.ClearLinksTo(other, part);
        foreach (Port port in node.AllPorts) DisconnectEdges(port);

        asset.parts.RemoveAt(index);
        nodeList.RemoveAt(index);
    }

    void RebindAll()
    {
        serializedObject = new SerializedObject(asset);
        SerializedProperty parts = serializedObject.FindProperty(nameof(FullTextAsset.parts));

        for (int i = 0; i < nodeList.Count && i < parts.arraySize; i++)
        {
            nodeList[i].BindEvents(serializedObject, parts.GetArrayElementAtIndex(i));
        }
    }

    void ClearEdgeLink(Edge edge)
    {
        if (edge.output?.node is not DialogueNode from || edge.input?.node is not DialogueNode to) return;

        if (from.GetLink(edge.output) == to.Part) from.SetLink(edge.output, null);
    }

    GraphViewChange OnGraphViewChanged(GraphViewChange change)
    {
        bool changed = false;
        bool structural = false;

        if (change.elementsToRemove != null)
        {
            change.elementsToRemove.RemoveAll(element => element is EntryNode);

            foreach (GraphElement element in change.elementsToRemove)
            {
                if (element is Edge edge)
                {
                    ClearEdgeLink(edge);
                    changed = true;
                }
            }

            foreach (GraphElement element in change.elementsToRemove)
            {
                if (element is DialogueNode node)
                {
                    RemovePart(node);
                    structural = true;
                    changed = true;
                }
            }
        }

        if (change.edgesToCreate != null)
        {
            foreach (Edge edge in change.edgesToCreate)
            {
                if (edge.output?.node is DialogueNode from && edge.input?.node is DialogueNode to)
                {
                    from.SetLink(edge.output, to.Part);
                    changed = true;
                }
            }
        }

        if (change.movedElements != null)
        {
            foreach (GraphElement element in change.movedElements)
            {
                if (element is DialogueNode node)
                {
                    node.Part.graphPosition = node.GetPosition().position;
                    changed = true;
                }
            }
        }

        if (changed) MarkDirty();
        if (structural) RebindAll();

        return change;
    }
}
