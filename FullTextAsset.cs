using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Texts")]
public class FullTextAsset : ScriptableObject
{
    [SerializeReference]
    public EntryPart entry;

    [SerializeReference, HideInInspector]
    public List<TextPart> parts = new();
}
