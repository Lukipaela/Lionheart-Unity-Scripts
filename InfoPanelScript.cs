using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoPanelScript : MonoBehaviour
{

    [SerializeField] private TMPro.TextMeshProUGUI UnitTypeField;
    [SerializeField] private TMPro.TextMeshProUGUI AttackAPField;
    [SerializeField] private TMPro.TextMeshProUGUI MoveAPField;
    [SerializeField] private TMPro.TextMeshProUGUI RotateAPField;
    [SerializeField] private TMPro.TextMeshProUGUI AttackTypeField;
    [SerializeField] private TMPro.TextMeshProUGUI DicePerAttackField;
    [SerializeField] private TMPro.TextMeshProUGUI HealthPerUnitField;
    [SerializeField] private TMPro.TextMeshProUGUI PanicBehaviourField;
    [SerializeField] private Transform Location_Hidden;
    [SerializeField] private Transform Location_Active;
    [SerializeField] private Transform infoPanel;
    private string animationState = "Hidden";
    private int panelTransitionSpeed = 1500;


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
                infoPanel.position = Vector3.MoveTowards(infoPanel.position, Location_Active.position, panelTransitionSpeed * Time.deltaTime);
                if (infoPanel.position == Location_Active.position)
                    animationState = "Active";
                break;

            case "Hiding":
                infoPanel.position = Vector3.MoveTowards(infoPanel.position, Location_Hidden.position, panelTransitionSpeed * Time.deltaTime);
                if (infoPanel.position == Location_Hidden.position)
                    animationState = "Hidden";
                break;

            default: break;
        }//animation state switch
    }


    /// <summary>
    /// Fills out the info panel's fields with the data related to the specified unit class.
    /// </summary>
    /// <param name="soldierClass">An object of type SoldierClassData, which contains all of the data related to the selected unit type.</param>
    public void SetData(SoldierClassData soldierClass)
    {
        UnitTypeField.text = "Unit Type: " + soldierClass.unitClass;
        AttackAPField.text = "AP To Attack: " + soldierClass.apCostToAttack.ToString();
        MoveAPField.text = "AP To Move: " + soldierClass.apCostToMove.ToString();
        RotateAPField.text = "AP To Turn: " + soldierClass.apCostToRotate.ToString();
        AttackTypeField.text = "Attacks With: " + soldierClass.attacksWith;
        DicePerAttackField.text = "Dice/Unit: " + soldierClass.dicePerUnit.ToString();
        HealthPerUnitField.text = "Health/Unit: " + soldierClass.healthPerUnit.ToString();
        PanicBehaviourField.text = "Panic Action: " + soldierClass.panicDieAction.ToString();
    }

    /// <summary>
    /// Switches the info panel between active and inactive, sliding it onto and off of the screen. 
    /// </summary>
    public void toggleVisibility()
    {
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
}//class
