using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitScript : MonoBehaviour
{
    public bool isCaptain = false;  //captain is designated to play sounds that apply to the whole squad together, like marching sfx and (future) greetings / responses
    public SquadScript parentSquadScript;

    //animation controls
    [SerializeField] private AudioSource unitAudioSource;
    [SerializeField] private AudioClip attackSound;
    [SerializeField] private AudioClip placementSound;
    [SerializeField] private AudioClip deathSound;
    [SerializeField] private AudioClip marchSound;
    [SerializeField] private AudioClip blockSound;
    [SerializeField] private AudioClip selectedSound;
    [SerializeField] private SkinnedMeshRenderer bodyMesh;
    [SerializeField] private ParticleSystem bloodParticleSystem;
    [SerializeField] private ParticleSystem sparkParticleSystem;
    [SerializeField] private string unitClass;    //archer, knight, etc
    private readonly int rotationSpeed = 3;
    private readonly int movementSpeed = 1;
    private List<AnimationTask> animationQueue = new List<AnimationTask>();
    private AnimationTask currentAnimationTask;
    private bool isDead = false;
    [SerializeField] public string animationState = "Idle";    //serialized to aid in debugging
    //debug
    private static readonly bool enableDebugging = false;


    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start()
    {
    }

    void Update()
    {
        if (currentAnimationTask == null)
        {
            animationState = "Idle";
            CheckAnimationQueue();
        }//no longer animating
        else if (animationState == "Rotating")
        {
            RotationFrameUpdate(currentAnimationTask.targetVector);
        }//rotating logic
        else if (animationState == "Marching")
        {
            MovementFrameUpdate(currentAnimationTask.targetVector);
        }//moving logic

        if (unitAudioSource.isPlaying && unitAudioSource.clip == marchSound && animationState != "Marching" && animationState != "Rotating" && animationState != "SquadMarching")
            unitAudioSource.Stop();

    }//Update


    /*********************
     * ANIMATION METHODS *
     *********************/
    /// <summary>
    /// Should be called by a repeating method (like Update), as it only turns one frame's distance per call.
    /// Rotates the unit toward the specified vector until the unit's Forward vector matches it in direction.
    /// </summary>
    /// <param name="desiredForwardVector">The coordinates of the location this unit is sliding towards.</param>
    private void RotationFrameUpdate(Vector3 desiredForwardVector)
    {
        // Calculate a rotation a step closer to the target and apply rotation to this object
        Vector3 newDirection = Vector3.RotateTowards(gameObject.transform.forward, desiredForwardVector, rotationSpeed * Time.deltaTime, 0.0f);
        gameObject.transform.rotation = Quaternion.LookRotation(newDirection);
        //if we have reached our target orientation, stop rotating.
        if (Vector3.Angle(gameObject.transform.forward, desiredForwardVector) < 1)
        {
            ConsolePrint("Rotation complete.");
            animationState = "Idle";
            currentAnimationTask = null;
        }
    }//RotationFrameUpdate

    /// <summary>
    /// Should be called by a repeating method (like Update), as it only moves one frame's distance per call.
    /// Physically move the unit toward the designated target. Should usually be accompanied by a Walk animation.
    /// </summary>
    /// <param name="desiredLocation">The coordinates of the location this unit is sliding towards.</param>
    private void MovementFrameUpdate(Vector3 desiredLocation)
    {
        gameObject.transform.position = Vector3.MoveTowards(gameObject.transform.position, desiredLocation, movementSpeed * Time.deltaTime);
        if (gameObject.transform.position == desiredLocation)
        {
            ConsolePrint("Movement complete.");
            animationState = "Idle";
            currentAnimationTask = null;
        }//done moving
    } //MovementFrameUpdate

    /// <summary>
    /// Receives an animation request by description, and converts it to an AnimationTask object and adds it to the queue.
    /// Ignores request if the unit has been marked as dead.
    /// </summary>
    /// <param name="animationType">The name of the animation bewing requested.</param>
    /// <param name="animationVector">The vector associated with that animation, iof any. Usually used for rotation animations.</param>
    public void AddAnimationToQueue(string animationType, Vector3 animationVector)
    {
        if (!isDead)
        {
            AnimationTask nextTask = new AnimationTask(animationType, animationVector);
            animationQueue.Add(nextTask);
        }//not dead
    }//AddAnimationToQueue

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
                case "March":
                    if (currentAnimationTask.targetVector == Vector3.one)
                        animationState = "SquadMarching";   //need to animate, but not physically move the unit. requires explicit order to stop.
                    else
                        animationState = "Marching";
                    AnimateUnit("March");
                    StartCoroutine(PlaySound(marchSound, true, true));
                    break;

                case "Rotate":
                    animationState = "Rotating";
                    AnimateUnit("March");
                    StartCoroutine(PlaySound(marchSound, true, true));
                    break;

                case "Attack":
                    animationState = "Attacking";
                    AnimateUnit("Attack");
                    break;

                case "Idle":
                    animationState = "Idle";
                    AnimateUnit("Idle");
                    currentAnimationTask = null;
                    break;

                case "Block":
                    animationState = "Blocking";
                    AnimateUnit("Block");
                    break;

                case "Cheer":
                    animationState = "Cheering";
                    AnimateUnit("Cheer");
                    break;

                case "Die":
                    animationState = "Dying";
                    Die();
                    break;
            }//animation type switch 
        }//animations remain in queue
    }//check animation queue

    /// <summary>
    /// Plays Death animation and sound effect. 
    /// Marks the unit as Dead.
    /// </summary>
    public void Die()
    {
        unitAudioSource.Stop(); //End marching sound
        //rotate a few degrees around the Y axis at random before animating the death, for variety 
        int rotationAmount = Random.Range(-35, 35); //measured in degrees
        Quaternion rotationToApply = Quaternion.Euler(0f, rotationAmount, 0f);
        transform.rotation = transform.rotation * rotationToApply;
        //call the death animation, mark unit dead and animating
        bloodParticleSystem.Play();
        AnimateUnit("Die");
        StartCoroutine(PlaySound(deathSound, true, false));
        isDead = true;
    }

    /// <summary>
    /// Issues the command to the Animator to trigger the requested animation.
    /// </summary>
    /// <param name="animationName">The name of the animation bewing requested.</param>
    private void AnimateUnit(string animationName)
    {
        transform.GetComponent<Animator>().SetTrigger(animationName);
    }//animateUnit

    /// <summary>
    /// Adds Idle to the animation queue. Clears currentAnimationTask. 
    /// Separated to a non-parameterized method to facilitate the use of Invoke().
    /// </summary>
    public void AddIdleTask()
    {
        currentAnimationTask = null;
        AddAnimationToQueue("Idle", Vector3.one);
    }



    /*************
     * UTILITIES *
     *************/

    /// <summary>
    /// called by GameControl at the start of the game, or when a mercenary is converted (future feature) to add team-based color to the soldiers' bodies.
    /// </summary>
    /// <param name="teamColor">The color associated with the team which now owns the unit.</param>
    public void SetColor(Color teamColor)
    {
        bodyMesh.material.color = teamColor;
    }

    /// <summary>
    /// Stops current sound (if any), and plays the requested track. Optionally can inseret a small random offset delay before starting, and can be made to loop.
    /// </summary>
    /// <param name="soundToPlay">The Audio Clip to be played.</param>
    /// <param name="useRandomDelay">Indicates if a small random delay should be imposed before playing the clip. 
    /// This helps prevent all members of the squad from playinmg the same sound at exactly the same moment, stacking the volume.</param>
    /// <param name="loopTrack">Indicates if the sound should loop indefinitely (until explicitly cancelled by some other process).</param>
    private IEnumerator PlaySound(AudioClip soundToPlay, bool useRandomDelay, bool loopTrack)
    {
        float delay = 0;
        if (useRandomDelay)
            delay = Random.Range(0, 0.5f);
        yield return new WaitForSeconds(delay);
        unitAudioSource.Stop();
        unitAudioSource.loop = loopTrack;
        unitAudioSource.clip = soundToPlay;
        if (isCaptain || soundToPlay != marchSound)  //only the captain plays marching sounds
            unitAudioSource.Play();
    }



    /**********
     * EVENTS *
     **********/

    /// <summary>
    /// Called by the ControlScript when an attacker has reached the AttackHit trigger, and this defender is not being killed.
    /// Plays SFX and activates particle effect.
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    public void Deflect()
    {
        ConsolePrint("Deflect called");
        sparkParticleSystem.Play();
        StartCoroutine(PlaySound(blockSound, true, false));
        currentAnimationTask = null;
    }

    /// <summary>
    /// Called at the exact frame when THIS unit's attack animation would make contact with an enemy. 
    /// Used in order to send a trigger to the enemy to Die or deflect
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    public void AttackHit()
    {
        ConsolePrint("AttackHit animation event called");
        parentSquadScript.ReportAttackHit();
    }

    /// <summary>
    /// Called by the final frame of the Block animation, to indicate that the attacker's animation can now begin.
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    public void BlockEnd()
    {
        ConsolePrint("BlockEnd animation event called");
        parentSquadScript.ReportBlockAnimationComplete();
        unitAudioSource.Stop(); //End marching sound
    }

    /// <summary>
    /// Called by the final frame of the Attack animation, to indicate that the attacker is now free to return to their home tile/stance.
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    public void AttackEnd()
    {
        ConsolePrint("AttackEnd animation event called");
        currentAnimationTask = null;
    }

    /// <summary>
    /// Called by the end of the death animation event, destroys the game object and decouples it from the squad.
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    public void DeathEnd()
    {
        ConsolePrint("DeathEnd called");
        //remove this unit from the parent tile's ownership, mark it for future destruction (if not persisting bodies on the field)
        transform.parent = null;
        if (!GameSettings.persistBodies && unitClass != "King")
            Destroy(gameObject, GameSettings.deathLingerDuration);//destroy this object X seconds after it has performed its animation
        currentAnimationTask = null;
    }

    /// <summary>
    /// Called at the final frame of Cheer, to trigger a transition back to Idle
    /// </summary>
    public void CheerEnd()
    {
        ConsolePrint("Cheer End animation event called");
        currentAnimationTask = null;
    }

    /// <summary>
    /// Called at the conclusion of a squad relocation march, as only the Squad knows when the destination was reached, not the Unit.
    /// </summary>
    public void MarchEnd()
    {
        AddIdleTask();
        unitAudioSource.Stop(); //End marching sound
    }

    /// <summary>
    /// A polling function called by the parent Squad script in order to determine if there are any unit-level animations running.
    /// Used in order to determine if new actions can be taken now, or if the squad must continue to wait for the current queue to clear. 
    /// If the only animation running is a king's cheer, will return False.
    /// </summary>
    public bool IsAnimating()
    {
        if (unitClass == "King" && currentAnimationTask != null && currentAnimationTask.animationType == "Cheer")
            return false;   //do not report a progress-blocking animation if it's just the king cheering. 
        if (currentAnimationTask != null || animationQueue.Count > 0)
            return true;
        else
            return false;
    }

    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("Unit Script - Team " + parentSquadScript.ownerID + ", " + gameObject.name + ": " + message);
        }
    }//console print

}//class
