using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LightingControl : MonoBehaviour
{
    public Light sun;
    public float sunRotationSpeed = 4;
    //public float sunAngleX = 0;
    public float sunAngle = 0;
    public string dayPhase = "Day"; // Day or Night

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
        //sunAngleX = sun.transform.rotation.eulerAngles.x;
        if (dayPhase == "Day" && sunAngle > 175 && sunAngle < 185)
            ChangeDayPhase("Night");
        else if(dayPhase == "Night" && (sunAngle > 355 || sunAngle < 5))
            ChangeDayPhase("Day");

    }   //Update


    private void ChangeDayPhase( string phase ){
        //update phase tracker
        dayPhase = phase;
        //trigger SFX / ambient track 
        ConsolePrint("Changing day phase to " + phase);
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
