using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingControl : MonoBehaviour
{
    public Light sun;

    [SerializeField] private AudioSource sunAudioSource;
    [SerializeField] private AudioClip morningStart;
    [SerializeField] private AudioClip nightStart;
    [SerializeField] private AudioClip morningAmbient;
    [SerializeField] private AudioClip nightAmbient;
    [SerializeField] private float sunRotationSpeed = 4;
    [SerializeField] private float sunAngle = 0;
    [SerializeField] private string dayPhase = "Day"; // Day or Night

    private readonly bool enableDebugging = true; //switch to enable/disable console logging for this script
    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start(){

    }


    void Update(){
        float rotationAmount = sunRotationSpeed * Time.deltaTime;
        sunAngle = (sunAngle + rotationAmount) % 360;
        sun.transform.Rotate(new Vector3(rotationAmount, 0, 0));
        //Set Day Phase
        if (dayPhase == "Day" && sunAngle > 175 && sunAngle < 185)
            ChangeDayPhase("Night");
        else if(dayPhase == "Night" && (sunAngle > 355 || sunAngle < 5))
            ChangeDayPhase("Day");
    }   //Update


    private void ChangeDayPhase( string phase ){
        ConsolePrint("Changing day phase to " + phase);
        dayPhase = phase;
        switch (phase) {
            case "Day":
                PlaySound(morningStart, false);
                break;
            case "Night":
                PlaySound(nightStart, false);
                break;
        }//switch
    }//ChangeDayPhase


    /// <summary>
    /// Stops current sound (if any), and plays the requested track. Optionally can be made to loop.
    /// </summary>
    /// <param name="soundToPlay">The Audio Clip to be played.</param>
    /// <param name="loopTrack">Indicates if the sound should loop indefinitely (until explicitly cancelled by some other process).</param>
    private void PlaySound(AudioClip soundToPlay, bool loopTrack) {
        sunAudioSource.Stop();
        sunAudioSource.loop = loopTrack;
        sunAudioSource.clip = soundToPlay;
        sunAudioSource.Play();
    }



    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message)    {
        if (enableDebugging == true)        {
            Debug.Log("LightingControl - " + message);
        }
    }//console print

}//class
