using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerScript : MonoBehaviour {
    public int squadsRemaining;
    public int playerID;
    public GameControl gameControlScript;
    public int apRemaining; //action points
    public SquadScript kingSquadScript;

    private static readonly bool enableDebugging = false;


    /********************
     * BUILT-IN METHODS *
     ********************/

    // Start is called before the first frame update
    void Start() {
        //build the array of squad objects, and assign them to their default positions. 
        gameControlScript = GameObject.FindGameObjectWithTag("GameControl").GetComponent<GameControl>();
        squadsRemaining = 10;
        apRemaining = 2;
    }

    // Update is called once per frame
    void Update() {
        //possibly no updates, this game will likely be entirely event-driven
    }


    /******************
     * PUBLIC METHODS *
     ******************/

    public void BeginTurn() {
        ConsolePrint("Player " + playerID + " beginning turn wth king " + kingSquadScript.ToString());
        apRemaining = 2;
        kingSquadScript.Cheer();
    }//begin turn

    public void SquadLost(string squadType) {
        squadsRemaining--;
        //game is over if the king has died, or if all OTHER troops have died
        if (squadsRemaining == 1 || squadType == "King")
            gameControlScript.ReportPlayerDefeated(playerID);
    }//squad lost

    public void ConsumeAP(int apUsed) {
        ConsolePrint("Updating AP for player " + playerID + " from " + apRemaining + " to " + (apRemaining - apUsed));
        apRemaining -= apUsed;
    }//consume AP


    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message) {
        if (enableDebugging == true) {
            Debug.Log("Player Script - " + message);
        }
    }//console print


}//player class
