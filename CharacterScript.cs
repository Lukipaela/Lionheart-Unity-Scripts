using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A ssuperclass to contain functionality common to both units in-game and spectators used as set decotratrion.
/// </summary>
public abstract class CharacterScript : MonoBehaviour
{
    //public
    public bool isCaptain = false;  //captain is designated to play sounds that apply to the whole squad together, like marching sfx and (future) greetings / responses
    public AnimationState animationState = AnimationState.Idle;

    //editor-exposed private attributes
    [SerializeField] protected ParticleSystem bloodParticleSystem;
    [SerializeField] protected ParticleSystem sparkParticleSystem;
    [SerializeField] protected ParticleSystem speechParticleSystem;
    [SerializeField] protected AudioSource oneShotAudioSource;
    [SerializeField] protected AudioSource loopingAudioSource;
    [SerializeField] protected SkinnedMeshRenderer bodyMesh;

    //private
    protected List<AnimationTask> animationQueue = new List<AnimationTask>();
    protected AnimationTask currentAnimationTask;
    protected bool isDead = false;

    //debug
    protected readonly bool enableDebugging = true;



    /********************
     * BUILT-IN METHODS *
     ********************/

    // Start is called before the first frame update
    void Start()
    {

    }


    /*********************
     * ANIMATION METHODS *
     *********************/

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
            currentAnimationTask = null;
        }//done moving
    } //MovementFrameUpdate

    /// <summary>
    /// Should be called by a repeating method (like Update), as it only turns one frame's distance per call.
    /// Rotates the unit toward the specified vector until the unit's Forward vector matches it in direction.
    /// </summary>
    /// <param name="desiredForwardVector">The coordinates of the location this unit is sliding towards.</param>
    protected void RotationFrameUpdate(Vector3 desiredForwardVector)
    {
        int rotationSpeed = 3;
        // Calculate a rotation a step closer to the target and apply rotation to this object
        Vector3 newDirection = Vector3.RotateTowards(gameObject.transform.forward, desiredForwardVector, rotationSpeed * Time.deltaTime, 0.0f);
        gameObject.transform.rotation = Quaternion.LookRotation(newDirection);
        //if we have reached our target orientation, stop rotating.
        if (Vector3.Angle(gameObject.transform.forward, desiredForwardVector) < 1)
        {
            ConsolePrint("Rotation complete.");
            animationState = AnimationState.Idle;
            currentAnimationTask = null;
        }
    }//RotationFrameUpdate

    /// <summary>
    /// Issues the command to the Animator to trigger the requested animation.
    /// </summary>
    /// <param name="animationName">The name of the animation being requested.</param>
    protected void Animate(AnimationType animationName)
    {
        transform.GetComponent<Animator>().SetTrigger(animationName.ToString());
    }//animateUnit

    /// <summary>
    /// Must be overridden by all inheriting classes
    /// </summary>
    protected virtual void CheckAnimationQueue()
    {
    }

    /// <summary>
    /// Receives an animation request by description, and converts it to an AnimationTask object and adds it to the queue.
    /// Ignores request if the unit has been marked as dead.
    /// </summary>
    /// <param name="animationType">The name of the animation being requested.</param>
    /// <param name="animationVector">The vector associated with that animation, iof any. Usually used for rotation animations.</param>
    public void AddAnimationToQueue(AnimationType animationType, Vector3 animationVector)
    {
        if (!isDead)
        {
            AnimationTask nextTask = new AnimationTask(animationType, animationVector);
            animationQueue.Add(nextTask);
        }//not dead
    }//AddAnimationToQueue



    /*************
     * UTILITIES *
     *************/

    /// <summary>
    /// A container for mechanics to run every frame on all Character classes. 
    /// Separated from the Update() method from Monobehavior, so that customized logic can still be added there for each inheriting class
    /// </summary>
    protected void CharacterFrameUpdate()
    {
        if (currentAnimationTask == null)
        {
            animationState = AnimationState.Idle;
            CheckAnimationQueue();
        }//no longer animating
        else if (animationState == AnimationState.Rotating)
        {
            RotationFrameUpdate(currentAnimationTask.targetVector);
        }//rotating logic
        else if (animationState == AnimationState.Marching)
        {
            MovementFrameUpdate(currentAnimationTask.targetVector);
        }//moving logic

        if (loopingAudioSource != null && loopingAudioSource.isPlaying && animationState != AnimationState.Marching && animationState != AnimationState.Rotating && animationState != AnimationState.SquadMarching && isCaptain)
            loopingAudioSource.Stop();
    }


    /// <summary>
    /// Used in order to update the body color of a character to indicate which player they are associated with.
    /// </summary>
    /// <param name="teamColor">The color used by the player associated with this character.</param>
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
    /// <param name="looping">Indicates if the sound should loop indefinitely (until explicitly cancelled by some other process).</param>
    /// <param name="triggerVocalEffect">Indicates if the sound should trigger the "speaking" particle effect.</param>
    protected IEnumerator PlaySound(SoundFile soundFileToPlay, bool useRandomDelay, bool looping, bool triggerVocalEffect)
    {
        ConsolePrint("Playing track: " + soundFileToPlay.audioClip.name);
        float delay = 0;
        if (useRandomDelay)
            delay = Random.Range(0.001f, 0.1f);
        yield return new WaitForSeconds(delay);

        if (looping)
        {
            if (isCaptain)//looping currently only means Marching, and only the captain makes those sounds (else it's chaos)
            {
                loopingAudioSource.clip = soundFileToPlay.audioClip;
                loopingAudioSource.volume = soundFileToPlay.volume;
                loopingAudioSource.Play();
            }
        }
        else
            oneShotAudioSource.PlayOneShot(soundFileToPlay.audioClip, soundFileToPlay.volume);

        if (triggerVocalEffect)
            speechParticleSystem.Play();
    }//PlaySound



    /***************
     * DEBUG STUFF *
     ***************/

    protected void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("Character Script - " + gameObject.name + ": " + message);
        }
    }//console print

}
