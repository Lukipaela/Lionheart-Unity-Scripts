using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDControlScript : MonoBehaviour
{
    //inspector linkages
    public GameObject squadPlacementPanel;
    public GameObject messagePanel;
    public GameObject kingWidget;
    public GameObject knightWidget;
    public GameObject infantryWidget;
    public GameObject archerWidget;
    public GameObject mercenaryWidget;
    public GameObject heavyInfantryWidget;
    public GameObject peasantWidget;
    public GameObject placementCompleteWidget;
    public GameControl gameControlScript;
    public GameObject oldMessageText;
    public GameObject newMessageText;
    public GameObject narratorImage;
    public GameObject turnDataPanel;
    public GameObject quickMenuPanel;
    public RectTransform messageBannerLocationVisible;
    public RectTransform messageBannerLocationHidden;
    public AudioControl audioControlScript;

    //variables for public access
    public string selectedSquadType;
    public int selectedSquadSize;

    //PRIVATE DATA
    private int kingCountMax;
    private int kingCountRemaining;
    private int knightCountMax;
    private int knightCountRemaining;
    private int infantryCountMax;
    private int infantryCountRemaining;
    private int archerCountMax;
    private int archerCountRemaining;
    private int mercenaryCountMax;
    private int mercenaryCountRemaining;
    private int heavyInfantryCountMax;
    private int heavyInfantryCountRemaining;
    private int peasantCountMax;
    private int peasantCountRemaining;
    private int squadsRemaining;   //initialize to 10, the max value allowed in all game modes

    //banner animation controls
    private RectTransform messageBannerRectTransform;
    private bool messageBannerAnimating = true;
    private string messageBannerAnimationState = "Ascending";//Ascending, Descending
    private readonly float bannerEntrySpeed = 500;
    private readonly float bannerExitSpeed = 200;
    private float bannerAscendTimestamp;
    private readonly float messageDuration = 8f;

    //debug
    private readonly bool enableDebugging = false;


    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start()
    {
        messageBannerRectTransform = messagePanel.GetComponent<RectTransform>();
    }

    void Update()
    {
        if (messageBannerAnimating)
        {
            //ConsolePrint("Message Board Animation State: " + messageBannerAnimationState);
            switch (messageBannerAnimationState)
            {
                case "Descending":
                    //move banner toward the 'visible' location
                    //ConsolePrint("Current Location: " + messageBannerRectTransform.anchoredPosition.ToString() + " target location: " + messageBannerLocationVisible.anchoredPosition.ToString());
                    messageBannerRectTransform.anchoredPosition = Vector3.MoveTowards(messageBannerRectTransform.anchoredPosition, messageBannerLocationVisible.anchoredPosition, bannerEntrySpeed * Time.deltaTime);
                    if (messageBannerRectTransform.anchoredPosition == messageBannerLocationVisible.anchoredPosition)
                            messageBannerAnimationState = "Holding";
                    break;

                case "Holding":
                    bannerAscendTimestamp -= Time.deltaTime;
                    if(bannerAscendTimestamp < 0)
                        messageBannerAnimationState = "Ascending";
                    break;

                case "Ascending":
                    //move banner toward the 'hidden' location
                    //ConsolePrint("Current Location: " + messageBannerRectTransform.anchoredPosition.ToString() + " target location: " + messageBannerLocationHidden.anchoredPosition.ToString());
                    messageBannerRectTransform.anchoredPosition = Vector3.MoveTowards(messageBannerRectTransform.anchoredPosition, messageBannerLocationHidden.anchoredPosition, bannerExitSpeed * Time.deltaTime);
                    if (messageBannerRectTransform.anchoredPosition == messageBannerLocationHidden.anchoredPosition)
                        messageBannerAnimating = false;
                    break;
            }//animation state switch
        } //banner animating

    } //Update


    /******************
     * CUSTOM METHODS *
     ******************/

    public void InitializeSquadPlacementUI( int playerID )
    {

        //enable/show the panel for squad placement. 
        squadPlacementPanel.SetActive(true);

        //set playername on screen
        squadPlacementPanel.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = GameSettings.playerNames[playerID];

        //enable/disable widgets on the panel, set squad counts based on game mode 
        switch (GameSettings.gameMode)
        {
            case "Standard":
                kingCountMax = 1;
                knightCountMax = 2;
                infantryCountMax = 5;
                archerCountMax = 2;
                mercenaryCountMax = 0;
                heavyInfantryCountMax = 0;
                peasantCountMax = 0;
                break;
            case "Special":
                kingCountMax = 1;
                knightCountMax = 2;
                infantryCountMax = 2;
                archerCountMax = 2;
                mercenaryCountMax = 1;
                heavyInfantryCountMax = 1;
                peasantCountMax = 1;
                break;
            case "MassiveArmy":
                kingCountMax = 1;
                knightCountMax = 2;
                infantryCountMax = 2;
                archerCountMax = 1;
                mercenaryCountMax = 1;
                heavyInfantryCountMax = 1;
                peasantCountMax = 1;
                break;
        }

        //set up the placement complete widget 
        squadsRemaining = kingCountMax + knightCountMax + infantryCountMax + archerCountMax + mercenaryCountMax + heavyInfantryCountMax + peasantCountMax;
        UpdateSquadCountUI();
        placementCompleteWidget.transform.GetChild(2).GetComponent<Button>().interactable = false;

        //set counts for default squad types
        kingWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Kings: " + kingCountMax;
        knightWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Knights: " + knightCountMax;
        infantryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Infantry: " + infantryCountMax;
        archerWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Archers: " + archerCountMax;

        //potentially disable the special squad types
        mercenaryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Mercenaries: " + mercenaryCountMax;
        if(mercenaryCountMax == 0)
            mercenaryWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;
        
        heavyInfantryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Heavy Infantry: " + heavyInfantryCountMax;
        if (heavyInfantryCountMax == 0)
            heavyInfantryWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;
        
        peasantWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Peasants: " + peasantCountMax;
        if (peasantCountMax == 0)
            peasantWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;

        //initialize counts
        kingCountRemaining = kingCountMax;
        knightCountRemaining = knightCountMax;
        infantryCountRemaining = infantryCountMax;
        archerCountRemaining = archerCountMax;
        mercenaryCountRemaining = mercenaryCountMax;
        heavyInfantryCountRemaining = heavyInfantryCountMax;
        peasantCountRemaining = peasantCountMax;

        //reinitialize button security
        EnableButtons();

    }//initializeSquadPlacementUI

    private void EnableButtons()
    {
        //enable all buttons with a nonzero remaining count
        if (kingCountRemaining > 0)
            kingWidget.transform.GetChild(0).GetComponent<Button>().interactable = true;
        if (knightCountRemaining > 0)
            knightWidget.transform.GetChild(0).GetComponent<Button>().interactable = true;
        if (infantryCountRemaining > 0)
            infantryWidget.transform.GetChild(0).GetComponent<Button>().interactable = true;
        if (archerCountRemaining > 0)
            archerWidget.transform.GetChild(0).GetComponent<Button>().interactable = true;
        if (mercenaryCountRemaining > 0)
            mercenaryWidget.transform.GetChild(0).GetComponent<Button>().interactable = true;
        if (heavyInfantryCountRemaining > 0)
            heavyInfantryWidget.transform.GetChild(0).GetComponent<Button>().interactable = true;
        if (peasantCountRemaining > 0)
            peasantWidget.transform.GetChild(0).GetComponent<Button>().interactable = true;
    }

    private void DisableButtons()
    {
        //disable all buttons with a nonzero remaining count (zeros are already disabled)
        if (kingCountRemaining > 0)
            kingWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;
        if (knightCountRemaining > 0)
            knightWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;
        if (infantryCountRemaining > 0)
            infantryWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;
        if (archerCountRemaining > 0)
            archerWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;
        if (mercenaryCountRemaining > 0)
            mercenaryWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;
        if (heavyInfantryCountRemaining > 0)
            heavyInfantryWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;
        if (peasantCountRemaining > 0)
            peasantWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;

    }

    private void AddSquadButtonPressed()
    {
        //generic actions that happen when any Add Squad button was pressed 
        audioControlScript.GeneralButtonClick();
        DisableButtons();
    }

    private void UpdateSquadCountUI()
    {
        //set the label to reflect the new count
        placementCompleteWidget.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = "Squads Remaining: " + squadsRemaining;

    }

    private void CheckSquadPlacementComplete()
    {
        //check squad count, if 0, enable button for completion
        if (squadsRemaining == 0)
        {
            placementCompleteWidget.transform.GetChild(2).GetComponent<Button>().interactable = true;
            DisableButtons();
        }

    }

    public bool SquadWasPlaced()
    {
        //returns indicator of if this was the last instance of the squad to be placed 
        //decrease squads remaining count 
        squadsRemaining--;
        UpdateSquadCountUI();

        bool squadTypeExhausted = false;
        //get count of remaining instances of the selected squad. 
        switch (selectedSquadType)
        {
            case "King":
                kingCountRemaining--;
                kingWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Kings: " + kingCountRemaining;
                if (kingCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
            case "Archer":
                archerCountRemaining--;
                archerWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Archers: " + archerCountRemaining;
                if (archerCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
            case "Knight":
                knightCountRemaining--;
                knightWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Knights: " + knightCountRemaining;
                if (knightCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
            case "Infantry":
                infantryCountRemaining--;
                infantryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Infantry: " + infantryCountRemaining;
                if (infantryCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
            case "HeavyInfantry":
                heavyInfantryCountRemaining--;
                heavyInfantryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Heavy Infantry: " + heavyInfantryCountRemaining;
                if (heavyInfantryCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
            case "Mercenary":
                mercenaryCountRemaining--;
                mercenaryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Mercenaries: " + mercenaryCountRemaining;
                if (mercenaryCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
            case "Peasant":
                peasantCountRemaining--;
                peasantWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Peasants: " + peasantCountRemaining;
                if (peasantCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
        }

        if (squadTypeExhausted)
        {
            EnableButtons();
            //check if we are done, and should enable the completion button
            CheckSquadPlacementComplete();
        }
        return squadTypeExhausted;
    }//squad was placed

    public void PrintMessage( string message)
    {
        ConsolePrint("Print message received:  " + message);
        //oldMessageText.GetComponent<TMPro.TextMeshProUGUI>().text = newMessageText.GetComponent<TMPro.TextMeshProUGUI>().text;
        newMessageText.GetComponent<TMPro.TextMeshProUGUI>().text = message;
        messageBannerAnimating = true;
        messageBannerAnimationState = "Descending";
        //reset timer
        bannerAscendTimestamp = messageDuration;
    }//print message

    public void SetTurnData( int playerID, int AP)
    {
        //child 0 is player name
        turnDataPanel.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Turn: " + GameSettings.playerNames[playerID];
        //child 1 is AP remaining
        turnDataPanel.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = "AP: " + AP;
    }//SetTurnData

    public void SetTurnDataPanelVisibility(bool makeVisible){
        turnDataPanel.SetActive(makeVisible);
        quickMenuPanel.SetActive(makeVisible);
    }//SetTurnDataPanelVisibility

    /******************
     * BUTTON METHODS *
     ******************/

    public void AddKingClicked()
    {
        selectedSquadType = "King";
        selectedSquadSize = 1;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.king1PrefabAddress);
    }

    public void AddKnightClicked()
    {
        selectedSquadType = "Knight";
        selectedSquadSize = 2;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.knight2PrefabAddress);
    }

    public void AddInfantryClicked()
    {
        selectedSquadType = "Infantry";
        selectedSquadSize = 4;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.infantry4PrefabAddress);
    }

    public void AddArcherClicked()
    {
        selectedSquadType = "Archer";
        selectedSquadSize = 4;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.archer4PrefabAddress);
    }

    public void AddHeavyInfantryClicked()
    {
        selectedSquadType = "HeavyInfantry";
        selectedSquadSize = 2;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.heavyInfantry2PrefabAddress);
    }

    public void AddMercenaryClicked()
    {
        selectedSquadType = "Mercenary";
        selectedSquadSize = 2;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.mercenary2PrefabAddress);
    }

    public void AddPeasantClicked()
    {
        selectedSquadType = "Peasant";
        selectedSquadSize = 4;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.peasant4PrefabAddress);
    }

    public void PlacementCompleteClicked()
    {
        gameControlScript.PlayerArmyPlacementComplete();
    }

    public void MuteClicked()
    {
        //All logic here is done by the music control script, as the only current actions are to pause/play the theme song
        audioControlScript.MuteButtonClick();
    }

    public void HelpClicked()
    {
        //bring up a panel of data explaining how each character behaves. (move, rotate, attack, special)
        audioControlScript.GeneralButtonClick();
        //TODO: Launch help menu panel 
    }

    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log(message);
        }
    }//console print

}//class
