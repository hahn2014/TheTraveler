using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEditor;

public class SettingsHandler : MonoBehaviour {
    [Header("Video Settings")]
    [Tooltip("Settings tab controls.")]
    [SerializeField] private GameObject[] tabs;
    [Tooltip("Switch between available native monitor resolutions.")]
    [SerializeField] private TMP_Dropdown availableResolutions;
    [Tooltip("Toggle between fullscreen and windowed.")]
    [SerializeField] private Toggle fullscreenToggle;
    [Tooltip("Switch between predetermined video qualities.")]
    [SerializeField] private TMP_Dropdown availableQualities;
    [Tooltip("Toggle on or off an minimalistic FPS counter.")]
    [SerializeField] private Toggle fpsCounter;

    private Resolution[] resolutions;
    private string[] qualities;

    void Start() {
        availableResolutions.ClearOptions();
        availableQualities.ClearOptions();
        string t = "";
        resolutions = Screen.resolutions;
        qualities = QualitySettings.names;


        foreach (Resolution res in resolutions) {
            t = res.width.ToString() + 'x' + res.height.ToString();
            availableResolutions.options.Add(new TMP_Dropdown.OptionData() { text = t });
        }
        foreach (string i in qualities) {
            availableQualities.options.Add(new TMP_Dropdown.OptionData() { text = i });
        }
        foreach(GameObject g in tabs) {
            g.GetComponent<Image>().color = new Color(100, 100, 100, 255);
            RectTransform rt = g.GetComponentInParent<RectTransform>(); //get the parent rectangle
            if (rt.GetSiblingIndex() == -1) {    //if  the sibling index is -1, it was the last tab clicked
                g.GetComponent<Image>().color = new Color(140, 140, 140, 255);
            }
        }


        availableResolutions.value = 1;
        availableQualities.value = 1;
        fullscreenToggle.enabled = false;
        fpsCounter.enabled = true;

        //action listeners
        availableQualities.onValueChanged.AddListener(delegate {
            changeGraphicsQuality(availableQualities.value);
        });
        availableResolutions.onValueChanged.AddListener(delegate {
            changeResolutionQualtiy(availableResolutions.value);
        });


        //check for past saved settings
        checkForPlayerPref();
    }

    public void changeGraphicsQuality(int i) {
        QualitySettings.SetQualityLevel(i, true);
    }
    public void changeResolutionQualtiy(int i) {
        string resString = (availableResolutions.options[i].text);
        string[] splitSTR = resString.Split('x');
        Vector2Int parsedString = new Vector2Int(int.Parse(splitSTR[0]), int.Parse(splitSTR[1]));

        Screen.SetResolution(parsedString.x, parsedString.y, Screen.fullScreen);
    }

    public void toggleFullscreen() {
        Screen.fullScreen = !Screen.fullScreen;
    }

    //we want to make sure there is no pre-saved settings
    private void checkForPlayerPref() {
        availableResolutions.value = PlayerPrefs.GetInt("Resolution");
        availableQualities.value = PlayerPrefs.GetInt("Quality");
        fullscreenToggle.isOn = (PlayerPrefs.GetInt("Fullscreen") == 1);
        fpsCounter.isOn = (PlayerPrefs.GetInt("FPSCounter") == 1);
    }

    public void SaveAndGoBack() {
        //save settings in userprof (bool as 0 or 1 int)
        PlayerPrefs.SetInt("Resolution", availableResolutions.value);
        PlayerPrefs.SetInt("Quality", availableQualities.value);
        PlayerPrefs.SetInt("Fullscreen", fullscreenToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("FPSCounter", fpsCounter.isOn ? 1 : 0);

        PlayerPrefs.Save(); //force save all keys
        SceneManager.LoadScene("MainMenu");
    }

    //although it might be annoying, it could come in handy to some
    //  who accidentally press the wrong button when trying to save
    public void GoBackWithoutSaving() {
        if (EditorUtility.DisplayDialog("Return to menu without saving?",
            "Are you sure you want to return to the main menu without saving settings first?",
            "Actually, save them!", "Just go back...")) {
            SaveAndGoBack();
        } else {
            SceneManager.LoadScene("MainMenu");
        }
    }
    public void ExitMenu() {
        Application.Quit();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
