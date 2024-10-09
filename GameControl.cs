using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;


[InitializeOnLoad]  //this property will build the library as soon as unity launches, reducing overhead during the game.
public class GameControl : MonoBehaviour
{
    // PUBLIC VARS
    public string gamePhase;    //PlaceArmyP1, PlaceArmyP2, DiceRoll, ActiveGame, GameOver, RollForFirst
    public string gameMode;
    public string turnPhase = "";    //SquadSelection, SquadAction, AlterPlacement
    public int activePlayerID;
    public string player1Name;
    public string player2Name;
    public GameObject currentSelectedSquad;
    public GameObject currentSelectedBoardTile;
    public HUDControlScript hudControlScript;
    public RotationArrowControlScript rotationArrowControlScript;
    public PlayerScript[] playerScripts;
    public GameObject[] ReferenceCenterTiles;
    public LightingControl lightControlScript;
    public AudioControl audioControlScript;

    //PRIVATE VARS
    private DieSpawner dieSpawnerScript;
    private CameraControl cameraControlScript;
    public int diceToRoll;    //tracks how many dice are being rolled 
    private int diceReported;    //tracks how many dice have stopped rolling, and settled on a result. 
    private string[] dieResults;
    private string squadToPlacePrefabAddress;
    private string placementState;//indicates if the player is choosing a squad to place, or choosing a location to place a squad: Idle , PlacingSquad
    private bool animating = false; //tracks if there is an animation running, so as to avoid advancing the game phase too soon. 
    private bool cameraIsAnimating = false; //tracks if there is an camera animation running, to allow repositioning to complete before proceeding with an animation
    private int animationCount = 0;
    private bool bonusRollIsActive = false;
    private bool complexAnimationActive = false;
    //variables for determining turn order
    private int[] playerScore = new int[] { 0, 0 };
    private bool diceRollIsValid = false;
    private string attackingDieType;    //holds the die type used by the current attacking squad: Axe, Arrow, Any
    private GameObject attackDefender;//holds a reference tot he victim of an attack (needed for attacks against mercenaries)
    private bool gameOver = false;
    private bool waitingForAttack = false; // a trigger to coordinate the timing of the attack animation and the death animation in a battle 
    private bool waitingForDefense = false; // a trigger to coordinate the timing of the defense animation and the death animation in a battle 
    

    //debug
    private readonly bool enableDebugging = true; //switch to enable/disable console logging for this script


    /********************
     * BUILT-IN METHODS *
     ********************/

    private void Start(){
        //initialize random seed
        Random.InitState(System.Environment.TickCount);

        //get object references
        dieSpawnerScript = GameObject.FindGameObjectWithTag("DiceControl").GetComponent<DieSpawner>();
        cameraControlScript = GameObject.FindGameObjectWithTag("CameraControl").GetComponent<CameraControl>();
        
        //set gameMode from main menu selection
        gameMode = GameSettings.gameMode;

        //initialize stuff
        dieResults = new string[4] { "None", "None", "None", "None" };
        diceToRoll = 0;
        diceReported = 0;
        activePlayerID = 0;

        ConsolePrint("Start called with Game Mode: " + gameMode);

        //start up the game
        if (gameMode == "QuickStart")
            QuickStart();
        else
            BeginArmyPlacement(activePlayerID);

        //begin BGM based on context
        audioControlScript.StartMusic("MainTheme");

    }//start

    private void Update()
    {
        CheckKeyboardCommands();

        //check on the animation status to potentially release the controls 
        if (animating && animationCount == 0 && !complexAnimationActive)
        {
            animating = false;
            ActionComplete();
        }

    }//Update


    /******************
     * CUSTOM METHODS *
     ******************/

    private void MoveSquadPosition( GameObject squadToMove, GameObject newBoardTile )
    {
        ConsolePrint("Move Squad Position called.");
        squadToMove.GetComponent<SquadScript>().MoveLocation(newBoardTile);
    }

    private void DeselectPreviousSquad()
    {
        ResetAllTileFlags();
        //if previous selected squad exists, send them a command to disable the selection cosmetics 
        if (currentSelectedSquad != null)
            currentSelectedSquad.GetComponent<SquadScript>().SetSelected(false);
        currentSelectedSquad = null;
        rotationArrowControlScript.HideArrows();
    }//DeselectPreviousSquad

    public void SquadToPlaceFromUI( string prefabAddress )
    {
        //this method is used to receive a message from the squad placement UI, telling it the prefab address of the next squad type to be placed. 
        squadToPlacePrefabAddress = prefabAddress;
        placementState = "PlacingSquad";
        DeselectPreviousSquad();
    }

    public void SetRotationArrowHighlighting(bool enabled, GameObject arrow)
    {
        //this method is just a passthrough. it was created to enforce a protocol where all objects communicate via tha gamecontroller instead of directly. 
        rotationArrowControlScript.SetHighlighting(enabled, arrow);
    }

    private void AnimateCamera(string cameraPhase)
    {
        ConsolePrint("Camera animation requested - " + cameraPhase);
        cameraIsAnimating = true;
        cameraControlScript.SetCameraGamePhase( cameraPhase );
    }//animate camera


    /*****************
     * ROUND CONTROL *
     *****************/

    private void QuickStart()
    {
        //This method is used so that units will be automatically placed on the board, for debug testing. 
        gamePhase = "QuickStart";
        //disable the unit placement hud
        hudControlScript.squadPlacementPanel.SetActive(false);

        int thisPlayerId = 0;
        // PLAYER 1 SETUP
        GameObject nextBoardTile = ReferenceCenterTiles[thisPlayerId];//first row center
        #region
            //infantry
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Infantry", 4, GameSettings.infantry4PrefabAddress);
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 1
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Infantry", 4, GameSettings.infantry4PrefabAddress);
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 2
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Infantry", 4, GameSettings.infantry4PrefabAddress);
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 3
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Infantry", 4, GameSettings.infantry4PrefabAddress);
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 4
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Archer", 4, GameSettings.archer4PrefabAddress);

            //Mercenaries
            nextBoardTile = ReferenceCenterTiles[thisPlayerId].GetComponent<BoardTileScript>().adjacentTileBottom;//Center - 1
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Mercenary", 2, GameSettings.mercenary2PrefabAddress);

            //Peasants
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileBottom;//center - 2
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Peasant", 4, GameSettings.peasant4PrefabAddress);

            //HeavyInfantry
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileBottom;//center - 3
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "HeavyInfantry", 2, GameSettings.heavyInfantry2PrefabAddress);


            //archers
            nextBoardTile = ReferenceCenterTiles[thisPlayerId].GetComponent<BoardTileScript>().adjacentTileRight; //second row center
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Infantry", 4, GameSettings.infantry4PrefabAddress);
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +1
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Archer", 4, GameSettings.archer4PrefabAddress);

            //knights
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +2
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Knight", 2, GameSettings.knight2PrefabAddress);
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +3
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Knight", 2, GameSettings.knight2PrefabAddress);

            //king
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +4
            playerScripts[0].kingSquadScript = CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "King", 1, GameSettings.king1PrefabAddress).GetComponent<SquadScript>();

        #endregion//player 2 instantiation code folder

        // PLAYER 2 SETUP
        thisPlayerId = 1;
        nextBoardTile = ReferenceCenterTiles[thisPlayerId];//first row center
        #region
            //infantry
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Infantry", 4, GameSettings.infantry4PrefabAddress);
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 1
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Infantry", 4, GameSettings.infantry4PrefabAddress);
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 2
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Infantry", 4, GameSettings.infantry4PrefabAddress);
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 3
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Infantry", 4, GameSettings.infantry4PrefabAddress);
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 4
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Archer", 4, GameSettings.archer4PrefabAddress);

            //Mercenary
            nextBoardTile = ReferenceCenterTiles[thisPlayerId].GetComponent<BoardTileScript>().adjacentTileBottom;//Center - 1
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Mercenary", 2, GameSettings.mercenary2PrefabAddress);

            //Peasants
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileBottom;//center - 2
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Peasant", 4, GameSettings.peasant4PrefabAddress);

            //HeavyInfantry
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileBottom;//center - 3
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "HeavyInfantry", 2, GameSettings.heavyInfantry2PrefabAddress);

            //archers
            nextBoardTile = ReferenceCenterTiles[thisPlayerId].GetComponent<BoardTileScript>().adjacentTileLeft; //second row center
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Infantry", 4, GameSettings.infantry4PrefabAddress);
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +1
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Archer", 4, GameSettings.archer4PrefabAddress);

            //knights
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +2
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Knight", 2, GameSettings.knight2PrefabAddress);
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +3
            CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "Knight", 2, GameSettings.knight2PrefabAddress);

            //king
            nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +4
            playerScripts[1].kingSquadScript = CreateAndPlaceSquad(nextBoardTile, thisPlayerId, "King", 1, GameSettings.king1PrefabAddress).GetComponent<SquadScript>();
        #endregion//player 2 instantiation code folder

        //final safety cleanup
        currentSelectedSquad = null;
        currentSelectedBoardTile = null;
        ResetAllTileFlags();
        //kick off the game
        activePlayerID = 0;
        BeginGame();
    }//quickstart

    private void BeginArmyPlacement(int playerID)
    {
        hudControlScript.SetTurnDataPanelVisibility(false);
        if (playerID == 0)
        {
            activePlayerID = 0;
            gamePhase = "PlaceArmyP1";
            placementState = "Idle";
            AnimateCamera("PlayerOneSetup");
            hudControlScript.InitializeSquadPlacementUI(gameMode, activePlayerID);
        }
        else if (playerID == 1)
        {
            activePlayerID = 1;
            gamePhase = "PlaceArmyP2";
            placementState = "Idle";
            AnimateCamera("PlayerTwoSetup");
            hudControlScript.InitializeSquadPlacementUI(gameMode, activePlayerID);
        }

    }//begin army placement 

    private void BeginGame()
    {
        ConsolePrint("Beginning Game");
        gamePhase = "ActiveGame";
        AnimateCamera("ActiveGame");
        //after both teams have been rendered, update their soldier's skin tones to match the team they belong to.
        AssignTeamColors();
        //tell player script to start
        playerScripts[activePlayerID].BeginTurn();
        //setup hud panel
        hudControlScript.SetTurnDataPanelVisibility(true);
        hudControlScript.SetTurnData(activePlayerID, playerScripts[activePlayerID].apRemaining);
    }//begin game

    private void ActionComplete()
    {
        ConsolePrint("Action complete called for Player " + activePlayerID + " with " + playerScripts[activePlayerID].apRemaining + " AP Remaining");

        if (gamePhase != "PlaceArmyP1" && gamePhase != "PlaceArmyP2")
        {
            //reenable the hud, in case it was hidden for diceroll/animation
            hudControlScript.SetTurnDataPanelVisibility(true);
            hudControlScript.SetTurnData(activePlayerID, playerScripts[activePlayerID].apRemaining);
            if (cameraControlScript.combatCameraActive)
                cameraControlScript.DisableCombatCamera();

            //reset all tile highlight effects and attackable/movable flags
            ResetAllTileFlags();

            //called after animations end, to  see if the turn is over. 
            if (playerScripts[activePlayerID].apRemaining == 0)
            {
                EndTurn();
            }
            else if (currentSelectedSquad != null)
            {
                //reapply the arrows if a squad is still selected, and the turn is not over.
                rotationArrowControlScript.AssignToSquad(currentSelectedSquad);
                currentSelectedSquad.GetComponent<SquadScript>().EnableActionHighlights();
                currentSelectedSquad.GetComponent<SquadScript>().SetSelected(true);
            }
            if (!gameOver)
                gamePhase = "ActiveGame";
            else
                GameOver();
        }
        else
        {
            //the only animation in army placement is the squad relocation mechanic. 
            rotationArrowControlScript.AssignToSquad(currentSelectedSquad);
        }
    }//action complete

    public void EndTurn()
    {
        ConsolePrint("Ending turn");
        //close off prior turn mechanisms
        DeselectPreviousSquad();
        ResetAllTileFlags();
        //begin new turn 
        activePlayerID = (activePlayerID + 1) % 2;
        playerScripts[activePlayerID].BeginTurn();
        //setup hud panel
        hudControlScript.SetTurnData(activePlayerID, playerScripts[activePlayerID].apRemaining);
    }//End Turn

    public void GameOver()
    {
        //play sfx for gameover 
        audioControlScript.GameOver();
        gamePhase = "GameOver";
        DeselectPreviousSquad();
    }


    /********************
     * EXTERNAL REPORTS *
     ********************/

    public void ReportAnimationStart( string reportData)
    {
        animating = true;
        animationCount++;
        ConsolePrint("Squad animation (" + reportData + ") started. " + animationCount + " total squad animations running.");
    }//report animation start

    public void ReportAnimationComplete(string reportData)
    {
        //called by other gameobjects when an animation has ended. once all have reported completion, we can move on.
        animationCount--;
        ConsolePrint("Squad animation (" + reportData + ") ended. " + animationCount + " squad animations remaining.");
    }//report animation complete

    public void ReportSquadElimenated(GameObject elimenatedSquad)
    {
        ConsolePrint("Squad Elimenation report received.");
        //collect information
        SquadScript elimenatedSquadControlScript = elimenatedSquad.GetComponent<SquadScript>();
        int associatedPlayerID = elimenatedSquadControlScript.ownerID;
        //tell theplayer a squad was lost
        playerScripts[associatedPlayerID].SquadLost(elimenatedSquadControlScript.squadType);
        //tell the tile that the tile is now vacant
        elimenatedSquadControlScript.occupiedGameTile.GetComponent<BoardTileScript>().ClearTile();
        if (elimenatedSquad == currentSelectedSquad)
        {
            currentSelectedSquad = null;
            rotationArrowControlScript.HideArrows();
        }
        //destroy the squad object 
        Destroy(elimenatedSquad, GameSettings.deathLingerDuration);
    }//ReportElimenatedSquad

    public void ReportPlayerDefeated( int defeatedPlayerID )
    {
        gamePhase = "GameOver";
        hudControlScript.PrintMessage("GAME OVER. " + GameSettings.playerNames[(defeatedPlayerID + 1) % 2] + " wins!");
        //set the active player indicator to an invalid setting
        gameOver = true;
        //play some animation
    }//Report player defeated

    public void ReportCameraAnimationComplete()
    {
        ConsolePrint("Camera animation reported complete");
        cameraIsAnimating = false;
    }//ReportCameraAnimationComplete

    public void ReportAttackAnimationBeginning()
    {
        ConsolePrint("Attack animation start report received");
        waitingForAttack = false;
    }

    public void ReportBlockAnimationBeginning()
    {
        ConsolePrint("Block animation start report received");
        waitingForDefense = false;
    }



    /******************
     * ATTACK METHODS *
     ******************/

    private IEnumerator Attack(GameObject attacker, GameObject defender, string attackType, int dieCount)    {
        complexAnimationActive = true;
        ConsolePrint("Initiating attack.");
        hudControlScript.SetTurnDataPanelVisibility(false);

        gamePhase = "Attacking";
        attackDefender = defender;
        attackingDieType = attackType;
        diceToRoll = dieCount;
        diceRollIsValid = true;
        hudControlScript.PrintMessage(GameSettings.playerNames[activePlayerID] + " - Press SPACE to roll when ready. Attacking with " + attackType + " dice.");

        //tell the camera to move, wait for it to confirm completion via a ReportCameraAnimationComplete call.
        AnimateCamera("DiceRoll");
        while (cameraIsAnimating){
            yield return new WaitForSeconds(0.1f);
        }

    }//attack

    private IEnumerator ResolveAttack(int damageDone, int panicsRolled){
        //pull in reference variables 
        string attackerSquadType = currentSelectedSquad.GetComponent<SquadScript>().squadType;
        SquadScript attackerSquadScript = currentSelectedSquad.GetComponent<SquadScript>();
        string defenderSquadType = attackDefender.GetComponent<SquadScript>().squadType;
        SquadScript defenderSquadScript = attackDefender.GetComponent<SquadScript>();
        Vector3 vectorToAttacker = (defenderSquadScript.gameObject.transform.position - attackerSquadScript.gameObject.transform.position) * -1;

        if (!bonusRollIsActive)
            ReportAnimationStart("Attack resolution initiated");
        bool bonusRollEarned = damageDone == 1 && diceToRoll == 1 && defenderSquadScript.isLargeUnit && !bonusRollIsActive;

        //add a delay to give the player time to observe the dice results
        yield return new WaitForSeconds(2f);
        ConsolePrint("Resolving attack dealing " + damageDone + " damage, with " + panicsRolled + " panics rolled.");
        //return the camera to active game position if no damage was done. else, we will move to combat position next
        if (damageDone == 0 && !bonusRollEarned ){
            AnimateCamera("ActiveGame");
            while (cameraIsAnimating){
                yield return new WaitForSeconds(0.1f);
            }
        }

        gamePhase = "ActiveGame";

        if (bonusRollIsActive == true){
            bonusRollIsActive = false;

            //if this is a bonus roll, one die of damage counts for double - allows one soldier to kill a 2-health opponent in 2 sequential rolls. 
            if (damageDone > 0){
                cameraControlScript.EnableCombatCamera(attackerSquadScript.gameObject.transform.GetChild(1).gameObject);
                //trigger the defense animation for the defender, and give them a moment to get into position
                defenderSquadScript.Defend(vectorToAttacker);
                waitingForDefense = true;
                while (waitingForDefense){
                    yield return new WaitForSeconds(0.1f);
                }

                //tell the attackers to animate an attack, and the defender to take damage 
                attackerSquadScript.Attack(attackDefender);
                waitingForAttack = true;
                while (waitingForAttack){
                    yield return new WaitForSeconds(0.1f);
                }
                defenderSquadScript.TakeDamage(damageDone * 2);
                //tell any remaining defender units to go back to idle
                yield return new WaitForSeconds(0.5f);
                defenderSquadScript.Idle();
            }

            //can only reach here with small units rolling one die. 
            if (panicsRolled == 1){
                Panic(1, attackerSquadScript, (attackerSquadScript.orientationIndex + 2) % 4);
                ResetAllTileFlags();
            }
        }//bonus roll
        else{  //deal damage to the defender
            if (damageDone > 0 && !bonusRollEarned){
                cameraControlScript.EnableCombatCamera(attackerSquadScript.gameObject.transform.GetChild(1).gameObject);
                //trigger the defense animation for the defender, and give them a moment to get into position
                defenderSquadScript.Defend(vectorToAttacker);
                waitingForDefense = true;
                while (waitingForDefense){
                    yield return new WaitForSeconds(0.1f);
                }

                //tell the attackers to animate an attack, and the defender to take damage 
                attackerSquadScript.Attack(attackDefender);
                waitingForAttack = true;
                while (waitingForAttack){
                    yield return new WaitForSeconds(0.1f);
                }

                defenderSquadScript.TakeDamage(damageDone);
                //tell any remaining defender units to go back to idle
                yield return new WaitForSeconds(0.5f);
                defenderSquadScript.Idle();
            }

            if (panicsRolled == diceToRoll && attackerSquadType != "King" && attackerSquadType != "Mercenary" && attackerSquadType != "Peasant"){
                Panic(1, attackerSquadScript, (attackerSquadScript.orientationIndex + 2) % 4);
                ResetAllTileFlags();
            }

            //if attacking as peasant, retreat once for every retreat rolled after applying damage
            else if (panicsRolled > 0 && attackerSquadType == "Peasant"){
                ConsolePrint("Peasant panicking after attack");
                Panic(panicsRolled, attackerSquadScript, (attackerSquadScript.orientationIndex + 2) % 4); 
                ResetAllTileFlags();
            }//panics triggered

            //mercs cause thier victims to panic
            else if(panicsRolled > 0 && attackerSquadType == "Mercenary"){
                ConsolePrint("Mercenary causing panic after attack");
                Panic(panicsRolled, defenderSquadScript, attackerSquadScript.orientationIndex );
                ResetAllTileFlags();
            }

            else if (bonusRollEarned) {//invoke special rules if  a single soldier attacked a 2-hit unit and succeeded    
                bonusRollIsActive = true;
                BonusAttack();
            }//bonus toll triggered

        }//standard roll
        if (!bonusRollIsActive){
            complexAnimationActive = false;
            ReportAnimationComplete("Attack resolution completed.");
        }
    }//resolve attack

    public void BonusAttack()    {
        hudControlScript.PrintMessage("Bonus roll earned!");
        ConsolePrint("Bonus attack earned.");
        StartCoroutine(Attack(currentSelectedSquad, attackDefender, currentSelectedSquad.GetComponent<SquadScript>().dieType, 1));
    }//bonus attack

    public void Panic(int panicDistance, SquadScript panickingSquad, int retreatDirection)    {
        ConsolePrint("Panicking distance of " + panicDistance);
        panickingSquad.Panic(panicDistance, retreatDirection);
    }//panic


    /****************
     * CLICK EVENTS *
     ****************/

    public void SquadClicked(GameObject selectedSquad){
        if (!animating){
            ConsolePrint(selectedSquad.name + " clicked.");

            SquadScript selectedSquadScript = selectedSquad.GetComponent<SquadScript>();

            //pass off control management to he board tile clicked method
            BoardTileClicked(selectedSquadScript.occupiedGameTile);
        }
    }//squad selected

    public void BoardTileClicked(GameObject selectedBoardTile){
        if (!animating){
            ConsolePrint(selectedBoardTile.name + " click detected by control script in phase " + gamePhase);
            BoardTileScript selectedTileControlScript = selectedBoardTile.GetComponent<BoardTileScript>();
            switch (gamePhase){
                case "PlaceArmyP1":
                case "PlaceArmyP2":
                    #region
                    if (placementState == "Idle")
                    {
                        if (selectedTileControlScript.isOccupied == true)
                        {
                            //if we are altering placement, but we pick a new squad, toggle to that squad instead.
                            if (currentSelectedSquad != null)
                                DeselectPreviousSquad();
                            if (currentSelectedSquad != selectedTileControlScript.occupyingSquad)
                            {
                                currentSelectedSquad = selectedTileControlScript.occupyingSquad;
                                currentSelectedSquad.GetComponent<SquadScript>().SetSelected(true);
                                rotationArrowControlScript.AssignToSquad(currentSelectedSquad);
                            }
                            else
                            {
                                //no action for selecting the same squad again
                            }
                        }//occupied
                        else
                        {
                            if (currentSelectedSquad != null)
                            {
                                //we have a squad selected, and we just clicked an empty space. relocate squad if valid
                                if ((activePlayerID == 0 && selectedTileControlScript.row > 1) || (activePlayerID == 1 && selectedTileControlScript.row < 7))
                                {
                                    hudControlScript.PrintMessage("Squads can only be placed on the back two rows!");
                                }//invalid row
                                else
                                {
                                    MoveSquadPosition(currentSelectedSquad, selectedBoardTile);
                                    rotationArrowControlScript.HideArrows();
                                }
                            }
                        }//empty tile clicked
                    }//idle state
                    else if (placementState == "PlacingSquad")
                    {
                        if (selectedTileControlScript.isOccupied == false)
                        {
                            if ((activePlayerID == 0 && selectedTileControlScript.row > 1) || (activePlayerID == 1 && selectedTileControlScript.row < 7))
                            {
                                audioControlScript.ErrorClick();
                                hudControlScript.PrintMessage("Squads can only be placed on the back two rows!");
                            }//invalid row
                            else
                            {
                                audioControlScript.GeneralButtonClick();
                                ConsolePrint("Instantiation object with address: " + squadToPlacePrefabAddress);
                                //create, place, define the squad
                                GameObject newSquad = CreateAndPlaceSquad(selectedBoardTile, activePlayerID, hudControlScript.selectedSquadType, hudControlScript.selectedSquadSize, squadToPlacePrefabAddress);
                                if (hudControlScript.selectedSquadType == "King")
                                {
                                    ConsolePrint("Assigning king for " + gamePhase);
                                    if (gamePhase == "PlaceArmyP1")
                                        playerScripts[0].kingSquadScript = newSquad.GetComponent<SquadScript>();
                                    else
                                        playerScripts[1].kingSquadScript = newSquad.GetComponent<SquadScript>();
                                }

                                //deselect squad after placing in order to do multi-drop
                                currentSelectedSquad = null;
                                bool squadTypeExhausted = hudControlScript.SquadWasPlaced();

                                //return to idle state, signal to HUD that placement is complete if remaining troop count for this squad type is zero
                                if (squadTypeExhausted)
                                    placementState = "Idle";
                            }//valid placement
                        }//unoccupied tile
                        else
                        {
                            audioControlScript.ErrorClick();
                            hudControlScript.PrintMessage("This space is already occupied!");
                        }//occupied tile
                    }//placing squad mode
                    #endregion 
                    break;

                case "ActiveGame":
                    ConsolePrint("BoardTileClicked - ActiveGame case.");
                    if (currentSelectedSquad == null) {
                        //ConsolePrint("Prior selected squad was null.");
                        if (selectedTileControlScript.occupyingSquad == null) {
                            //No Squad selected, and clicked tile is empty. ignore.
                            ConsolePrint("No Squad selected, and clicked tile is empty. Click ignored");
                        }
                        else{
                            GameObject clickedSquad = selectedTileControlScript.occupyingSquad;
                            if (clickedSquad.GetComponent<SquadScript>().ownerID != activePlayerID){
                                //ignore command, selected squad doesnt belong to the current player.
                                ConsolePrint("Enemy Squad clicked, invalid. Click ignored");
                            }
                            else{
                                //player has clicked on one of their own squad tiles
                                audioControlScript.GeneralButtonClick();
                                currentSelectedSquad = clickedSquad;
                                currentSelectedSquad.GetComponent<SquadScript>().SetSelected(true);
                                rotationArrowControlScript.AssignToSquad(currentSelectedSquad);
                            }//player has clicked on one of their own squad tiles
                        }//tile was occupied
                    }//no squad currently selected
                    else{
                        SquadScript currentSelectedSquadScript = currentSelectedSquad.GetComponent<SquadScript>();

                        if (selectedTileControlScript.occupyingSquad == null){
                            //clicked tile is empty
                            if (selectedTileControlScript.validMoveTarget){
                                int apRequired = currentSelectedSquadScript.apCostToMove;
                                int apRemaining = playerScripts[activePlayerID].apRemaining;
                                if (apRemaining < apRequired){
                                    audioControlScript.ErrorClick();
                                    hudControlScript.PrintMessage("Insufficient AP for this action.");
                                }
                                else{
                                    //decrease the player's AP
                                    audioControlScript.GeneralButtonClick();
                                    rotationArrowControlScript.HideArrows();
                                    playerScripts[activePlayerID].ConsumeAP(apRequired);
                                    ResetAllTileFlags();
                                    MoveSquadPosition(currentSelectedSquad, selectedBoardTile);
                                }//MOVE
                            }//valid move target
                            else{
                                //the clicked tile is not a valid move target, ignore command.
                            }
                        }//clicked tile is empty
                        else{
                            //clicked tile is NOT empty
                            GameObject clickedSquad = selectedTileControlScript.occupyingSquad;

                            if (clickedSquad.GetComponent<SquadScript>().ownerID != activePlayerID){
                                //targeting an enemy tile
                                ConsolePrint("BoardTileClicked - attack logic.");
                                //check if the tile is a valid attack target 
                                if (selectedTileControlScript.validAttackTarget){
                                    //attack requested
                                    //check if we have enough AP 
                                    int apRequired = currentSelectedSquadScript.apCostToAttack;
                                    int apRemaining = playerScripts[activePlayerID].apRemaining;
                                    if (apRemaining < apRequired){
                                        audioControlScript.ErrorClick();
                                        hudControlScript.PrintMessage("Insufficient AP for this action.");
                                    }
                                    else{  //begin attack mechanic
                                        audioControlScript.GeneralButtonClick();
                                        rotationArrowControlScript.HideArrows();
                                        //decrease the player's AP
                                        playerScripts[activePlayerID].ConsumeAP(apRequired);
                                        //get squad dice type 
                                        //get squad count
                                        attackingDieType = currentSelectedSquadScript.dieType;
                                        int diceForSquad = currentSelectedSquadScript.GetDiceCount();
                                        //attack
                                        ResetAllTileFlags();
                                        StartCoroutine(Attack(currentSelectedSquad, clickedSquad, attackingDieType, diceForSquad));
                                    }// attack
                                }//in attack range 
                            }//enemy targeted
                            else{  //clicked a different friendly squad, shift focus there
                                ConsolePrint("BoardTileClicked - change selection logic.");
                                audioControlScript.GeneralButtonClick();
                                DeselectPreviousSquad();
                                currentSelectedSquad = clickedSquad;
                                currentSelectedSquad.GetComponent<SquadScript>().SetSelected(true);
                                rotationArrowControlScript.AssignToSquad(currentSelectedSquad);
                            }//player has clicked on one of their own squad tiles
                        }//tile was occupied
                    }//player had already selected a squad to issue orders to 
                    break;
            }//phase switch 
        }// !animating
    }//board tile clicked

    public void RotationArrowClicked(GameObject arrow){
        if (!animating){
            ConsolePrint("Rotation arrow clicked.");
            RotationArrowControlScript arrowControl = arrow.transform.parent.GetComponent<RotationArrowControlScript>();
            string rotationDirection = arrowControl.GetDirection(arrow);

            switch (gamePhase){
                case "PlaceArmyP1":
                case "PlaceArmyP2":
                    //find out which way to rotate
                    //tell the associated squad to turn. (no validations around AP remianing in this phase)
                    audioControlScript.RotationArrowClick();
                    arrowControl.associatedSquad.GetComponent<SquadScript>().RotateSquad(rotationDirection);
                    break;

                case "ActiveGame":
                    int apRequired = currentSelectedSquad.GetComponent<SquadScript>().apCostToRotate;
                    int apRemaining = playerScripts[activePlayerID].apRemaining;
                    if (apRemaining < apRequired){
                        audioControlScript.ErrorClick();
                        hudControlScript.PrintMessage("Insufficient AP for this action");
                    }
                    else{
                        audioControlScript.RotationArrowClick();
                        SquadScript currentSelectedSquadScript = arrowControl.associatedSquad.GetComponent<SquadScript>();
                        //decrease the player's AP
                        playerScripts[activePlayerID].ConsumeAP(apRequired);
                        //tell the associated squad to turn. (no validations around AP remianing in this phase)
                        currentSelectedSquadScript.RotateSquad(rotationDirection);
                        ResetAllTileFlags();
                        rotationArrowControlScript.HideArrows();
                    }//valid action
                    break;

            }//game phase switch
        }// !animating
    }//rotation arrow clicked

    public void PlayerArmyPlacementComplete(){
        audioControlScript.GeneralButtonClick();
        if (!animating){
            DeselectPreviousSquad();
            //called from HUD when the player has hit "PlacementComplete" in the HUD, after placing all squads.
            if (activePlayerID == 0){
                //first player finished, second player must begin
                BeginArmyPlacement(1);
            }
            else{
                //all players finished, ready to start game 
                RollForTurnOrder();
            }
        }// !animating
    }//playerArmyPlacementComplete


    /*******************
     * DICE MANAGEMENT *
     *******************/

    private void RollForTurnOrder(){
        activePlayerID = 0;
        //set phase
        gamePhase = "RollForFirst";
        //hide the squad placement panel 
        hudControlScript.squadPlacementPanel.SetActive(false);
        //set camera to Dice Roll angle mode
        AnimateCamera("DiceRoll");
        //give the player instructions
        hudControlScript.PrintMessage("Roll to see who goes first! Player 1, hit SPACE to roll when ready.");
        //enable dice rolls
        diceRollIsValid = true;
        diceToRoll = 4;
    }

    public void DiceRollBegin(int dieCount){
        //disable additional dice rolls
        diceRollIsValid = false;
        //this method is called when a dice roll begins, and should reinitialize all tracker values
        DestroyDice();
        ConsolePrint("Rolling " + dieCount + " dice.");
        dieSpawnerScript.RollDice(dieCount);
        diceReported = 0;
        diceToRoll = dieCount;
    }

    private void DestroyDice(){
        GameObject[] dice = GameObject.FindGameObjectsWithTag("Die");
        foreach (GameObject die in dice){
            Destroy(die);
        }
    }

    public void DiceRollReport(string dieResult){
        //this method is called by a die when it generates a result. 
        ConsolePrint(dieResult + " reported to control script");

        dieResults[diceReported] = dieResult;
        diceReported++;
        if (diceReported == diceToRoll){
            DiceRollEnd();
        }
    }

    private void DiceRollEnd(){
        //this method should be called once the required number of dice roll reports have been received, and should take action based on the result. 
        ConsolePrint("All dice finished rolling in game phase " + gamePhase);

        int axeCount = 0;
        int arrowCount = 0;
        int panicCount = 0;
        for (int i = 0; i < diceToRoll; i++){
            if (dieResults[i] == "Axe")
                axeCount++;
            else if (dieResults[i] == "Arrow")
                arrowCount++;
            else if (dieResults[i] == "Panic")
                panicCount++;
        }//die analysis for loop

        switch (gamePhase){
            case "RollForFirst":
                #region
                playerScore[activePlayerID] = axeCount;
                switch (activePlayerID)
                {
                    case 0:
                        hudControlScript.PrintMessage(GameSettings.playerNames[0] + " scored a " + axeCount + ". " + GameSettings.playerNames[1] + " Press SPACE to roll.");
                        activePlayerID = 1;
                        diceRollIsValid = true;
                        break;
                    case 1:
                        if (playerScore[0] > playerScore[1])
                        {
                            hudControlScript.PrintMessage(GameSettings.playerNames[1] + " scored a " + axeCount + ". " + GameSettings.playerNames[0] + " wins and will go first.");
                            activePlayerID = 0;
                            Invoke("BeginGame", 4);
                        }
                        else if (playerScore[0] < playerScore[1])
                        {
                            hudControlScript.PrintMessage(GameSettings.playerNames[1] + " scored a " + axeCount + ". " + GameSettings.playerNames[1] + " wins and will go first.");
                            activePlayerID = 1;
                            Invoke("BeginGame", 4);
                        }
                        else
                        {
                            hudControlScript.PrintMessage(GameSettings.playerNames[1] + " scored a " + axeCount + ". " + "Tie! " + GameSettings.playerNames[0] + ", roll again.");
                            activePlayerID = 0;
                            diceRollIsValid = true;
                        }
                        break;
                }
                #endregion
                break;
            case "Attacking":
                int damage;
                if (attackingDieType == "Axe")
                    damage = axeCount;
                else if (attackingDieType == "Arrow")
                    damage = arrowCount;
                else //"Any" attacks with whatever die type you roll the most of
                    damage = Mathf.Max(axeCount, arrowCount);
                //report the results
                StartCoroutine(ResolveAttack(damage, panicCount));
                break;
        }//gamephase switch

    }//dice roll end


    /*************
     * UTILITIES *
     *************/

    private void CheckKeyboardCommands(){
        //check if the user has pressed any keys that mve the camera
        if (Input.GetKeyUp(KeyCode.C)){
            //move camera to position the next in the current locations array
            cameraControlScript.MoveToNextLocation();
        }

        if (Input.GetKeyUp(KeyCode.Space)){
            if ((gamePhase == "RollForFirst" || gamePhase == "Attacking") && diceRollIsValid)
            {
                DiceRollBegin(diceToRoll);
            }//valid to roll 
        }//space pressed


        //TODO: Disable the next line for real gameplay
        CheckDebugCommands();

    }//CheckKeyboardCommands

    private GameObject CreateAndPlaceSquad(GameObject boardTile, int playerID, string squadType, int squadSize, string prefabAddress){
        GameObject newSquad = Instantiate(Resources.Load<GameObject>(prefabAddress), boardTile.transform.position, boardTile.transform.rotation);
        string orientation = "Right";
        if (playerID == 1){
            newSquad.transform.Rotate(0, 180, 0);
            orientation = "Left";
        }
        //define script parameters
        newSquad.GetComponent<SquadScript>().DefineSquad(playerID, squadType, squadSize, orientation, boardTile);
        //tell the board tile that it is now occupied by this unit 
        boardTile.GetComponent<BoardTileScript>().PlaceSquad(newSquad);
        return newSquad;
    }

    public void ResetAllTileFlags(){
        ConsolePrint("Clearing all highlights");
        //clears all highlights and markers for movable / attackable targets on the board
        GameObject gameBoard = GameObject.FindGameObjectWithTag("Board");
        BoardTileScript[] tileControls = gameBoard.GetComponentsInChildren<BoardTileScript>();
        foreach (BoardTileScript script in tileControls){
            script.ClearAllHighlights();
        }
    }//ResetAllTileFlags

    public void AssignTeamColors(){
        //assigns the selected team color for each player to the skins of all of their soldiers.
        // create empty lists for each team's squads
        List<UnitScript> p1Units = new List<UnitScript>();
        List<UnitScript> p2Units = new List<UnitScript>();
        //find all squad scripts in the game
        UnitScript[] allUnits = FindObjectsOfType(typeof(UnitScript)) as UnitScript[];
        //loop over all squads and sort into team lists
        foreach (UnitScript thisUnit in allUnits){
            if (thisUnit.parentSquadScript.ownerID == 0)
                p1Units.Add(thisUnit);
            else
                p2Units.Add(thisUnit);
        }

        //collect team colors
        Color p1Color = GameSettings.playerColors[0];
        Color p2Color = GameSettings.playerColors[1];
        //loop over each list and apply the team color to all child scripts of type 
        foreach (UnitScript thisUnit in p1Units){
            thisUnit.SetColor(p1Color);
        }
        foreach (UnitScript thisUnit in p2Units){
            thisUnit.SetColor(p2Color);
        }
    }
    /***************
     * DEBUG STUFF *
     ***************/

    private void CheckDebugCommands(){
        if (Input.GetKeyUp(KeyCode.P)){
            if(currentSelectedSquad != null){
                cameraControlScript.EnableCombatCamera(currentSelectedSquad.transform.GetChild(1).gameObject);
            }
        }//p pressed

        //these are keyboard shortcuts that should be disabled in real gameplay. 
        if (Input.GetKeyUp(KeyCode.A)){
            //move camera to position the next in the current locations array
            cameraControlScript.SetCameraGamePhase("ActiveGame");
        }

        if (Input.GetKeyUp(KeyCode.Alpha1)){
            //move camera to position the next in the current locations array
            cameraControlScript.SetCameraGamePhase("PlayerOneSetup");
        }

        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            //move camera to position the next in the current locations array
            cameraControlScript.SetCameraGamePhase("PlayerTwoSetup");
        }

        if (Input.GetKeyUp(KeyCode.Alpha0))
        {
            //quick and dirty fix if aniumation counter fails and play is frozen.
            animationCount = 0;
        }

        if (Input.GetKeyUp(KeyCode.R)){
            //move camera to position the next in the current locations array
            DiceRollBegin(4);
        }

        if (Input.GetKeyUp(KeyCode.F)){
            //demo code to trigger an animation
            //demoGuyAnimator.SetTrigger("FlipTrigger");
        }

    }//check debug commands

    public void ConsolePrint( string message){
        if (enableDebugging == true){
            Debug.Log("GameControl - " + message);
        }
    }//console print

    private void PrintSquad( GameObject squad){
        SquadScript selectedSquadScript = squad.GetComponent<SquadScript>();
        string message = "Squad data - CLICK HERE!! \n" +
            "PlayerID: " + selectedSquadScript.ownerID.ToString() + "\n" +
            "Unit Type: " + selectedSquadScript.squadType + "\n" +
            "Unit Count: " + selectedSquadScript.unitsInSquad.ToString() + "\n";
        ConsolePrint(message);
    }

}//class
