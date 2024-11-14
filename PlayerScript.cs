using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour
{
    public int squadsRemaining;
    public int playerID;
    public GameControl gameControlScript;
    public int apRemaining; //action points
    public SquadScript kingSquadScript;
    public HumanSFXLibrary humanSFXLibrary;

    private static readonly bool enableDebugging = false;



    /********************
     * BUILT-IN METHODS *
     ********************/

    // Start is called before the first frame update
    void Start()
    {
        //build the array of squad objects, and assign them to their default positions. 
        gameControlScript = GameObject.FindGameObjectWithTag("GameControl").GetComponent<GameControl>();
        squadsRemaining = 10;
        apRemaining = 2;
    }



    /******************
     * PUBLIC METHODS *
     ******************/

    public void BeginTurn()
    {
        ConsolePrint("Player " + playerID + " beginning turn wth king " + kingSquadScript.ToString());
        apRemaining = 2;
        kingSquadScript.Cheer();
    }//begin turn

    public void SquadLost(SoldierClass squadType)
    {
        squadsRemaining--;
        //game is over if the king has died, or if all OTHER troops have died
        if (squadsRemaining == 1 || squadType == SoldierClass.King)
            gameControlScript.ReportPlayerDefeated(playerID);
    }//squad lost

    public void ConsumeAP(int apUsed)
    {
        ConsolePrint("Updating AP for player " + playerID + " from " + apRemaining + " to " + (apRemaining - apUsed));
        apRemaining -= apUsed;
    }//consume AP

    /// <summary>
    /// Called by GameControl if the user clicks Rematch after the conclusion of a game. 
    /// Initiates a reinitialization of all game parameters / references
    /// </summary>
    public void Reset()
    {
        squadsRemaining = 10;
    }



    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("Player Script - " + message);
        }
    }//console print


}//player class
