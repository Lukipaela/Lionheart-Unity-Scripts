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
    [SerializeField] private FlameControl[] flameSources;

    private readonly bool enableDebugging = false; //switch to enable/disable console logging for this script

    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start()
    {
        PlaySound(morningAmbient, true);
    }


    void Update()
    {
        float rotationAmount = sunRotationSpeed * Time.deltaTime;
        sunAngle = (sunAngle + rotationAmount) % 360;
        sun.transform.Rotate(new Vector3(rotationAmount, 0, 0));
        //Set Day Phase
        if (dayPhase == "Day" && sunAngle > 125 && sunAngle < 135)
            ChangeDayPhase("Night");
        else if (dayPhase == "Night" && (sunAngle > 350 || sunAngle < 0))
            ChangeDayPhase("Day");
    }   //Update


    private void ChangeDayPhase(string phase)
    {
        ConsolePrint("Changing day phase to " + phase);
        dayPhase = phase;
        switch (phase)
        {
            case "Day":
                ToggleFires(false);
                PlaySound(morningAmbient, true);
                PlaySound(morningStart, false);
                break;
            case "Night":
                ToggleFires(true);
                PlaySound(nightAmbient, true);
                PlaySound(nightStart, false);
                break;
        }//switch
    }//ChangeDayPhase

    /// <summary>
    /// Stops current sound (if any), and plays the requested track. Optionally can be made to loop.
    /// </summary>
    /// <param name="soundToPlay">The Audio Clip to be played.</param>
    /// <param name="loopTrack">Indicates if the sound should loop indefinitely (until explicitly cancelled by some other process).</param>
    private void PlaySound(AudioClip soundToPlay, bool loopTrack)
    {
        if (!loopTrack)
            sunAudioSource.PlayOneShot(soundToPlay);
        else
        {
            sunAudioSource.Stop();
            sunAudioSource.loop = loopTrack;
            sunAudioSource.clip = soundToPlay;
            sunAudioSource.Play();
        }
    }

    /// <summary>
    /// Enables/Disables campfire and torch point source lighting and particle effects, and plays a corresponding sound effect.
    /// Each fire source will be toggled with a different random timing.
    /// </summary>
    private void ToggleFires(bool enableEffects)
    {
        foreach (FlameControl thisFlameControl in flameSources)
        {
            StartCoroutine(thisFlameControl.ToggleEffects(enableEffects));
        }
    }

    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("LightingControl - " + message);
        }
    }//console print

}//class
