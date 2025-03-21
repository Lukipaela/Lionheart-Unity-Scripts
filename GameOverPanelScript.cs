using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class GameOverPanelScript : MonoBehaviour
{
    public PanelHider panelHider;

    //inspector linkages
    [SerializeField] private GameControl gameControlScript;
    [SerializeField] private AudioControl audioControlScript;

    //debug
    private readonly bool enableDebugging = true;

    /********************
     * BUILT-IN METHODS *
     ********************/

    // Start is called before the first frame update
    void Start()
    {
        panelHider = new PanelHider(transform.GetChild(1).position, transform.GetChild(2).position, transform.GetChild(0).GetComponent<RectTransform>(), 1500);
    }

    // Update is called once per frame
    void Update()
    {
        panelHider.ManualUpdate();
    }



    /******************
     * BUTTON METHODS *
     ******************/

    /// <summary>
    /// Called when the GameOver Panel is presented and the user clicks the Exit button.
    /// Responsible for resetting all match variables and returning to the main menu scene 
    /// </summary>
    public void ExitGameClicked()
    {
        ConsolePrint("Exit Game button click detected.");
        audioControlScript.GeneralButtonClick();
        SceneManager.LoadScene("MainMenuScene");
    }

    /// <summary>
    /// Called when the GameOver Panel is presented and the user clicks the Rematch button 
    /// Responsible for resetting all match variables and beginning the army placement phase
    /// </summary>
    public void RematchClicked()
    {
        ConsolePrint("Rematch button click detected.");
        audioControlScript.GeneralButtonClick();
        gameControlScript.ResetGame();
    }



    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("GameOverPanelScript: " + message);
        }
    }//console print

}
