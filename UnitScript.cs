using UnityEngine;

public class UnitScript : CharacterScript
{
    //public
    public SquadScript parentSquadScript;

    //private
    [SerializeField] private SoldierClass unitClass;    //archer, knight, etc

    //debug - override CharacterScript logging at Unit level
    new private readonly bool enableDebugging = false;


    /********************
     * BUILT-IN METHODS *
     ********************/

    void Update()
    {
        CharacterFrameUpdate();
    }//Update



    /*********************
     * ANIMATION METHODS *
     *********************/


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
                    StartCoroutine(PlaySound(parentSquadScript.GetUnitSoundEffect("Movement"), useRandomDelay: false, looping: true, triggerVocalEffect: false));
                    break;

                case AnimationType.Rotate:
                    animationState = AnimationState.Rotating;
                    Animate(AnimationType.March);
                    StartCoroutine(PlaySound(parentSquadScript.GetUnitSoundEffect("Movement"), useRandomDelay: false, looping: true, triggerVocalEffect: false));
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

                case AnimationType.Die:
                    animationState = AnimationState.Dying;
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
        //rotate a few degrees around the Y axis at random before animating the death, for variety 
        int rotationAmount = UnityEngine.Random.Range(-35, 35); //measured in degrees
        Quaternion rotationToApply = Quaternion.Euler(0f, rotationAmount, 0f);
        transform.rotation = transform.rotation * rotationToApply;
        //call the death animation, mark unit dead and animating
        bloodParticleSystem.Play();
        Animate(AnimationType.Die);
        //if the death was that of a knight, also call a horse scream sound effect
        if (unitClass == SoldierClass.Knight)
            StartCoroutine(PlaySound(parentSquadScript.GetUnitSoundEffect("Horse")
                                        , useRandomDelay: true
                                        , looping: false
                                        , triggerVocalEffect: false));
        StartCoroutine(PlaySound(parentSquadScript.GetUnitSoundEffect("Die")
                                    , useRandomDelay: true
                                    , looping: false
                                    , triggerVocalEffect: false));
        isDead = true;
    }//die

    /// <summary>
    /// An override of Die which contains the attacker's successful hit sound effect. 
    /// plays that sound effect, then invokes standard Die() mechanics
    /// </summary>
    /// <param name="attackerHitSound">An AudioClip of the sound that the attacker's weapon makes on successful hit.</param>
    public void Die(SoundFile attackerHitSound)
    {
        ConsolePrint("Dying due to sound: " + attackerHitSound.name);
        StartCoroutine(PlaySound(attackerHitSound
                                    , useRandomDelay: false
                                    , looping: false
                                    , triggerVocalEffect: false));
        Die();
    }

    /// <summary>
    /// Adds Idle to the animation queue. Clears currentAnimationTask. 
    /// Separated to a non-parameterized method to facilitate the use of Invoke().
    /// </summary>
    public void AddIdleTask()
    {
        currentAnimationTask = null;
        AddAnimationToQueue(AnimationType.Idle, Vector3.one);
    }



    /*************
     * UTILITIES *
     *************/


    /**********
     * EVENTS *
     **********/

    /// <summary>
    /// Called by the ControlScript when an attacker has reached the AttackHit trigger, and this defender is not being killed.
    /// Plays SFX and activates particle effect.
    /// </summary>
    public void Deflect()
    {
        ConsolePrint("Deflect called");
        sparkParticleSystem.Play();
        SoundFile soundEffect = parentSquadScript.GetUnitSoundEffect("Block");
        StartCoroutine(PlaySound(soundFileToPlay: soundEffect, useRandomDelay: true, looping: false, triggerVocalEffect: false));
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
        parentSquadScript.ReportBlockAnimationComplete();
        oneShotAudioSource.Stop(); //End marching sound
    }

    /// <summary>
    /// Called by the frame of the Attack animation where the weapon makes it's first sound. 
    /// On most units, this is swinging the weapon. on archers, this is drawing their bow.
    /// </summary>
    public void AttackSoundPrimary()
    {
        ConsolePrint("AttackSoundPrimary animation event called");
        StartCoroutine(PlaySound(soundFileToPlay: parentSquadScript.GetUnitSoundEffect("AttackPrimary")
                                    , useRandomDelay: true
                                    , looping: false
                                    , triggerVocalEffect: false));
    }

    public void AttackSoundSecondary()
    {
        StartCoroutine(PlaySound(soundFileToPlay: parentSquadScript.GetUnitSoundEffect("AttackSecondary")
                                    , useRandomDelay: true
                                    , looping: false
                                    , triggerVocalEffect: false));
    }

    /// <summary>
    /// Called at the exact frame when THIS unit's attack animation would make contact with an enemy. 
    /// Used in order to send a trigger to the enemy to Die or deflect
    /// </summary>
    public void AttackHit()
    {
        ConsolePrint("AttackHit animation event called");
        parentSquadScript.ReportAttackHit();
    }

    /// <summary>
    /// Called by the final frame of the Attack animation, to indicate that the attacker is now free to return to their home tile/stance.
    /// </summary>
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
        if (!GameSettings.persistBodies && unitClass != SoldierClass.King)
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
        oneShotAudioSource.Stop(); //End marching sound
    }

    /// <summary>
    /// A polling function called by the parent Squad script in order to determine if there are any unit-level animations running.
    /// Used in order to determine if new actions can be taken now, or if the squad must continue to wait for the current queue to clear. 
    /// If the only animation running is a king's cheer, will return False.
    /// </summary>
    public bool IsAnimating()
    {
        if (currentAnimationTask != null || animationQueue.Count > 0)
            return true;
        else
            return false;
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
            Debug.Log("Unit Script - Team " + parentSquadScript.ownerID + ", " + gameObject.name + ": " + message);
        }
    }//console print

}//class
