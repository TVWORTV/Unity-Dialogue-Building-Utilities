using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Texts")]
public class FullTextAsset : ScriptableObject
{
    [SerializeReference]
    public EntryPart entry;

#if UNITY_EDITOR
    [SerializeReference, HideInInspector]
    public List<TextPart> parts = new();
#endif
}
