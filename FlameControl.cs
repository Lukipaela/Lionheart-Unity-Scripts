using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FlameControl : MonoBehaviour
{
    [SerializeField] private ParticleSystem sparkParticleSystem;
    [SerializeField] private ParticleSystem flameParticleSystem;
    [SerializeField] private Light fireLight;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip flameOnSound;
    [SerializeField] private AudioClip flameOffSound;

    private readonly bool enableDebugging = true; //switch to enable/disable console logging for this script


    /********************
     * BUILT-IN METHODS *
     ********************/

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }



    /******************
     * CUSTOM METHODS *
     ******************/

    public IEnumerator ToggleEffects(bool enableEffects)
    {
        float delay = Random.Range(0, 2.5f);
        yield return new WaitForSeconds(delay);
        if (enableEffects)
        {
            sparkParticleSystem.Play();
            flameParticleSystem.Play();
            fireLight.enabled = true;
            audioSource.PlayOneShot(flameOnSound);
        }   //on
        else
        {
            sparkParticleSystem.Stop();
            flameParticleSystem.Stop();
            fireLight.enabled = false;
            audioSource.PlayOneShot(flameOffSound);
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
