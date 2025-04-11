using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A script to control NPC soldiers arrayed around the outside of the battlefield in the background. 
/// Contains methods for passively animating random actions based on where the soldier is located. 
/// </summary>
public class SpectatorScript : CharacterScript
{
    //private attributes exposed in the UI 
    [SerializeField] private int playerID;
    [SerializeField] private SpectatorGroupScript parentGroupScript;

    //private 
    private bool loopAttacking = false;
    private float loopAttackCooldown = 0;

    //debug - override CharacterScript logging at Unit level
    new private bool enableDebugging = false;



    /********************
     * BUILT-IN METHODS *
     ********************/

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        CharacterFrameUpdate();
        if (loopAttacking)
            LoopAttack();
    }



    /**********************
     * ANIMATION CONTROLS *
     **********************/

    /// <summary>
    /// Overides the method from the CharacterScript to perform animations related to an active game piece.
    /// Will pull the next animation from the queue and run it if the queue is not empty, and the character is idle.
    /// </summary>
    protected override void CheckAnimationQueue()
    {
        // if not currently doing anything, check the animation queue for a new action. 
        if (animationQueue.Count > 0)
        {
            loopAttacking = false;  // override idle looping attack animation, if active.
            currentAnimationTask = animationQueue[0];
            animationQueue.RemoveAt(0);
            ConsolePrint("Moving to animation: " + currentAnimationTask.animationType);

            switch (currentAnimationTask.animationType)
            {
                case AnimationType.Cue:
                    if (isCaptain)
                        parentGroupScript.AnimationCue();
                    currentAnimationTask = null;
                    break;

                case AnimationType.WaitForCue:
                    animationState = AnimationState.WaitingForCue;
                    break;

                case AnimationType.March:
                    if (currentAnimationTask.targetVector == Vector3.one)
                        animationState = AnimationState.SquadMarching;   //need to animate, but not physically move the unit. requires explicit order to stop.
                    else
                        animationState = AnimationState.Marching;
                    Animate(AnimationType.March);
                    break;

                case AnimationType.Rotate:
                    animationState = AnimationState.Rotating;
                    ConsolePrint("Current forward vector: " + transform.forward.ToString() + " - Target location: " + currentAnimationTask.targetVector.ToString());
                    Animate(AnimationType.March);
                    break;

                case AnimationType.Attack:
                    animationState = AnimationState.Attacking;
                    Animate(AnimationType.Attack);
                    break;

                case AnimationType.Idle:
                    animationState = AnimationState.Idle;
                    Animate(AnimationType.Idle);
                    currentAnimationTask = null;
                    break;

                case AnimationType.Block:
                    animationState = AnimationState.Blocking;
                    Animate(AnimationType.Block);
                    break;

                case AnimationType.IdleAttacking:
                    loopAttacking = true;
                    currentAnimationTask = null;
                    break;
            }//animation type switch 
        }//animations remain in queue
    }//check animation queue

    /// <summary>
    /// An externally triggered cue to indicate that some external process has completed, 
    /// and this script may advance to the next item in its animation queue.
    /// This is accomplished by setting the aniumationTask to null, allowing CharacterFrameUpdate
    /// to pull a new item from the queue.
    /// Will do nothing if a cue is received and the character is not waiting for one is no longer waiting for a cue.
    /// </summary>
    public void AnimationCue()
    {
        if (animationState == AnimationState.WaitingForCue)
            currentAnimationTask = null;
    }


    /// <summary>
    /// Called when a loping animation is playing, in order to end that loop and proceed to somethign new.
    /// </summary>
    public void HaltAnimation()
    {
        ConsolePrint("Halting Animation");
        currentAnimationTask = null;
        Animate(AnimationType.Idle);
        loopAttacking = false;
    }

    /// <summary>
    /// Invokes a repeating attack animation, used as a form of idle animation.
    /// </summary>
    private void LoopAttack()
    {
        if (loopAttackCooldown <= 0)
        {
            ConsolePrint("Loop attacking");
            Animate(AnimationType.Attack);
            loopAttackCooldown = 5;
        }
        else
            loopAttackCooldown -= Time.deltaTime;
    }



    /********************
     * ANIMATION EVENTS *
     ********************/

    /// <summary>
    /// Called by the ControlScript when an attacker has reached the AttackHit trigger, and this defender is not being killed.
    /// Plays SFX and activates particle effect.
    /// </summary>
    public void Deflect()
    {
        ConsolePrint("Deflect called");
        sparkParticleSystem.Play();
        currentAnimationTask = null;
    }

    /// <summary>
    /// Called by the final frame of the Block animation, to indicate that the attacker's animation can now begin.
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    public void BlockEnd()
    {
        ConsolePrint("BlockEnd animation event called");
    }

    /// <summary>
    /// Called by the frame of the Attack animation where the weapon makes it's first sound. 
    /// On most units, this is swinging the weapon. on archers, this is drawing their bow.
    /// </summary>
    public void AttackSoundPrimary()
    {
        //ConsolePrint("AttackSoundPrimary animation event called");
    }

    /// <summary>
    /// Called by the frame of the Attack animation where the weapon makes it's second sound. 
    /// On archers, this is loosing their bow.
    /// </summary>
    public void AttackSoundSecondary()
    {
        //ConsolePrint("AttackSoundSecondary animation event called");
    }

    /// <summary>
    /// Called at the exact frame when THIS unit's attack animation would make contact with an enemy. 
    /// Used in order to send a trigger to the enemy to Die or deflect
    /// </summary>
    public void AttackHit()
    {
        if (isCaptain && currentAnimationTask != null && currentAnimationTask.animationType == AnimationType.Attack)
        {
            ConsolePrint("Reporting attack hit to group.");
            parentGroupScript.ReportAttackHit();
        }
    }

    /// <summary>
    /// Called by the final frame of the Attack animation, to indicate that the attacker is now free to return to their home tile/stance.
    /// </summary>
    public void AttackEnd()
    {
        ConsolePrint("AttackEnd animation event called");
        if (currentAnimationTask != null && currentAnimationTask.animationType != AnimationType.WaitForCue)
            currentAnimationTask = null;
    }



    /***************
     * DEBUG STUFF *
     ***************/

    /// <summary>
    /// Overrides the version of this method defined by the parent class CharacterScript
    /// </summary>
    new private void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("Spectator Script - Team " + playerID + ", " + gameObject.name + ": " + message);
        }
    }//console print

}
