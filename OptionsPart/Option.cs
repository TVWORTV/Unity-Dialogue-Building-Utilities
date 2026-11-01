using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class Option
{
    public string text;
    public List<Condition> conditions = new();

    [SerializeReference]
    public TextPart leadsTo;

    public bool available()
    {
        foreach (Condition condition in conditions)
        {
            if (condition == null)
            {
                Debug.LogWarning("Option entry contains an unassigned condition and is treated as false");
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
