using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class OptionsTable : TextPart
{
    public List<Option> options = new();

    [NonSerialized]
    Option selected;

    [NonSerialized]
    List<Option> availableOptions;

    public override void OnEnter()
    {
        selected = null;
        base.OnEnter();
    }

    public IReadOnlyList<Option> GetAvailableOptions()
    {
        availableOptions ??= new();
        availableOptions.Clear();

        foreach (Option option in options)
        {
            if (option != null && option.available()) availableOptions.Add(option);
        }

        return availableOptions;
    }

    public bool Select(Option option)
    {
        if (option == null || !options.Contains(option))
        {
            Debug.LogError("Selected option does not belong to this options table");
            return false;
        }

        if (!option.available())
        {
            Debug.LogError("Selected option is not available");
            return false;
        }

        selected = option;
        return true;
    }

    public override TextPart GetNext()
    {
        if (selected == null)
        {
            Debug.LogError("Options table has no selected option");
            return null;
        }

        return selected.leadsTo;
    }
}
