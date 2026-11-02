using UnityEngine;

[CreateAssetMenu(menuName = "Dialogue/Speaker")]
public class Speaker : ScriptableObject
{
    public string displayName;
    public Sprite portrait;
    public Color nameColor = Color.white;

    public string DisplayName => string.IsNullOrEmpty(displayName) ? name : displayName;
}