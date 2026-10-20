using System.IO;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

public class DialogueEditorWindow : EditorWindow
{
    const string Root = "Assets/Resources/Dialogues";

    [SerializeField]
    string currentPath;

    DialogueGraphView graphView;
    VisualElement graphHost;
    TextField nameField;
    Label currentLabel;

    [MenuItem("Tools/Dialogue Editor")]
    static void Open()
    {
        GetWindow<DialogueEditorWindow>("Dialogue Editor");
    }

    void CreateGUI()
    {
        Toolbar toolbar = new();

        ToolbarButton editButton = new() { text = "Edit Existing" };
        editButton.clicked += () => ShowExistingMenu(editButton.worldBound);
        toolbar.Add(editButton);

        toolbar.Add(new ToolbarSpacer());

        nameField = new TextField();
        nameField.style.width = 160;
        toolbar.Add(nameField);
        toolbar.Add(new ToolbarButton(CreateNew) { text = "Create New" });

        toolbar.Add(new ToolbarSpacer());
        toolbar.Add(new ToolbarButton(Save) { text = "Save" });

        currentLabel = new Label();
        currentLabel.style.marginLeft = 12;
        currentLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
        toolbar.Add(currentLabel);

        rootVisualElement.Add(toolbar);

        graphHost = new VisualElement();
        graphHost.style.flexGrow = 1;
        rootVisualElement.Add(graphHost);

        if (!string.IsNullOrEmpty(currentPath)) LoadAtPath(currentPath);
    }

    void OnDisable()
    {
        graphView?.Save();
    }

    void Save()
    {
        graphView?.Save();
    }

    void ShowExistingMenu(Rect buttonRect)
    {
        GenericMenu menu = new();
        bool any = false;

        if (AssetDatabase.IsValidFolder(Root))
        {
            foreach (string folder in AssetDatabase.GetSubFolders(Root))
            {
                string[] guids = AssetDatabase.FindAssets("t:FullTextAsset", new[] { folder });
                if (guids.Length == 0) continue;

                string path = AssetDatabase.GUIDToAssetPath(guids[0]);
                menu.AddItem(new GUIContent(Path.GetFileName(folder)), path == currentPath, () => LoadAtPath(path));
                any = true;
            }
        }

        if (!any) menu.AddDisabledItem(new GUIContent("No dialogues found"));

        menu.DropDown(buttonRect);
    }

    void CreateNew()
    {
        string dialogueName = nameField.value.Trim();

        if (dialogueName.Length == 0 || dialogueName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            EditorUtility.DisplayDialog("Invalid name", "Enter a valid dialogue name.", "OK");
            return;
        }

        string folder = $"{Root}/{dialogueName}";
        if (AssetDatabase.IsValidFolder(folder))
        {
            EditorUtility.DisplayDialog("Already exists", $"A dialogue named '{dialogueName}' already exists.", "OK");
            return;
        }

        EnsureFolder("Assets", "Resources");
        EnsureFolder("Assets/Resources", "Dialogues");
        AssetDatabase.CreateFolder(Root, dialogueName);

        FullTextAsset asset = CreateInstance<FullTextAsset>();
        asset.entry = new EntryPart { graphPosition = new Vector2(80, 80) };
        asset.parts.Add(asset.entry);

        string path = $"{folder}/{dialogueName}.asset";
        AssetDatabase.CreateAsset(asset, path);
        AssetDatabase.SaveAssets();

        nameField.value = string.Empty;
        LoadAtPath(path);
    }

    void LoadAtPath(string path)
    {
        FullTextAsset asset = AssetDatabase.LoadAssetAtPath<FullTextAsset>(path);
        if (asset == null)
        {
            currentPath = null;
            return;
        }

        graphView?.Save();
        graphHost.Clear();

        graphView = new DialogueGraphView(asset);
        graphHost.Add(graphView);

        currentPath = path;
        currentLabel.text = asset.name;
    }

    static void EnsureFolder(string parent, string folderName)
    {
        if (!AssetDatabase.IsValidFolder($"{parent}/{folderName}"))
        {
            AssetDatabase.CreateFolder(parent, folderName);
        }
    }
}
