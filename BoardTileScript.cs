using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardTileScript : MonoBehaviour
{
    public GameObject adjacentTileLeft;
    public GameObject adjacentTileRight;
    public GameObject adjacentTileTop;
    public GameObject adjacentTileBottom;
    public GameObject occupyingSquad;   //when occupied, holds a reference to the squad tile that is occupying it.
    public bool isOccupied;
    public int row;
    public int column;
    public bool validMoveTarget = false;
    public bool validAttackTarget = false;

    private GameControl gameControlScript;
    private bool selectedEffectOn = false;
    private bool validMoveTargetEffectOn = false;
    private bool validAttackTargetEffectOn = false;
    private Material thisTileMaterial;

    private static readonly bool enableDebugging = false;


    /********************
     * BUILT-IN METHODS *
     ********************/

    private void Start()
    {
        gameControlScript = GameObject.FindGameObjectWithTag("GameControl").GetComponent<GameControl>();
        isOccupied = false;
        occupyingSquad = null;
        thisTileMaterial = transform.GetComponent<MeshRenderer>().material;
    }//start


    /******************
     * CUSTOM METHODS *
     ******************/

    public void ClearTile()
    {
        isOccupied = false;
        occupyingSquad = null;
        thisTileMaterial.color = GameSettings.defaultBoardTileColor;
        ClearAllHighlights();
    }

    public void PlaceSquad( GameObject squadObject)
    {
        isOccupied = true;
        occupyingSquad = squadObject;
        EnableHighlight("Selected");
        thisTileMaterial.color = GameSettings.playerColors[squadObject.GetComponent<SquadScript>().ownerID];
    }

    public void EnableHighlight( string highlightType )
    {
        ConsolePrint("Toggling highlight of type " + highlightType + " for " + gameObject.name);

        switch (highlightType)
        {
            case "Selected":
                gameObject.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().Play();
                selectedEffectOn = true;
                break;
            case "ValidMoveTarget":
                validMoveTarget = true;
                gameObject.transform.GetChild(0).GetChild(1).GetComponent<ParticleSystem>().Play(); //first child call gets the particle system bucket. second gets the individual particle system
                validMoveTargetEffectOn = true;
                break;
            case "ValidAttackTarget":
                validAttackTarget = true;
                gameObject.transform.GetChild(0).GetChild(2).GetComponent<ParticleSystem>().Play(); //first child call gets the particle system bucket. second gets the individual particle system
                validAttackTargetEffectOn = true;
                break;
        }//switch highlight type
        
    }//toggleHighlighting

    public void ClearAllHighlights()
    {
        selectedEffectOn = false;
        gameObject.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().Stop();//first child call gets the particle system bucket. second gets the individual particle system
        gameObject.transform.GetChild(0).GetChild(0).GetComponent<ParticleSystem>().Clear();//first child call gets the particle system bucket. second gets the individual particle system

        validMoveTarget = false;
        gameObject.transform.GetChild(0).GetChild(1).GetComponent<ParticleSystem>().Stop();//first child call gets the particle system bucket. second gets the individual particle system
        gameObject.transform.GetChild(0).GetChild(1).GetComponent<ParticleSystem>().Clear();//first child call gets the particle system bucket. second gets the individual particle system
        validMoveTarget = false;

        validAttackTarget = false;
        gameObject.transform.GetChild(0).GetChild(2).GetComponent<ParticleSystem>().Stop();//first child call gets the particle system bucket. second gets the individual particle system
        gameObject.transform.GetChild(0).GetChild(2).GetComponent<ParticleSystem>().Clear();//first child call gets the particle system bucket. second gets the individual particle system
        validAttackTarget = false;
    }//ClearAllHighlights


    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("Board Tile Script - " + message);
        }
    }//console print

}//class
