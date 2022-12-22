using UnityEngine;
using UnityEditor;
using System.IO;
using Newtonsoft.Json;

public class NewLevelWindow : ScriptableWizard {

    [Range(2, 128)] public int col = 10;
    [Range(2, 128)] public int row = 10;

    private string levelName = "";

    public static void CreateNewLevel() {
        DisplayWizard<NewLevelWindow>("创建新地图", "创建");
    }

    private void OnWizardCreate() {
        string dir = Application.persistentDataPath + Level.DIRECTORY;
        if (!Directory.Exists(dir)) {
            Directory.CreateDirectory(dir);
        }
        SaveItem saveItem = SaveItem.GetDefaultSaveItem(col, row);
        string json = JsonConvert.SerializeObject(saveItem);
        File.WriteAllText(dir + levelName + ".txt", json);
    }

    protected override bool DrawWizardGUI() {
        bool modified = base.DrawWizardGUI();
        using (new EditorGUILayout.HorizontalScope()) {
            GUILayout.Label("地图名：");
            levelName = GUILayout.TextField(levelName);
            GUILayout.Label(".txt");
            isValid = CanCreate();
        }
        return modified;
    }

    private bool CanCreate() {
        if (string.IsNullOrEmpty(levelName)) {
            return false;
        }
        if (File.Exists(Application.persistentDataPath + Level.DIRECTORY + levelName + ".txt")) {
            ShowNotification(new GUIContent(string.Format("已存在名为 {0} 的地图！", levelName + ".txt")));
            return false;
        }
        return true;
    }
}
