using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameControl : MonoBehaviour
{
    [SerializeField] private ParticleSystem sparkParticleSystem;
    [SerializeField] private ParticleSystem flameParticleSystem;
    [SerializeField] private Light fireLight;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private FlameSFXLibrary flameSFXLibrary;

    private readonly bool enableDebugging = false; //switch to enable/disable console logging for this script



    /******************
     * CUSTOM METHODS *
     ******************/

    /// <summary>
    /// Called LightingControl script when transitioning between Day and Night. 
    /// After a random delay, initiates a sound effect and toggles lights according to new time of day.
    /// </summary>
    /// <param name="enableEffects">If true, signals that Night has arrived, and the fire effects should be turned on.
    /// If false, signals that Day has arrived, and fire effects should be disabled.</param>
    public IEnumerator ToggleEffects(bool enableEffects)
    {
        float delay = Random.Range(0, 2.5f);
        yield return new WaitForSeconds(delay);
        if (enableEffects)
        {
            sparkParticleSystem.Play();
            flameParticleSystem.Play();
            fireLight.enabled = true;
            SoundFile igniteSound = flameSFXLibrary.getIgnitionSound();
            audioSource.PlayOneShot(igniteSound.audioClip, igniteSound.volume);
        }   //on
        else
        {
            sparkParticleSystem.Stop();
            flameParticleSystem.Stop();
            fireLight.enabled = false;
            SoundFile extinguishSound = flameSFXLibrary.getExtinguishSound();
            audioSource.PlayOneShot(extinguishSound.audioClip, extinguishSound.volume);
        }   //off
    }//ToggleEffects


    /***************
     * DEBUG STUFF *
     ***************/
    public void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("FireScript - " + this.name + ": " + message);
        }
    }//console print

}
