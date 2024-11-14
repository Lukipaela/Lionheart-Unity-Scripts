using System.Collections;
using System.Collections.Generic;
using UnityEngine.SceneManagement;
using UnityEngine;

public class GameOverPanelScript : MonoBehaviour
{

    //inspector linkages
    [SerializeField] private GameControl gameControlScript;
    [SerializeField] private AudioControl audioControlScript;
    [SerializeField] private Transform Location_Hidden;
    [SerializeField] private Transform Location_Active;
    [SerializeField] private Transform gameOverPanel;

    //private vars
    private string animationState = "Hidden";
    private int panelTransitionSpeed = 1500;

    //debug
    private readonly bool enableDebugging = true;

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
        switch (animationState)
        {
            case "Activating":
                gameOverPanel.position = Vector3.MoveTowards(gameOverPanel.position, Location_Active.position, panelTransitionSpeed * Time.deltaTime);
                if (gameOverPanel.position == Location_Active.position)
                    animationState = "Active";
                break;

            case "Hiding":
                gameOverPanel.position = Vector3.MoveTowards(gameOverPanel.position, Location_Hidden.position, panelTransitionSpeed * Time.deltaTime);
                if (gameOverPanel.position == Location_Hidden.position)
                    animationState = "Hidden";
                break;

            default: break;
        }//animation state switch

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



    /// <summary>
    /// Switches the info panel between active and inactive, sliding it onto and off of the screen. 
    /// </summary>
    public void toggleVisibility()
    {
        ConsolePrint("Toggling visibility.");
        switch (animationState)
        {
            case "Active":
            case "Activating":
                animationState = "Hiding";
                break;
            case "Hidden":
            case "Hiding":
                animationState = "Activating";
                break;
        }//animation state switch
    }//toggle visibility


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
