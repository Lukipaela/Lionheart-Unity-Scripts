using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A class to coordinate and manage the individual spectators around the arena. 
/// Will trigger periodic events for the spectators to perform.
/// </summary>
public class SpectatorControl : MonoBehaviour
{
    [SerializeField] private List<SpectatorScript> invaderList = new List<SpectatorScript>();
    [SerializeField] private List<SpectatorScript> defenderList = new List<SpectatorScript>();
    [SerializeField] private SpectatorGroupScript peasantGroup;
    [SerializeField] private SpectatorGroupScript roamingInfantryGroup;
    [SerializeField] private SpectatorGroupScript roamingKnightGroup;
    [SerializeField] private SpectatorGroupScript heavyInfantryGroup;
    [SerializeField] private SpectatorGroupScript archerGroup;
    [SerializeField] private SpectatorGroupScript mercenaryGroup;

    private float randomEventTimer = 10;
    private bool randomEventActive = false;
    private int animatingGroups = 0;

    /********************
     * BUILT-IN METHODS *
     ********************/

    // Start is called before the first frame update
    void Start()
    {
        Invoke("AssignColors", 0.5f);
    }

    // Update is called once per frame
    void Update()
    {
        if (!randomEventActive)
        {
            randomEventTimer -= Time.deltaTime;
            if (randomEventTimer < 0)
                TriggerRandomEvent();
        }
    }


    /******************
     * EVENT MANAGERS *
     ******************/

    /// <summary>
    /// Randomly selects a Spectator animation to activate. 
    /// Locks out further random event animations until this one is completed and a new cooldown timer is set. 
    /// </summary>
    private void TriggerRandomEvent()
    {
        //reset random timer
        randomEventTimer = Random.Range(15, 25);
        //Lock out further animation triggers until this one is reported complete
        randomEventActive = true;
        SpectatorAnimation animation = (SpectatorAnimation)Random.Range(0, System.Enum.GetValues(typeof(SpectatorAnimation)).Length);
        switch (animation)
        {
            case SpectatorAnimation.TentWalk:
                // invader walks into / out of tents in camp
                peasantGroup.Animate(animation);
                animatingGroups = 1;
                break;
            case SpectatorAnimation.WanderingInfantry:
                // defender squad of infantry marches around the left sideline, moving to position, facing their king and pausing, then moving back to their starting position, facing board center. 
                roamingInfantryGroup.Animate(animation);
                animatingGroups = 1;
                break;
            case SpectatorAnimation.WanderingKnights:
                // the attacker knights move down the right side of the board, face their king, pause, then return and face board center
                roamingKnightGroup.Animate(animation);
                animatingGroups = 1;
                break;
            case SpectatorAnimation.HeavyInfantryVsArchers:
                // EVENT 3: a pair of heavy infantry invaders advance up the right side of the board, then stop and defend. 
                // a group of defender archers approach from the castle side and fire on them. 
                // defenders block, with spark effect 
                // all units return to their original positions. 
                heavyInfantryGroup.Animate(animation);
                archerGroup.Animate(animation);
                animatingGroups = 2;
                break;
            case SpectatorAnimation.MercenariesVsInfantry:
                // EVENT 4: a pair of mercenary Invaders move down the right side of the board. 
                // a group of infantry defenders move up the right side of the board to the same point
                // each group takes a turn blocking and being attacked. 
                // no deaths result. 
                // all units return to their positions. 
                roamingInfantryGroup.Animate(animation);
                mercenaryGroup.Animate(animation);
                animatingGroups = 2;
                break;
        }//switch
    }//triggerRandomEvent


    /********************
     * EXTERNAL REPORTS *
     ********************/

    /// <summary>
    /// A method called by downstream spectator groups to notify the master control script that their animation sequence is complete.
    /// Once all animating groups have reported that they are done, reactivates the random event timer. 
    /// </summary>
    public void ReportAnimationComplete()
    {
        animatingGroups -= 1;
        if (animatingGroups == 0)
            randomEventActive = false;
    }

    /*************
     * UTILITIES *
     *************/

    /// <summary>
    /// Assigns the selected army color for each player to their respective spectators.
    /// </summary>
    private void AssignColors()
    {
        Color targetColor = GameSettings.armyRaces[GameSettings.playerRaces[1]].armyColor;
        foreach (SpectatorScript spectator in invaderList)
        {
            spectator.SetColor(targetColor);
        }
        targetColor = GameSettings.armyRaces[GameSettings.playerRaces[0]].armyColor;
        foreach (SpectatorScript spectator in defenderList)
        {
            spectator.SetColor(targetColor);
        }
    }
}
