using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class MainMenuHandler : MonoBehaviour {

    void Update() {
        
    }

    public void StartNewGame() {
        SceneManager.LoadScene("DemoLevel");
    }
    public void LoadSavedGames() {
        SceneManager.LoadScene("LoadSaveScene");
    }
    public void ViewSettings() {
        SceneManager.LoadScene("SettingsScene");
    }
    public void ExitMenu() {
        Application.Quit();
        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }
}
