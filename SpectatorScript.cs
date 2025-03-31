using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// A script to control NPC soldiers arrayed around the outside of the battlefield in the background. 
/// Contains methods for passively animating random actions based on where the soldier is located. 
/// </summary>
public class SpectatorScript : MonoBehaviour
{

    [SerializeField] private int playerID;
    [SerializeField] private SpectatorScript pairedEnemy;   //allows two spectators to be paired up for randomized battle repeating in the background. 


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
