using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ConditionEntry
{
    public List<Condition> conditions = new();
    public int priority;

    [SerializeReference]
    public TextPart leadsTo;

    public bool True()
    {
        foreach (Condition condition in conditions)
        {
            if (condition == null)
            {
                Debug.LogWarning("Condition entry contains an unassigned condition and is treated as false");
                return false;
            }

            if (condition.True())
            {
                continue;
            }
            else
            {
                return false;
            }
        }
        return true;
    }
}
