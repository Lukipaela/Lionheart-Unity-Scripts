using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class MouseDetector : MonoBehaviour
{
    public GameControl gameControlScript;

    private Ray ray;
    private RaycastHit hit;
    private string lastRayCastHitObjectName;
    private string selectedObjectTag;
    private GameObject highlightedArrow = null;

    private readonly bool enableDebugging = false;


    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start(){
        lastRayCastHitObjectName = "none";
    }

    void Update()
    {
        ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out hit))
        {
            //ignore any raycasting that is not over a gameobject (meaning, anything pointed at a UI canvas)
            if (!EventSystem.current.IsPointerOverGameObject()) { 

                if (hit.collider.name != lastRayCastHitObjectName)
                {
                    selectedObjectTag = hit.collider.gameObject.tag;

                    //handle special logic for rotation arrow highlighting 
                    if (selectedObjectTag == "RotationArrow")
                        ArrowHover();
                    else
                        NonArrowHover();

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
                        case "RotationArrow":
                            gameControlScript.RotationArrowClicked(hit.collider.gameObject);
                            break;
                    }//switch
                }//clicked
            }//mouse is not over UI item 
        }//raycast hit (mouse hovering)
        else
        {
            NonArrowHover();
        }//no raycast hit

    }//update


    /******************
     * CUSTOM METHODS *
     ******************/

    private void ArrowHover()
    {
        //if this is a different arrow we are hovering over now, turn off the old one first. 
        if (hit.collider.gameObject != highlightedArrow)
            NonArrowHover();

        highlightedArrow = hit.collider.gameObject;
        gameControlScript.SetRotationArrowHighlighting(true, highlightedArrow);
    }

    private void NonArrowHover()
    {
        if (highlightedArrow != null)
        {
            gameControlScript.SetRotationArrowHighlighting(false, highlightedArrow);
            highlightedArrow = null;
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
