using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using System.Collections.Generic;
public class OutlineSelection : MonoBehaviour
{
    private Transform highlight;
    public Transform Selection
    {get; set;}
    private RaycastHit raycastHit;
    [SerializeField] TextAdder textAdder;

    void Update()
    {
        // Highlight
        if (highlight != null)
        {
            highlight.GetComponent<Outline>().enabled = false;
            highlight = null;
        }
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));;
        if (Physics.Raycast(ray, out raycastHit)) //Make sure you have EventSystem in the hierarchy before using EventSystem
        {
            highlight = raycastHit.transform;
            if (!highlight.CompareTag("Base") && highlight != Selection)
            {
                if (highlight.parent.CompareTag("box")) highlight = highlight.parent;

                if (highlight.GetComponent<Outline>() != null)
                {
                    highlight.GetComponent<Outline>().enabled = true;
                }
                else
                {
                    Outline outline = highlight.gameObject.AddComponent<Outline>();
                    outline.enabled = true;
                    outline.OutlineColor = Color.red;
                    outline.OutlineWidth = 7f;
                }
            }
            else
            {
                highlight = null;
            }
        }

        // Selection
        if (Input.GetKeyDown(KeyCode.T))
        {

            if (highlight)
            {
                if (highlight.name == "Input")
                    Selection = raycastHit.transform;

                // Needed to put this here due to order, should be in Text Adder
                textAdder.ChildComponentsetter(highlight);
                textAdder.Turnoncanvas(highlight);
                
                highlight = null;
            }
            else if (Selection)
            {
                Selection.GetComponent<Outline>().enabled = false;
                Selection = null;

                // And might as well add this
                textAdder.Turnoffcanvas();
            }
        }
    }

    public bool Highlight()
    {
        return highlight;
    }
}
