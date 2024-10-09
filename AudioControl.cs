using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioControl : MonoBehaviour{
    public AudioSource musicSource;
    public AudioSource buttonFXSource;

    public AudioClip click;
    public AudioClip themeSong;
    public AudioClip error;
    public AudioClip rotate;
    public AudioClip victory;

    private bool bgmEnabled = false;

    // Start is called before the first frame update
    void Start(){
        musicSource.volume = GameSettings.bgmVolume;
    }

    // Update is called once per frame
    void Update(){
    }

    public void StartMusic(string track){
        //TODO: Add more clips as needed based on number of BGM themes needed 
        // setup, active game, victory? 
        switch (track){
            case "MainTheme":
                if (bgmEnabled)
                    musicSource.Play();
                break;
        }//switch track 

    }//StartMusic 

     public void GameOver(){
        //plays fanfare sound effect that occurs at gameover 
        buttonFXSource.PlayOneShot(victory);
    }

    public void RotationArrowClick(){
        buttonFXSource.PlayOneShot(rotate);
    }

    public void GeneralButtonClick(){
        //called when a button is clicked and the input is accepted
        buttonFXSource.PlayOneShot(click);
    }

    public void ErrorClick(){
        //called when a button is clicked and the input is accepted
        buttonFXSource.PlayOneShot(error);
    }

    public void MuteButtonClick(){
        //called when the mute button is clicked, toggles BGM
        buttonFXSource.PlayOneShot(click);
        if (bgmEnabled)
            musicSource.Pause();
        else
            musicSource.Play();
        bgmEnabled = !bgmEnabled;
    }

}//class
