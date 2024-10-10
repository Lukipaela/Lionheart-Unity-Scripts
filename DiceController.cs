using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceController : MonoBehaviour
{
    public DieSpawner dieSpawnerScript;
    public AudioSource dieAudioSource;
    public AudioClip dieColliderSound;
    private Rigidbody dieRigidBody;
    private Transform groundTransform;
    private bool inMotion;
    private bool activeDie;

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
        inMotion = true;
        activeDie = true;
    }

    // Update is called once per frame
    void Update()
    {
        if (dieRigidBody.IsSleeping() && activeDie)
        {
            //find out which side faces up and report it back to the game controller 

            string result = GetRollResult();
            int particleEffectIndex = 0;
            switch (result)
            {
                case "Error":   //die not settled yet
                    ConsolePrint("Nudging..");
                    int nudgeMagnitude = 100;
                    gameObject.GetComponent<Rigidbody>().AddTorque(transform.up * Random.Range(-nudgeMagnitude, nudgeMagnitude) + transform.right * Random.Range(-nudgeMagnitude, nudgeMagnitude) + transform.forward * Random.Range(-nudgeMagnitude, nudgeMagnitude));
                    break;
                case "Panic":
                    activeDie = false;
                    particleEffectIndex = 0;
                    break;
                case "Arrow":
                    activeDie = false;
                    particleEffectIndex = 1;
                    break;
                case "Axe":
                    activeDie = false;
                    particleEffectIndex = 2;
                    break;
            }

            if (!activeDie){
                //die has settled, report result and play particle effect
                dieSpawnerScript.DieRollReport(result);
                transform.GetChild(particleEffectIndex).transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                transform.GetChild(particleEffectIndex).GetComponent<ParticleSystem>().Play();
            }
        }//if sleeping, but was previously active

    }//update

    private void OnCollisionEnter(Collision collision)    {
        dieAudioSource.PlayOneShot(dieColliderSound);
    }

    /******************
     * CUSTOM METHODS *
     ******************/

    private string GetRollResult()
    {
        if ((gameObject.transform.up == groundTransform.up) || (gameObject.transform.right == -1 * groundTransform.up) || (gameObject.transform.forward == -1 * groundTransform.up))
            return "Axe";
        else if ((gameObject.transform.up == -1 * groundTransform.up) || (gameObject.transform.forward == groundTransform.up))
            return "Arrow";
        else if (gameObject.transform.right == groundTransform.up)
            return "Panic";
        else
            return "Error";
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
