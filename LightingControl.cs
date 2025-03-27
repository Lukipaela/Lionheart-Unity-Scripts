using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingControl : MonoBehaviour
{
    public Light sun;

    [SerializeField] private AudioSource sunAudioSource;
    [SerializeField] private SoundFile morningStart;
    [SerializeField] private SoundFile nightStart;
    [SerializeField] private SoundFile morningAmbient;
    [SerializeField] private SoundFile nightAmbient;
    [SerializeField] private float sunRotationSpeed = 4;    //serialized for fine-tuning
    [SerializeField] private float sunAngle = 0;    //serialized for debugging
    [SerializeField] private DayPhase dayPhase = DayPhase.Day;
    [SerializeField] private FlameControl[] flameSources;
    private readonly int dawnAngle = 350;
    private readonly int duskAngle = 125;
    //debug
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
        if (GameSettings.timeCycleMode == TimeCycleMode.Cycle)
        {
            float rotationAmount = sunRotationSpeed * Time.deltaTime;
            sunAngle = (sunAngle + rotationAmount) % 360;
            sun.transform.Rotate(new Vector3(rotationAmount, 0, 0));
            //Set Day Phase
            if (dayPhase == DayPhase.Day && sunAngle > duskAngle && sunAngle < duskAngle + 20)
                ChangeDayPhase(DayPhase.Night);
            else if (dayPhase == DayPhase.Night && (sunAngle > dawnAngle || sunAngle < (dawnAngle + 20) % 360))
                ChangeDayPhase(DayPhase.Day);
        }
    }   //Update

    public void SetTimeCycleMode(TimeCycleMode newTimeCycleMode)
    {
        float rotationAmount = 1;
        switch (newTimeCycleMode)
        {
            case TimeCycleMode.Day:
                while (sunAngle < dawnAngle && sunAngle > (dawnAngle + 15) % 360)
                {
                    sunAngle = (sunAngle + rotationAmount) % 360;
                    sun.transform.Rotate(new Vector3(rotationAmount, 0, 0));
                }
                ChangeDayPhase(DayPhase.Day);
                break;
            case TimeCycleMode.Night:
                while (sunAngle < duskAngle || sunAngle > duskAngle + 15)
                {
                    sunAngle = (sunAngle + rotationAmount) % 360;
                    sun.transform.Rotate(new Vector3(rotationAmount, 0, 0));
                }
                ChangeDayPhase(DayPhase.Night);
                break;
            default:
                // no changes needed for Pause or Cycle
                break;
        }
    }

    private void ChangeDayPhase(DayPhase phase)
    {
        ConsolePrint("Changing day phase to " + phase);
        dayPhase = phase;
        switch (phase)
        {
            case DayPhase.Day:
                ToggleFires(false);
                PlaySound(morningAmbient, true);
                PlaySound(morningStart, false);
                break;
            case DayPhase.Night:
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
    private void PlaySound(SoundFile soundToPlay, bool loopTrack)
    {
        if (!loopTrack)
            sunAudioSource.PlayOneShot(soundToPlay.audioClip, soundToPlay.volume);
        else
        {
            sunAudioSource.Stop();
            sunAudioSource.loop = loopTrack;
            sunAudioSource.clip = soundToPlay.audioClip;
            sunAudioSource.volume = soundToPlay.volume;
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
