using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitScript : MonoBehaviour
{
    public SquadScript parentSquadScript;
    public AudioSource unitAudioSource;
    public AudioClip attackSound;
    public AudioClip placementSound;
    public AudioClip deathSound;
    public AudioClip marchSound;
    public AudioClip selectedSound;
    public string unitClass;    //archer, knight, etc
    public SkinnedMeshRenderer bodyMesh;

    //animation controls
    private bool isRotating = false;
    private bool isMoving = false;
    private bool isAnimating = false;
    private readonly int rotationSpeed = 3;
    private readonly int movementSpeed = 1;
    private Vector3 rotationTargetVector;
    private Vector3 movementTargetVector;
    private List<AnimationTask> animationQueue = new List<AnimationTask>();
    private AnimationTask currentAnimationTask;
    private bool isDead = false;
    private bool animatingAttack = false;
    private bool animatingDeath = false;
    //debug
    private static readonly bool enableDebugging = true;


    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start(){
        animationQueue = new List<AnimationTask>();
    }

    void Update(){
        if (isRotating){
            RotationFrameUpdate(gameObject, rotationTargetVector);
        }//rotating logic

        if (isMoving){
            MovementFrameUpdate(gameObject, currentAnimationTask.targetVector);
        }//moving logic

        if (!isMoving && !isRotating && !animatingAttack && !animatingDeath){
            CheckAnimationQueue();
        }//no longer animating
    }//Update


    /*********************
     * ANIMATION METHODS *
     *********************/

    private void RotationFrameUpdate(GameObject rotatingObject, Vector3 desiredForwardVector)
    {
        //if we have reached our target orientation, stop rotating.
        if (Vector3.Angle(rotatingObject.transform.forward, desiredForwardVector) < 1)
        {
            isRotating = false;
        }
        else
        {
            // Rotate the forward vector towards the target direction by one step
            Vector3 newDirection = Vector3.RotateTowards(rotatingObject.transform.forward, desiredForwardVector, rotationSpeed * Time.deltaTime, 0.0f);
            // Calculate a rotation a step closer to the target and applies rotation to this object
            rotatingObject.transform.rotation = Quaternion.LookRotation(newDirection);
        }

    }//RotationFrameUpdate

    private void MovementFrameUpdate(GameObject movingObject, Vector3 desiredLocation)
    {
        movingObject.transform.position = Vector3.MoveTowards(movingObject.transform.position, desiredLocation, movementSpeed * Time.deltaTime);
        if (movingObject.transform.position == desiredLocation)
        {
            isMoving = false;
        }//done moving
    } //MovementFrameUpdate

    public void AddAnimationToQueue( string animationType, Vector3 animationVector )
    {
        //ConsolePrint("Adding animation to " + gameObject.name + " Type: " + animationType + " Along vector " + animationVector);
        if (!isDead)
        {
            AnimationTask nextTask = new AnimationTask(animationType, animationVector);
            //ConsolePrint(nextTask.ToString());
            animationQueue.Add(nextTask);
        }

}//AddAnimationToQueue

    private void CheckAnimationQueue()
    {
        // if not currently doing anything, check the animation queue for a new action. 
        if (animationQueue.Count > 0){
            if (!isAnimating){
                parentSquadScript.ReportUnitAnimationStart(gameObject.name + " starting animation queue");
                isAnimating = true;
            }

            currentAnimationTask = animationQueue[0];
            animationQueue.RemoveAt(0);
            ConsolePrint("Moving to animation: " + currentAnimationTask.animationType);
            switch (currentAnimationTask.animationType){

                case "March":
                    if (currentAnimationTask.targetVector != Vector3.one)
                    {
                        isMoving = true;
                        movementTargetVector = currentAnimationTask.targetVector;
                    }
                    AnimateUnit("March");
                    unitAudioSource.loop = true;
                    unitAudioSource.clip = marchSound;
                    if (!unitAudioSource.isPlaying)
                        unitAudioSource.Play();
                    break;

                case "Rotate":
                    isRotating = true;
                    rotationTargetVector = currentAnimationTask.targetVector;
                    AnimateUnit("March");
                    unitAudioSource.loop = true;
                    unitAudioSource.clip = marchSound;
                    if(!unitAudioSource.isPlaying)
                        unitAudioSource.Play();
                    break;

                case "Attack":
                    unitAudioSource.Stop(); //End marching sound
                    AnimateUnit("Attack"); 
                    animatingAttack = true;
                    break;

                case "Idle":
                    unitAudioSource.Stop(); //End marching sound
                    AnimateUnit("Idle");
                    break;

                case "Block":
                    unitAudioSource.Stop(); //End marching sound
                    AnimateUnit("Block");
                    break;

                case "Cheer":
                    unitAudioSource.Stop(); //End marching sound
                    AnimateUnit("Cheer");
                    break;

                case "Die":
                    unitAudioSource.Stop(); //End marching sound
                    //rotate a few degrees around the Y axis at random before animating the death, for variety 
                    int maxRotationMagnitude = 35;
                    int rotationAmount = Random.Range(-maxRotationMagnitude, maxRotationMagnitude); //measured in degrees
                    Quaternion rotationToApply = Quaternion.Euler(0f, rotationAmount, 0f);
                    transform.rotation = transform.rotation * rotationToApply;
                    //call doe death animation, mark unit dead and animating
                    AnimateUnit("Die");
                    unitAudioSource.loop = false;
                    unitAudioSource.clip = deathSound;
                    unitAudioSource.Play();
                    animatingDeath = true;
                    isDead = true;
                    break;
                    
            }//animation type switch 
        }//animations remain in queue
        else if (isAnimating)
        {
            isAnimating = false;
            parentSquadScript.ReportUnitAnimationComplete( gameObject.name + " end of animation queue");
        }
    }//check animation queue

    private ulong RandomizedSFXDelay(){
        //returns a random short duration to delay SFX generation by, so that not all units make the same sound at exactly the same time.
        const float sampleRate = 44100;
        const float secondsDelayed = .05f;
        return (ulong) Random.Range(0, sampleRate/ secondsDelayed);
    }

    private void AnimateUnit( string animationName ){
        //ConsolePrint("Setting animation trigger " + animationName);
        transform.GetComponent<Animator>().SetTrigger(animationName);
    }//animateUnit

    public void AttackHit()
    {
        ConsolePrint("AttackHit animation event called");
        parentSquadScript.ReportAttackAnimationBeginning();
    }

    public void BlockEnd()
    {
        ConsolePrint("BlockEnd animation event called");
        parentSquadScript.ReportBlockAnimationBeginning();
    }

    public void AttackEnd()
    {
        ConsolePrint("AttackEnd animation event called");
        animatingAttack = false;
    }

    public void DeathEnd()
    {
        ConsolePrint("DeathEnd called");
        //called at the end of each death animation, allows the queue to be halted to wait for the end of this animation 
        //note the end of the animation by removing the flag.
        animatingDeath = false;
        //remove this unit from the parent tile's ownership, mark it for future destruction
        transform.parent = null;
        if (!GameSettings.persistBodies && unitClass != "King")
            Destroy(gameObject, GameSettings.deathLingerDuration);//destroy this object X seconds after it has performed its animation
    }

    public void InitializeUnit(){
        // this method should do any initial setup needed after the soldier has been assigned to a squad and player
        //set body color to match team color
    }

    public void SetColor(Color teamColor){
        //called by GameControl at the start of the game, or when a mercenary is converted (future feature) to add team-based color to the soldiers' bodies.
        bodyMesh.material.color = teamColor;
    }
    
    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message){
        if (enableDebugging == true){
            Debug.Log("Unit Script - Team " + parentSquadScript.ownerID + ", " + gameObject.name + ": " + message);
        }
    }//console print

}//class
