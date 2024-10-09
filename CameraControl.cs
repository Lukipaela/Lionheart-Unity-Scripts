using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraControl : MonoBehaviour
{
    public GameObject[] diceRollCameraLocations;
    public GameObject[] activeGameCameraLocations;
    public GameObject[] playerOneSetupCameraLocations;
    public GameObject[] playerTwoSetupCameraLocations;
    public GameObject mainCamera;
    public GameObject castleFront;
    public GameControl gameControlScript;
    public GameObject combatCameraBox;

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
    private bool animating = false;
    private GameObject cameraTrackedObject = null;
    private float cameraZoom_combat = 1;
    private float cameraZoom_default = 3.5f;

    private static readonly bool enableDebugging = false; //switch to enable/disable console logging for this script


    /********************
     * BUILT-IN METHODS *
     ********************/
     
    void Start()
    {
        gamePhase = "DiceRoll";
        currentLocationIndex = 0;
        targetCamLocation = diceRollCameraLocations[currentLocationIndex];
    }
    
    void Update()
    {
        if(animating)
            MoveCamera();
    }//update


    /******************
     * CUSTOM METHODS *
     ******************/

    private void MoveCamera()
    {
        if (!combatCameraActive)
        {
            ConsolePrint("Non Combat Camera Update");
            //standard camera rules of motion
            if (Vector3.Angle(mainCamera.transform.forward, targetCamLocation.transform.forward) > 0.5f || Vector3.Distance(mainCamera.transform.position, targetCamLocation.transform.position) > 0.5)
            {
                mainCamera.transform.rotation = Quaternion.Lerp(mainCamera.transform.rotation, targetCamLocation.transform.rotation, Time.deltaTime * cameraRotationSpeed);
                mainCamera.transform.position = Vector3.Lerp(mainCamera.transform.position, targetCamLocation.transform.position, cameraMoveSpeed * Time.deltaTime);
            }
            else
            {
                animating = false;
                gameControlScript.ReportCameraAnimationComplete();
            }
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

    public void SetCameraGamePhase( string phase )
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

    public void EnableCombatCamera( GameObject focalObject )
    {
        ConsolePrint("Enabling combat camera with target " + focalObject.name);
        //this method switches the camera to perspective mode and moves it to the location of the active combatant
        combatCameraActive = true;
        animating = true;

        //identify the object to be tracked by the camera
        cameraTrackedObject = focalObject;

        //place the combat box based on the input location data 
        combatCameraBox.transform.position = cameraTrackedObject.transform.position;
        combatCameraBox.transform.rotation = cameraTrackedObject.transform.rotation;
        //directly relocate the camera to the combat box
        mainCamera.transform.position = combatCameraBox.transform.GetChild(0).transform.position;
        mainCamera.transform.rotation = combatCameraBox.transform.GetChild(0).transform.rotation;

        Quaternion angleToTarget = Quaternion.LookRotation((cameraTrackedObject.transform.position + (cameraTrackedObject.transform.forward * 0.1f) + (cameraTrackedObject.transform.up * 0.6f)) - mainCamera.transform.position);
        mainCamera.transform.rotation = angleToTarget;// Quaternion.Slerp(transform.rotation, angleToTarget, Time.deltaTime);

        //toggle to perspective mode
        //mainCamera.GetComponent<Camera>().orthographic = false;
        //zoom camera instead
        mainCamera.GetComponent<Camera>().orthographicSize = cameraZoom_combat;
    }//enable combat camera 

    public void DisableCombatCamera()
    {
        //this method switches the camera back to ortho mode and moves it to the active game camera location 
        combatCameraActive = false;
        animating = false;

        //return camera to ortho perspective
        //mainCamera.GetComponent<Camera>().orthographic = true;
        //zoom camera instead
        mainCamera.GetComponent<Camera>().orthographicSize = cameraZoom_default;

        //return camera position to active game camera position
        mainCamera.transform.position = activeGameCameraLocations[0].transform.position;
        mainCamera.transform.rotation = activeGameCameraLocations[0].transform.rotation;

        //safety - reset the game phase flag in case it enters combat mode without that update having been done
        gamePhase = "ActiveGame";

    }//disable combat camera 


    /***************
     * DEBUG STUFF *
     ***************/

    public void ToggleCameraProjection()
    {
        mainCamera.GetComponent<Camera>().orthographic = !mainCamera.GetComponent<Camera>().orthographic;
    }

    public void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("Camera Control - " + message);
        }
    }//console print

}//class
