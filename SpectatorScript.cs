using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A script to control NPC soldiers arrayed around the outside of the battlefield in the background. 
/// Contains methods for passively animating random actions based on where the soldier is located. 
/// </summary>
public class SpectatorScript : CharacterScript
{

    [SerializeField] private int playerID;



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
            currentAnimationTask = animationQueue[0];
            animationQueue.RemoveAt(0);
            ConsolePrint("Moving to animation: " + currentAnimationTask.animationType);

            switch (currentAnimationTask.animationType)
            {
                case AnimationType.March:
                    if (currentAnimationTask.targetVector == Vector3.one)
                        animationState = AnimationState.SquadMarching;   //need to animate, but not physically move the unit. requires explicit order to stop.
                    else
                        animationState = AnimationState.Marching;
                    Animate(AnimationType.March);
                    //StartCoroutine(PlaySound(parentSquadScript.GetUnitSoundEffect("Movement"), useRandomDelay: false, looping: true, triggerVocalEffect: false));
                    break;

                case AnimationType.Rotate:
                    animationState = AnimationState.Rotating;
                    Animate(AnimationType.March);
                    //StartCoroutine(PlaySound(parentSquadScript.GetUnitSoundEffect("Movement"), useRandomDelay: false, looping: true, triggerVocalEffect: false));
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

                case AnimationType.Cheer:
                    animationState = AnimationState.Cheering;
                    Animate(AnimationType.Cheer);
                    break;

            }//animation type switch 
        }//animations remain in queue
    }//check animation queue




}
