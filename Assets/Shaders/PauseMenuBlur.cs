using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseMenuBlur : MonoBehaviour {
    void Update() {
        if (Time.timeScale == 1) {          //Game in Progress

        } else if (Time.timeScale == 0) {   //Game Paused
            //Blur background



            //display pause menu options/buttons/logos

        }
    }
}
