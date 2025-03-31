using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceController : MonoBehaviour
{
    //private fields
    [SerializeField] private DieSpawner dieSpawnerScript;
    [SerializeField] private AudioSource dieAudioSource;
    [SerializeField] private AudioClip dieColliderSound;
    private Rigidbody dieRigidBody;
    private Transform groundTransform;
    private bool activeDie;

    //debug
    private static readonly bool enableDebugging = false;


    /********************
     * BUILT-IN METHODS *
     ********************/

    // Start is called before the first frame update
    void Start()
    {
        dieSpawnerScript = GameObject.FindGameObjectWithTag("DiceControl").GetComponent<DieSpawner>();
        groundTransform = GameObject.FindGameObjectWithTag("Ground").transform;
        dieRigidBody = gameObject.GetComponent<Rigidbody>();
        activeDie = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (dieRigidBody.IsSleeping() && activeDie)
        {
            //find out which side faces up and report it back to the die spawner
            DieType result = GetRollResult();
            int particleEffectIndex = 0;
            switch (result)
            {
                case DieType.None:   //the die is no longe rmoving, but has also failed to land flat. 
                    ConsolePrint("Nudging crack die..");
                    int nudgeMagnitude = 100;
                    gameObject.GetComponent<Rigidbody>().AddTorque(transform.up * Random.Range(-nudgeMagnitude, nudgeMagnitude) + transform.right * Random.Range(-nudgeMagnitude, nudgeMagnitude) + transform.forward * Random.Range(-nudgeMagnitude, nudgeMagnitude));
                    break;
                case DieType.Panic:
                    activeDie = false;
                    particleEffectIndex = 0;
                    break;
                case DieType.Arrow:
                    activeDie = false;
                    particleEffectIndex = 1;
                    break;
                case DieType.Axe:
                    activeDie = false;
                    particleEffectIndex = 2;
                    break;
            }

            if (!activeDie)
            {
                //die has settled, report result and play particle effect
                dieSpawnerScript.DieRollReport(result);
                transform.GetChild(particleEffectIndex).transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                transform.GetChild(particleEffectIndex).GetComponent<ParticleSystem>().Play();
            }
        }//if sleeping, but was previously active

    }//update

    private void OnCollisionEnter(Collision collision)
    {
        dieAudioSource.PlayOneShot(dieColliderSound);
    }



    /******************
     * CUSTOM METHODS *
     ******************/

    private DieType GetRollResult()
    {
        ConsolePrint("Checking die result..");
        if ((gameObject.transform.up == groundTransform.up) || (gameObject.transform.right == -1 * groundTransform.up) || (gameObject.transform.forward == -1 * groundTransform.up))
            return DieType.Axe;
        else if ((gameObject.transform.up == -1 * groundTransform.up) || (gameObject.transform.forward == groundTransform.up))
            return DieType.Arrow;
        else if (gameObject.transform.right == groundTransform.up)
            return DieType.Panic;
        else
        {
            //the die has not settled flat on the ground yet. will be nudged.
        }
        return DieType.None;
    }

    /// <summary>
    /// Called to trigger a special particle effect when all dice land the same way up.
    /// </summary>
    /// <param name="effectType">A value from the DieDype Enum which indicates which side of the dice was face-up.</param>
    public void TriggerSpecialEffect(DieType effectType)
    {
        int particleEffectIndex = 0;
        switch (effectType)
        {
            case DieType.Panic:
                activeDie = false;
                particleEffectIndex = 3;
                break;
            case DieType.Arrow:
                activeDie = false;
                particleEffectIndex = 4;
                break;
            case DieType.Axe:
                activeDie = false;
                particleEffectIndex = 5;
                break;
        }
        transform.GetChild(particleEffectIndex).transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
        transform.GetChild(particleEffectIndex).GetComponent<ParticleSystem>().Play();
    }

    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("Die Controller - " + message);
        }
    }//console print

}//class
