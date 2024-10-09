using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DiceController : MonoBehaviour
{
    public GameControl gameControlScript;
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
        gameControlScript = GameObject.FindGameObjectWithTag("GameControl").GetComponent<GameControl>();
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
            string face = GetRollResult();
            if (face != "Error")
            {
                activeDie = false;
                gameControlScript.DiceRollReport(face);
                if (face == "Panic")
                {
                    //ensure that the particle system is facing up before activating it. 
                    transform.GetChild(0).transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                    transform.GetChild(0).GetComponent<ParticleSystem>().Play();
                }
                else if (face == "Arrow")
                {
                    //ensure that the particle system is facing up before activating it. 
                    transform.GetChild(1).transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                    transform.GetChild(1).GetComponent<ParticleSystem>().Play();
                }
                else if (face == "Axe")
                {
                    //ensure that the particle system is facing up before activating it. 
                    transform.GetChild(2).transform.rotation = Quaternion.LookRotation(Vector3.up, Vector3.forward);
                    transform.GetChild(2).GetComponent<ParticleSystem>().Play();
                }
            }
            else
            {
                ConsolePrint("Nudging..");
                int nudgeMagnitude = 100;
                gameObject.GetComponent<Rigidbody>().AddTorque(transform.up * Random.Range(-nudgeMagnitude, nudgeMagnitude) + transform.right * Random.Range(-nudgeMagnitude, nudgeMagnitude) + transform.forward * Random.Range(-nudgeMagnitude, nudgeMagnitude));
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
