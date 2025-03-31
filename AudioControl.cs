using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioControl : MonoBehaviour
{
    public AudioSource musicSource;
    public AudioSource buttonFXSource;

    public AudioClip click;
    public AudioClip themeSong;
    public AudioClip error;
    public AudioClip rotate;
    public AudioClip victory;

    private bool bgmEnabled = false;


    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start()
    {
        musicSource.volume = GameSettings.bgmVolume;
    }


    /******************
     * CUSTOM METHODS *
     ******************/

    public void StartMusic(string track)
    {
        // setup, active game, victory? 
        switch (track)
        {
            case "MainTheme":
                if (bgmEnabled)
                    musicSource.Play();
                break;
        }//switch track 

    }//StartMusic 

    public void GameOver()
    {
        //plays fanfare sound effect that occurs at gameover 
        buttonFXSource.PlayOneShot(victory);
    }

    public void RotationArrowClick()
    {
        buttonFXSource.PlayOneShot(rotate);
    }

    public void GeneralButtonClick()
    {
        //called when a button is clicked and the input is accepted
        buttonFXSource.PlayOneShot(click);
    }

    public void ErrorClick()
    {
        //called when a button is clicked and the input is accepted
        buttonFXSource.PlayOneShot(error);
    }

    public bool MuteButtonClick()
    {
        //called when the mute button is clicked, toggles BGM
        buttonFXSource.PlayOneShot(click);
        if (bgmEnabled)
            musicSource.Pause();
        else
            musicSource.Play();
        bgmEnabled = !bgmEnabled;
        return bgmEnabled;
    }

}//class
