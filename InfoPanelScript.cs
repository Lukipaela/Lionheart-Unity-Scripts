using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class InfoPanelScript : MonoBehaviour
{
    public PanelHider panelHider;

    [SerializeField] private TMPro.TextMeshProUGUI UnitTypeField;
    [SerializeField] private TMPro.TextMeshProUGUI AttackAPField;
    [SerializeField] private TMPro.TextMeshProUGUI MoveAPField;
    [SerializeField] private TMPro.TextMeshProUGUI RotateAPField;
    [SerializeField] private TMPro.TextMeshProUGUI AttackTypeField;
    [SerializeField] private TMPro.TextMeshProUGUI DicePerAttackField;
    [SerializeField] private TMPro.TextMeshProUGUI HealthPerUnitField;
    [SerializeField] private TMPro.TextMeshProUGUI PanicBehaviourField;



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
     * CUSTOM METHODS *
     ******************/

    /// <summary>
    /// Fills out the info panel's fields with the data related to the specified unit class.
    /// </summary>
    /// <param name="soldierClass">An object of type SoldierClassData, which contains all of the data related to the selected unit type.</param>
    public void SetData(SoldierClassAttributes soldierClass)
    {
        UnitTypeField.text = "Unit Type: " + soldierClass.soldierClass;
        AttackAPField.text = "AP To Attack: " + soldierClass.apCostToAttack.ToString();
        MoveAPField.text = "AP To Move: " + soldierClass.apCostToMove.ToString();
        RotateAPField.text = "AP To Turn: " + soldierClass.apCostToRotate.ToString();
        AttackTypeField.text = "Attacks With: " + soldierClass.attacksWith;
        DicePerAttackField.text = "Dice/Unit: " + soldierClass.dicePerUnit.ToString();
        HealthPerUnitField.text = "Health/Unit: " + soldierClass.healthPerUnit.ToString();
        PanicBehaviourField.text = "Panic Action: " + soldierClass.panicDieAction.ToString();
    }

}//class
