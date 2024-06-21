using System.Collections;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using System.Text.RegularExpressions;
using System.Threading;
using Unity.VisualScripting;
public class Generate : MonoBehaviour
{
    [SerializeField] GameObject button, genfield, inputBlock, outputBlock;
    [SerializeField] Transform parent;
    [SerializeField] Dataset dataset;
    [SerializeField] OutlineSelection outlineSelection;
    private IEnumerator coroutine;
    public bool Freeze
    {get; set;} = false;

    private bool textclicked;
    private TextMeshProUGUI buttontext;
    private Vector3 index;
    private float pushx, pushz;
    [SerializeField] TextAdder textAdder;
    // Start is called before the first frame update
    void Start()
    {
        genfield.SetActive(false);
        button.SetActive(true);
        buttontext = button.GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Update()
    {
        if (!outlineSelection.Selection)
        {
            if (!textclicked && Input.GetKeyDown(KeyCode.G)) Switch();
        }
    }

    public void Switch()
    {
        if (buttontext.text == "Generate")
        {
            buttontext.text = "Design";
            genfield.SetActive(true);
            Freeze = true;
        }

        else
        {
            buttontext.text = "Generate";
            genfield.SetActive(false);
            Freeze = false;
        }
    }

    public void Criterion(string input)
    {
        
        if (dataset.AddressObjects.Any())
            pushx = dataset.AddressObjects.Values.Max(tuple => tuple.x) + 1;
        else pushx = 0;

        if (pushx != 0)
            pushz = dataset.AddressObjects.Values.ToList()[0].z;
        else pushz = 5;


        List<string> regex = new()
        {
            @"^(([a-zA-Z]+)(?:\+(\d+))?)\s*->\s*(-?\d+(?:\.\d+)?)$",
            @"^([a-zA-Z]+)\s*:=\s*malloc\((\d+)\)$",
            @"^free\(([a-zA-Z]+(?:\+\d+)?)\)$",
            @"^\[([a-zA-Z]+(?:\+\d+)?)\]\+\+$",
            @"^\[([a-zA-Z]+(?:\+\d+)?)\]\s*:=\s*(-)?(?:(\d+(?:\.\d+)?)|\[([a-zA-Z]+(?:\+\d+)?)\])(\s*(?:-|\+|\*\s*-?|/\s*-?)\s*(?:\d+(?:\.\d+)?|\[[a-zA-Z]+(?:\+\d+)?\]))*$",
        };
        GroupCollection extracted;
        switch (true)
        {
            case bool assume when Regex.IsMatch(input, regex[0]):
                extracted = dataset.GetValues(input, regex[0]);
                CreateState(extracted);
                break;
            case bool malloc when Regex.IsMatch(input, regex[1]):
                extracted = dataset.GetValues(input, regex[1]);
                coroutine = BuildMalloc(extracted[1].Value, int.Parse(extracted[2].Value), 4.4f);
                StartCoroutine(coroutine);
                break;
            case bool free when Regex.IsMatch(input, regex[2]):
                extracted = dataset.GetValues(input, regex[2]);
                StartCoroutine(Free(extracted[1].Value));
                break;
            case bool increment when Regex.IsMatch(input, regex[3]):
                extracted = dataset.GetValues(input, regex[3]);
                StartCoroutine(Increment(extracted[1].Value));
                break;
            case bool assign when Regex.IsMatch(input, regex[4]):
                extracted = dataset.GetValues(input, regex[4]);
                string assigned = Assign(input, extracted);
                break;
            default:
                break;
        }
        textclicked = false;
    }

    private void CreateState(GroupCollection extracted)
    {
        int mallocvalue;
        string addressvalue;
        mallocvalue = (extracted[3].Value != "") ? int.Parse(extracted[3].Value) + 1 : 1;
        addressvalue = extracted[4].Value;
        
        StartCoroutine(Pauseandwait());

        IEnumerator Pauseandwait()
        {
            if (!dataset.AddressObjects.ContainsKey(extracted[1].Value))
            {
                coroutine = BuildMalloc(extracted[2].Value, mallocvalue, 6.4f);
                yield return StartCoroutine(coroutine);
            }

            string text = $"[{extracted[1].Value}] := {addressvalue}";

            coroutine = AddInput(extracted[1].Value, text);
            yield return StartCoroutine(coroutine);

            if (index.y > 4.4f)
                index -= new Vector3(0, 1.2f, 0);
            else 
                index -= new Vector3(0, 0.4f, 0);
            
            dataset.AddressObjects[extracted[1].Value] = new Vector3(index.x, (float) Math.Round(index.y, 1), index.z);

            coroutine = AddOutput(extracted[1].Value);
            yield return StartCoroutine(coroutine);
        }
    }

    private IEnumerator BuildMalloc(string address, int mallocvalue, float yvalue)
    {
        index = new Vector3(pushx, yvalue, pushz);

        Transform selection;
        for (int i = 0; i < mallocvalue; i++)
        {
            selection = Instantiate(inputBlock, index, Quaternion.identity, parent).transform;
            index += new Vector3(1, 0, 0);
            selection.name = "Input";
            yield return new WaitForSeconds(0.2f);
        }

        selection = parent.GetChild(parent.childCount - 1);
        textAdder.ChildComponentsetter(selection);
        string text = $"{address} := malloc({mallocvalue})";
        textAdder.ReadStringInput(text);
            
        index = new Vector3(pushx, yvalue - 0.4f, pushz);

        for (int i = 0; i < mallocvalue; i++)
        {
            string temp = dataset.GetVal(index, "address");

            coroutine = AddOutput(temp);
            yield return StartCoroutine(coroutine);

            index += new Vector3(1, 0, 0);
        }
    }

    private IEnumerator Free(string address)
    {
        if (!dataset.AddressObjects.ContainsKey(address)) yield break;

        List<Vector2> checkFor = new(){new(dataset.AddressObjects[address].x, dataset.AddressObjects[address].z)};

        coroutine = DestroyOutputs(checkFor, dataset.AddressObjects[address].y);
        yield return StartCoroutine(coroutine);
        
        string text = $"free({address})";

        coroutine = AddInput(address, text);
        yield return StartCoroutine(coroutine);

        dataset.AddressObjects.Remove(address);
    }

    private IEnumerator Increment(string address)
    {
        if (!dataset.AddressObjects.ContainsKey(address)) yield break;

        List<Vector2> checkFor = new(){new(dataset.AddressObjects[address].x, dataset.AddressObjects[address].z)};

        coroutine = DestroyOutputs(checkFor, dataset.AddressObjects[address].y);
        yield return StartCoroutine(coroutine);

        string text = $"[{address}]++";

        coroutine = AddInput(address, text);
        yield return StartCoroutine(coroutine);

        index -= new Vector3(0, 0.4f, 0);
        dataset.AddressObjects[address] = new Vector3(index.x, (float) Math.Round(index.y, 1), index.z);

        coroutine = AddOutput(address);
        yield return StartCoroutine(coroutine);
    }

    private IEnumerator AddInput(string address, string text)
    {
        index = dataset.AddressObjects[address];

        Transform selection = Instantiate(inputBlock, index, Quaternion.identity, parent).transform;
        selection.name = selection.name[..(selection.name.Length-7)];
        textAdder.ChildComponentsetter(selection);

        textAdder.ReadStringInput(text);

        yield return new WaitForSeconds(0.2f);
    }

    private IEnumerator AddOutput(string address)
    {
        index = dataset.AddressObjects[address];
        Transform selection = Instantiate(outputBlock, index, Quaternion.identity, parent).transform;
        selection.name = selection.name[..(selection.name.Length-7)];
        textAdder.ChildComponentsetter(selection);
        textAdder.Turnoncanvas(selection);
        index -= new Vector3(0, 0.4f, 0);

        dataset.AddressObjects[address] = new Vector3(index.x, (float) Math.Round(index.y, 1), index.z);

        yield return new WaitForSeconds(0.2f);
    }

    private string Assign(string input, GroupCollection extracted)
    {
        List<string> reformat =  new(){extracted[1].Value};
        for (int i = 2; i < extracted.Count; i++)
        {
            foreach (Capture capture in extracted[i].Captures)
            {
                string output = Regex.Replace(capture.Value, @"\s+|\[|\]", "");
                switch (true)
                {
                    case bool first when Regex.IsMatch(output, @"^(\*-|/-)"):
                        string part1 = output[..2];
                        string part2 = output[2..];
                        reformat.Add(part1);
                        reformat.Add(part2);
                        continue;
                    case bool second when Regex.IsMatch(output, @"^(\*|/|\+|-)[a-zA-Z]+(?:\+\d+)?"):
                        part1 = output[..1];
                        part2 = output[1..];
                        reformat.Add(part1);
                        reformat.Add(part2);
                        continue;
                    default:
                        reformat.Add(output);
                        continue;
                }
            }
        }

        Regex regex = new(@"([a-zA-Z]+(?:\+\d+)?)");
        reformat = reformat.Where(item => regex.IsMatch(item)).ToList();

        reformat = reformat.Distinct().ToList();
        
        List<Vector2> checkFor = new();
        foreach (string address in reformat)
        {
            if (!dataset.AddressObjects.ContainsKey(address)) return address + " not found";
            checkFor.Add(new Vector2(dataset.AddressObjects[address].x, dataset.AddressObjects[address].z));
        }

        reformat = reformat.OrderBy(adr => dataset.AddressObjects[adr].x).ToList();
        List<string> updatedReformat = new(reformat);

        for (int i = 0; i < updatedReformat.Count - 1; i++)
        {
            float difference = dataset.AddressObjects[updatedReformat[i+1]].x - dataset.AddressObjects[updatedReformat[i]].x - 1;

            for (int j = 0; j < difference; j++) 
                updatedReformat.Insert(i+1, "");

            i += (int) difference;
        }
        
        List<(int, int)> swapList = new();

        if (updatedReformat.Count != reformat.Count)
        {
            int n = updatedReformat.Count;

            // Step 1: Collect indices of non-empty strings
            List<int> nonEmptyIndices = new();
            for (int i = 0; i < n; i++)
            {
                if (!string.IsNullOrEmpty(updatedReformat[i]))
                    nonEmptyIndices.Add(i);
            }

            int numNonEmpty = nonEmptyIndices.Count;

            // Step 2: Find the minimum swap sequence to get the non-empty strings together
            int minSwaps = int.MaxValue;

            for (int start = 0; start <= n - numNonEmpty; start++)
            {
                List<(int, int)> tempswapList = new();

                IEnumerable<int> range = Enumerable.Range(start, numNonEmpty);

                List<int> onesToSwap = nonEmptyIndices.Except(range).ToList();

                List<int> withToSwap = range.Except(nonEmptyIndices).Reverse().ToList();
                int swaps = onesToSwap.Count;

                for (int i = 0; i < swaps; i++)
                    tempswapList.Add((onesToSwap[i], withToSwap[i]));

                if (swaps < minSwaps)
                {
                    minSwaps = swaps;
                    swapList = tempswapList;
                }

                if (minSwaps == 1)
                {
                    start = n - numNonEmpty;
                    break;
                }
            }
        }

        coroutine = secondCorountine(swapList);
        StartCoroutine(coroutine);

        IEnumerator secondCorountine(List<(int, int)> swapList)
        {
            if (updatedReformat.Count != reformat.Count)
            {
                coroutine = SwappartofAssign(updatedReformat, swapList);
                yield return StartCoroutine(coroutine);
            }
            float targetY = reformat.Min(address => dataset.AddressObjects[address].y);

            coroutine = DestroyOutputs(checkFor, targetY);
            yield return StartCoroutine(coroutine);

            List<GameObject> outputObjects = GameObject.FindGameObjectsWithTag("Output").ToList();

            outputObjects = outputObjects.Where(obj => checkFor.Contains(
                new Vector2(obj.transform.position.x, obj.transform.position.z))).ToList();

            List<IGrouping<string, GameObject>> important1 = outputObjects
                .GroupBy(obj => dataset.GetVal(obj.transform.position, "address"))
                .ToList();

            List<GameObject> important2 = important1
                .Select(obj => obj.OrderBy(obj2 => obj2.transform.position.y).First()).ToList();

            foreach (GameObject obj in important2)
            {
                string address = dataset.GetVal(obj.transform.position, "address");
                if (obj.transform.position.y <= dataset.AddressObjects[address].y)
                {
                    index = obj.transform.position;
                    index -= new Vector3(0, 0.4f, 0);
                    
                    dataset.AddressObjects[address] = new Vector3(index.x, (float) Math.Round(index.y, 1), index.z);
                }
            }

            Transform selection;
            foreach (string address in reformat)
            {
                int difference = (int) Math.Round((dataset.AddressObjects[address].y - targetY) / 0.4f);

                for (int i = 0; i < difference; i++)
                {
                    index = dataset.AddressObjects[address];
                    selection = Instantiate(outputBlock, index, Quaternion.identity, parent).transform;
                    selection.name = selection.name[..(selection.name.Length-7)];
                    index -= new Vector3(0, 0.4f, 0);
                    dataset.AddressObjects[address] = new Vector3(index.x, (float) Math.Round(index.y, 1), index.z);
                    yield return new WaitForSeconds(1/5f);
                }

                index = dataset.AddressObjects[address];
                selection = Instantiate(inputBlock, index, Quaternion.identity, parent).transform;
                selection.name = selection.name[..(selection.name.Length-7)];
                index -= new Vector3(0, 0.4f, 0);

                dataset.AddressObjects[address] = new Vector3(index.x, (float) Math.Round(index.y, 1), index.z);
                yield return new WaitForSeconds(1/5f);
            }

            selection = parent.GetChild(parent.childCount - 1);
            textAdder.ChildComponentsetter(selection);
            textAdder.ReadStringInput(input);

            foreach (string address in reformat)
            {
                coroutine = AddOutput(address);
                yield return StartCoroutine(coroutine);
            }
        }
        return input;
    }

    private IEnumerator SwappartofAssign(List<string> updatedReformat, List<(int, int)> swapList)
    {
        foreach((int, int) swap in swapList)
        {
            string address1 = updatedReformat[swap.Item1];
            Vector3 adr1pos = dataset.AddressObjects[address1];

            Vector3 adr2pos = adr1pos - new Vector3(swap.Item1 - swap.Item2, 100, 0);
            string address2 = dataset.GetVal(adr2pos, "address");
            adr2pos = dataset.AddressObjects[address2];

            List<Vector3> temp = new(){adr1pos, adr2pos};
            adr1pos = temp.OrderByDescending(adr => adr.y).First();
            adr2pos = temp.OrderByDescending(adr => adr.y).Last();

            address1 = dataset.GetVal(adr1pos, "address");
            address2 = dataset.GetVal(adr2pos, "address");

            List<Vector2> checkFor = new()
            {
                new Vector2(dataset.AddressObjects[address1].x, dataset.AddressObjects[address1].z),
                new Vector2(dataset.AddressObjects[address2].x, dataset.AddressObjects[address2].z)
            };

            coroutine = DestroyOutputs(checkFor, adr2pos.y);
            yield return StartCoroutine(coroutine);

            List<GameObject> outputObjects = GameObject.FindGameObjectsWithTag("Output").ToList();

            outputObjects = outputObjects.Where(obj => checkFor.Contains(
                new Vector2(obj.transform.position.x, obj.transform.position.z))).ToList();

            List<IGrouping<string, GameObject>> important1 = outputObjects
                .GroupBy(obj => dataset.GetVal(obj.transform.position, "address"))
                .ToList();

            List<GameObject> important2 = important1
            .Select(obj => obj.OrderBy(obj2 => obj2.transform.position.y).First()).ToList();

            foreach (GameObject obj in important2)
            {
                string address = dataset.GetVal(obj.transform.position, "address");
                if (obj.transform.position.y <= dataset.AddressObjects[address].y)
                {
                    index = obj.transform.position;
                    index -= new Vector3(0, 0.4f, 0);
                    
                    dataset.AddressObjects[address] = new Vector3(index.x, (float) Math.Round(index.y, 1), index.z);
                    adr1pos = dataset.AddressObjects[address];
                }
            }

            int difference = (int) Math.Round((adr1pos.y - adr2pos.y) / 0.4f);

            for (int i = 0; i < difference; i++)
            {
                Transform selection = Instantiate(outputBlock, adr1pos, Quaternion.identity, parent).transform;
                selection.name = selection.name[..(selection.name.Length-7)];
                adr1pos -= new Vector3(0, 0.4f, 0);
                yield return new WaitForSeconds(1/5f);
            }
            
            dataset.AddressObjects[address1] = new Vector3(adr1pos.x, (float) Math.Round(adr1pos.y, 1), adr1pos.z);

            coroutine = AddInput(address1, "swap");
            yield return StartCoroutine(coroutine);

            coroutine = AddInput(address2, "swap");
            yield return StartCoroutine(coroutine);

            yield return new WaitForSeconds(3);

            coroutine = AddOutput(address1);
            yield return StartCoroutine(coroutine);

            coroutine = AddOutput(address2);
            yield return StartCoroutine(coroutine);
        }
    }

    private IEnumerator DestroyOutputs(List<Vector2> checkFor, float comparer)
    {
        List<GameObject> outputObjects = GameObject.FindGameObjectsWithTag("Output").ToList();

        outputObjects = outputObjects.Where(obj => checkFor.Contains(
            new Vector2(obj.transform.position.x, obj.transform.position.z))).ToList();

        outputObjects.Reverse();

        List<IGrouping<string, GameObject>> important1 = outputObjects
            .GroupBy(obj => dataset.GetVal(obj.transform.position, "address"))
            .ToList();
            
        foreach (IGrouping<string, GameObject> grouping in important1)
        {   
            foreach (GameObject gameObject in grouping)
            {
                if (gameObject.transform.position.y <= comparer)
                {
                    Destroy(gameObject);
                    yield return new WaitForSeconds(0.2f);
                }
            }
        }
    }

    public void Textselected()
    {
        textclicked = true;
    }
}
