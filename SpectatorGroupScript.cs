using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SpectatorGroupScript : MonoBehaviour
{
    //private, but exposed in UI
    [SerializeField] private List<SpectatorScript> spectatorList = new List<SpectatorScript>(); //list of all characters in the group, for distributing animation commands
    [SerializeField] private List<Transform> targetLocationList = new List<Transform>();    //a list of Transforms used by this group's animations (locations, and orientation angles)
    [SerializeField] private SoldierClass spectatorClass;
    [SerializeField] private SpectatorControl parentControlScript;
    [SerializeField] private SpectatorGroupScript opposingSpectatorGroup;
    [SerializeField] private GameControl gameControlScript;

    //private
    private List<AnimationTask> animationQueue = new List<AnimationTask>();
    private AnimationTask currentAnimationTask;
    private AnimationState animationState = AnimationState.Idle;
    private float delayTimer = 1;

    //debug
    private bool enableDebugging = false;


    /********************
     * BUILT-IN METHODS *
     ********************/

    // Start is called before the first frame update
    void Start()
    {
        //the group of archers are standing at an archery range, and should be constantly shooting
        if (spectatorClass == SoldierClass.Archer)
            AnimateSpectators(AnimationType.IdleAttacking, Vector3.one);
    }

    // Update is called once per frame
    void Update()
    {
        GroupAnimationFrameUpdate();
        if (currentAnimationTask != null && currentAnimationTask.animationType == AnimationType.Delay)
        {
            delayTimer -= Time.deltaTime;
            if (delayTimer < 0)
                currentAnimationTask = null;
        }
    }



    /**********************
     * ANIMATION CONTROLS *
     **********************/

    /// <summary>
    /// Handles the generation of the animation queue associated with a given speectator animation sequence 
    /// and passing out those orders to all spectators associated with the group.
    /// </summary>
    /// <param name="requestedAnimation">The name of the animation being requested.</param>
    public void Animate(SpectatorAnimation requestedAnimation)
    {
        //assemble animation queues based on animation requested
        switch (requestedAnimation)
        {
            case SpectatorAnimation.TentWalk:
                ConsolePrint("Building Tent Walk Animation queues..");
                // -- invader walks into / out of tents in camp --
                // rotate to face the tent (SPECTATORS look at transform 1)
                AddAnimationToQueue(AnimationType.WaitForCue, Vector3.one);
                AnimateSpectators(AnimationType.Rotate, targetLocationList[1].position - targetLocationList[0].position);
                // initiate walking animation (SPECTATORS Walk)
                AnimateSpectators(AnimationType.Cue, Vector3.one);
                AnimateSpectators(AnimationType.March, Vector3.one);
                // move group to the target location (GROUP move to transform 1)
                AddAnimationToQueue(AnimationType.MoveSquad, targetLocationList[1].position);
                // pause for a duration (SPECTATOR idle [duration])
                AnimateSpectators(AnimationType.Idle, Vector3.one);
                AnimationDelay(UnityEngine.Random.Range(5, 15));
                // rotate to look at original position (SPECTATORS look at transform 0)
                AnimateSpectators(AnimationType.Rotate, targetLocationList[0].position - targetLocationList[1].position);
                AnimateSpectators(AnimationType.Cue, Vector3.one);
                AnimateSpectators(AnimationType.March, Vector3.one);
                // move group to new location (GROUP move to transform 0)
                AddAnimationToQueue(AnimationType.MoveSquad, targetLocationList[0].position);
                AddAnimationToQueue(AnimationType.WaitForCue, Vector3.one);
                // rotate to starting angle (SPECTATOR align with orientation of transform 0)
                AnimateSpectators(AnimationType.Rotate, targetLocationList[0].forward);
                // return to idle animation (SPECTATOR Idle)
                AnimateSpectators(AnimationType.Cue, Vector3.one);
                AnimateSpectators(AnimationType.Idle, Vector3.one);
                //report animation complete.
                AddAnimationToQueue(AnimationType.Done, Vector3.one);
                break;

            case SpectatorAnimation.WanderingInfantry:
                // march around the left sideline, moving to position, facing their king and pausing, then moving back to their starting position, facing board center. 
                AddAnimationToQueue(AnimationType.WaitForCue, Vector3.one);
                // Rotate group to first orientation and go there
                AnimateWaypointTraversal(1, 0);
                // rotate to face the team's king and pause
                AnimateSpectators(AnimationType.Rotate, gameControlScript.playerScripts[0].kingSquadScript.transform.position - targetLocationList[0].position);
                AnimateSpectators(AnimationType.Cue, Vector3.one);
                AnimateSpectators(AnimationType.Idle, Vector3.one);
                AnimationDelay(UnityEngine.Random.Range(5, 10));
                //rotate to look at the other king and pause
                AnimateSpectators(AnimationType.Rotate, gameControlScript.playerScripts[1].kingSquadScript.transform.position - targetLocationList[0].position);
                AnimateSpectators(AnimationType.Cue, Vector3.one);
                AnimateSpectators(AnimationType.Idle, Vector3.one);
                AnimationDelay(UnityEngine.Random.Range(4, 12));
                // return to starting position                 
                AnimateWaypointTraversal(0, 1);
                // rotate to starting angle
                AnimateSpectators(AnimationType.Rotate, targetLocationList[1].forward);
                AnimateSpectators(AnimationType.Cue, Vector3.one);
                AnimateSpectators(AnimationType.Idle, Vector3.one);
                //report animation complete.
                AddAnimationToQueue(AnimationType.Done, Vector3.one);
                break;

            case SpectatorAnimation.WanderingKnights:
                // the attacker knights move down the right side of the board, face their king, pause, then return and face board center
                AddAnimationToQueue(AnimationType.WaitForCue, Vector3.one);
                //move to location 1, next to the board
                AnimateWaypointTraversal(0, 1);
                // rotate to face the team's king, then pause
                AnimateSpectators(AnimationType.Rotate, gameControlScript.playerScripts[0].kingSquadScript.transform.position - targetLocationList[1].position);
                AnimateSpectators(AnimationType.Cue, Vector3.one);
                AnimateSpectators(AnimationType.Idle, Vector3.one);
                AnimationDelay(UnityEngine.Random.Range(5, 12));
                //rotate to look at the other king and pause
                AnimateSpectators(AnimationType.Rotate, gameControlScript.playerScripts[1].kingSquadScript.transform.position - targetLocationList[1].position);
                AnimateSpectators(AnimationType.Cue, Vector3.one);
                AnimateSpectators(AnimationType.Idle, Vector3.one);
                AnimationDelay(UnityEngine.Random.Range(4, 15));
                // walk forward through all waypoints, back to starting location
                AnimateWaypointTraversal(1, targetLocationList.Count);
                // rotate to starting angle 
                AnimateSpectators(AnimationType.Rotate, targetLocationList[0].forward);
                // return to idle animation (SPECTATOR Idle)
                AnimateSpectators(AnimationType.Cue, Vector3.one);
                AnimateSpectators(AnimationType.Idle, Vector3.one);
                //report animation complete.
                AddAnimationToQueue(AnimationType.Done, Vector3.one);
                break;

            case SpectatorAnimation.HeavyInfantryVsArchers:
                // EVENT 3: a pair of heavy infantry invaders advance up the right side of the board, then stop and defend. 
                // a group of defender archers approach from the castle side and fire on them. defenders block, with spark effect 
                // all units return to their original positions. 
                if (spectatorClass == SoldierClass.HeavyInfantry)
                {
                    AddAnimationToQueue(AnimationType.WaitForCue, Vector3.one);
                    AnimateWaypointTraversal(0, targetLocationList.Count - 1);
                    //stand in place anbd block until attacked
                    AnimateSpectators(AnimationType.Block, Vector3.one);
                    //go to idle animation and wait, as if considering pressing the attack
                    AnimateSpectators(AnimationType.Idle, Vector3.one);
                    AnimateSpectators(AnimationType.Cue, Vector3.one);
                    AnimationDelay(1.5f);
                    //return to starting point                 
                    AnimateWaypointTraversal(targetLocationList.Count - 1, 0);
                    //face campfire
                    foreach (SpectatorScript spectator in spectatorList)
                    {
                        spectator.AddAnimationToQueue(AnimationType.Rotate, targetLocationList[0].position - spectator.transform.position);
                    }
                    // return to idle animation
                    AnimateSpectators(AnimationType.Idle, Vector3.one);
                    AnimateSpectators(AnimationType.Cue, Vector3.one);
                    //report animation complete.
                    AddAnimationToQueue(AnimationType.Done, Vector3.one);
                }//heavy infantry animation queue 
                else    //archers
                {
                    //end idle archery practice animation
                    HaltSpectators();
                    //small delay to finish prior animations and to sync with opponents
                    AnimationDelay(10);
                    //traverse the waypoints.
                    AnimateWaypointTraversal(0, targetLocationList.Count - 1);
                    // pause to ensure blocker animation is ready for deflect command
                    AnimateSpectators(AnimationType.Idle, Vector3.one);
                    AnimateSpectators(AnimationType.Cue, Vector3.one);
                    AnimationDelay(2);
                    //attack
                    AnimateSpectators(AnimationType.Attack, Vector3.one);
                    AnimateSpectators(AnimationType.Idle, Vector3.one);
                    //wait for defenders to give up and retreat
                    AnimateSpectators(AnimationType.Cue, Vector3.one);
                    AnimationDelay(3);
                    // return to base
                    AnimateWaypointTraversal(targetLocationList.Count - 1, 0);
                    // Face archery range 
                    AnimateSpectators(AnimationType.Rotate, targetLocationList[0].forward);
                    // return to idle attack animation
                    AnimateSpectators(AnimationType.Idle, Vector3.one);
                    AnimateSpectators(AnimationType.Cue, Vector3.one);
                    AnimateSpectators(AnimationType.IdleAttacking, Vector3.one);
                    //report animation complete.
                    AddAnimationToQueue(AnimationType.Done, Vector3.one);
                }//archer animation queue 
                break;

            case SpectatorAnimation.MercenariesVsInfantry:
                // EVENT 4: a pair of mercenary Invaders and wandering infantry defenders to the right side of the board. 
                // each group takes a turn blocking and being attacked. 
                // all units return to their positions. 
                int attackDefendIterations = 3;
                if (spectatorClass == SoldierClass.Mercenary)
                {
                    AddAnimationToQueue(AnimationType.WaitForCue, Vector3.one);
                    //traverse the location list                    
                    AnimateWaypointTraversal(0, targetLocationList.Count - 1);
                    //perform attack/block cycles
                    for (int x = 0; x < attackDefendIterations; x++)
                    {
                        //block until counterattack is received
                        AnimateSpectators(AnimationType.Block, Vector3.one);
                        AnimateSpectators(AnimationType.Idle, Vector3.one);
                        // delay to allow groups to complete animations and sync
                        AnimateSpectators(AnimationType.Cue, Vector3.one);
                        AnimationDelay(3);
                        // attack
                        AnimateSpectators(AnimationType.Attack, Vector3.one);
                        AnimateSpectators(AnimationType.Idle, Vector3.one);
                    }

                    //go to idle animation and wait a few seconds, standing in stalemate
                    AnimateSpectators(AnimationType.Cue, Vector3.one);
                    AnimationDelay(1.5f);
                    // retreat
                    AnimateWaypointTraversal(targetLocationList.Count - 1, 0);
                    //face weapon rack (rotation vector must be customized per spectator, not per group)
                    foreach (SpectatorScript spectator in spectatorList)
                    {
                        spectator.AddAnimationToQueue(AnimationType.Rotate, targetLocationList[0].position - spectator.transform.position);
                    }
                    // return to idle animation
                    AnimateSpectators(AnimationType.Idle, Vector3.one);
                    AnimateSpectators(AnimationType.Cue, Vector3.one);
                    //report animation complete.
                    AddAnimationToQueue(AnimationType.Done, Vector3.one);
                }//mercenary animation queue 
                else    //infantry
                {
                    AnimationDelay(1.75f);
                    //traverse the waypoints.
                    AnimateWaypointTraversal(1, targetLocationList.Count - 1);
                    AnimateSpectators(AnimationType.Idle, Vector3.one);
                    // perform attack/defend cycles
                    for (int x = 0; x < attackDefendIterations; x++)
                    {
                        // delay 2 seconds to allow groups to complete animations and sync
                        AnimateSpectators(AnimationType.Cue, Vector3.one);
                        AnimationDelay(3f);
                        // attack
                        AnimateSpectators(AnimationType.Attack, Vector3.one);
                        AnimateSpectators(AnimationType.Idle, Vector3.one);
                        //block until counterattack is received
                        AnimateSpectators(AnimationType.Block, Vector3.one);
                        AnimateSpectators(AnimationType.Idle, Vector3.one);
                    }
                    //wait for defenders to give up and retreat
                    AnimateSpectators(AnimationType.Cue, Vector3.one);
                    AnimationDelay(3f);
                    // return to base
                    AnimateWaypointTraversal(targetLocationList.Count - 1, 1);
                    // Face the field
                    AnimateSpectators(AnimationType.Rotate, targetLocationList[0].forward);
                    // return to idle animation
                    AnimateSpectators(AnimationType.Idle, Vector3.one);
                    AnimateSpectators(AnimationType.Cue, Vector3.one);
                    //report animation complete.
                    AddAnimationToQueue(AnimationType.Done, Vector3.one);
                }//infantry animation queue 
                break;
        }//switch
    }//animate

    private void GroupAnimationFrameUpdate()
    {

        if (currentAnimationTask == null)
        {
            animationState = AnimationState.Idle;
            CheckAnimationQueue();
        }//no longer animating
        else if (animationState == AnimationState.SquadMarching)
        {
            MovementFrameUpdate(currentAnimationTask.targetVector);
        }//moving logic
    }

    /// <summary>
    /// Will pull the next animation from the queue and run it if the queue is not empty, and the character is idle.
    /// </summary>
    private void CheckAnimationQueue()
    {
        // if not currently doing anything, check the animation queue for a new action. 
        if (animationQueue.Count > 0)
        {
            currentAnimationTask = animationQueue[0];
            animationQueue.RemoveAt(0);
            ConsolePrint("Moving to animation: " + currentAnimationTask.animationType);

            switch (currentAnimationTask.animationType)
            {
                case AnimationType.MoveSquad:
                    ConsolePrint("Moving group.");
                    //the entire group moves across the field
                    animationState = AnimationState.SquadMarching;
                    //ConsolePrint("Current location: " + transform.position.ToString() + " - Target location: " + currentAnimationTask.targetVector.ToString());
                    break;

                case AnimationType.WaitForCue:
                    ConsolePrint("Waiting for cue.");
                    //hold this state and do not advance through animation queue until given an external trigger.
                    animationState = AnimationState.WaitingForCue;
                    break;

                case AnimationType.Cue:
                    ConsolePrint("Cueing associated spectators.");
                    // notify child scripts that they may proceed with their animation queue
                    foreach (SpectatorScript spectator in spectatorList)
                        spectator.AnimationCue();
                    currentAnimationTask = null;
                    break;

                case AnimationType.Delay:
                    //the magnitude of the animation vector on any axis (all are equal) is the delay value.
                    delayTimer = currentAnimationTask.targetVector.x;
                    ConsolePrint("Delaying for : " + delayTimer.ToString() + " seconds.");
                    break;

                case AnimationType.Done:
                    ConsolePrint("Done.");
                    parentControlScript.ReportAnimationComplete();
                    currentAnimationTask = null;
                    break;

            }//animation type switch 
        }//animations remain in queue
    }//check animation queue

    /// <summary>
    /// Takes an animation request and distributes it to all spectators assopciated with the group.
    /// </summary>
    /// <param name="animationName">The name of the animation being requested.</param>
    /// <param name="animationVector">The vector associated with that animation, if any. Usually used for rotation animations.</param>
    private void AnimateSpectators(AnimationType animationName, Vector3 animationVector)
    {
        foreach (SpectatorScript spectator in spectatorList)
        {
            spectator.AddAnimationToQueue(animationName, animationVector);
        }
    }

    /// <summary>
    /// Receives an animation request by description, and converts it to an AnimationTask object and adds it to the queue.
    /// Copied from CharacterScript, likely meaning further hierarchy updates needed among scripts, but that optimization can be added later
    /// </summary>
    /// <param name="animationType">The name of the animation being requested.</param>
    /// <param name="animationVector">The vector associated with that animation, if any. Usually used for rotation animations.</param>
    private void AddAnimationToQueue(AnimationType animationType, Vector3 animationVector)
    {
        AnimationTask nextTask = new AnimationTask(animationType, animationVector);
        animationQueue.Add(nextTask);
    }//AddAnimationToQueue

    /// <summary>
    /// Should be called by a repeating method (like Update), as it only moves one frame's distance per call.
    /// Physically move the unit toward the designated target. Should usually be accompanied by a Walk animation.
    /// </summary>
    /// <param name="desiredLocation">The coordinates of the location this unit is sliding towards.</param>
    protected void MovementFrameUpdate(Vector3 desiredLocation)
    {
        int movementSpeed = 1;
        gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, desiredLocation, movementSpeed * Time.deltaTime);
        if (gameObject.transform.position == desiredLocation)
        {
            ConsolePrint("Movement complete.");
            animationState = AnimationState.Idle;
            //tell the spectators' march animations.
            HaltSpectators();
            currentAnimationTask = null;
        }//done moving
    } //MovementFrameUpdate

    /// <summary>
    /// A canned set of animation instructions used when a spectator group needs to pause for a specified duration.
    /// Handles cueing of group and spectators automatically
    /// </summary>
    private void AnimationDelay(float duration)
    {
        AnimateSpectators(AnimationType.WaitForCue, Vector3.one);
        AddAnimationToQueue(AnimationType.Delay, Vector3.one * duration);
        AddAnimationToQueue(AnimationType.Cue, Vector3.one);
        AddAnimationToQueue(AnimationType.WaitForCue, Vector3.one);
    }

    /// <summary>
    /// A canned set of animation instructions used when a spectator group needs to move through a list of waypoints
    /// </summary>
    private void AnimateWaypointTraversal(int startingIndex, int endingIndex)
    {
        int increment = 1;
        if (startingIndex > endingIndex)
            increment = -1;

        for (int x = startingIndex; x != endingIndex; x += increment)
        {
            AnimateSpectators(AnimationType.Rotate, targetLocationList[(x + increment) % targetLocationList.Count].position - targetLocationList[x].position);
            AnimateSpectators(AnimationType.Cue, Vector3.one);
            AnimateSpectators(AnimationType.March, Vector3.one);
            AddAnimationToQueue(AnimationType.MoveSquad, targetLocationList[(x + increment) % targetLocationList.Count].position);
            AddAnimationToQueue(AnimationType.WaitForCue, Vector3.one);
        }
    }



    /*********************
     * EXTERNAL TRIGGERS *
     *********************/

    /// <summary>
    /// An externally triggered cue to indicate that some external process has completed, 
    /// and this script may advance to the next item in its animation queue.
    /// This is accomplished by setting the animationTask to null, allowing GroupAnimationFrameUpdate
    /// to pull a new item from the queue.
    /// Will do nothing if a cue is received but the group is no longer waiting for a cue.
    /// </summary>
    public void AnimationCue()
    {
        if (animationState == AnimationState.WaitingForCue)
            currentAnimationTask = null;
    }

    /// <summary>
    /// Used when the animation involves 2 spectator groups in combat, to synchronize the attack and block animations.
    /// Called by this group's animating spectators at the exact moment when an attack makes contact with the enemy.
    /// </summary>
    public void ReportAttackHit()
    {
        opposingSpectatorGroup.TriggerBlockEffects();
    }

    /// <summary>
    /// Used when the animation involves 2 spectator groups in combat, to synchronize the attack and block animations.
    /// Called by the opposing group at the exact moment that their attack lands, so this group can trigger block particle effects.
    /// </summary>
    public void TriggerBlockEffects()
    {
        ConsolePrint("Trioggering block effects.");
        foreach (SpectatorScript spectator in spectatorList)
            spectator.Deflect();
    }



    /*************
     * UTILITIES *
     *************/

    /// <summary>
    /// Used to override the current animation task of all units with Idle. 
    /// This method is called specifically when the group is being relocated, 
    /// to tell the individual spectators when to stop the marching aniomation.
    /// </summary>
    private void HaltSpectators()
    {
        ConsolePrint("Halting spectator animations");
        foreach (SpectatorScript spectator in spectatorList)
            spectator.HaltAnimation();
    }



    /***************
     * DEBUG STUFF *
     ***************/

    /// <summary>
    /// Print debug methods when in debug mode
    /// </summary>
    private void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("SpectatorGroupScript " + gameObject.name + ": " + message);
        }
    }//console print


}//SpectatorGroupScript
