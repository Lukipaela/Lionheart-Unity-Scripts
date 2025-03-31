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

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }

    public void AssignColors()
    {

    }
}
