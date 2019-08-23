using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using System.Collections;

public class LoadWorldHandler : MonoBehaviour {
    [Header("Load World Settings")]
    [Tooltip("Switch between available native monitor resolutions.")]
    [SerializeField] private RectTransform scrollViewPort;
    [Tooltip("Savefile count size of how many game saves were found.")]
    [SerializeField] private int savecount = 0;
    [Tooltip("Scrollbar Reference.")]
    [SerializeField] private GameObject scrollbar;

    [Header("Visual Adjustments")]
    [Tooltip("Adjust Saveblock x start")]
    [SerializeField] private int xStart = 150;
    [Tooltip("Adjust Saveblock y start")]
    [SerializeField] private int yStart = 168;
    [Tooltip("Adjust Saveblock Width")]
    [SerializeField] private int boxWidth = 1248;
    [Tooltip("Adjust Saveblock Height")]
    [SerializeField] private int boxHeight = 100;
    [Tooltip("Adjust Scrollview width")]
    [SerializeField] private int scrollWidth = 1278;
    [Tooltip("Adjust Scrollview height")]
    [SerializeField] private int scrollHeight = 600;


    private SaveFile[] saveFiles;
    private GUIStyle fontStyle;
    private GUIStyle boxStyleEven;
    private GUIStyle boxStyleOdd;
    private Vector2 scrollPosition = Vector2.zero;

    void Start() {
        saveFiles = new SaveFile[10];

        //check for previously available saves
        savecount = checkForGameSaves();
    }

    //we want to make sure there is no pre-saved gamefiles
    private int checkForGameSaves() {
        DirectoryInfo di = new DirectoryInfo(Application.dataPath + "/saves/");
        var i = 0;
        if (di.Exists) {
            foreach (var fi in di.GetFiles("*.save")) {
                saveFiles[i] = new SaveFile(fi.Name, fi.FullName, fi.Length);
                i++;
            }
            return i;
        } else {
            di.Create();
            return 0; //return 0 savefiles found
        }
    }

    private void InitStyles() {
        boxStyleEven = new GUIStyle(GUI.skin.box);
        boxStyleEven.normal.background = MakeTex(boxWidth, boxHeight, ConvertColor(140, 140, 140, 255));
        boxStyleEven.onHover.background = MakeTex(boxWidth, boxHeight, ConvertColor(120, 120, 120, 205));
        boxStyleEven.fontSize = 30;
        boxStyleEven.alignment = TextAnchor.UpperLeft;
        boxStyleEven.padding = new RectOffset(10, 0, 10, 0);

        boxStyleOdd = new GUIStyle(GUI.skin.box);
        boxStyleOdd.normal.background = MakeTex(boxWidth, boxHeight, ConvertColor(100, 100, 100, 255));
        boxStyleOdd.onHover.background = MakeTex(boxWidth, boxHeight, ConvertColor(120, 120, 120, 205));
        boxStyleOdd.fontSize = 30;
        boxStyleOdd.alignment = TextAnchor.UpperLeft;
        boxStyleOdd.padding = new RectOffset(10, 0, 10, 0);

        fontStyle = new GUIStyle();
        fontStyle.fontSize = 30;
    }

    void OnGUI() {
        scrollPosition = GUI.BeginScrollView(new Rect(xStart, yStart, scrollWidth, scrollHeight),
            scrollPosition, new Rect(xStart, yStart, scrollWidth - 40, scrollHeight), false, true,
            new GUIStyle(), new GUIStyle());
        InitStyles();

        if (savecount >= 1) {
            for (int i = 0; i < savecount; i++) {
                GUI.Box(new Rect(xStart, yStart + (boxHeight * i), boxWidth, boxHeight),
                    saveFiles[i].getName(), (i % 2 == 0) ? boxStyleEven : boxStyleOdd);
                //GUI.Label(new Rect(120, 200 + (80 * i), 100, 80), saveFiles[i].getName(), fontStyle);
            }
        } else {
            GUI.Label(new Rect((Screen.width / 2) - 100, (Screen.height / 2) - 10, 200, 20), "No Save Files Were Found.");
        }
        GUI.EndScrollView();
    }

    public void GoBack() {
        SceneManager.LoadScene("MainMenu");
    }

    public void ExitMenu() {
        Application.Quit();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    private Color ConvertColor(int r, int g, int b, int a) {
        return new Color(r / 255.0f, g / 255.0f, b / 255.0f, a / 255.0f);
    }

    private Texture2D MakeTex(int width, int height, Color col) {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; ++i) {
            pix[i] = col;
        }
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}

public class SaveFile : MonoBehaviour {
    private string saveName;
    private string location;
    private long filesize;

    public SaveFile(string name, string loc, long bytes) {
        saveName = name;
        location = loc;
        filesize = bytes;
    }

    public void addFile(string name, string loc, long bytes) {
        saveName = name;
        location = loc;
        filesize = bytes;
    }

    public string getName() {
        return saveName;
    }

    public string getLocation() {
        return location;
    }

    public long getSize() {
        return filesize;
    }
}
