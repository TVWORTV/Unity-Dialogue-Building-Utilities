# Unity Dialogue Building Utilities

A small toolkit for building branching dialogue in Unity. Dialogue is stored as a `ScriptableObject` made of linked **parts**, and edited in a node-based graph window.

- **Entry**: the start of every dialogue. It has one output.
- **Simple Text**: a line of dialogue that leads to one next part.
- **Condition Table**: a list of entries, each with a set of `Condition` assets, a priority and a target part. The first matching target (highest priority, random among ties) is chosen at runtime.

- **Options Table**: a list of player choices, each with text, a set of `Condition` assets and a target part. Only the options whose conditions are all true are available, and the one the player selects decides where the dialogue goes next.

Every part can be given a **name**, and every part has `onEnter` / `onExit` `UnityEvent`s that can be wired up from the graph.

## Prerequisites

- **Unity 2021.2 or newer.** The scripts use C# 9 features (target-typed `new()` and `??=`), and the editor uses `UnityEditor.Experimental.GraphView` and UI Toolkit.
- No extra packages are required.
- The repository folder must be placed **inside your project's `Assets` folder** (for example `Assets/DialogueUtilities/`). Keep the `Editor` subfolder named exactly `Editor` so Unity treats those scripts as editor-only.
- Dialogue assets are saved under `Assets/Resources/Dialogues/` and conditions are loaded from `Resources/Conditions`. The tool creates the `Dialogues` folder for you; you create the `Conditions` folder yourself.

## Installation

1. Copy this repository into your project, e.g. `Assets/DialogueUtilities/`.
2. Wait for Unity to finish compiling.
3. Open the editor from the menu: **Tools > Dialogue Editor**.

## Using the editor

### Creating and opening dialogues

- Type a name in the text box in the toolbar and press **Create New**. This creates `Assets/Resources/Dialogues/<Name>/<Name>.asset` and opens it. The name must be a valid file name and must not already exist.
- Press **Edit Existing** to pick from the dialogues found in `Assets/Resources/Dialogues/`.
- Press **Save** to write the graph to disk. The graph is also saved when you switch dialogues or close the window.

### Working in the graph

| Action | How |
| --- | --- |
| Add a part | Right-click an empty area of the graph and choose **Simple Text**, **Condition Table** or **Options Table** |
| Connect parts | Drag from an output port to the **In** port of another part |
| Remove a connection | Select the edge and press Delete |
| Delete a part | Select the node and press Delete (the Entry node cannot be deleted) |
| Pan | Drag with the middle mouse button, or hold Alt and drag with the left button |
| Zoom | Scroll wheel |
| Multi-select | Drag a rectangle on the background with the left mouse button |

Each output port accepts a single connection. A part's input port accepts any number of connections.

### Naming parts

Every node has a **Name** field at the top of its body.

- Type a name and the node title updates immediately to `Name (Type)`, for example `Greeting (Text)` or `Shop check (Condition Table)`.
- Clear the field to go back to the plain type title.
- The name is stored in `TextPart.partName`, so it is available at runtime as well.
- Names are labels only. They are not required to be unique and nothing links parts by name.
- Dialogues created before names existed keep working. Their parts simply have an empty name until you set one.

### Condition Table nodes

- **Add Entry** adds a row. Each row has a **Priority**, a **Leads to** port and a **Remove** button.
- **+** adds a condition slot to the row and **-** removes it. Each slot is a dropdown listing every `Condition` asset found in a `Resources/Conditions` folder.
- A row is valid only if all of its conditions are true. Among valid rows the highest priority wins, and ties are broken randomly.

### Options Table nodes

- **Add Option** adds a row. Each row has a **Text** field, a **Leads to** port, **Remove** and up/down buttons to reorder.
- **+** adds a condition slot to the option and **-** removes it. Slots work exactly as in Condition Table nodes.
- An option is available only if all of its conditions are true. An option with no conditions is always available.
- Reordering keeps each option's connection.

### Events

Each node shows `onEnter` and `onExit` event lists at the bottom. They are standard `UnityEvent`s and are invoked by `OnEnter()` and `OnExit()` at runtime. Events serialized in the dialogue asset can only reference other assets, not scene objects, so use them to trigger asset-level behaviour or have your own code subscribe to dialogue progress.

## Playing a dialogue at runtime

Load the asset, start at `entry`, and move forward with `GetNext()`. Call `GetNext()` only once per step, because a Condition Table picks randomly among equal-priority matches every time it is called.

```csharp
using System.Collections.Generic;
using UnityEngine;

public class DialoguePlayer : MonoBehaviour
{
    TextPart current;

    public void Begin(string dialogueName)
    {
        FullTextAsset asset = Resources.Load<FullTextAsset>($"Dialogues/{dialogueName}/{dialogueName}");
        current = asset.entry;
        current.OnEnter();
        Advance();
    }

    public void Advance()
    {
        while (current != null)
        {
            TextPart next = current.GetNext();
            current.OnExit();
            current = next;

            if (current == null) return;

            current.OnEnter();

            if (current is SimpleText text)
            {
                Debug.Log($"{text.partName}: {text.line}");
                return;
            }

            if (current is OptionsTable table)
            {
                IReadOnlyList<Option> available = table.GetAvailableOptions();
                for (int i = 0; i < available.Count; i++)
                {
                    Debug.Log($"{i}: {available[i].text}");
                }
                return;
            }
        }
    }

    public void Choose(int index)
    {
        if (current is not OptionsTable table) return;

        IReadOnlyList<Option> available = table.GetAvailableOptions();
        if (index < 0 || index >= available.Count) return;

        if (table.Select(available[index])) Advance();
    }
}
```

An Options Table does not pick its own target. It waits at the table until your code calls `Select` with one of the options returned by `GetAvailableOptions()`, and only then is `GetNext()` valid.

## Extending the tool

### Adding a new Condition

Conditions are `ScriptableObject` assets that the Condition Table can check.

1. Create a script whose file name matches the class name:

```csharp
using UnityEngine;

[CreateAssetMenu(menuName = "Conditions/Has Key")]
public class HasKeyCondition : Condition
{
    public override bool True()
    {
        return PlayerInventory.HasKey;
    }
}
```

2. Create the asset with **Assets > Create > Conditions > Has Key**.
3. Put the asset in a folder named `Conditions` that sits inside a `Resources` folder, e.g. `Assets/Resources/Conditions/`.
4. It now appears in the condition dropdowns of Condition Table nodes. Nodes that are already open only pick it up after the dialogue is reopened (use **Edit Existing**).

### Adding a new kind of part

A new part needs a runtime class, a graph node, and a few registrations. The example below adds a `Relay` part that simply forwards to the next part.

1. **Runtime class.** Derive from `TextPart`. Mark it `[Serializable]`, and use `[SerializeReference]` for any field that points to another part. `partName`, `onEnter` and `onExit` come from the base class.

```csharp
using System;
using UnityEngine;

[Serializable]
public class RelayPart : TextPart
{
    [SerializeReference]
    public TextPart next;

    public override TextPart GetNext()
    {
        return next;
    }
}
```

2. **Node class** in the `Editor` folder. Derive from `DialogueNode`. The constructor arguments are the default title, the title colour, whether the node has an input port, and the node width. The base class already adds the name field and the event fields, so `Build()` only has to create your own ports and controls.

```csharp
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class RelayNode : DialogueNode
{
    Port output;

    public RelayNode(DialogueGraphView graph)
        : base(graph, "Relay", new Color(0.4f, 0.2f, 0.5f), true, 300)
    {
    }

    public override IEnumerable<Port> OutputPorts
    {
        get { yield return output; }
    }

    public override TextPart GetLink(Port port)
    {
        return ((RelayPart)Part).next;
    }

    public override void SetLink(Port port, TextPart target)
    {
        ((RelayPart)Part).next = target;
    }

    protected override void Build()
    {
        output = CreateOutputPort("Next", 0);
        outputContainer.Add(output);
    }
}
```

   Use `CreateOutputPort(name, slot)` for each output. The `slot` value is stored in `port.userData` and is how you tell your ports apart in `GetLink` / `SetLink`. See `ConditionTableNode` for a node with a variable number of ports. Call `graph.MarkDirty()` whenever a control changes serialized data.

3. **Register the node** in `DialogueGraphView`:
   - Add a case to `CreateNode`: `case RelayPart: return new RelayNode(this);`
   - Add a menu entry in `BuildContextualMenu`: `evt.menu.AppendAction("Relay", _ => AddPart(new RelayPart(), position));`

4. **Register the links** in `DialogueAssetUtility`. Add a case for your part to both `GetChildren` (yield every part it leads to) and `ClearLinksTo` (null out any field pointing at the given target). Without these, orphaned parts are not recovered when a dialogue is opened and deleting a node leaves dangling references.

Parts of a type the graph does not know about are shown as a red **Unsupported** node so that data is never silently lost.

## Project layout

```
TextPart.cs               Base class for all parts (name, position, events)
EntryPart.cs              Start of a dialogue
SimpleText.cs             A line of text
FullTextAsset.cs          The dialogue ScriptableObject
ConditionPart/            Condition, ConditionEntry and ConditionTable
OptionsPart/              Option and OptionsTable
Editor/
  DialogueEditorWindow.cs Toolbar, create / open / save
  DialogueGraphView.cs    The graph, context menu and link bookkeeping
  DialogueNode.cs         Base node: name field, events, ports
  EntryNode.cs            Node for EntryPart
  SimpleTextNode.cs       Node for SimpleText
  ConditionTableNode.cs   Node for ConditionTable
  OptionsTableNode.cs     Node for OptionsTable
  UnsupportedNode.cs      Placeholder for unknown part types
  DialogueAssetUtility.cs Link traversal and asset repair helpers
```

## Troubleshooting

- **The menu item does not appear.** Check the console for compile errors and confirm the `Editor` folder is inside `Assets`.
- **A condition is missing from the dropdown.** The asset must live under a folder named `Conditions` inside a `Resources` folder, and its class must derive from `Condition`.
- **A part shows as Unsupported.** Its class is not handled in `DialogueGraphView.CreateNode`, or its script was renamed or deleted.
