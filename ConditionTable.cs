using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ConditionTable : TextPart
{
    public List<ConditionEntry> entries = new();

    [NonSerialized]
    List<ConditionEntry> temp;

    [NonSerialized]
    List<ConditionEntry> priorityFilter;

    public override TextPart GetNext()
    {
        ConditionEntry entry = GetNextCondEntry();
        return entry?.leadsTo;
    }

    ConditionEntry GetNextCondEntry()
    {
        temp ??= new();
        priorityFilter ??= new();

        temp.Clear();
        foreach (ConditionEntry entry in entries)
        {
            if (entry.True()) temp.Add(entry);
        }
        if (temp.Count == 0)
        {
            Debug.LogError("Condition table has no entry whose conditions are all true");
            return null;
        }
        else if (temp.Count == 1)
        {
            return temp[0];
        }
        else
        {
            priorityFilter.Clear();

            int biggestPriority = int.MinValue;

            foreach (ConditionEntry entry in temp)
            {
                if (entry.priority > biggestPriority)
                {
                    biggestPriority = entry.priority;
                    priorityFilter.Clear();
                    priorityFilter.Add(entry);
                }
                else if (entry.priority == biggestPriority)
                {
                    priorityFilter.Add(entry);
                }
            }

            return priorityFilter[
                UnityEngine.Random.Range(0, priorityFilter.Count)
            ];
        }
    }
}
