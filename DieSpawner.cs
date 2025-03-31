using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DieSpawner : MonoBehaviour
{
    //public fields
    public DiceData diceData = new DiceData();
    //private ields
    [SerializeField] private GameControl gameControlScript;
    [SerializeField] private GameObject[] spawnPointArray;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private SoundFile successSound;
    [SerializeField] private SoundFile failureSound;
    private readonly string diePrefabAddress = "Prefabs/Objects/Lionheart_Die";


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

    }//update 


    /******************
     * CUSTOM METHODS *
     ******************/

    /// <summary>
    /// Instantiates and launches the specified number of dice from a random spawn point.
    /// </summary>
    /// <param name="dieCount">How many dice to roll.</param>
    /// <returns></returns>
    public void RollDice(int dieCount)
    {
        //destroy existing dice first
        DestroyDice();
        //set up new dice roll 
        diceData.dieCount = dieCount;
        diceData.rollStatus = "Rolling";
        int spawnLocationIndex = Random.Range(0, spawnPointArray.Length);
        GameObject spawnLocation = spawnPointArray[spawnLocationIndex];
        Vector3 newDiePosition = spawnLocation.transform.position;
        Quaternion newDieRotation = spawnLocation.transform.rotation;

        for (int i = 0; i < dieCount; i++)
        {
            //spawn one die at a 'safe' location near the spawn point (offset each die spawnpoint slightly so they dont collide on instantiation and get launched)
            GameObject newDie = Instantiate(Resources.Load<GameObject>(diePrefabAddress), newDiePosition, newDieRotation);

            newDiePosition += new Vector3(0, 1 * (i % 2), 1 * ((i + 1) % 2));
            newDieRotation *= Quaternion.Euler(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));

            newDie.GetComponent<Rigidbody>().AddForce(newDie.transform.forward * Random.Range(30, 100), ForceMode.Impulse);
            newDie.GetComponent<Rigidbody>().AddTorque(newDie.transform.up * Random.Range(-200, 200) + newDie.transform.right * Random.Range(-200, 200), ForceMode.Impulse);

        }//die instantiation FOR 
    }//roll dice

    /// <summary>
    /// Called by individual dice when they come to a stop and have determined their result
    /// </summary>
    /// <param name="result">Axe, Panic, or Arrow</param>
    /// <returns></returns>
    public void DieRollReport(DieType result)
    {
        diceData.NewResult(result);
        if (diceData.results.Count == diceData.dieCount)
            RollCompleted();
    }

    /// <summary>
    /// Destroys all instantiated dice and resets diceData to prep for new roll.
    /// </summary>
    /// <returns></returns>
    private void DestroyDice()
    {
        GameObject[] dice = GameObject.FindGameObjectsWithTag("Die");
        foreach (GameObject die in dice)
        {
            Destroy(die);
        }
        diceData.Reset();
    }

    /// <summary>
    /// Called when all die results have been tabulated, reports back to game controller.
    /// </summary>
    /// <param name=""></param>
    /// <returns></returns>
    private void RollCompleted()
    {
        diceData.rollStatus = "Idle";
        if (diceData.dieCount > 1)
        {
            if (diceData.dieCount == diceData.axeCount)
                TriggerSpecialDieAnimations(DieType.Axe);
            if (diceData.dieCount == diceData.arrowCount)
                TriggerSpecialDieAnimations(DieType.Arrow);
            if (diceData.dieCount == diceData.panicCount)
                TriggerSpecialDieAnimations(DieType.Panic);
        }
        gameControlScript.RollCompletedReport();
    }

    /// <summary>
    /// Called when all dice land with the same side up
    /// Sends a message to all rolled dice to activate a special effect    
    /// </summary>
    /// <param name="effectType">A value from the DieDype Enum which indicates which side of the dice was face-up.</param>
    private void TriggerSpecialDieAnimations(DieType effectType)
    {
        if (effectType == DieType.Axe || effectType == DieType.Arrow)
            audioSource.PlayOneShot(successSound.audioClip, successSound.volume);
        else
            audioSource.PlayOneShot(failureSound.audioClip, failureSound.volume);

        foreach (GameObject die in GameObject.FindGameObjectsWithTag("Die"))
        {
            die.GetComponent<DiceController>().TriggerSpecialEffect(effectType);
        }
    }


}//class

/// <summary>
/// A struct designed to contain the data relavent to a single roll of one or more dice.
/// </summary>
/// <param name=""></param>
/// <returns></returns>
public struct DiceData
{
    public string rollStatus;
    public int dieCount;
    public List<DieType> results;
    public int axeCount;
    public int arrowCount;
    public int panicCount;

    /// <summary>
    /// Restores all Die data to the default values in preparation for a new roll.
    /// </summary>
    public void Reset()
    {
        rollStatus = "Idle";
        dieCount = 0;
        results = new List<DieType>();
        axeCount = 0;
        arrowCount = 0;
        panicCount = 0;
    }

    /// <summary>
    /// A method used in order to add the new die result to the tracker and incremement totals
    /// </summary>
    /// <param name="result">The name of the die face which is being reported: Axe, Arrow, or Panic.</param>
    public void NewResult(DieType result)
    {
        results.Add(result);
        switch (result)
        {
            case DieType.Axe:
                axeCount++;
                break;
            case DieType.Arrow:
                arrowCount++;
                break;
            case DieType.Panic:
                panicCount++;
                break;
        }//switch
    }//NewResult

}   //DiceData struct

