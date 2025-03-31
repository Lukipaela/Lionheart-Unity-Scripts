using UnityEngine;

public class BoardTileScript : MonoBehaviour
{
    public GameObject adjacentTileLeft;
    public GameObject adjacentTileRight;
    public GameObject adjacentTileTop;
    public GameObject adjacentTileBottom;
    public GameObject occupyingSquad;   //when occupied, holds a reference to the squad tile that is occupying it.
    public bool isOccupied;
    public int row;
    public int column;
    public bool validMoveTarget = false;
    public bool validAttackTarget = false;

    private ParticleSystem attackTargetParticleSystem;
    private ParticleSystem moveTargetParticleSystem;
    private ParticleSystem selectedTileParticleSystem;
    private GameControl gameControlScript;

    private static readonly bool enableDebugging = false;


    /********************
     * BUILT-IN METHODS *
     ********************/

    private void Start()
    {
        Transform particleSystemCollection = gameObject.transform.Find("BoardTileParticleSystems");
        attackTargetParticleSystem = particleSystemCollection.Find("AttackParticleSystem").GetComponent<ParticleSystem>();
        moveTargetParticleSystem = particleSystemCollection.Find("PointerParticleSystem").GetComponent<ParticleSystem>();
        selectedTileParticleSystem = particleSystemCollection.Find("SelectedParticleSystem").GetComponent<ParticleSystem>();
        isOccupied = false;
        occupyingSquad = null;
        gameControlScript = GameObject.FindGameObjectWithTag("GameControl").GetComponent<GameControl>();
    }//start


    /******************
     * CUSTOM METHODS *
     ******************/

    public void ClearTile()
    {
        string squadName = "";
        if (occupyingSquad.gameObject is not null)
            squadName = occupyingSquad.gameObject.name;
        ConsolePrint("clearing tile: " + gameObject.name + " of previous occupying squad: " + squadName);
        isOccupied = false;
        occupyingSquad = null;
        ClearAllHighlights();
    }

    public void PlaceSquad(GameObject squadObject)
    {
        ConsolePrint("placing squad: " + squadObject.name);
        isOccupied = true;
        occupyingSquad = squadObject;
        EnableHighlight("Selected");
    }

    public void EnableHighlight(string highlightType)
    {
        switch (highlightType)
        {
            case "Selected":
                selectedTileParticleSystem.Play();
                break;
            case "ValidMoveTarget":
                validMoveTarget = true;
                moveTargetParticleSystem.transform.forward = gameControlScript.currentSelectedSquad.transform.forward * -1;
                moveTargetParticleSystem.Play();
                break;
            case "ValidAttackTarget":
                validAttackTarget = true;
                attackTargetParticleSystem.Play();
                break;
        }//switch highlight type
    }//toggleHighlighting

    public bool MoveTargetIndicatorIsActive()
    {
        return moveTargetParticleSystem.isPlaying;
    }

    public void IntensifyMoveTargetIndicator(bool enable)
    {
        float alphaValue = 0.4235294f;
        if (enable)
            alphaValue = 1f;
        Color color = new Color(0.2277095f, 0.7075472f, 0, alphaValue);
        moveTargetParticleSystem.GetComponent<ParticleSystemRenderer>().material.SetColor("_Color", color);
    }

    public void ClearAllHighlights()
    {
        selectedTileParticleSystem.Stop();
        selectedTileParticleSystem.Clear();

        validMoveTarget = false;
        moveTargetParticleSystem.Stop();
        moveTargetParticleSystem.Clear();
        validMoveTarget = false;

        validAttackTarget = false;
        attackTargetParticleSystem.Stop();
        attackTargetParticleSystem.Clear();
        validAttackTarget = false;
    }//ClearAllHighlights


    /***************
     * DEBUG STUFF *
     ***************/

    private void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("Board Tile Script " + gameObject.name + ": " + message);
        }
    }//console print

}//class
