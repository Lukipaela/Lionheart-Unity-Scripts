using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;


/// <summary>
/// A script to attach to a UI Button. 
/// Attach a text GameObject and this script will hide/show it when hovering over the button it's attached to after a short duration.
/// </summary>
public class HoverToolTip : MonoBehaviour, IPointerEnterHandler
{
    [SerializeField] private GameObject toolTip; //The object that is shown as a result of hovering over over the previous object 

    private float hoverTimer = 0;    //tracker for when hovering begins, in order to implement the short delay before enabling the tooltip
    private TooltipState tooltipState = TooltipState.Hidden;    //indicates that the mouse is currently hovering over the object


    /********************
     * BUILT-IN METHODS *
     ********************/

    // Start is called before the first frame update
    void Start()
    {
        if (toolTip != null)
            toolTip.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        switch (tooltipState)
        {
            case TooltipState.Showing:
                //tooltip is currently active. double check that the mouse is still present on the hoverObject
                if (!EventSystem.current.IsPointerOverGameObject())
                    HoverEnd();
                break;

            case TooltipState.Hovering:
                //hovering, but the tooltip is not yet active
                hoverTimer -= Time.deltaTime;
                if (hoverTimer < 0)
                {
                    tooltipState = TooltipState.Showing;
                    toolTip.SetActive(true);
                }// timer expired
                break;
        }//state switch
    }// Update


    /*********************
     * POINTER OVERRIDES *
     *********************/

    public void OnPointerEnter(PointerEventData eventData)
    {
        hoverTimer = 0.5f; //delayed before activating tooltip
        tooltipState = TooltipState.Hovering;
    }


    /******************
     * CUSTOM METHODS *
     ******************/

    private void HoverEnd()
    {
        hoverTimer = 0; //reset
        tooltipState = TooltipState.Hidden;
        toolTip.SetActive(false);
    }

}//HoverToolTip class
