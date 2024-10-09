using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DieSpawner : MonoBehaviour
{
    public GameObject[] spawnPointArray;

    private readonly string diePrefabAddress = "Prefabs/Objects/Lionheart_Die";


    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }//update 

    public void RollDice( int dieCount)
    {
        int spawnLocationIndex =  Random.Range(0, spawnPointArray.Length);
        GameObject spawnLocation = spawnPointArray[spawnLocationIndex];
        Vector3 newDiePosition = spawnLocation.transform.position;
        Quaternion newDieRotation = spawnLocation.transform.rotation;


        for (int i = 0; i< dieCount; i++)
        {
            //spawn one die at a 'safe' location near the spawn point (offset each die spawnpoint slightly so they dont collide on instantiation and get launched)
            //transform.position + new Vector3(-4 * tileDimension, 0.5f, -4 * tileDimension);
            //int verticalOffset = Random.Range
            //create the die
            GameObject newDie = Instantiate(Resources.Load<GameObject>(diePrefabAddress), newDiePosition, newDieRotation);

            newDiePosition += new Vector3(0, 1 * (i %2), 1* ((i + 1)%2));
            newDieRotation *= Quaternion.Euler(Random.Range(-10, 10), Random.Range(-10, 10), Random.Range(-10, 10));

            newDie.GetComponent<Rigidbody>().AddForce(newDie.transform.forward * Random.Range(30, 100), ForceMode.Impulse);
            newDie.GetComponent<Rigidbody>().AddTorque(newDie.transform.up * Random.Range(-200, 200) + newDie.transform.right * Random.Range(-200, 200), ForceMode.Impulse);

        }//die instantiation FOR 

    }//roll dice

}//class
