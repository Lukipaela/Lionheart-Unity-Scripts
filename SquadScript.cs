using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquadScript : MonoBehaviour {
    public int ownerID;    //which player owns this squad 
    public int orientationIndex;   //corresponds to the orientations array in GameSettings. Indicates which direction this squad is facing
    public GameObject occupiedGameTile; //the tile which this unit is currently standing on 

    public SoldierClassData soldierClassData;
    public GameControl gameControlScript;
    private bool isSelected = false;
    private bool isBlocking = false; //indicates if the most recent animation requested was a block. Allows troops to rotate toward enemy and back again after surviving

    //animation controls
    private bool isRotating = false;
    private bool isMoving = false;
    private bool squadTileIsAnimating = false;
    private bool unitsAreAnimating = false;
    private int activeUnitAnimations = 0;   //tracks how many units are still animating 
    private float rotationSpeed = 2.3f;  //holds the actual value used in animation, written to by one of the below constants
    private float movementSpeed = 1.3f;  //holds the actual value used in animation, written to by one of the below constants
    private readonly float setupRotationSpeed = 8;
    private readonly float activeGameRotationSpeed = 2.2f;
    private readonly float setupMovementSpeed = 5;
    private readonly float activeGameMovementSpeed = 1.5f;
    private Vector3 rotationTargetVector;
    private Vector3 movementTargetVector;
    private List<AnimationTask> animationQueue;
    private AnimationTask currentAnimationTask; //holds the details of whichever animation task is currently active

    //debug
    private static readonly bool enableDebugging = true;


    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start() {
        animationQueue = new List<AnimationTask>();
        gameControlScript = GameObject.FindGameObjectWithTag("GameControl").GetComponent<GameControl>();
    }

    void Update() {
        if (isRotating) {
            RotationFrameUpdate(gameObject, rotationTargetVector);
        }//rotating logic

        if (isMoving) {
            MovementFrameUpdate(gameObject, currentAnimationTask.targetVector);
        }//moving logic

        if (!isMoving && !isRotating && !unitsAreAnimating) {
            CheckAnimationQueue();
        }//no longer animating
    }//Update 


    /******************
     * CUSTOM METHODS *
     ******************/

    public void DefineSquad(int ownerID, string squadType, int unitsInSquad, string facingDirection, GameObject location) {
        //called after instantiation, to initialize the tile's parameters (type, count, etc)
        this.ownerID = ownerID;
        soldierClassData.unitClass = squadType;

        //all squads are initially placed facing one of two directions, then can later be rotated if desired. 
        if (facingDirection == "Left")
            orientationIndex = 3;
        else
            orientationIndex = 1;

        occupiedGameTile = location;
        soldierClassData.InitializeSquad(squadType, unitsInSquad);
    }

    public void TakeDamage(int damage) {
        //total up units lost, requiring 2 damage to kill a large unit,and clamping to a max of UnitsInSquad
        int unitsLost = Mathf.Min(damage / soldierClassData.healthPerUnit, soldierClassData.unitsInSquad);
        soldierClassData.unitsInSquad -= unitsLost;

        if (unitsLost > 0)
            gameControlScript.ReportAnimationStart("Squad death animations beginning for - Team " + ownerID + ", " + gameObject.name);

        GameObject[] unitsKilled = new GameObject[4];   //max of 4 units can be killed in a single attack TODO: UPDATE THIS FOR MASSIVE ARMY MODE
        //loop over units in squad for unitsLost iterations and kill one per iteration
        for (int i = 0; i < unitsLost; i++) {
            unitsKilled[i] = transform.GetChild(i + 1).gameObject;//offset index by 1, as child 0 is the base, not a unit
        }

        //TODO: retool this to call the block particle effect on each survivor
        foreach (GameObject unit in unitsKilled) {
            if (unit != null)
                unit.GetComponent<UnitScript>().AddAnimationToQueue("Die", Vector3.one);
        }

        if (soldierClassData.unitsInSquad == 0)
            SquadLost();
    }//TakeDamage

    private void SquadLost() {
        ConsolePrint("Squad lost - " + gameObject.name);
        gameControlScript.ReportSquadElimenated(gameObject);
        occupiedGameTile.GetComponent<BoardTileScript>().ClearTile();
    }//squad lost


    /*************
     * ANIMATION *
     *************/

    private void CheckAnimationQueue() {
        // if not currently doing anything, check the animation queue for a new action. 
        if (animationQueue.Count > 0) {
            //ConsolePrint("Pulling task from animation queue with SquadTileIsAnimating = " + squadTileIsAnimating + ", and activeUnitAnimations = " + activeUnitAnimations);
            if (!squadTileIsAnimating && activeUnitAnimations == 0) {
                ConsolePrint("Check Animation Queue startup logic hit.");
                gameControlScript.ReportAnimationStart("Beginning animation queue from squad - Team " + ownerID + ", " + gameObject.name);
                squadTileIsAnimating = true;
            }

            currentAnimationTask = animationQueue[0];
            animationQueue.RemoveAt(0);
            ConsolePrint("Moving to animation: " + currentAnimationTask.animationType);
            switch (currentAnimationTask.animationType) {
                case "Block":
                    //rotate towards attacker, then block
                    isBlocking = true;
                    AnimateSquad(soldierClassData.unitsInSquad, "Rotate", currentAnimationTask.targetVector);
                    AnimateSquad(soldierClassData.unitsInSquad, "Block", Vector3.one);
                    break;

                case "Cheer":
                    AnimateSquad(soldierClassData.unitsInSquad, "Cheer", Vector3.one);
                    break;

                case "Die":
                    AnimateSquad(soldierClassData.unitsInSquad, "Die", Vector3.one);
                    break;

                case "Idle":
                    if (isBlocking) {
                        //if we were blocking before, rotate back forwards before going to idle
                        isBlocking = false;
                        AnimateSquad(soldierClassData.unitsInSquad, "Rotate", transform.forward);
                    }
                    AnimateSquad(soldierClassData.unitsInSquad, "Idle", Vector3.one);
                    break;

                case "MoveSquad":
                    isMoving = true;
                    movementTargetVector = currentAnimationTask.targetVector;
                    AnimateSquad(soldierClassData.unitsInSquad, "March", Vector3.one);
                    break;

                case "RotateSquad":
                    isRotating = true;
                    rotationTargetVector = currentAnimationTask.targetVector;
                    break;

                case "RotateUnits":
                    AnimateSquad(soldierClassData.unitsInSquad, "Rotate", currentAnimationTask.targetVector);
                    break;

                case "SqaudAttack":
                    GenerateAttackAnimationQueue(currentAnimationTask);
                    break;

                case "SquadLost":
                    //short circuit the animation completion message, as this unit is about to be destroyed
                    gameControlScript.ReportSquadElimenated(gameObject);
                    gameControlScript.ReportAnimationComplete("Team " + ownerID + ", " + gameObject.name + " - Squad Lost animation task");
                    break;

            }//animation type switch 
        }//animations remain in queue
        else if (squadTileIsAnimating) {
            squadTileIsAnimating = false;
            if (activeUnitAnimations == 0)
                gameControlScript.ReportAnimationComplete("Team " + ownerID + ", " + gameObject.name + " - End of animation queue");
        }
    }//check animation queue

    private void RotationFrameUpdate(GameObject rotatingObject, Vector3 desiredForwardVector) {
        //if we have reached our target orientation, stop rotating.
        if (Vector3.Angle(rotatingObject.transform.forward, desiredForwardVector) < 1) {
            isRotating = false;
            //pre-emptively mark units as animating, as we are about to issue them a command which may take a frame to activate
            unitsAreAnimating = true;
            AnimateSquad(soldierClassData.unitsInSquad, "Idle", Vector3.one);
        }
        else {
            // Rotate the forward vector towards the target direction by one step
            Vector3 newDirection = Vector3.RotateTowards(rotatingObject.transform.forward, desiredForwardVector, rotationSpeed * Time.deltaTime, 0.0f);
            // Calculate a rotation a step closer to the target and applies rotation to this object
            rotatingObject.transform.rotation = Quaternion.LookRotation(newDirection);
        }

    }//RotationFrameUpdate

    private void MovementFrameUpdate(GameObject movingObject, Vector3 desiredLocation) {
        movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, desiredLocation, movementSpeed * Time.deltaTime);
        if (movingObject.transform.position == desiredLocation) {
            isMoving = false;
            occupiedGameTile.GetComponent<BoardTileScript>().PlaceSquad(gameObject);
            unitsAreAnimating = true;
            AnimateSquad(soldierClassData.unitsInSquad, "Idle", Vector3.one);
        }//done moving
    } //MovementFrameUpdate

    private void GenerateAttackAnimationQueue(AnimationTask currentAnimationTask) {
        ConsolePrint(gameObject.name + " Generating attack animation queue.");
        //init local variables
        UnitScript[] unitControlScriptArray;
        switch (soldierClassData.unitClass) {
            case "Infantry":
            case "Knight":
            case "Peasant":
            case "Mercenary":
                Vector3 positionDelta = currentAnimationTask.targetVector - transform.position;
                unitControlScriptArray = gameObject.GetComponentsInChildren<UnitScript>();
                foreach (UnitScript thisUnitScript in unitControlScriptArray) {
                    float walkDistance = 0.85f;
                    if (soldierClassData.unitClass == "Knight")
                        walkDistance = 0.35f;
                    if (soldierClassData.unitClass == "Mercenary")
                        walkDistance = 0.45f;
                    //walk forward to the target's tile, maintaining formation
                    Vector3 thisUnitLocation = thisUnitScript.transform.position;
                    thisUnitScript.AddAnimationToQueue("March", thisUnitLocation + (positionDelta * walkDistance));
                    //animate the attack
                    thisUnitScript.AddAnimationToQueue("Attack", Vector3.one);
                    //turn to face the origin tile
                    thisUnitScript.AddAnimationToQueue("Rotate", thisUnitScript.transform.forward * -1);
                    //return to the origin tile
                    thisUnitScript.AddAnimationToQueue("March", thisUnitLocation);
                    //turn back to face forward again
                    thisUnitScript.AddAnimationToQueue("Rotate", thisUnitScript.transform.forward);
                    //go back to idle
                    thisUnitScript.AddAnimationToQueue("Idle", Vector3.one);
                }
                break;

            case "HeavyInfantry":
            case "Archer":
            case "King":
                Vector3 angleToTarget = currentAnimationTask.targetVector - transform.position;

                unitControlScriptArray = gameObject.GetComponentsInChildren<UnitScript>();
                foreach (UnitScript thisUnitScript in unitControlScriptArray) {
                    //walk forward to the target's tile, maintaining formation
                    thisUnitScript.AddAnimationToQueue("Rotate", angleToTarget);
                    //animate the attack
                    thisUnitScript.AddAnimationToQueue("Attack", currentAnimationTask.targetVector);
                    //turn back to face forward again
                    thisUnitScript.AddAnimationToQueue("Rotate", thisUnitScript.transform.forward);
                    //go back to idle
                    thisUnitScript.AddAnimationToQueue("Idle", Vector3.one);
                }
                break;

        }//switch squadtype

    }//GenerateAttackAnimationQueue

    public void AnimateSquad(int soldierCount, string animationType, Vector3 animationVector) {
        ConsolePrint("Animation " + animationType + " requested for " + soldierCount + " units of type " + soldierClassData.unitClass);
        UnitScript[] unitControlScriptArray = gameObject.GetComponentsInChildren<UnitScript>();

        if (soldierCount == -1) //default used to mean "all units do this animation"
            soldierCount = unitControlScriptArray.Length;

        for (int i = 0; i < soldierCount; i++) {
            unitControlScriptArray[i].AddAnimationToQueue(animationType, animationVector);
        }
    }//AnimateSquad


    /*******************
     * BOARD UTILITIES *
     *******************/

    public void SetSelected(bool selected) {
        ConsolePrint("Set Selected " + selected + " called");
        isSelected = selected;
        if (selected) {
            occupiedGameTile.GetComponent<BoardTileScript>().EnableHighlight("Selected");
            if (gameControlScript.gamePhase != "PlaceArmyP1" && gameControlScript.gamePhase != "PlaceArmyP2")
                EnableActionHighlights();
        }
        else {
            occupiedGameTile.GetComponent<BoardTileScript>().ClearAllHighlights();
        }
    }//SetSelected

    public void EnableActionHighlights() {
        ConsolePrint("Enabling Action Highlights for squad: " + gameObject.name);

        bool continueScanning = true;
        BoardTileScript thisBoardTileScript = occupiedGameTile.GetComponent<BoardTileScript>();

        switch (soldierClassData.unitClass) {
            case "King"://king and knight have the same movement and atack patterns
            case "Knight":
                bool attackTileAssessed = false;    //used in order to use only the first iteration of the loop to do attack highlighting. this unit can only attack with a range of 1 tile forward.
                while (continueScanning) {
                    thisBoardTileScript = ScanNextTile(thisBoardTileScript, orientationIndex);
                    //determine if the next tile has an enemy, if so highlight it.
                    if (!attackTileAssessed) {
                        attackTileAssessed = true;
                        AssessAttackable(thisBoardTileScript);
                    }
                    //determine if next tile has an enemy in it, if so highlight it.
                    continueScanning = AssessMovable(thisBoardTileScript);
                }//loop
                break;

            case "Infantry":
            case "Mercenary":
            case "Peasant":
                thisBoardTileScript = ScanNextTile(thisBoardTileScript, orientationIndex);
                //determine if the next tile has an enemy, if so highlight it.
                AssessAttackable(thisBoardTileScript);
                //determine if next tile has an enemy in it, if so highlight it.
                continueScanning = AssessMovable(thisBoardTileScript);
                break;

            case "HeavyInfantry":
                //heavy infantry code folder
                #region
                BoardTileScript forwardBoardTileScript = ScanNextTile(thisBoardTileScript, orientationIndex);
                //determine if next tile has an enemy in it, if so highlight it.
                AssessMovable(forwardBoardTileScript);
                //this squad can atack in any direction. assess all 8 adjacent / diagonal tiles. 
                //up
                BoardTileScript upBoardTileScript = null;
                upBoardTileScript = ScanNextTile(thisBoardTileScript, GameSettings.orientation_Up);
                AssessAttackable(upBoardTileScript);

                //left
                BoardTileScript leftBoardTileScript = null;
                leftBoardTileScript = ScanNextTile(thisBoardTileScript, GameSettings.orientation_Left);
                AssessAttackable(leftBoardTileScript);

                //right
                BoardTileScript rightBoardTileScript = null;
                rightBoardTileScript = ScanNextTile(thisBoardTileScript, GameSettings.orientation_Right);
                AssessAttackable(rightBoardTileScript);

                //down
                BoardTileScript downBoardTileScript = null;
                downBoardTileScript = ScanNextTile(thisBoardTileScript, GameSettings.orientation_Down);
                AssessAttackable(downBoardTileScript);

                //up, right
                BoardTileScript upRightBoardTileScript = null;
                if (upBoardTileScript != null)
                    upRightBoardTileScript = ScanNextTile(upBoardTileScript, GameSettings.orientation_Right);
                AssessAttackable(upRightBoardTileScript);

                //up, left
                BoardTileScript upLeftBoardTileScript = null;
                if (upBoardTileScript != null)
                    upLeftBoardTileScript = ScanNextTile(upBoardTileScript, GameSettings.orientation_Left);
                AssessAttackable(upLeftBoardTileScript);

                //down, right
                BoardTileScript downRightBoardTileScript = null;
                if (downBoardTileScript != null)
                    downRightBoardTileScript = ScanNextTile(downBoardTileScript, GameSettings.orientation_Right);
                AssessAttackable(downRightBoardTileScript);

                //down, left
                BoardTileScript downLeftBoardTileScript = null;
                if (downBoardTileScript != null)
                    downLeftBoardTileScript = ScanNextTile(downBoardTileScript, GameSettings.orientation_Left);
                AssessAttackable(downLeftBoardTileScript);
                #endregion //heavy infantry code folder
                break;

            case "Archer":
                //archer code folder
                #region
                thisBoardTileScript = ScanNextTile(thisBoardTileScript, orientationIndex);
                //determine if the next tile has an enemy, if so highlight it.
                continueScanning = AssessMovable(thisBoardTileScript);

                //archer attacks a grid of 3x3 directly infront of the current tile scan all 9
                //determine if next tile has an enemy in it, if so highlight it.
                BoardTileScript centerTileScript = thisBoardTileScript;
                BoardTileScript leftTileScript = null;
                BoardTileScript rightTileScript = null;
                //define a local reference version of the 4 directional vectors
                int localLeft = 0;
                int localRight = 0;
                if (orientationIndex == GameSettings.orientation_Up) {
                    localLeft = GameSettings.orientation_Left;
                    localRight = GameSettings.orientation_Right;
                }
                else if (orientationIndex == GameSettings.orientation_Left) {
                    localLeft = GameSettings.orientation_Down;
                    localRight = GameSettings.orientation_Up;
                }
                else if (orientationIndex == GameSettings.orientation_Down) {
                    localLeft = GameSettings.orientation_Right;
                    localRight = GameSettings.orientation_Left;
                }
                else if (orientationIndex == GameSettings.orientation_Right) {
                    localLeft = GameSettings.orientation_Up;
                    localRight = GameSettings.orientation_Down;
                }
                for (int i = 0; i < 3; i++) {
                    if (centerTileScript == null)
                        break;  //exit loop when the next row is empty (the edge of the board)
                                //check left and right tiles
                    leftTileScript = ScanNextTile(centerTileScript, localLeft);
                    rightTileScript = ScanNextTile(centerTileScript, localRight);
                    AssessAttackable(centerTileScript);
                    AssessAttackable(leftTileScript);
                    AssessAttackable(rightTileScript);
                    //get new center tile
                    centerTileScript = ScanNextTile(centerTileScript, orientationIndex);
                }
                #endregion //archer code folder
                break;

        }//switch squad type
    }//EnableActionHighlights

    private BoardTileScript ScanNextTile(BoardTileScript thisBoardTileScript, int orientation) {
        // ConsolePrint("Scanning tile " + thisBoardTileScript.gameObject.name + " looking at vector " + orientation);
        bool edgeOfBoard = false;
        //get the next time in line
        switch (orientation) {
            case 0: //up
                try {
                    thisBoardTileScript = thisBoardTileScript.adjacentTileTop.GetComponent<BoardTileScript>();
                }
                catch {
                    edgeOfBoard = true;
                }
                break;
            case 1: //right
                try {
                    thisBoardTileScript = thisBoardTileScript.adjacentTileRight.GetComponent<BoardTileScript>();
                }
                catch {
                    edgeOfBoard = true;
                }
                break;
            case 2: //down
                try {
                    thisBoardTileScript = thisBoardTileScript.adjacentTileBottom.GetComponent<BoardTileScript>();
                }
                catch {
                    edgeOfBoard = true;
                }
                break;
            case 3: //left
                try {
                    thisBoardTileScript = thisBoardTileScript.adjacentTileLeft.GetComponent<BoardTileScript>();
                }
                catch {
                    edgeOfBoard = true;
                }
                break;
        }//switch

        if (!edgeOfBoard)
            return thisBoardTileScript;
        else
            return null;
    }//ScanNextTile

    private void AssessAttackable(BoardTileScript targetTileScript) {
        if (targetTileScript != null) {
            //ConsolePrint("Assessing Attackability for tile " + targetTileScript.gameObject.name);
            if (targetTileScript.isOccupied) {
                if (targetTileScript.occupyingSquad.GetComponent<SquadScript>().ownerID != ownerID) {
                    targetTileScript.EnableHighlight("ValidAttackTarget");
                }//occupying squad is an enemy
            }//tile is occupied
        }//tile exists
    }//assess attackable

    private bool AssessMovable(BoardTileScript targetTileScript) {
        if (targetTileScript == null)
            return false;

        //ConsolePrint("Assessing Movability for tile " + targetTileScript.gameObject.name);

        if (targetTileScript.occupyingSquad == null) {
            //if empty, highlight the tile as a valid move target
            targetTileScript.EnableHighlight("ValidMoveTarget");
            return true;
        }//tile exists, but is empty 

        else
            return false;
    } //assess movable


    /****************
     * GAME ACTIONS *
     ****************/

    public void Attack(GameObject attackTarget) {
        ConsolePrint("Squad attacking");
        //add animation to the queue 
        AnimationTask thisAnimationTask = new AnimationTask("SqaudAttack", attackTarget.transform.position);
        animationQueue.Add(thisAnimationTask);

    }//attack

    public void Panic(int distance, int retreatDirection) {
        ConsolePrint("Squad panicking");
        // Determine the rotation needed to align this squad with the retreatr vector - always run the direction the attacker is facing.
        int netRotation = retreatDirection - orientationIndex;
        if (Mathf.Abs(netRotation) == 3)
            netRotation = netRotation / -3;
        for (int i = 0; i < Mathf.Abs(netRotation); i++) {
            if (netRotation > 0)
                RotateSquad("Right");
            else
                RotateSquad("Left");
        }

        //calculate the next target tile
        BoardTileScript nextTileScript;
        for (int i = 0; i < distance; i++) {
            nextTileScript = ScanNextTile(occupiedGameTile.GetComponent<BoardTileScript>(), orientationIndex);
            if (nextTileScript == null) {
                SquadLost();
                break;
            }
            else {
                //check if it is occupied
                if (nextTileScript.isOccupied) {
                    string occupyingSquadType = nextTileScript.occupyingSquad.GetComponent<SquadScript>().soldierClassData.unitClass;
                    if (occupyingSquadType == "King" || occupyingSquadType == "Mercenary" || nextTileScript.occupyingSquad.GetComponent<SquadScript>().ownerID != this.ownerID) {
                        SquadLost();
                        break;
                    }
                    else
                        StartCoroutine(DelayThenPanic(nextTileScript.occupyingSquad.GetComponent<SquadScript>(), retreatDirection));
                }//next tile is occupied
                MoveLocation(nextTileScript.gameObject);
            }//next tile exists
        }//loop

    }//panic

    public void MoveLocation(GameObject newBoardTile) {
        //clear the previous tile
        occupiedGameTile.GetComponent<BoardTileScript>().ClearTile();

        //designate the new target as occupied
        occupiedGameTile = newBoardTile;

        //set up motion speeds 
        string gamePhase = gameControlScript.gamePhase;
        if (gamePhase == "PlaceArmyP1" || gamePhase == "PlaceArmyP2")
            movementSpeed = setupMovementSpeed;
        else
            movementSpeed = activeGameMovementSpeed;

        //add animation to the queue 
        AnimationTask thisAnimationTask = new AnimationTask("MoveSquad", occupiedGameTile.transform.position);
        animationQueue.Add(thisAnimationTask);
    }//move Location

    public void RotateSquad(string direction) {
        ConsolePrint("Rotation orders received: " + direction);

        if (direction == "Left")
            orientationIndex = (orientationIndex - 1 + 4) % 4;
        if (direction == "Right")
            orientationIndex = (orientationIndex + 1) % 4;

        //determine target vector
        rotationTargetVector = GameSettings.orientations[orientationIndex];

        //set up motion speeds 
        string gamePhase = gameControlScript.gamePhase;
        if (gamePhase == "PlaceArmyP1" || gamePhase == "PlaceArmyP2")
            rotationSpeed = setupRotationSpeed;
        else
            rotationSpeed = activeGameRotationSpeed;

        //add animation to the queue 
        AnimationTask thisAnimationTask = new AnimationTask("RotateSquad", rotationTargetVector);
        animationQueue.Add(thisAnimationTask);
    }//RotateSquad

    private IEnumerator DelayThenPanic(SquadScript targetSquad, int retreatDirection) {
        yield return new WaitForSeconds(0.3f);
        targetSquad.Panic(1, retreatDirection);
    }

    public void Defend(Vector3 blockDirection) {
        //called when this squad is under attack 
        AnimationTask thisAnimationTask = new AnimationTask("Block", blockDirection);
        animationQueue.Add(thisAnimationTask);
    } //defend

    public void Idle() {
        //called when an attack on this squad has ended.
        if (soldierClassData.unitsInSquad > 0) {
            AnimationTask thisAnimationTask = new AnimationTask("Idle", Vector3.one);
            animationQueue.Add(thisAnimationTask);
        }
    } //idle

    public void Cheer() {
        //called at the start of the turn, or at game over for the winner
        AnimationTask thisAnimationTask = new AnimationTask("Cheer", Vector3.one);
        animationQueue.Add(thisAnimationTask);
    }

    /***********
     * REPORTS *
     ***********/

    public void ReportUnitAnimationStart(string reportData) {
        //setting the bool is probably redundant here, since we preemptively set it elsewhere, this is a safeguard
        unitsAreAnimating = true;
        activeUnitAnimations++;
        ConsolePrint("Unit animation (" + reportData + ") started. " + activeUnitAnimations + " total unit animations running.");
    }//report unit animation start

    public void ReportUnitAnimationComplete(string reportData) {
        activeUnitAnimations--;
        ConsolePrint("Unit animation (" + reportData + ") ended. " + activeUnitAnimations + " unit animations remaining.");
        if (activeUnitAnimations == 0) {
            unitsAreAnimating = false;
            if (!squadTileIsAnimating)
                gameControlScript.ReportAnimationComplete("Team " + ownerID + ", " + gameObject.name + " All units reported complete");
        }
    }//report unit animation complete

    public void ReportAttackAnimationBeginning() {
        gameControlScript.ReportAttackAnimationBeginning();
    }

    public void ReportBlockAnimationBeginning() {
        gameControlScript.ReportBlockAnimationBeginning();
    }

    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message) {
        if (enableDebugging == true) {
            Debug.Log("Squad Script - Team " + ownerID + ", " + gameObject.name + ": " + message);
        }
    }//console print

}//class 

/// <summary>
/// A struct designed to contain the data relavent to a squad consisting of one or more units of the same type.
/// </summary>
/// <param name=""></param>
/// <returns></returns>
public struct SoldierClassData {
    public string unitClass;    //Archer, Infantry, Knight, King etc
    public int unitsInSquad;
    public int apCostToAttack;
    public int apCostToMove;
    public int apCostToRotate;
    public string attacksWith;  //Arrow, Axe, Any(for peasants)
    public int dicePerUnit;
    public int healthPerUnit;
    public string panicBehavior;   //Standard, AlwaysPanic (Peasants), PanicTargets(Mercenary), NeverPanic(King)

    /// <summary>
    /// Sets up the squad definition based on which troop type is supplied. Uses default tile definition.
    /// </summary>
    /// <param name="soldierClass">Infantry, Archer, Knight, King, Mercenary, HeavyInfantry, or Peasant.</param>
    /// <param name="startingUnitCount">How many of this type of soldier should be on in the squad to start the game.</param>
    /// <returns></returns>
    public void InitializeSquad(string soldierClass, int startingUnitCount) {
        unitClass = soldierClass;
        apCostToRotate = 1;
        unitsInSquad = startingUnitCount;
        if (soldierClass == "Infantry" || soldierClass == "Archer" || soldierClass == "Peasant") {
            apCostToAttack = 1;
            apCostToMove = 1;
            dicePerUnit = 1;
            healthPerUnit = 1;
        }
        else {//knight, king, mercenary, heavyinfantry
            dicePerUnit = 2;

            if (soldierClass == "HeavyInfantry") {
                apCostToAttack = 2;
                apCostToMove = 2;
            }
            else {
                apCostToAttack = 1;
                apCostToMove = 1;
            }

            if (soldierClass == "Mercenary")
                healthPerUnit = 1;
            else
                healthPerUnit = 2;
        }

        //define attacking dice for all units
        if (soldierClass == "Infantry" || soldierClass == "Mercenary" || soldierClass == "HeavyInfantry" || soldierClass == "King" || soldierClass == "Knight")
            attacksWith = "Axe";
        else if (soldierClass == "Archer")
            attacksWith = "Arrow";
        else //Peasant
            attacksWith = "Any";

        //define panic types
        if (soldierClass == "Infantry" || soldierClass == "Archer" || soldierClass == "HeavyInfantry" || soldierClass == "Knight")
            panicBehavior = "Standard";
        else if (soldierClass == "Mercenary")
            panicBehavior = "PanicTargets";
        else if (soldierClass == "King")
            panicBehavior = "NeverPanic";
        else //Peasant
            panicBehavior = "AlwaysPanic";


    }

    public int GetDiceCount() {
        return unitsInSquad * dicePerUnit;
    }

}

class AnimationTask {
    public string animationType;   //Move, Attack, Rotate, Die
    public Vector3 targetVector = Vector3.one; //used as a location for Move and Attack. used as a directional vector for rotate
    public Animator targetAnimator = null; //used when we need to invoke animations connected to a specific unit out of the whole squad
    //constructor overloads
    public AnimationTask(string animationType, Vector3 targetLocation) {   //used for move, attack
        this.animationType = animationType;
        this.targetVector = targetLocation;
    }
    public AnimationTask(string animationType, Animator targetAnimator) {//used for Die
        this.animationType = animationType;
        this.targetAnimator = targetAnimator;
    }

    public override string ToString() {
        return "ANIMATION TASK - Type: " + animationType + " Vector: " + targetVector.ToString();// + " Animator: " + targetAnimator.ToString();
    }

}//AnimationTask