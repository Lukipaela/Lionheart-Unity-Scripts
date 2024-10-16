using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SquadScript : MonoBehaviour {
    public int ownerID;    //which player owns this squad 
    public int orientationIndex;   //corresponds to the orientations array in GameSettings. Indicates which direction this squad is facing
    public GameObject occupiedGameTile; //the tile which this unit is currently standing on 
    public SoldierClassData soldierClassData;
    public int unitsRemaining;

    [SerializeField] private GameControl gameControlScript;
    private bool isSelected = false;
    private bool isBlocking = false; //indicates if the most recent animation requested was a block. Allows troops to rotate toward enemy and back again after surviving
    //animation controls
    private bool isRotating = false;
    private bool isMoving = false;
    private float rotationSpeed = 2.3f;  //holds the actual value used in animation, written to by one of the below constants
    private float movementSpeed = 1.3f;  //holds the actual value used in animation, written to by one of the below constants
    private readonly float setupRotationSpeed = 8;
    private readonly float activeGameRotationSpeed = 2.2f;
    private readonly float setupMovementSpeed = 5;
    private readonly float activeGameMovementSpeed = 1.5f;
    private List<AnimationTask> animationQueue = new List<AnimationTask>();
    private AnimationTask currentAnimationTask; //holds the details of whichever animation task is currently active
    private List<UnitScript> unitList = new List<UnitScript>();

    //debug
    private static readonly bool enableDebugging = true;


    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start() {
        gameControlScript = GameObject.FindGameObjectWithTag("GameControl").GetComponent<GameControl>();
    }

    void Update() {
        if (isRotating) {
            RotationFrameUpdate(gameObject, currentAnimationTask.targetVector);
        }//rotating logic

        if (isMoving) {
            MovementFrameUpdate(gameObject, currentAnimationTask.targetVector);
        }//moving logic

        if (!isMoving && !isRotating && !AreUnitsAnimating()) {
            CheckAnimationQueue();
        }//no longer animating
    }//Update 



    /*****************
     * DAMAGE / LOSS *
     *****************/

    public void TakeDamage(int damage) {
        //total up units lost, requiring 2 damage to kill a large unit, and clamping to a max of UnitsInSquad
        int unitsLost = Mathf.Min(damage / soldierClassData.healthPerUnit, unitsRemaining);
        unitsRemaining -= unitsLost;
        
        for (int i = 0; i < unitList.Count; i++) {
            if(i < unitsLost)
                unitList[i].Die();  //Call directly, unit is halted in block animation and not reading its queue
            else
                unitList[i].Deflect(); //Call directly, unit is halted in block animation and not reading its queue
        }

        unitList.RemoveRange(0, unitsLost);

        if (unitsRemaining == 0)
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
        if (animationQueue.Count > 0) {
            currentAnimationTask = animationQueue[0];
            animationQueue.RemoveAt(0);
            ConsolePrint("Moving to animation: " + currentAnimationTask.animationType);
            switch (currentAnimationTask.animationType) {
                case "Block":
                    //rotate towards attacker, then block
                    isBlocking = true;
                    AnimateSquad(-1, "Rotate", currentAnimationTask.targetVector);
                    AnimateSquad(-1, "Block", Vector3.one);
                    break;

                case "Cheer":
                    AnimateSquad(-1, "Cheer", Vector3.one);
                    AnimateSquad(-1, "Idle", Vector3.one);
                    break;

                case "Die":
                    AnimateSquad(-1, "Die", Vector3.one);
                    SquadLost();
                    break;

                case "Idle":
                    if (isBlocking) {
                        //if we were blocking before, rotate back forwards before going to idle
                        isBlocking = false;
                        AnimateSquad(-1, "Rotate", transform.forward);
                    }
                    AnimateSquad(-1, "Idle", Vector3.one);
                    break;

                case "MoveSquad":
                    isMoving = true;
                    AnimateSquad(-1, "March", Vector3.one);
                    break;

                case "RotateSquad":
                    isRotating = true;
                    break;

                case "RotateUnits":
                    AnimateSquad(-1, "Rotate", currentAnimationTask.targetVector);
                    break;

                case "SqaudAttack":
                    GenerateAttackAnimationQueue(currentAnimationTask);
                    break;

            }//animation type switch 
        }//animations remain in queue
    }//check animation queue

    private void RotationFrameUpdate(GameObject rotatingObject, Vector3 desiredForwardVector) {
        // Rotate the forward vector towards the target direction by one step
        Vector3 newDirection = Vector3.RotateTowards(rotatingObject.transform.forward, desiredForwardVector, rotationSpeed * Time.deltaTime, 0.0f);
        // Calculate a rotation a step closer to the target and applies rotation to this object
        rotatingObject.transform.rotation = Quaternion.LookRotation(newDirection);

        //if we have reached our target orientation, stop rotating.
        if (Vector3.Angle(rotatingObject.transform.forward, desiredForwardVector) < 1) {
            isRotating = false;
            HaltUnits();
        }

    }//RotationFrameUpdate

    private void MovementFrameUpdate(GameObject movingObject, Vector3 desiredLocation) {
        movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, desiredLocation, movementSpeed * Time.deltaTime);
        
        //if we have reached our destination, stop moving
        if (movingObject.transform.position == desiredLocation) {
            isMoving = false;
            if (occupiedGameTile != null)
                occupiedGameTile.GetComponent<BoardTileScript>().PlaceSquad(gameObject);  
            HaltUnits();
        }//done moving
    } //MovementFrameUpdate

    private void GenerateAttackAnimationQueue(AnimationTask currentAnimationTask) {
        ConsolePrint(gameObject.name + " Generating attack animation queue.");
        //init local variables
        switch (soldierClassData.unitClass) {
            case "Infantry":
            case "Knight":
            case "Peasant":
            case "Mercenary":
                Vector3 positionDelta = currentAnimationTask.targetVector - transform.position;
                foreach (UnitScript thisUnitScript in unitList) {
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
                foreach (UnitScript thisUnitScript in unitList) {
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

    public void AnimateSquad(int soldiersToAnimate, string animationType, Vector3 animationVector) {
        ConsolePrint("Animation " + animationType + " requested for " + soldiersToAnimate + " units of type " + soldierClassData.unitClass);

        if (soldiersToAnimate == -1) //default used to mean "all units do this animation"
            soldiersToAnimate = unitList.Count;

        for (int i = 0; i < soldiersToAnimate && i < unitList.Count; i++) {
            unitList[i].AddAnimationToQueue(animationType, animationVector);
        }
    }//AnimateSquad

    /// <summary>
    /// Relinquish the current tile from this squad's ownbership, and move the squadf forward a bit,
    /// then die. Note - does not rely on there being a board tile ahead.
    /// </summary>
    private void PanicDeath( Vector3 marchVector ) {
        ConsolePrint("Panic death triggered");
        occupiedGameTile.GetComponent<BoardTileScript>().ClearTile();
        occupiedGameTile = null;
        animationQueue.Add(new AnimationTask("MoveSquad", transform.position + (marchVector.normalized * 1.1f)));
        animationQueue.Add(new AnimationTask("Die", Vector3.one));
    }



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
        animationQueue.Add(new AnimationTask("SqaudAttack", attackTarget.transform.position));
    }//attack

    /// <summary>
    /// Called by the ghame control when this unit has been told to panic. 
    /// Will do nothing if the unit class does not panic. 
    /// Can invoke a chain reaction if it runs into another unit. 
    /// </summary>
    /// <param name="distance">How many tiles to try to retreat.</param>
    /// <param name="distance">The orientation index along which the retreat should be performed.</param>
    public IEnumerator Panic(int distance, int retreatDirection) {
        ConsolePrint("Squad panicking along orientation index " + retreatDirection + ". Can receive panic: " + soldierClassData.canReceivePanic);
        //if this class of soldier cannot be made to panic, return that value to the caller and perform no followup actions
        if (soldierClassData.canReceivePanic) {
            //In case this animation was triggered by this unit's own attacks (peasant), wait for current animations to conclude before proceeding.
            while(IsTheSquadAnimating()) {
                yield return new WaitForSeconds(0.1f);
            }

            // Determine the rotation needed to align this squad with the retreat vector 
            int netRotation = retreatDirection - orientationIndex;
            if (Mathf.Abs(netRotation) == 3)
                netRotation = netRotation / -3;
            for (int i = 0; i < Mathf.Abs(netRotation); i++) {
                if (netRotation > 0)
                    RotateSquad("Right");
                else
                    RotateSquad("Left");
            }
            while (IsTheSquadAnimating()) {
                yield return new WaitForSeconds(0.1f);
            }

            //do panic animation, including suicide or panicking the next guy over
            BoardTileScript nextTileScript;
            for (int i = 0; i < distance; i++) {
                nextTileScript = ScanNextTile(occupiedGameTile.GetComponent<BoardTileScript>(), orientationIndex);
                if (nextTileScript == null) {
                    ConsolePrint("Edge of board detected, invoking panic death");
                    PanicDeath(GameSettings.orientations[retreatDirection]);
                    break;
                }
                else if (nextTileScript.isOccupied) {
                    SquadScript occupyingSquadScript = nextTileScript.occupyingSquad.GetComponent<SquadScript>();
                    if (occupyingSquadScript.ownerID != this.ownerID || !occupyingSquadScript.soldierClassData.canReceivePanic) {
                        ConsolePrint("Next tile can't be panicked, invoking panic death");
                        PanicDeath(GameSettings.orientations[retreatDirection]);
                    }
                    else {//invoke panic on the next unit, and wait for the tile to be released before moving into it. 
                        ConsolePrint("Beginning panic chain.");
                        StartCoroutine(occupyingSquadScript.Panic(distance: 1, retreatDirection));
                        while (nextTileScript.isOccupied) {
                            yield return new WaitForSeconds(0.1f);
                        }
                        MoveLocation(nextTileScript.gameObject);
                        while (IsTheSquadAnimating()) {
                            yield return new WaitForSeconds(0.1f);
                        }
                    }//next tile occupied by something I can push out                   
                }//next tile is occupied
                else { //next tile is unoccupied, move in and continue to panic if applicable
                    ConsolePrint("Next tile open, performing unimpeded panic movement.");
                    MoveLocation(nextTileScript.gameObject);
                    while (IsTheSquadAnimating()) {
                        yield return new WaitForSeconds(0.1f);
                    }
                }//retreating to empty tile
                //wait between iterations of the retreat loop to allow the chain reaction to flow to the end of the line first. 
                yield return new WaitForSeconds(1);
            }//loop
        }// this unit type can panic
        yield return new WaitForSeconds(0); //dummy return value when no delay is needed - happens when this unit cannot panic
    }//panic

    public void MoveLocation(GameObject newBoardTile) {
        //clear the previous tile
        occupiedGameTile.GetComponent<BoardTileScript>().ClearTile();

        ConsolePrint("Vector to next tile is: " + (newBoardTile.transform.position - occupiedGameTile.transform.position).ToString());

        //designate the new target as occupied
        occupiedGameTile = newBoardTile;

        //set up motion speeds 
        if (gameControlScript.gamePhase == "PlaceArmyP1" || gameControlScript.gamePhase == "PlaceArmyP2")
            movementSpeed = setupMovementSpeed;
        else
            movementSpeed = activeGameMovementSpeed;

        animationQueue.Add(new AnimationTask("MoveSquad", occupiedGameTile.transform.position)); 
    }//move Location

    public void RotateSquad(string direction) {
        ConsolePrint("Rotation orders received: " + direction);

        if (direction == "Left")
            orientationIndex = (orientationIndex - 1 + 4) % 4;
        if (direction == "Right")
            orientationIndex = (orientationIndex + 1) % 4;

        //determine target vector
        Vector3 rotationTargetVector = GameSettings.orientations[orientationIndex];

        //set up motion speeds
        if (gameControlScript.gamePhase == "PlaceArmyP1" || gameControlScript.gamePhase == "PlaceArmyP2")
            rotationSpeed = setupRotationSpeed;
        else
            rotationSpeed = activeGameRotationSpeed;

        animationQueue.Add(new AnimationTask("RotateSquad", rotationTargetVector));
    }//RotateSquad

    /// <summary>
    /// Called by the GameControl request that all units in the squad prepare for attack.
    /// </summary>
    /// /// <param name="blockDirection">A vector pointing from the squad's center to the center of the attacking squad.</param>
    public void Defend(Vector3 blockDirection) {
        //called when this squad is under attack 
        AnimationTask thisAnimationTask = new AnimationTask("Block", blockDirection);
        animationQueue.Add(thisAnimationTask);
    } //defend

    public void Idle() {
        //called when an attack on this squad has ended.
        if (unitsRemaining > 0) {
            animationQueue.Add(new AnimationTask("Idle", Vector3.one));
        }
    } //idle
      
    /// <summary>
    /// Called by the GameControl to queue a cheer animation.
    /// </summary>
    public void Cheer() {
        //called at the start of the turn, or at game over for the winner
        animationQueue.Add(new AnimationTask("Cheer", Vector3.one));
    }

    /// <summary>
    /// Tells all unit scripts to stop their marching animation and go back to Idle after a squad move or squad rotation.
    /// </summary>
    private void HaltUnits() {
        foreach (UnitScript thisUnitScript in unitList)
            thisUnitScript.MarchEnd();
    }

    /// <summary>
    /// Checks if any unit in the squad is currently animating
    /// </summary>
    private bool AreUnitsAnimating() {
        foreach (UnitScript thisUnit in unitList) {
            if (thisUnit.IsAnimating()) {
                ConsolePrint(thisUnit.name + " is animating with " + thisUnit.animationState);
                return true;
            }
        }
        return false;

    }

    /// <summary>
    /// Checks if the squad has queued animations, OR any of the units is currently animating, or has a queued animation
    /// </summary>
    public bool IsTheSquadAnimating() {
        //check for individual unit animations AND this squad's queue
        if (AreUnitsAnimating() || animationQueue.Count > 0)
            return true;
        else
            return false;
    }



    /*************
     * UTILITIES *
     *************/

    public int GetDiceCount() {
        return unitsRemaining * soldierClassData.dicePerUnit;
    }//getDiceCount

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
        soldierClassData.InitializeSquad(squadType);
        unitsRemaining = unitsInSquad;
        //find all units in the squad, put them into a List for future reference
        UnitScript[] unitControlScriptArray = gameObject.GetComponentsInChildren<UnitScript>();
        unitList.AddRange(unitControlScriptArray);
        unitList[unitList.Count - 1].isCaptain = true;  
    }



    /***********
     * REPORTS *
     ***********/

    /// <summary>
    /// Passes along a message from a squad member up to the control script to notify the GameController that the attack animation
    /// has reached the frame where it makes contact with the enemy. The enemy may then react.
    /// </summary>
    public void ReportAttackHit() {
        gameControlScript.ReportAttackHit();
    }

    /// <summary>
    /// Passes along a message from a squad member up to the control script to notify the GameController that the Block animation
    /// has completed and is holding position in the guarding pose. The GameController can now commence attack animations.
    /// </summary>
    public void ReportBlockAnimationComplete() {
        gameControlScript.ReportBlockAnimationComplete();
    }



    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message) {
        if (enableDebugging == true) 
            Debug.Log("Squad Script - Team " + ownerID + ", " + gameObject.name + ": " + message);
    }//console print
}//class 

/// <summary>
/// A struct designed to contain the data relavent to a squad consisting of one or more units of the same type.
/// </summary>
public struct SoldierClassData {
    public string unitClass;    //Archer, Infantry, Knight, King etc
    public int apCostToAttack;
    public int apCostToMove;
    public int apCostToRotate;
    public string attacksWith;  //Arrow, Axe, Any(for peasants)
    public int dicePerUnit;
    public int healthPerUnit;
    public string panicDieAction;   //Standard, AlwaysPanic (Peasants), PanicTargets(Mercenary), NeverPanic(King)
    public bool canReceivePanic;    // indicates if the unit has to react to having panic inflicted on it (either via an attack from a mercenary, or being run into by a panicking squad)

    /// <summary>
    /// Sets up the squad definition based on which troop type is supplied. Uses default tile definition.
    /// </summary>
    /// <param name="soldierClass">Infantry, Archer, Knight, King, Mercenary, HeavyInfantry, or Peasant.</param>
    /// <param name="startingUnitCount">How many of this type of soldier should be on in the squad to start the game.</param>
    /// <returns></returns>
    public void InitializeSquad(string soldierClass) {
        unitClass = soldierClass;
        apCostToRotate = 1;
        if (soldierClass == "Infantry" || soldierClass == "Archer" || soldierClass == "Peasant") {
            apCostToAttack = 1;
            apCostToMove = 1;
            dicePerUnit = 1;
            healthPerUnit = 1;
        }
        else {//knight, king, mercenary, heavyinfantry
            dicePerUnit = 2;
            apCostToAttack = 1;

            if (soldierClass == "HeavyInfantry") 
                apCostToMove = 2;
            else 
                apCostToMove = 1;

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
        if (soldierClass == "Infantry" || soldierClass == "Archer" || soldierClass == "HeavyInfantry" || soldierClass == "Knight") {
            panicDieAction = "Standard";
            canReceivePanic = true;
        }
        else if (soldierClass == "Mercenary") {
            panicDieAction = "PanicTargets";
            canReceivePanic = false;
        }
        else if (soldierClass == "King") { 
            panicDieAction = "NeverPanic";
            canReceivePanic = false;
        }
        else {//Peasant
            panicDieAction = "AlwaysPanic";
            canReceivePanic = true;
        }
    }//InitializeSquad method

}//SoldierClassData struct

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

