using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class SettingsHandler : MonoBehaviour {

    void Awake() {
        
    }

    void Update() {
        
    }

    public void SaveAndGoBack() {
        //save settings in userprof

        SceneManager.LoadScene("MainMenu");
    }
    public void GoBackWithoutSaving() {
        SceneManager.LoadScene("MainMenu");
    }
    public void ExitMenu() {
        Application.Quit();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
