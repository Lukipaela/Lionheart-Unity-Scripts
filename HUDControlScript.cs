using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDControlScript : MonoBehaviour
{
    //inspector linkages
    [SerializeField] private GameObject squadPlacementPanel;
    [SerializeField] private GameObject messagePanel;
    [SerializeField] private GameObject turnDataPanel;
    [SerializeField] private GameObject quickMenuPanel;
    [SerializeField] private GameObject kingWidget;
    [SerializeField] private GameObject knightWidget;
    [SerializeField] private GameObject infantryWidget;
    [SerializeField] private GameObject archerWidget;
    [SerializeField] private GameObject mercenaryWidget;
    [SerializeField] private GameObject heavyInfantryWidget;
    [SerializeField] private GameObject peasantWidget;
    [SerializeField] private GameObject placementCompleteWidget;
    [SerializeField] private RectTransform messageBannerLocationVisible;
    [SerializeField] private RectTransform messageBannerLocationHidden;
    [SerializeField] private AudioControl audioControlScript;
    [SerializeField] private InfoPanelScript infoPanelScript;
    [SerializeField] private GameControl gameControlScript;
    [SerializeField] private GameObject newMessageText;
    [SerializeField] private Sprite[] volumeImages;

    //variables for public access
    public SoldierClass selectedSquadType;
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
            switch (messageBannerAnimationState)
            {
                case "Descending":
                    //move banner toward the 'visible' location
                    messageBannerRectTransform.position = Vector3.MoveTowards(messageBannerRectTransform.position, messageBannerLocationVisible.position, bannerEntrySpeed * Time.deltaTime);
                    if (messageBannerRectTransform.position == messageBannerLocationVisible.position)
                        messageBannerAnimationState = "Holding";
                    break;

                case "Holding":
                    bannerAscendTimestamp -= Time.deltaTime;
                    if (bannerAscendTimestamp < 0)
                        messageBannerAnimationState = "Ascending";
                    break;

                case "Ascending":
                    //move banner toward the 'hidden' location
                    messageBannerRectTransform.position = Vector3.MoveTowards(messageBannerRectTransform.position, messageBannerLocationHidden.position, bannerExitSpeed * Time.deltaTime);
                    if (messageBannerRectTransform.position == messageBannerLocationHidden.position)
                        messageBannerAnimating = false;
                    break;
            }//animation state switch
        } //banner animating
    } //Update



    /******************
     * CUSTOM METHODS *
     ******************/

    public void InitializeSquadPlacementUI(int playerID)
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
        placementCompleteWidget.transform.GetChild(1).GetComponent<Button>().interactable = false;

        //set counts for default squad types
        kingWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Kings: " + kingCountMax;
        knightWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Knights: " + knightCountMax;
        infantryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Infantry: " + infantryCountMax;
        archerWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Archers: " + archerCountMax;

        //potentially disable the special squad types
        mercenaryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Mercs: " + mercenaryCountMax;
        if (mercenaryCountMax == 0)
            mercenaryWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;

        heavyInfantryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Heavy Inf: " + heavyInfantryCountMax;
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
        placementCompleteWidget.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Squads Remaining: " + squadsRemaining;

    }

    private void CheckSquadPlacementComplete()
    {
        //check squad count, if 0, enable button for completion
        if (squadsRemaining == 0)
        {
            placementCompleteWidget.transform.GetChild(1).GetComponent<Button>().interactable = true;
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
            case SoldierClass.King:
                kingCountRemaining--;
                kingWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Kings: " + kingCountRemaining;
                if (kingCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
            case SoldierClass.Archer:
                archerCountRemaining--;
                archerWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Archers: " + archerCountRemaining;
                if (archerCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
            case SoldierClass.Knight:
                knightCountRemaining--;
                knightWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Knights: " + knightCountRemaining;
                if (knightCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
            case SoldierClass.Infantry:
                infantryCountRemaining--;
                infantryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Infantry: " + infantryCountRemaining;
                if (infantryCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
            case SoldierClass.HeavyInfantry:
                heavyInfantryCountRemaining--;
                heavyInfantryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Heavy Inf: " + heavyInfantryCountRemaining;
                if (heavyInfantryCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
            case SoldierClass.Mercenary:
                mercenaryCountRemaining--;
                mercenaryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Mercs: " + mercenaryCountRemaining;
                if (mercenaryCountRemaining == 0)
                    squadTypeExhausted = true;
                else
                    squadTypeExhausted = false;
                break;
            case SoldierClass.Peasant:
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

    public void PrintMessage(string message)
    {
        ConsolePrint("Print message received:  " + message);
        newMessageText.GetComponent<TMPro.TextMeshProUGUI>().text = message;
        messageBannerAnimating = true;
        messageBannerAnimationState = "Descending";
        //reset timer
        bannerAscendTimestamp = messageDuration;
    }//print message

    public void SetTurnData(int playerID, int AP)
    {
        //child 0 is player name
        turnDataPanel.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Turn: " + GameSettings.playerNames[playerID];
        //child 1 is AP remaining
        turnDataPanel.transform.GetChild(1).GetComponent<TMPro.TextMeshProUGUI>().text = "AP: " + AP;
    }//SetTurnData

    public void SetTurnDataPanelVisibility(bool makeVisible)
    {
        ConsolePrint("Switching turn data panel enabled to: " + makeVisible);
        turnDataPanel.SetActive(makeVisible);
        quickMenuPanel.SetActive(makeVisible);
    }//SetTurnDataPanelVisibility

    public void DismissBanner()
    {
        messageBannerAnimationState = "Ascending";
    }

    public void HideSquadPlacementPanel()
    {
        squadPlacementPanel.SetActive(false);
    }



    /******************
     * BUTTON METHODS *
     ******************/

    public void AddKingClicked()
    {
        selectedSquadType = SoldierClass.King;
        selectedSquadSize = 1;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.king1PrefabAddress);
    }

    public void AddKnightClicked()
    {
        selectedSquadType = SoldierClass.Knight;
        selectedSquadSize = 2;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.knight2PrefabAddress);
    }

    public void AddInfantryClicked()
    {
        selectedSquadType = SoldierClass.Infantry;
        selectedSquadSize = 4;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.infantry4PrefabAddress);
    }

    public void AddArcherClicked()
    {
        selectedSquadType = SoldierClass.Archer;
        selectedSquadSize = 4;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.archer4PrefabAddress);
    }

    public void AddHeavyInfantryClicked()
    {
        selectedSquadType = SoldierClass.HeavyInfantry;
        selectedSquadSize = 2;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.heavyInfantry2PrefabAddress);
    }

    public void AddMercenaryClicked()
    {
        selectedSquadType = SoldierClass.Mercenary;
        selectedSquadSize = 2;
        AddSquadButtonPressed();
        gameControlScript.DefineSquadToPlaceFromUI(GameSettings.mercenary2PrefabAddress);
    }

    public void AddPeasantClicked()
    {
        selectedSquadType = SoldierClass.Peasant;
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
        ConsolePrint("Mute button click detected.");
        if (audioControlScript.MuteButtonClick())
            quickMenuPanel.transform.GetChild(1).GetComponent<Image>().sprite = volumeImages[1];
        else
            quickMenuPanel.transform.GetChild(1).GetComponent<Image>().sprite = volumeImages[0];
    }

    /// <summary>
    /// Called when the usder clicks the Help button (currently a question mark icon)
    /// Presents the unit data panel which explains the characteristics of the selected squad's unit class.
    /// </summary>
    public void HelpClicked()
    {
        ConsolePrint("Help button click detected.");
        audioControlScript.GeneralButtonClick();
        infoPanelScript.toggleVisibility();
    }



    /***************
     * DEBUG STUFF *
     ***************/

    public void ConsolePrint(string message)
    {
        if (enableDebugging == true)
        {
            Debug.Log("HudControlScript: " + message);
        }
    }//console print

}//class
