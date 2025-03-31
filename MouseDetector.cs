using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseDetector : MonoBehaviour
{
    [SerializeField] private GameControl gameControlScript;
    private Ray ray;
    private RaycastHit hit;
    private string lastRayCastHitObjectName;
    private string selectedObjectTag;
    private BoardTileScript priorHoveredTileScript = null;

    private readonly bool enableDebugging = false;


    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start()
    {
        lastRayCastHitObjectName = "none";
    }

    void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            //ignore any raycasting that is not over a gameobject (meaning, anything pointed at a UI canvas)
            if (!EventSystem.current.IsPointerOverGameObject())
            {

                if (hit.collider.name != lastRayCastHitObjectName)
                {
                    selectedObjectTag = hit.collider.gameObject.tag;

                    //handle special logic for rotation arrow highlighting 
                    if (selectedObjectTag == "BoardTile")
                        TileHoverStart();
                    else
                        TileHoverStop();

                    lastRayCastHitObjectName = hit.collider.name;

                    ConsolePrint(hit.collider.name + " hovered over, with tag: " + selectedObjectTag);
                }//NEW hover

                if (Input.GetMouseButtonDown(0))
                {
                    //report the click to the game controller based on what type of object was clicked
                    switch (selectedObjectTag)
                    {
                        case "BoardTile":
                            gameControlScript.BoardTileClicked(hit.collider.gameObject);
                            break;
                        case "SquadTile":
                            gameControlScript.SquadClicked(hit.collider.gameObject);
                            break;
                    }//switch
                }//clicked
            }//mouse is not over UI item 
        }//raycast hit (mouse hovering)
        else
        {
            //no raycast hit
        }

    }//update


    /******************
     * CUSTOM METHODS *
     ******************/

    private void TileHoverStart()
    {
        BoardTileScript newHoverTileScript = hit.collider.gameObject.GetComponent<BoardTileScript>();
        //if hovering over a tile which has an active Movement particle effect, intensify it
        if (newHoverTileScript != priorHoveredTileScript)
        {
            if (priorHoveredTileScript != null)
                priorHoveredTileScript.IntensifyMoveTargetIndicator(false);
            priorHoveredTileScript = newHoverTileScript;
            newHoverTileScript.IntensifyMoveTargetIndicator(true);
        }
    }

    private void TileHoverStop()
    {
        if (priorHoveredTileScript != null)
        {
            priorHoveredTileScript.IntensifyMoveTargetIndicator(false);
            priorHoveredTileScript = null;
        }
    }


    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log(message);
        }
    }//console print


}//class
