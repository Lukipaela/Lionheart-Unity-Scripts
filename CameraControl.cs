using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    [SerializeField] private GameObject[] diceRollCameraLocations;
    [SerializeField] private GameObject[] activeGameCameraLocations;
    [SerializeField] private GameObject[] playerOneSetupCameraLocations;
    [SerializeField] private GameObject[] playerTwoSetupCameraLocations;
    [SerializeField] private GameObject orthoCamera;
    [SerializeField] private GameObject perspectiveCamera;
    [SerializeField] private GameObject cameraCollection;

    [SerializeField] private GameObject castleFront;
    [SerializeField] private GameControl gameControlScript;
    [SerializeField] private GameObject combatCameraBox;
    public bool animating = false;
    public bool combatCameraActive = false;

    private readonly int diceCamLocationCount = 4;
    private readonly int activeCamLocationCount = 4;
    private readonly int p1SetupCamLocationCount = 3;
    private readonly int p2SetupCamLocationCount = 3;
    private string gamePhase;
    private int currentLocationIndex;
    private readonly float cameraMoveSpeed = 2;
    private readonly float cameraRotationSpeed = 2;
    private GameObject targetCamLocation;

    private static readonly bool enableDebugging = false; //switch to enable/disable console logging for this script


    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start()
    {
        currentLocationIndex = 0;
        targetCamLocation = diceRollCameraLocations[currentLocationIndex];
    }

    void Update()
    {
        if (animating)
            MoveCamera();
    }//update


    /******************
     * CUSTOM METHODS *
     ******************/

    /// <summary>
    /// Enables the requested camera type, disables all others
    /// </summary>
    /// <param name="cameraType">CameraType which should be activated.</param>
    private void SetActiveCamera(CameraType cameraType)
    {
        if (cameraType == CameraType.Orthographic)
        {
            orthoCamera.SetActive(true);
            perspectiveCamera.SetActive(false);
        }
        else if (cameraType == CameraType.Perspective)
        {
            perspectiveCamera.SetActive(true);
            orthoCamera.SetActive(false);
        }
    }

    private void MoveCamera()
    {
        if (!combatCameraActive)
        {
            ConsolePrint("Non Combat Camera Update");
            //standard camera rules of motion
            if (Vector3.Angle(cameraCollection.transform.forward, targetCamLocation.transform.forward) > 0.1f || Vector3.Distance(cameraCollection.transform.position, targetCamLocation.transform.position) > 0.5)
            {
                cameraCollection.transform.rotation = Quaternion.Lerp(cameraCollection.transform.rotation, targetCamLocation.transform.rotation, Time.deltaTime * cameraRotationSpeed);
                cameraCollection.transform.position = Vector3.Lerp(cameraCollection.transform.position, targetCamLocation.transform.position, cameraMoveSpeed * Time.deltaTime);
            }
            else
                animating = false;
        }
    }//move camera

    public void MoveToNextLocation()
    {
        ConsolePrint("Cam location toggle command received, cycling to next " + gamePhase + " location");
        switch (gamePhase)
        {
            case "PlayerOneSetup":
                currentLocationIndex = (currentLocationIndex + 1) % p1SetupCamLocationCount;
                targetCamLocation = playerOneSetupCameraLocations[currentLocationIndex];
                break;
            case "PlayerTwoSetup":
                currentLocationIndex = (currentLocationIndex + 1) % p2SetupCamLocationCount;
                targetCamLocation = playerTwoSetupCameraLocations[currentLocationIndex];
                break;
            case "DiceRoll":
                currentLocationIndex = (currentLocationIndex + 1) % diceCamLocationCount;
                targetCamLocation = diceRollCameraLocations[currentLocationIndex];
                break;
            case "ActiveGame":
                currentLocationIndex = (currentLocationIndex + 1) % activeCamLocationCount;
                targetCamLocation = activeGameCameraLocations[currentLocationIndex];
                break;
        }//switch

        animating = true;
    }//move to next location

    public void SetCameraGamePhase(string phase)
    {
        ConsolePrint("Cam game phase changed to " + phase);
        if (phase == "DiceRoll")
            castleFront.SetActive(false);
        else
            castleFront.SetActive(true);

        gamePhase = phase;
        currentLocationIndex = -1;  //start at -1 because the  next method will auto-increment it immediately, resulting in the desired 0
        MoveToNextLocation();
    }   //set camera game phase

    public void EnableCombatCamera(GameObject attacker, GameObject defender)
    {
        ConsolePrint("Enabling combat camera with target " + defender.name);
        //this method switches the camera to perspective mode and moves it to the location of the active combatant
        combatCameraActive = true;
        animating = true;
        SetActiveCamera(CameraType.Perspective);

        //place the combat box based on the input location data 
        combatCameraBox.transform.position = attacker.transform.position;
        combatCameraBox.transform.rotation = attacker.transform.rotation;
        //directly relocate the camera to the combat box
        cameraCollection.transform.position = combatCameraBox.transform.GetChild(0).transform.position;
        //cameraCollection.transform.rotation = combatCameraBox.transform.GetChild(0).transform.rotation;

        //Quaternion angleToTarget = Quaternion.LookRotation((defender.transform.position + (attacker.transform.forward * 0.1f) + (attacker.transform.up * 0.6f)) - cameraCollection.transform.position);
        Quaternion angleToTarget = Quaternion.LookRotation(((
            (defender.transform.position + attacker.transform.position) * 0.5f) //the point halfway between attacker and defender
            + (attacker.transform.up * 0.5f)) //offset vertically, since unit positions are at ground level
            - (attacker.transform.forward * 0.4f) //since the camera is already above the attacker, shift the target from the middle between the two squads toward the attacker a little
            - cameraCollection.transform.position);//subtract from camera's current location to form a vector from the camera to the above calculated location
        cameraCollection.transform.rotation = angleToTarget;

    }//enable combat camera 

    public void DisableCombatCamera()
    {
        //this method switches the camera back to ortho mode and moves it to the active game camera location 
        combatCameraActive = false;
        animating = false;
        SetActiveCamera(CameraType.Orthographic);

        //return camera position to active game camera position
        cameraCollection.transform.position = activeGameCameraLocations[0].transform.position;
        cameraCollection.transform.rotation = activeGameCameraLocations[0].transform.rotation;

        //safety - reset the game phase flag in case it enters combat mode without that update having been done
        gamePhase = "ActiveGame";

    }//disable combat camera 

    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("Camera Control - " + message);
        }
    }//console print

    public void TogglePerspective()
    {
        bool isOrtho = orthoCamera.activeSelf;
        if (isOrtho)
            SetActiveCamera(CameraType.Perspective);
        else
            SetActiveCamera(CameraType.Orthographic);
    }

}//class
