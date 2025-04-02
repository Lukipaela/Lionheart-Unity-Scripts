using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpectatorGroupScript : MonoBehaviour
{
    //public
    bool isAnimating = false;

    //private, exposed in UI
    [SerializeField] private List<SpectatorScript> spectatorList = new List<SpectatorScript>(); //list of all characters in the group, for distributing animation commands
    [SerializeField] private List<Transform> targetLocationList = new List<Transform>();    //a list of Transforms used by this group's animations (locations, and orientation angles)
    [SerializeField] private SoldierClass groupClass;

    /********************
     * BUILT-IN METHODS *
     ********************/

    // Start is called before the first frame update
    void Start()
    {
        //the group of archers are standing at an archery range, and should be constantly shooting
        if (groupClass == SoldierClass.Archer)
            AnimateSpectators(AnimationType.IdleAttacking, Vector3.one);
    }

    // Update is called once per frame
    void Update()
    {
        if (isAnimating)
        {
            MonitorAnimations();
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
        switch (requestedAnimation)
        {
            case SpectatorAnimation.TentWalk:
                // -- invader walks into / out of tents in camp --
                isAnimating = true;
                // initiate walking animation (SPECTATORS Walk)
                // rotate to face the tent (SPECTATORS look at transform 1)
                // move group to the target location (GROUP move to transform 1)
                // pause for a duration (SPECTATOR idle [duration])
                // initiate walk animation (SPECTATOR walk)
                // rotate to look at original position (SPECTATORS look at transform 0)
                // move group to new location (GROUP move to transform 0)
                // rotate to starting angle (SPECTATOR align with orientation of transform 0)
                // return to idle animation (SPECTATOR Idle)
                break;
            case SpectatorAnimation.WanderingInfantry:
                // march around the left sideline, moving to position, facing their king and pausing, then moving back to their starting position, facing board center. 
                isAnimating = true;
                // initiate walking animation (SPECTATORS Walk)
                // rotate to face the destination (SPECTATORS look at transform 1)
                // move group to the target location (GROUP move to transform 1)
                // rotate to face the team's king (SPECTATORS look at [get transform of king from GameControl?])
                // pause for a duration (SPECTATOR idle [duration])
                // initiate walk animation (SPECTATOR walk)
                // rotate to look at original position (SPECTATORS look at transform 0)
                // move group to new location (GROUP move to transform 0)
                // rotate to starting angle (SPECTATOR align with orientation of transform 0)
                // return to idle animation (SPECTATOR Idle)
                break;
            case SpectatorAnimation.WanderingKnights:
                // the attacker knights move down the right side of the board, face their king, pause, then return and face board center
                isAnimating = true;
                break;
            case SpectatorAnimation.HeavyInfantryVsArchers:
                // EVENT 3: a pair of heavy infantry invaders advance up the right side of the board, then stop and defend. 
                // a group of defender archers approach from the castle side and fire on them. 
                // defenders block, with spark effect 
                // all units return to their original positions. 
                isAnimating = true;
                break;
            case SpectatorAnimation.MercenariesVsInfantry:
                // EVENT 4: a pair of mercenary Invaders move down the right side of the board. 
                // a group of infantry defenders move up the right side of the board to the same point
                // each group takes a turn blocking and being attacked. 
                // no deaths result. 
                // all units return to their positions. 
                isAnimating = true;
                break;
        }//switch
    }//animate

    private void MonitorAnimations()
    {
        //TODO: loop over the linked spectators. if they are no longer animating, reset boolean and report back tot he parent script. 
    }

    private void AnimateSpectators(AnimationType animationName, Vector3 animationVector)
    {
        foreach (SpectatorScript spectator in spectatorList)
        {
            spectator.AddAnimationToQueue(animationName, animationVector);
        }
    }


}//SpectatorGroupScript
