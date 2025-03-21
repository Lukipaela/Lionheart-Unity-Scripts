using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class HUDControlScript : MonoBehaviour
{
    //variables for public access
    public SoldierClass selectedSquadType;
    public int selectedSquadSize;

    //inspector linkages
    [SerializeField] private GameObject squadPlacementPanel;
    [SerializeField] private GameObject turnDataPanel;
    [SerializeField] private GameObject quickMenuPanel;
    [SerializeField] private GameObject settingsPanel;
    [SerializeField] private GameObject kingWidget;
    [SerializeField] private GameObject knightWidget;
    [SerializeField] private GameObject infantryWidget;
    [SerializeField] private GameObject archerWidget;
    [SerializeField] private GameObject mercenaryWidget;
    [SerializeField] private GameObject heavyInfantryWidget;
    [SerializeField] private GameObject peasantWidget;
    [SerializeField] private GameObject placementCompleteWidget;
    [SerializeField] private AudioControl audioControlScript;
    [SerializeField] private InfoPanelScript infoPanelScript;
    [SerializeField] private GameControl gameControlScript;
    [SerializeField] private Sprite[] volumeImages;
    [SerializeField] private GameObject diceRollButton;

    //PRIVATE DATA
    private int kingCountRemaining;
    private int knightCountRemaining;
    private int infantryCountRemaining;
    private int archerCountRemaining;
    private int mercenaryCountRemaining;
    private int heavyInfantryCountRemaining;
    private int peasantCountRemaining;
    private int squadsRemaining;   //initialize to 10, the max value allowed in all game modes
    private TMPro.TextMeshProUGUI messageBannerText;

    //banner animation controls
    private float bannerAscendTimestamp;
    private readonly float messageDuration = 8f;
    private PanelHider messageBannerHider;
    private PanelHider placementPanelHider;

    //debug
    private readonly bool enableDebugging = false;



    /********************
     * BUILT-IN METHODS *
     ********************/

    void Start()
    {
        RectTransform messageBannerCollection = GameObject.Find("MessagePanelCollection").GetComponent<RectTransform>();
        messageBannerHider = new PanelHider(messageBannerCollection.Find("ActiveLocation").position, messageBannerCollection.Find("HiddenLocation").position, messageBannerCollection.Find("MessagePanel").GetComponent<RectTransform>(), 500);
        messageBannerText = messageBannerCollection.Find("MessagePanel").transform.Find("NewMessage").GetComponent<TMPro.TextMeshProUGUI>();

        RectTransform placementPanelCollection = GameObject.Find("PlacementPanelCollection").GetComponent<RectTransform>();
        placementPanelHider = new PanelHider(placementPanelCollection.Find("ActiveLocation").position, placementPanelCollection.Find("HiddenLocation").position, placementPanelCollection.Find("ArmyPlacementPanel").GetComponent<RectTransform>(), 500);
    }

    void Update()
    {
        placementPanelHider.ManualUpdate();
        //perform any pending message banner animations
        messageBannerHider.ManualUpdate();
        if (bannerAscendTimestamp != -1)
        {
            ConsolePrint("Managing banner timer with " + bannerAscendTimestamp + " seconds remaining");
            bannerAscendTimestamp -= Time.deltaTime;
            if (bannerAscendTimestamp < 0)
            {
                ConsolePrint("Hiding banner after timer expiration");
                messageBannerHider.AssignState(HiderAnimationState.Hiding);
                bannerAscendTimestamp = -1;
            }
        }
    } //Update



    /******************
     * CUSTOM METHODS *
     ******************/

    public void InitializeSquadPlacementUI(int playerID)
    {
        //enable/show the panel for squad placement. 
        placementPanelHider.AssignState(HiderAnimationState.Activating);

        //set playername on screen
        squadPlacementPanel.transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = GameSettings.playerNames[playerID];

        //enable/disable widgets on the panel, set squad counts based on game mode 
        //set up the placement complete widget 
        squadsRemaining = GameSettings.gameModeAttributes.totalUnits;
        UpdateSquadCountUI();
        placementCompleteWidget.transform.GetChild(1).GetComponent<Button>().interactable = false;

        //initialize counts
        kingCountRemaining = GameSettings.gameModeAttributes.kingCount;
        knightCountRemaining = GameSettings.gameModeAttributes.knightCount;
        infantryCountRemaining = GameSettings.gameModeAttributes.infantryCount;
        archerCountRemaining = GameSettings.gameModeAttributes.archerCount;
        mercenaryCountRemaining = GameSettings.gameModeAttributes.mercenaryCount;
        heavyInfantryCountRemaining = GameSettings.gameModeAttributes.heavyInfantryCount;
        peasantCountRemaining = GameSettings.gameModeAttributes.peasantCount;

        //set counts for default squad types
        kingWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Kings: " + kingCountRemaining;
        knightWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Knights: " + knightCountRemaining;
        infantryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Infantry: " + infantryCountRemaining;
        archerWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Archers: " + archerCountRemaining;

        //potentially disable the special squad types
        mercenaryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Mercs: " + mercenaryCountRemaining;
        if (mercenaryCountRemaining == 0)
            mercenaryWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;

        heavyInfantryWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Heavy Inf: " + heavyInfantryCountRemaining;
        if (heavyInfantryCountRemaining == 0)
            heavyInfantryWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;

        peasantWidget.transform.GetChild(0).transform.GetChild(0).GetComponent<TMPro.TextMeshProUGUI>().text = "Peasants: " + peasantCountRemaining;
        if (peasantCountRemaining == 0)
            peasantWidget.transform.GetChild(0).GetComponent<Button>().interactable = false;


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
        ClickSound();
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
        messageBannerText.text = message;
        messageBannerHider.AssignState(HiderAnimationState.Activating);
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
        messageBannerHider.AssignState(HiderAnimationState.Hiding);
    }

    public void HideSquadPlacementPanel()
    {
        placementPanelHider.AssignState(HiderAnimationState.Hiding);
    }

    public void ToggleDiceRollButtonVisibility()
    {
        diceRollButton.SetActive(!diceRollButton.activeSelf);
    }

    public void SetInfoPanelVisibility(bool makeVisible)
    {
        infoPanelScript.gameObject.SetActive(makeVisible);
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
        ClickSound();
        gameControlScript.PlayerArmyPlacementComplete();
    }

    public void MuteClicked()
    {
        //All logic here is done by the music control script, as the only current actions are to pause/play the theme song
        ConsolePrint("Mute button click detected.");
        ClickSound();
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
        ClickSound();
        infoPanelScript.panelHider.ToggleVisibility();
    }

    /// <summary>
    /// Called when the user clicks the Settings button (currently a gear icon)
    /// Presents the same settings window from the home screen via asset re-use
    /// </summary>
    public void SettingsClicked()
    {
        ConsolePrint("Settings button click detected.");
        ClickSound();
        settingsPanel.SetActive(true);
    }

    /// <summary>
    /// Called when the Save button is clicked on the settings panel.
    /// Closes the settings panel to resume play.
    /// </summary>
    public void SaveSettingsClicked()
    {
        ConsolePrint("Save Settings button click detected.");
        audioControlScript.GeneralButtonClick();
        settingsPanel.SetActive(false);
    }

    /// <summary>
    /// Handles the UI event of clicking on the dice button.
    /// </summary>
    public void DiceRollClicked()
    {
        ConsolePrint("DiceRollClickedClicked called.");
        ClickSound();
        gameControlScript.DiceRollBegin();
    }

    /// <summary>
    /// A pass-through function to invoke the clickm SFGX on the audio controller
    /// made for use by scripts which are slave to this one. to avoid the need to also link
    /// those scripts to the audio manager. 
    /// </summary>
    public void ClickSound()
    {
        ConsolePrint("ClickSound called.");
        audioControlScript.GeneralButtonClick();
    }

    /// <summary>
    /// Called when the clicking Return from the rulebook panel
    /// closes the rulebok and presents the pause menu again
    /// </summary>
    public void CloseRuleBook()
    {
        ConsolePrint("CloseRuleBook called.");
        //TODO: close rulebook, show pause menu again

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
