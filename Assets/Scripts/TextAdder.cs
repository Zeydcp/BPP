using UnityEngine;
using TMPro;
using System.Collections.Generic;
using System;
public class TextAdder : MonoBehaviour
{
    private TextMeshProUGUI textBox;
    [SerializeField] GameObject textField;
    [SerializeField] Dataset dataset;

    public void ReadStringInput(string input)
    {
        List<Vector3> positions = Calculatepositions();
        
        if (input == "swap")
            textBox.transform.parent.parent.parent.tag = input;
        
        textBox.text = dataset.Checker(positions, input);
        Resize(textBox.transform.parent.parent.parent);
    }

    public void ChildComponentsetter(Transform selection)
    {
        textBox = selection.GetComponentInChildren<TextMeshProUGUI>();
    }

    public void Turnoncanvas(Transform selection)
    {
        if (selection.name == "Input") 
            textField.SetActive(true);
        else
        {
            Vector3 position = textBox.transform.parent.parent.parent.position;
            textBox.text = dataset.Retriever(position);
        }
    }

    public void Turnoffcanvas()
    {
        textField.SetActive(false);
    }

    private List<Vector3> Calculatepositions()
    {
        List<Vector3> positions = new();

        // Initialize variables needed for position calculation
        int xscale = (int) Math.Round(textBox.transform.parent.parent.parent.localScale.x);
        Vector3 pos = textBox.transform.parent.parent.parent.position;
        float xpos = pos.x;
        xpos -= (xscale - 1) / 2f;
        pos = new Vector3(xpos, pos.y, pos.z);
        for (int i = 0; i < xscale; i++)
        {
            positions.Add(pos);
            pos += new Vector3(1, 0, 0);
        }
        return positions;
    }

    private void Resize(Transform block)
    {
        if (block.localScale.x % 1 != 0.9f) 
        {
            block.localScale = new Vector3(
                (float) Math.Round(block.localScale.x) - 0.1f, block.localScale.y, block.localScale.z);

            Transform canvas = block.GetChild(0).GetChild(0).GetChild(0);
            RectTransform rt = canvas.GetComponent<RectTransform>();

            rt.sizeDelta = new Vector2(125*block.localScale.x, rt.sizeDelta.y);
            rt.localScale = new Vector3(1/rt.sizeDelta.x, rt.localScale.y, rt.localScale.z);
        }
    }
}
