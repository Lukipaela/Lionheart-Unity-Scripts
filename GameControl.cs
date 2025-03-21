using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEngine.UI;
using Unity.VisualScripting;


public class GameControl : MonoBehaviour
{
    // PUBLIC VARS
    public GamePhase gamePhase;    //PlaceArmyP1, PlaceArmyP2, DiceRoll, ActiveGame, GameOver, RollForFirst
    public int activePlayerID = 0;
    public PlayerScript[] playerScripts;

    //PRIVATE VARS (Serialized fields are accessible within the editor, but aren't exposed to other scripts as a facet of this class
    [SerializeField] private GameObject[] ReferenceCenterTiles;
    [SerializeField] private LightingControl lightControlScript;
    [SerializeField] private AudioControl audioControlScript;
    [SerializeField] private RotationArrowControl rotationArrowControlScript;
    [SerializeField] private HUDControlScript hudControlScript;
    [SerializeField] private GameOverPanelScript gameOverPanelScript;
    [SerializeField] private InfoPanelScript infoPanelScript;
    [SerializeField] private DieSpawner dieSpawnerScript;
    [SerializeField] private CameraControl cameraControlScript;
    public GameObject currentSelectedSquad;
    private string squadToPlacePrefabAddress;
    private string placementState;//indicates if the player is choosing a squad to place, or choosing a location to place a squad: Idle , PlacingSquad
    //variables for determining turn order
    private int[] playerScore = new int[] { 0, 0 };
    private bool diceRollIsValid = false;
    private bool waitingForAttack = false; // a trigger to coordinate the timing of the attack animation and the death animation in a battle 
    private bool waitingForDefense = false; // a trigger to coordinate the timing of the defense animation and the death animation in a battle 
    private bool actionTaken = false;   //indicates if some ap-consuming action has been taken in this loop
    private CombatData combatData;
    private ObjectHider armyHider;
    //debug
    private readonly bool enableDebugging = true; //switch to enable/disable console logging for this script



    /********************
     * BUILT-IN METHODS *
     ********************/

    private void Start()
    {
        hudControlScript.ToggleDiceRollButtonVisibility();
        ConsolePrint("Start called with Game Mode: " + GameSettings.gameMode);

        //initialize random seed
        Random.InitState(System.Environment.TickCount);

        //start up the game
        if (GameSettings.gameMode == GameMode.QuickStart)
            Invoke("QuickStart", 0.1f);
        else
            BeginArmyPlacement(activePlayerID);

        //begin BGM based on context
        audioControlScript.StartMusic("MainTheme");

        // find and store Army Hider object
        Transform armyHiderCollection = GameObject.FindGameObjectWithTag("ArmyHider").transform;
        armyHider = new ObjectHider(armyHiderCollection.GetChild(1).position, armyHiderCollection.GetChild(2).position, armyHiderCollection.GetChild(0), 200);

        //initialize the time of day
        lightControlScript.SetTimeCycleMode(GameSettings.timeCycleMode);
    }//start

    private void Update()
    {
        CheckKeyboardCommands();
        //check on the animation status to potentially release the controls 
        if (!SquadsAreAnimating() && gamePhase == GamePhase.Active && actionTaken)
        {
            ActionComplete();
        }

        armyHider.ManualUpdate();
    }//Update



    /******************
     * CUSTOM METHODS *
     ******************/

    private void MoveSquadPosition(GameObject squadToMove, GameObject newBoardTile)
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

    public void DefineSquadToPlaceFromUI(string prefabAddress)
    {
        //this method is used to receive a message from the squad placement UI, telling it the prefab address of the next squad type to be placed. 
        squadToPlacePrefabAddress = prefabAddress;
        placementState = "PlacingSquad";
        DeselectPreviousSquad();
    }

    /// <summary>
    /// Tell the camera where to move to, and indicate if the system should wait for the movement to end before continuing to read inputs.
    /// </summary>
    /// <param name="cameraPhase">See CameraControl for a list of valid values camera phases</param>
    /// <param name="waitUntilComplete">If true, will pause untiul the camera stops moving. Else will return control immediately while the camera is in motion.</param>
    /// <returns></returns>
    private IEnumerator AnimateCamera(string cameraPhase, bool waitUntilComplete)
    {
        ConsolePrint("Camera animation requested - " + cameraPhase);
        cameraControlScript.SetCameraGamePhase(cameraPhase);
        if (waitUntilComplete)
        {
            while (cameraControlScript.animating)
            {
                yield return new WaitForSeconds(0.1f);
            }
        }
        else
            yield break;

    }//begin army placement 



    /*****************
     * ROUND CONTROL *
     *****************/

    private void QuickStart()
    {
        //This method is used so that units will be automatically placed on the board, for debug testing. 
        gamePhase = GamePhase.QuickStart;
        //disable the unit placement hud
        hudControlScript.HideSquadPlacementPanel();

        int thisPlayerId = 0;
        // PLAYER 1 SETUP
        GameObject nextBoardTile = ReferenceCenterTiles[thisPlayerId];//first row center
        #region
        //infantry
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Infantry, 4, GameSettings.infantry4PrefabAddress);
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 1
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Infantry, 4, GameSettings.infantry4PrefabAddress);
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 2
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Infantry, 4, GameSettings.infantry4PrefabAddress);
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 3
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Infantry, 4, GameSettings.infantry4PrefabAddress);
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 4
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Archer, 4, GameSettings.archer4PrefabAddress);

        //Mercenaries
        nextBoardTile = ReferenceCenterTiles[thisPlayerId].GetComponent<BoardTileScript>().adjacentTileBottom;//Center - 1
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Mercenary, 2, GameSettings.mercenary2PrefabAddress);

        //Peasants
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileBottom;//center - 2
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Peasant, 4, GameSettings.peasant4PrefabAddress);

        //HeavyInfantry
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileBottom;//center - 3
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.HeavyInfantry, 2, GameSettings.heavyInfantry2PrefabAddress);


        //archers
        nextBoardTile = ReferenceCenterTiles[thisPlayerId].GetComponent<BoardTileScript>().adjacentTileRight; //second row center
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Infantry, 4, GameSettings.infantry4PrefabAddress);
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +1
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Archer, 4, GameSettings.archer4PrefabAddress);

        //knights
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +2
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Knight, 2, GameSettings.knight2PrefabAddress);
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +3
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Knight, 2, GameSettings.knight2PrefabAddress);

        //king
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +4
        playerScripts[0].kingSquadScript = CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.King, 1, GameSettings.king1PrefabAddress).GetComponent<SquadScript>();

        #endregion//player 2 instantiation code folder

        // PLAYER 2 SETUP
        thisPlayerId = 1;
        nextBoardTile = ReferenceCenterTiles[thisPlayerId];//first row center
        #region
        //infantry
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Infantry, 4, GameSettings.infantry4PrefabAddress);
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 1
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Infantry, 4, GameSettings.infantry4PrefabAddress);
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 2
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Infantry, 4, GameSettings.infantry4PrefabAddress);
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 3
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Infantry, 4, GameSettings.infantry4PrefabAddress);
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center + 4
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Archer, 4, GameSettings.archer4PrefabAddress);

        //Mercenary
        nextBoardTile = ReferenceCenterTiles[thisPlayerId].GetComponent<BoardTileScript>().adjacentTileBottom;//Center - 1
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Mercenary, 2, GameSettings.mercenary2PrefabAddress);

        //Peasants
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileBottom;//center - 2
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Peasant, 4, GameSettings.peasant4PrefabAddress);

        //HeavyInfantry
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileBottom;//center - 3
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.HeavyInfantry, 2, GameSettings.heavyInfantry2PrefabAddress);

        //archers
        nextBoardTile = ReferenceCenterTiles[thisPlayerId].GetComponent<BoardTileScript>().adjacentTileLeft; //second row center
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Infantry, 4, GameSettings.infantry4PrefabAddress);
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +1
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Archer, 4, GameSettings.archer4PrefabAddress);

        //knights
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +2
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Knight, 2, GameSettings.knight2PrefabAddress);
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +3
        CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.Knight, 2, GameSettings.knight2PrefabAddress);

        //king
        nextBoardTile = nextBoardTile.GetComponent<BoardTileScript>().adjacentTileTop;//center +4
        playerScripts[1].kingSquadScript = CreateAndPlaceSquad(nextBoardTile, thisPlayerId, SoldierClass.King, 1, GameSettings.king1PrefabAddress).GetComponent<SquadScript>();
        #endregion//player 2 instantiation code folder

        //final safety cleanup
        currentSelectedSquad = null;
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
            gamePhase = GamePhase.PlaceArmyP1;
            placementState = "Idle";
            StartCoroutine(AnimateCamera("PlayerOneSetup", true));
            hudControlScript.InitializeSquadPlacementUI(activePlayerID);
        }
        else if (playerID == 1)
        {
            activePlayerID = 1;
            gamePhase = GamePhase.PlaceArmyP2;
            placementState = "Idle";
            StartCoroutine(AnimateCamera("PlayerTwoSetup", true));
            hudControlScript.InitializeSquadPlacementUI(activePlayerID);
        }

    }//begin army placement 

    private void BeginGame()
    {
        ConsolePrint("Beginning Game");
        gamePhase = GamePhase.Active;
        StartCoroutine(AnimateCamera("ActiveGame", false));
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
        ConsolePrint("Action complete called for Player " + activePlayerID
                        + " (" + GameSettings.playerNames[activePlayerID]
                        + ") with " + playerScripts[activePlayerID].apRemaining
                        + " AP Remaining, in game phase " + gamePhase);
        actionTaken = false;

        switch (gamePhase)
        {
            case GamePhase.PlaceArmyP1:
            case GamePhase.PlaceArmyP2:
                //the only animation in army placement is the squad relocation mechanic. 
                rotationArrowControlScript.AssignToSquad(currentSelectedSquad);
                break;
            case GamePhase.Active:
            case GamePhase.Attacking:
            case GamePhase.QuickStart:
            case GamePhase.RollForFirst:
                //reenable the hud, in case it was hidden for diceroll/animation
                hudControlScript.SetTurnDataPanelVisibility(true);
                hudControlScript.SetInfoPanelVisibility(true);
                hudControlScript.SetTurnData(activePlayerID, playerScripts[activePlayerID].apRemaining);

                //reset all tile highlight effects and attackable/movable flags
                ResetAllTileFlags();

                if (cameraControlScript.combatCameraActive)
                    cameraControlScript.DisableCombatCamera();

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
                gamePhase = GamePhase.Active;
                break;
            case GamePhase.GameOver:
                //reset all tile highlight effects and attackable/movable flags
                ResetAllTileFlags();
                break;
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
        ConsolePrint("GameOver called.");
        //play sfx for gameover 
        audioControlScript.GameOver();
        gamePhase = GamePhase.GameOver;
        DeselectPreviousSquad();
        gameOverPanelScript.panelHider.ToggleVisibility();
        lightControlScript.SetTimeCycleMode(TimeCycleMode.Pause);
    }

    /// <summary>
    /// Called if the user clicks Rematch after the conclusion of a game. 
    /// Initiates a reinitialization of all game parameters and returns to army placement phase. 
    /// </summary>
    public void ResetGame()
    {
        ConsolePrint("ResetGame called.");
        gameOverPanelScript.panelHider.ToggleVisibility();
        playerScripts[0].Reset();
        playerScripts[1].Reset();
        ClearAllUnits();
        lightControlScript.SetTimeCycleMode(GameSettings.timeCycleMode);    //restart time cycling, as this paused during gameover
        activePlayerID = 0;
        if (GameSettings.gameMode == GameMode.QuickStart)
            QuickStart();
        else
        {
            BeginArmyPlacement(activePlayerID);
        }
    }



    /********************
     * EXTERNAL REPORTS *
     ********************/

    public void ReportSquadElimenated(GameObject elimenatedSquad)
    {
        ConsolePrint("Squad Elimenation report received.");
        //collect information
        SquadScript elimenatedSquadControlScript = elimenatedSquad.GetComponent<SquadScript>();
        int associatedPlayerID = elimenatedSquadControlScript.ownerID;
        //tell the player a squad was lost
        playerScripts[associatedPlayerID].SquadLost(elimenatedSquadControlScript.soldierClassAttributes.soldierClass);
        //tell the tile that the tile is now vacant
        if (elimenatedSquad == currentSelectedSquad)
        {
            currentSelectedSquad = null;
            rotationArrowControlScript.HideArrows();
        }
        //destroy the squad object 
        Destroy(elimenatedSquad, GameSettings.deathLingerDuration);
    }//ReportElimenatedSquad

    public void ReportPlayerDefeated(int defeatedPlayerID)
    {
        ConsolePrint("Player Defeated report received.");
        gamePhase = GamePhase.GameOver;
        DeselectPreviousSquad();
        hudControlScript.PrintMessage(GameSettings.playerNames[(defeatedPlayerID + 1) % 2] + " wins!");
        Invoke("GameOver", 5);
    }//Report player defeated

    /// <summary>
    /// Called by an attacker script in order to signal to the control script that the defender 
    /// should immediately react (deflect animation or death animation)
    /// </summary>
    public void ReportAttackHit()
    {
        ConsolePrint("Attack animation start report received");
        waitingForAttack = false;
    }

    /// <summary>
    /// Called by a defender squad in order to signal that it has finished its block animation and the attacker can proceed
    /// </summary>
    public void ReportBlockAnimationComplete()
    {
        ConsolePrint("Block animation start report received");
        waitingForDefense = false;
    }



    /******************
     * ATTACK METHODS *
     ******************/

    private void Attack(GameObject attacker, GameObject defender, bool isBonusRoll)
    {
        ConsolePrint("Initiating attack.");
        gamePhase = GamePhase.Attacking;
        hudControlScript.SetTurnDataPanelVisibility(false);
        hudControlScript.SetInfoPanelVisibility(false);
        diceRollIsValid = true;
        hudControlScript.ToggleDiceRollButtonVisibility();
        if (!isBonusRoll)
        {
            ConsolePrint("Not a bonus roll.");
            //create combat object to house related data  if not a reroll
            combatData = new CombatData(attacker, defender);
            hudControlScript.PrintMessage(GameSettings.playerNames[activePlayerID] + " - Press SPACE to roll when ready. Attacking with " + combatData.attackType + " dice.");
        }
        else
        {
            hudControlScript.PrintMessage("Bonus roll earned! Roll successfully a second time to kill the armored target.");
            ConsolePrint("Bonus roll attack beginning.");
        }

        StartCoroutine(AnimateCamera("DiceRoll", true));
    }//attack

    private IEnumerator ResolveAttack()
    {
        ConsolePrint("Resolving attack dealing " + combatData.damageDealt + " damage, with " + dieSpawnerScript.diceData.panicCount + " panics rolled.");

        //handle bonus rolls
        if (combatData.bonusRollEarned == true)
        { //the bonus roll deals double damage, as it means that this is the second throw for a small unit against a large
            ConsolePrint("Bonus attack processed, doubling any damage.");
            combatData.damageDealt *= 2;
            combatData.bonusRollEarned = false;
            dieSpawnerScript.diceData.panicCount = 0;   //panics do not apply on the bonus roll
        }
        else if (combatData.damageDealt == 1 && combatData.bonusRollEligible)
        {
            ConsolePrint("Bonus roll earned.");
            combatData.bonusRollEarned = true;
            combatData.bonusRollEligible = false;   //prevent a second bonus roll
            combatData.damageDealt = 0; //do no damage on this roll, perform bonus roll instead
        }

        //if damage was done, perform attack animations
        if (combatData.damageDealt > 0)
        {
            cameraControlScript.EnableCombatCamera(combatData.attackerSquad.gameObject, combatData.defenderSquad.gameObject);
            //trigger the defense animation for the defender, and wait for it to complete
            combatData.defenderSquadScript.Defend(combatData.vectorToAttacker);
            waitingForDefense = true;
            while (waitingForDefense)
            {
                yield return new WaitForSeconds(0.1f);
            }

            //tell the attackers to animate an attack, and the defender to take damage 
            combatData.attackerSquadScript.Attack(combatData.defenderSquad);
            waitingForAttack = true;
            while (waitingForAttack)
            {
                yield return new WaitForSeconds(0.1f);
            }
            combatData.defenderSquadScript.TakeDamage(combatData.damageDealt, combatData.attackerSquadScript.unitSFXLibrary);

            //tell any remaining defender units to go back to idle
            yield return new WaitForSeconds(0.5f);
            combatData.defenderSquadScript.Idle();
        }

        //handle panic events, if any defenders remain
        if (combatData.defenderSquadScript.unitsRemaining > 0)
        {
            switch (combatData.panicResult)
            {
                case "AttackerPanics":
                    ConsolePrint("Attacker panicks");
                    StartCoroutine(AnimateCamera("ActiveGame", true));
                    StartCoroutine(combatData.attackerSquadScript.Panic(combatData.panicDistance, (combatData.attackerSquadScript.orientationIndex + 2) % 4));
                    DeselectPreviousSquad();
                    break;
                case "DefenderPanics":
                    ConsolePrint("Defender panicks");
                    StartCoroutine(AnimateCamera("ActiveGame", true));
                    StartCoroutine(combatData.defenderSquadScript.Panic(combatData.panicDistance, combatData.attackerSquadScript.orientationIndex));
                    break;
                default:
                    //no panic occurred (insufficient panics rolled, or unit can not panic)
                    break;
            }//panic switch
        }//if defenders remain

        //return the camera/gamePhase to active if no bonus roll earned, signifying end of action.
        if (!combatData.bonusRollEarned)
        {
            while (SquadsAreAnimating())
            {
                yield return new WaitForSeconds(0.1f);
            }
            if (gamePhase != GamePhase.GameOver)
            {
                StartCoroutine(AnimateCamera("ActiveGame", true));
                gamePhase = GamePhase.Active;
            }
        }
        else //trigger a repeat attack roll, classified as a "bonus"
            Attack(combatData.attackerSquad, combatData.defenderSquad, true);
    }//resolve attack



    /****************
     * CLICK EVENTS *
     ****************/

    public void SquadClicked(GameObject selectedSquad)
    {
        ConsolePrint(selectedSquad.name + " clicked.");

        SquadScript selectedSquadScript = selectedSquad.GetComponent<SquadScript>();

        //pass off control management to he board tile clicked method
        BoardTileClicked(selectedSquadScript.occupiedGameTile);
    }//squad selected

    public void BoardTileClicked(GameObject selectedBoardTile)
    {
        ConsolePrint(selectedBoardTile.name + " click detected by control script in phase " + gamePhase);
        BoardTileScript selectedTileControlScript = selectedBoardTile.GetComponent<BoardTileScript>();
        switch (gamePhase)
        {
            case GamePhase.PlaceArmyP1:
            case GamePhase.PlaceArmyP2:
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
                            if (hudControlScript.selectedSquadType == SoldierClass.King)
                            {
                                ConsolePrint("Assigning king for " + gamePhase);
                                if (gamePhase == GamePhase.PlaceArmyP1)
                                    playerScripts[0].kingSquadScript = newSquad.GetComponent<SquadScript>();
                                else
                                    playerScripts[1].kingSquadScript = newSquad.GetComponent<SquadScript>();
                            }

                            //deselect squad after placing in order to do multi-drop
                            DeselectPreviousSquad();
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

            case GamePhase.Active:
                ConsolePrint("BoardTileClicked - ActiveGame case.");
                if (currentSelectedSquad == null)
                {
                    //ConsolePrint("Prior selected squad was null.");
                    if (selectedTileControlScript.occupyingSquad == null)
                    {
                        //No Squad selected, and clicked tile is empty. ignore.
                        ConsolePrint("No Squad selected, and clicked tile is empty. Click ignored");
                    }
                    else
                    {
                        GameObject clickedSquad = selectedTileControlScript.occupyingSquad;
                        if (clickedSquad.GetComponent<SquadScript>().ownerID != activePlayerID)
                        {
                            //ignore command, selected squad doesnt belong to the current player.
                            ConsolePrint("Enemy Squad clicked, invalid. Click ignored");
                        }
                        else
                        {//player has clicked on one of their own squad tiles
                            audioControlScript.GeneralButtonClick();
                            currentSelectedSquad = clickedSquad;
                            infoPanelScript.SetData(currentSelectedSquad.GetComponent<SquadScript>().soldierClassAttributes);
                            currentSelectedSquad.GetComponent<SquadScript>().SetSelected(true);
                            rotationArrowControlScript.AssignToSquad(currentSelectedSquad);
                        }//player has clicked on one of their own squad tiles
                    }//tile was occupied
                }//no squad currently selected
                else
                {
                    SquadScript currentSelectedSquadScript = currentSelectedSquad.GetComponent<SquadScript>();

                    if (selectedTileControlScript.occupyingSquad == null)
                    {
                        //clicked tile is empty
                        if (selectedTileControlScript.validMoveTarget)
                        {
                            int apRequired = currentSelectedSquadScript.soldierClassAttributes.apCostToMove;
                            int apRemaining = playerScripts[activePlayerID].apRemaining;
                            if (apRemaining < apRequired)
                            {
                                audioControlScript.ErrorClick();
                                hudControlScript.PrintMessage("Insufficient AP for this action.");
                            }
                            else
                            {
                                //decrease the player's AP
                                actionTaken = true;
                                audioControlScript.GeneralButtonClick();
                                rotationArrowControlScript.HideArrows();
                                playerScripts[activePlayerID].ConsumeAP(apRequired);
                                ResetAllTileFlags();
                                MoveSquadPosition(currentSelectedSquad, selectedBoardTile);
                            }//MOVE
                        }//valid move target
                        else
                        {
                            //the clicked tile is not a valid move target, ignore command.
                        }
                    }//clicked tile is empty
                    else
                    {
                        //clicked tile is NOT empty
                        GameObject clickedSquad = selectedTileControlScript.occupyingSquad;

                        if (clickedSquad.GetComponent<SquadScript>().ownerID != activePlayerID)
                        {
                            //targeting an enemy tile
                            ConsolePrint("BoardTileClicked - attack logic.");
                            //check if the tile is a valid attack target 
                            if (selectedTileControlScript.validAttackTarget)
                            {
                                //attack requested
                                //check if we have enough AP 
                                int apRequired = currentSelectedSquadScript.soldierClassAttributes.apCostToAttack;
                                int apRemaining = playerScripts[activePlayerID].apRemaining;
                                if (apRemaining < apRequired)
                                {
                                    audioControlScript.ErrorClick();
                                    hudControlScript.PrintMessage("Insufficient AP for this action.");
                                }
                                else
                                {  //begin attack mechanic
                                    actionTaken = true;
                                    audioControlScript.GeneralButtonClick();
                                    rotationArrowControlScript.HideArrows();
                                    //decrease the player's AP
                                    playerScripts[activePlayerID].ConsumeAP(apRequired);
                                    //attack
                                    ResetAllTileFlags();
                                    Attack(currentSelectedSquad, clickedSquad, false);
                                }// attack
                            }//in attack range 
                        }//enemy targeted
                        else
                        {  //clicked a different friendly squad, shift focus there
                            ConsolePrint("BoardTileClicked - change selection logic.");
                            audioControlScript.GeneralButtonClick();
                            DeselectPreviousSquad();
                            currentSelectedSquad = clickedSquad;
                            infoPanelScript.SetData(currentSelectedSquad.GetComponent<SquadScript>().soldierClassAttributes);
                            currentSelectedSquad.GetComponent<SquadScript>().SetSelected(true);
                            rotationArrowControlScript.AssignToSquad(currentSelectedSquad);
                        }//player has clicked on one of their own squad tiles
                    }//tile was occupied
                }//player had already selected a squad to issue orders to 
                break;
        }//phase switch 
    }//board tile clicked

    public void RotationArrowClicked(GameObject arrow)
    {
        ConsolePrint("Rotation arrow clicked.");
        RotationArrowControl arrowControl = arrow.transform.parent.GetComponent<RotationArrowControl>();
        string rotationDirection = arrowControl.GetDirection(arrow);

        switch (gamePhase)
        {
            case GamePhase.PlaceArmyP1:
            case GamePhase.PlaceArmyP2:
                //find out which way to rotate
                //tell the associated squad to turn. (no validations around AP remianing in this phase)
                audioControlScript.RotationArrowClick();
                arrowControl.associatedSquad.GetComponent<SquadScript>().RotateSquad(rotationDirection);
                break;

            case GamePhase.Active:
                int apRequired = currentSelectedSquad.GetComponent<SquadScript>().soldierClassAttributes.apCostToRotate;
                int apRemaining = playerScripts[activePlayerID].apRemaining;
                if (apRemaining < apRequired)
                {
                    audioControlScript.ErrorClick();
                    hudControlScript.PrintMessage("Insufficient AP for this action");
                }
                else
                {
                    actionTaken = true;
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
    }//rotation arrow clicked

    public void PlayerArmyPlacementComplete()
    {
        audioControlScript.GeneralButtonClick();
        DeselectPreviousSquad();
        //called from HUD when the player has hit "PlacementComplete" in the HUD, after placing all squads.
        if (activePlayerID == 0) //first player finished, second player must begin
        {
            armyHider.ToggleVisibility();
            BeginArmyPlacement(1);
        }
        else //all players finished, ready to start game 
        {
            armyHider.ToggleVisibility();
            RollForTurnOrder();
        }
    }//playerArmyPlacementComplete



    /*******************
     * DICE MANAGEMENT *
     *******************/

    private void RollForTurnOrder()
    {
        activePlayerID = 0;
        //set phase
        gamePhase = GamePhase.RollForFirst;
        //hide the squad placement panel 
        hudControlScript.HideSquadPlacementPanel();
        //set camera to Dice Roll angle mode
        StartCoroutine(AnimateCamera("DiceRoll", true));
        //give the player instructions
        hudControlScript.PrintMessage("Roll to see who goes first! " + GameSettings.playerNames[0] + ", hit SPACE to roll when ready.");
        //enable dice rolls
        diceRollIsValid = true;
        hudControlScript.ToggleDiceRollButtonVisibility();
    }

    public void DiceRollBegin()
    {
        //disable additional dice rolls
        diceRollIsValid = false;
        hudControlScript.ToggleDiceRollButtonVisibility();

        //determine how many dice to roll
        int dieCount = 0;
        if (gamePhase == GamePhase.RollForFirst)
            dieCount = 4;
        else
            dieCount = combatData.diceAvailable;
        ConsolePrint("Rolling " + dieCount + " dice.");

        //pass control to die spawner
        dieSpawnerScript.RollDice(dieCount);
    }

    /// <summary>
    /// Called by the DieSpawner script when all dice have finished rolling and results are ready to be read.
    /// </summary>
    public void RollCompletedReport()
    {
        ConsolePrint("Roll completion reported to control script");
        StartCoroutine(DiceRollEnd());
    }

    /// <summary>
    /// Called by the RollCompletedReport in order to begin processing the results of a dice roll
    /// </summary>
    private IEnumerator DiceRollEnd()
    {
        yield return new WaitForSeconds(2f);
        switch (gamePhase)
        {
            case GamePhase.RollForFirst:
                #region
                playerScore[activePlayerID] = dieSpawnerScript.diceData.axeCount;
                switch (activePlayerID)
                {
                    case 0:
                        hudControlScript.PrintMessage(GameSettings.playerNames[0] + " scored a " + dieSpawnerScript.diceData.axeCount + ". " + GameSettings.playerNames[1] + " Press SPACE to roll.");
                        activePlayerID = 1;
                        diceRollIsValid = true;
                        hudControlScript.ToggleDiceRollButtonVisibility();
                        break;
                    case 1:
                        if (playerScore[0] > playerScore[1])
                        {
                            hudControlScript.PrintMessage(GameSettings.playerNames[1] + " scored a " + dieSpawnerScript.diceData.axeCount + ". " + GameSettings.playerNames[0] + " wins and will go first.");
                            activePlayerID = 0;
                            Invoke("BeginGame", 4);
                        }
                        else if (playerScore[0] < playerScore[1])
                        {
                            hudControlScript.PrintMessage(GameSettings.playerNames[1] + " scored a " + dieSpawnerScript.diceData.axeCount + ". " + GameSettings.playerNames[1] + " wins and will go first.");
                            activePlayerID = 1;
                            Invoke("BeginGame", 4);
                        }
                        else
                        {
                            hudControlScript.PrintMessage(GameSettings.playerNames[1] + " scored a " + dieSpawnerScript.diceData.axeCount + ". " + "Tie! " + GameSettings.playerNames[0] + ", roll again.");
                            activePlayerID = 0;
                            diceRollIsValid = true;
                            hudControlScript.ToggleDiceRollButtonVisibility();
                        }
                        break;
                }
                #endregion
                break;
            case GamePhase.Attacking:
                ConsolePrint("DiceRollEnd called during an Attack.");
                combatData.EvaluateDamage(dieSpawnerScript.diceData);
                //report the results
                StartCoroutine(ResolveAttack());
                if (!combatData.bonusRollEarned)
                    hudControlScript.DismissBanner();
                break;
        }//gamephase switch
    }//dice roll end



    /*************
     * UTILITIES *
     *************/

    private void CheckKeyboardCommands()
    {
        //check if the user has pressed any keys that mve the camera
        if (Input.GetKeyUp(KeyCode.C))
        {
            //move camera to position the next in the current locations array
            cameraControlScript.MoveToNextLocation();
        }

        if (Input.GetKeyUp(KeyCode.Space))
        {
            if ((gamePhase == GamePhase.RollForFirst || gamePhase == GamePhase.Attacking) && diceRollIsValid)
            {
                DiceRollBegin();
            }//valid to roll 
        }//space pressed


        //TODO: Disable the next line for real gameplay
        CheckDebugCommands();

    }//CheckKeyboardCommands

    private GameObject CreateAndPlaceSquad(GameObject boardTile, int playerID, SoldierClass squadType, int squadSize, string prefabAddress)
    {
        GameObject newSquad = Instantiate(Resources.Load<GameObject>(prefabAddress), boardTile.transform.position, boardTile.transform.rotation);
        string orientation = "Right";
        if (playerID == 1)
        {
            newSquad.transform.Rotate(0, 180, 0);
            orientation = "Left";
        }
        //define script parameters
        newSquad.GetComponent<SquadScript>().DefineSquad(playerID, squadType, squadSize, orientation, boardTile);
        //tell the board tile that it is now occupied by this unit 
        boardTile.GetComponent<BoardTileScript>().PlaceSquad(newSquad);
        return newSquad;
    }

    public void ResetAllTileFlags()
    {
        //clears all highlights and markers for movable / attackable targets on the board
        ConsolePrint("Clearing all highlights");

        GameObject gameBoard = GameObject.FindGameObjectWithTag("Board");
        BoardTileScript[] tileControls = gameBoard.GetComponentsInChildren<BoardTileScript>();
        foreach (BoardTileScript script in tileControls)
        {
            script.ClearAllHighlights();
        }
    }//ResetAllTileFlags

    public void AssignTeamColors()
    {
        //assigns the selected team color for each player to the skins of all of their soldiers.
        List<UnitScript> p1Units = new List<UnitScript>();
        List<UnitScript> p2Units = new List<UnitScript>();

        //find all unit scripts in the game
        UnitScript[] allUnits = FindObjectsOfType(typeof(UnitScript)) as UnitScript[];

        //loop over all squads and sort into team lists
        foreach (UnitScript thisUnit in allUnits)
        {
            if (thisUnit.parentSquadScript.ownerID == 0)
                p1Units.Add(thisUnit);
            else
                p2Units.Add(thisUnit);
        }


        //in quickstart mode, this code fires before GameSettings.Awake(), so some features need to be manually initialized.
        if (GameSettings.armyRaces == null)
            GameSettings.DefineArmies();

        //loop over each list and apply the team color to all child scripts of type 
        foreach (UnitScript thisUnit in p1Units)
        {
            thisUnit.SetColor(GameSettings.armyRaces[GameSettings.playerRaces[0]].armyColor);
        }
        foreach (UnitScript thisUnit in p2Units)
        {
            thisUnit.SetColor(GameSettings.armyRaces[GameSettings.playerRaces[1]].armyColor);
        }
    }

    private bool SquadsAreAnimating()
    {
        SquadScript[] allSquads = FindObjectsOfType(typeof(SquadScript)) as SquadScript[];
        foreach (SquadScript thisSquad in allSquads)
        {
            if (thisSquad.IsTheSquadAnimating())
            {
                //ConsolePrint(thisSquad.ToString() + " Is animating");
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Called by the Reset. Will locate all squad scripts and command them to die, then destroy them, and release their tile.
    /// </summary>
    private void ClearAllUnits()
    {
        foreach (GameObject thisSquad in GameObject.FindGameObjectsWithTag("Squad"))
            thisSquad.GetComponent<SquadScript>().Reset();
        foreach (GameObject thisUnit in GameObject.FindGameObjectsWithTag("Unit"))
            Destroy(thisUnit, 0.5f);
        ResetAllTileFlags();
    }



    /***************
     * DEBUG STUFF *
     ***************/

    private void CheckDebugCommands()
    {
        if (Input.GetKeyUp(KeyCode.P))
        {
            if (currentSelectedSquad != null)
            {
                cameraControlScript.EnableCombatCamera(currentSelectedSquad.gameObject, currentSelectedSquad.gameObject);
            }
        }//p pressed

        //these are keyboard shortcuts that should be disabled in real gameplay. 
        if (Input.GetKeyUp(KeyCode.A))
        {
            //move camera to position the next in the current locations array
            cameraControlScript.SetCameraGamePhase("ActiveGame");
        }

        if (Input.GetKeyUp(KeyCode.Alpha1))
        {
            //move camera to position the next in the current locations array
            cameraControlScript.SetCameraGamePhase("PlayerOneSetup");
        }

        if (Input.GetKeyUp(KeyCode.Alpha2))
        {
            //move camera to position the next in the current locations array
            cameraControlScript.SetCameraGamePhase("PlayerTwoSetup");
        }

        if (Input.GetKeyUp(KeyCode.R))
        {
            //move camera to position the next in the current locations array
            DiceRollBegin();
        }

        if (Input.GetKeyUp(KeyCode.F))
        {
            ConsolePrint("Start F response");
            ConsolePrint("End F response");

        }

    }//check debug commands

    private void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("GameControl - " + message);
        }
    }//console print

    private void PrintSquad(GameObject squad)
    {
        SquadScript selectedSquadScript = squad.GetComponent<SquadScript>();
        string message = "Squad data - CLICK HERE!! \n" +
            "PlayerID: " + selectedSquadScript.ownerID.ToString() + "\n" +
            "Unit Type: " + selectedSquadScript.soldierClassAttributes.soldierClass + "\n" +
            "Unit Count: " + selectedSquadScript.unitsRemaining.ToString() + "\n";
        ConsolePrint(message);
    }

}//class
