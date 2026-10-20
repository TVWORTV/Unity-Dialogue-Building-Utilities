using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public static class DialogueAssetUtility
{
    public static IEnumerable<TextPart> GetChildren(TextPart part)
    {
        switch (part)
        {
            case EntryPart entryPart:
                if (entryPart.next != null) yield return entryPart.next;
                break;
            case SimpleText simpleText:
                if (simpleText.next != null) yield return simpleText.next;
                break;
            case ConditionTable table:
                foreach (ConditionEntry conditionEntry in table.entries)
                {
                    if (conditionEntry.leadsTo != null) yield return conditionEntry.leadsTo;
                }
                break;
        }
    }

    public static void ClearLinksTo(TextPart part, TextPart target)
    {
        switch (part)
        {
            case EntryPart entryPart:
                if (entryPart.next == target) entryPart.next = null;
                break;
            case SimpleText simpleText:
                if (simpleText.next == target) simpleText.next = null;
                break;
            case ConditionTable table:
                foreach (ConditionEntry conditionEntry in table.entries)
                {
                    if (conditionEntry.leadsTo == target) conditionEntry.leadsTo = null;
                }
                break;
        }
    }

    public static void Sanitize(FullTextAsset asset)
    {
        bool changed = false;

        asset.parts ??= new List<TextPart>();

        if (asset.parts.RemoveAll(part => part == null) > 0) changed = true;

        if (asset.entry == null)
        {
            asset.entry = new EntryPart();
            changed = true;
        }

        if (!asset.parts.Contains(asset.entry))
        {
            asset.parts.Insert(0, asset.entry);
            changed = true;
        }

        HashSet<TextPart> visited = new() { asset.entry };
        Queue<(TextPart part, int depth)> queue = new();
        Dictionary<int, int> rows = new();
        queue.Enqueue((asset.entry, 0));

        while (queue.Count > 0)
        {
            (TextPart current, int depth) = queue.Dequeue();

            foreach (TextPart child in GetChildren(current))
            {
                if (!visited.Add(child)) continue;

                if (!asset.parts.Contains(child))
                {
                    rows.TryGetValue(depth + 1, out int row);
                    rows[depth + 1] = row + 1;
                    child.graphPosition = new Vector2((depth + 1) * 420f, row * 320f);
                    asset.parts.Add(child);
                    changed = true;
                }

                queue.Enqueue((child, depth + 1));
            }
        }

        if (changed) EditorUtility.SetDirty(asset);
    }
}
