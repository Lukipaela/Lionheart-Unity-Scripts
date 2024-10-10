using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationArrowControl : MonoBehaviour{
    public GameObject leftArrow;
    public GameObject rightArrow;
    public GameObject mainCamera;
    public GameObject hiddenLocation;   //location where the camera should go when hidden
    public Material highlightedMaterial;
    public Material standardMaterial;

    public GameObject associatedSquad; //the squad which is being rotated by these arrows

    private readonly bool enableDebugging = false;


    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start(){
        HideArrows();
    }

    void Update(){       
    }//update


    /******************
     * CUSTOM METHODS *
     ******************/

    public void AssignToSquad( GameObject squad){
        ConsolePrint("Assigning rotation arrows.");
        associatedSquad = squad;
        transform.position = associatedSquad.transform.position;
        transform.forward = associatedSquad.transform.forward;
    }//assign to squad

    public void HideArrows(){
        ConsolePrint("Hiding rotation arrows.");
        associatedSquad = null;
        //reset rotation of the object, and hide it in a tent
        transform.position = hiddenLocation.transform.position;
        transform.forward = Vector3.forward;
        transform.up = Vector3.up;
    }//HideArrows

    public void SetHighlighting (bool enabled, GameObject arrow ){
        if (enabled)
            arrow.GetComponent<MeshRenderer>().material = highlightedMaterial;
        else
            arrow.GetComponent<MeshRenderer>().material = standardMaterial;
        
    }//set highlighting 

    public string GetDirection( GameObject arrow){
        if (arrow == leftArrow)
            return "Left";

        else if (arrow == rightArrow)
            return "Right";
        else
            return "Error";

    }//GetDirection


    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message){
        if (enableDebugging == true){
            Debug.Log("Rotation Arrow Control Script - " + message);
        }
    }//console

}//class
