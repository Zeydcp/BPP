using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;
using UnityEngine.UIElements.Experimental;
using System.Linq;
using UnityEngine.PlayerLoop;
using Unity.VisualScripting;

public class Dataset : MonoBehaviour
{
    [SerializeField] GameObject swapdiagram;
    [SerializeField] Transform parent;
    [SerializeField] BuildSystem buildSystem;
    private readonly Dictionary<Vector2, SortedDictionary<(float, string), string>> myDictionary = new();
    public Dictionary<string, Vector3> AddressObjects
    {get; set;} = new();
    public GameObject SplineObject
    {get; set;}
    private readonly Dictionary<float2, List<float3>> swaPositions = new();



    public string Checker(List<Vector3> positions, string input)
    {
        GroupCollection extracted;
        // commands implemented
        switch (true)
        {
            case bool malloc when Regex.IsMatch(input, @"^([a-zA-Z]+)\s*:=\s*malloc\((\d+)\)$"):
                extracted = GetValues(input, @"^([a-zA-Z]+)\s*:=\s*malloc\((\d+)\)$");
                if (positions.Count.ToString() != extracted[2].Value) return "Input Size != Malloc Size";
                input = MallocDictionary(input, positions, extracted[1].Value);
                break;
            case bool free when Regex.IsMatch(input, @"^free\(([a-zA-Z]+(?:\+\d+)?)\)$"):
                extracted = GetValues(input, @"^free\(([a-zA-Z]+(?:\+\d+)?)\)$");
                input = FreeDictionary(input, positions, extracted[1].Value);
                break;
            case bool assign when Regex.IsMatch(input, @"^\[([a-zA-Z]+(?:\+\d+)?)\]\s*:=\s*(-)?(?:(\d+(?:\.\d+)?)|\[([a-zA-Z]+(?:\+\d+)?)\])(\s*(?:-|\+|\*\s*-?|/\s*-?)\s*(?:\d+(?:\.\d+)?|\[[a-zA-Z]+(?:\+\d+)?\]))*$"):
                extracted = GetValues(input, @"^\[([a-zA-Z]+(?:\+\d+)?)\]\s*:=\s*(-)?\s*(?:(\d+(?:\.\d+)?)|\[([a-zA-Z]+(?:\+\d+)?)\])(\s*(?:-|\+|\*\s*-?|/\s*-?)\s*(?:\d+(?:\.\d+)?|\[[a-zA-Z]+(?:\+\d+)?\]))*$");
                input = AssignDictionary(input, positions, extracted);
                break;
            case bool increment when Regex.IsMatch(input, @"^\[([a-zA-Z]+(?:\+\d+)?)\]\+\+$"):
                extracted = GetValues(input, @"^\[([a-zA-Z]+(?:\+\d+)?)\]\+\+$");
                input = IncrementDictionary(input, positions, extracted[1].Value);
                break;
            case bool swap when Regex.IsMatch(input, @"^swap$"):
                input = Precendence(positions);
                break;
            default:            
                input = "Invalid Command";
                break;
        }
        return input;
    }

    public GroupCollection GetValues(string input, string pattern)
    {
        // Create a Regex object.
        Regex regex = new (pattern);

        // Find matches.
        Match match = regex.Match(input);
        return match.Groups;
    }

    private string MallocDictionary(string input, List<Vector3> positions, string keyPrefix)
    {
        string key = keyPrefix;
        List<Vector2> xzpos = positions.Select(v => new Vector2(v.x, v.z)).ToList();
        foreach (KeyValuePair<Vector2, SortedDictionary<(float, string), string>> kvp in myDictionary)
        {
            foreach ((float, string) tuple in kvp.Value.Keys)
                if (!xzpos.Contains(kvp.Key) && keyPrefix == tuple.Item2) return keyPrefix + " was taken";
        }
        myDictionary[xzpos[0]] = new SortedDictionary<(float, string), string>(new AscendingTuple<(float, string)>())
        {
            // For now, let's set the value to an underscore string
            [((float) Math.Round(positions[0].y, 1), key)] = "_"
        };

        Vector3 altposition = positions[0] - new Vector3(0, 0.4f, 0);
        AddressObjects[key] = new Vector3(altposition.x, (float) Math.Round(altposition.y, 1), altposition.z);

        for (int i = 1; i < positions.Count; i++)
        {
            key = keyPrefix + "+" + i;
            myDictionary[xzpos[i]] = new SortedDictionary<(float, string), string>(new AscendingTuple<(float, string)>())
            {
                [((float) Math.Round(positions[i].y, 1), key)] = "_"
            };
            altposition = positions[i] - new Vector3(0, 0.4f, 0);
            AddressObjects[key] = new Vector3(altposition.x, (float) Math.Round(altposition.y, 1), altposition.z);
        }

        return input;
    }

    private string FreeDictionary(string input, List<Vector3> positions, string keyPrefix)
    {
        // if you can find corresponding variable free it
        foreach (Vector3 position in positions)
        {
            if (GetVal(position, "address") == keyPrefix) 
            {
                myDictionary[new Vector2(position.x, position.z)][((float) Math.Round(position.y, 1), "")] = "";
                Vector3 altposition = position - new Vector3(0, 0.4f, 0);
                AddressObjects[keyPrefix] = new Vector3(altposition.x, (float) Math.Round(altposition.y, 1), altposition.z);
                return input;
            }
        }
        // else return empty string
        return keyPrefix + " not found";
    }

    private string AssignDictionary(string input, List<Vector3> positions, GroupCollection extracted)
    {
        // Checks if the positions contain the addresses referenced in extracted values
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
                    case bool second when Regex.IsMatch(output, @"^(\*|/|\+|-)(\d+(?:\.\d+)?|[a-zA-Z]+(?:\+\d+)?)"):
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

        for (int i = 0; i < reformat.Count; i++)
        {
            Match address = Regex.Match(reformat[i], @"^[a-zA-Z]+(\+\d+)?");
            if (address.Success)
            {
                bool containsAddress = false;
                foreach (Vector3 position in positions)
                {  
                    string adr = GetVal(position, "address");
                    if (adr == reformat[i])
                    {
                        containsAddress = true;
                        reformat[i] = GetVal(position, "value");

                        if (i > 0 && reformat[i] == "_") return adr + " unassigned";
                    }
                }

                if (!containsAddress) return reformat[i] + " not found";
            }
        }

        reformat.RemoveAt(0);

        if (reformat[0] == "-")
        {
            reformat.RemoveAt(0);

            double.TryParse(reformat[0], out double number);
            reformat[0] = (-number).ToString();
        }
        
        List<int> indexList = new();
        List<string> symbols = new() {"/-", "*-"};

        // Search for the first condition
        foreach (string symbol in symbols)
        {
            int index = reformat.IndexOf(symbol);
            while (index != -1)
            {
                indexList.Add(index);
                index = reformat.IndexOf(symbol, index+1);
            }
        }

        // Now you can work with each index
        foreach (int idx in indexList)
        {
            reformat[idx] = reformat[idx].Remove(reformat[idx].Length-1);

            double.TryParse(reformat[idx+1], out double number);
            reformat[idx+1] = (-number).ToString();
        }
    

        reformat.Reverse();

        symbols.Clear();
        symbols.AddRange(new string[] {"/", "*", "-", "+"});

        foreach (string symbol in symbols)
        {
            int index = reformat.IndexOf(symbol);
            List<int> indices = new();
            while (index != -1)
            {
                indices.Add(index);
                index = reformat.IndexOf(symbol, index+1);
            }
            indices.Reverse();

            foreach (int idx in indices)
            {
                double.TryParse(reformat[idx+1], out double number1); 
                double.TryParse(reformat[idx-1], out double number2);

                for (int _ = 0; _ < 2; _++) reformat.RemoveAt(idx);
                switch (symbol)
                {
                    case "/":
                        reformat[idx-1] = (number1/number2).ToString();
                        break;
                    case "*":
                        reformat[idx-1] = (number1*number2).ToString();
                        break;
                    case "-":
                        reformat[idx-1] = (number1-number2).ToString();
                        break;
                    case "+":
                        reformat[idx-1] = (number1+number2).ToString();
                        break;
                    default:
                        break;
                }
            }
        }
        
        foreach (Vector3 position in positions)
        {
            string address = GetVal(position, "address");
            Vector3 altposition = position - new Vector3(0, 0.4f, 0);
            AddressObjects[address] = new Vector3(altposition.x, (float) Math.Round(altposition.y, 1), altposition.z);

            if (address == extracted[1].Value)
            {
                myDictionary[new Vector2(position.x, position.z)]
                    [((float) Math.Round(position.y, 1), extracted[1].Value)] = reformat[0];
            }
        }

        return input;
    }

    private string IncrementDictionary(string input, List<Vector3> positions, string key)
    {
        foreach (Vector3 position in positions)
        {
            if (GetVal(position, "address") == key)
            {
                string svalue = GetVal(position, "value");
                if(double.TryParse(svalue, out double ivalue))
                {
                    ivalue += 1;
                    svalue = ivalue.ToString();

                    myDictionary[new Vector2(position.x, position.z)][((float) Math.Round(position.y, 1), key)] = svalue;

                    Vector3 altposition = position - new Vector3(0, 0.4f, 0);
                    AddressObjects[key] = new Vector3(altposition.x, (float) Math.Round(altposition.y, 1), altposition.z);

                    return input;
                }
                else return key + " unassigned";
            }
        }
        return key + " not found";
    }

    private string Precendence(List<Vector3> positions)
    {
        if (positions.Count > 2) return "Swap block size exceeds 2";

        List<float3> floatPosition = positions.ConvertAll(vec => new float3(vec.x, vec.y, vec.z));
        if (positions.Count == 2)
        {
            SwapDictionary(floatPosition);
            return "swap";
        }
        
        float2 floatyz = new((float) Math.Round(floatPosition[0].y, 1), floatPosition[0].z);

        if (swaPositions.ContainsKey(floatyz))
            swaPositions[floatyz].Add(floatPosition[0]);
        else swaPositions[floatyz] = new() {floatPosition[0]};

        if (swaPositions[floatyz].Count > 1)
        {
            SwapDictionary(swaPositions[floatyz]);
            swaPositions.Remove(floatyz);
        }

        return "swap";
    }

    private void SwapDictionary(List<float3> givenPositions)
    {
        if (givenPositions[0].x > givenPositions[1].x) 
            (givenPositions[0], givenPositions[1]) = (givenPositions[1], givenPositions[0]);

        List<Vector3> convertback = givenPositions.ConvertAll(vec => new Vector3(vec.x, vec.y, vec.z));

        foreach (GameObject swapBlock in GameObject.FindGameObjectsWithTag("swap"))
        {
            if (swapBlock.transform.localScale.x > 1 || convertback.Contains(swapBlock.transform.position))
                Destroy(swapBlock);
        }

        givenPositions = givenPositions.ConvertAll(vec => vec + new float3(0, 0.2f, 0));
        Quaternion rotation = Quaternion.Euler(-90, 0, 0);
        SplineObject = Instantiate(swapdiagram, givenPositions[0], rotation, parent);

        SplineContainer splineContainer = SplineObject.GetComponent<SplineContainer>();
        IReadOnlyList<Spline> spline = splineContainer.Splines;

        BezierKnot bezierKnot = new(givenPositions[1] - givenPositions[0]);
        float width = bezierKnot.Position.x;

        BoxCollider boxCollider = SplineObject.transform.GetChild(0).GetComponent<BoxCollider>();

        boxCollider.center = new Vector3(bezierKnot.Position.x, 0, -2.4f);
        float3 tangent = new(0, 0, 0.05f);
        rotation = Quaternion.Euler(0, 180, 315);

        spline[1][0] = new(bezierKnot.Position, -tangent, tangent, rotation);

        bezierKnot.Position.z = -2.4f;
        rotation = Quaternion.Euler(0, 180, 45);
        spline[0][5] = new(bezierKnot.Position, -tangent, tangent, rotation);

        bezierKnot.Position.z = -1.9f;
        tangent = new(0, 0, 0.2375f);
        spline[0][4] = new(bezierKnot.Position, -tangent, tangent, rotation);

        bezierKnot.Position.z = -0.5f;
        rotation = Quaternion.Euler(0, 180, 315);
        spline[1][1] = new(bezierKnot.Position, -tangent, tangent, rotation);

        bezierKnot.Position.y = bezierKnot.Position.x > 1 ? -1f : -0.7f;
        bezierKnot.Position.x -= 0.1f;
        bezierKnot.Position.z = -1f;
        spline[1][2] = bezierKnot;

        bezierKnot.Position.y = -bezierKnot.Position.y;
        bezierKnot.Position.z = -1.4f;
        spline[0][3] = bezierKnot;

        bezierKnot.Position.x = 0.1f;
        bezierKnot.Position.z = -1f;
        spline[0][2] = bezierKnot;

        bezierKnot.Position.y = -bezierKnot.Position.y;
        bezierKnot.Position.z = -1.4f;
        spline[1][3] = bezierKnot;

        List<Vector2> pos = givenPositions.ConvertAll(pos => new Vector2(pos.x, pos.z));

        List<string> lastValue = new()
        {
            GetVal(givenPositions[0], "value"),
            GetVal(givenPositions[1], "value")
        };

        List<string> address = new()
        {
            GetVal(givenPositions[0], "address"),
            GetVal(givenPositions[1], "address")
        };

        myDictionary[pos[0]][((float) Math.Round(givenPositions[0].y, 1), address[1])] = lastValue[1];
        myDictionary[pos[1]][((float) Math.Round(givenPositions[1].y, 1), address[0])] = lastValue[0];

        List<float3> altposition = givenPositions.ConvertAll(vec => vec - new float3(0, 2.6f, 0));
        AddressObjects[address[0]] = new Vector3(altposition[1].x, (float) Math.Round(altposition[1].y, 1), altposition[1].z);
        AddressObjects[address[1]] = new Vector3(altposition[0].x, (float) Math.Round(altposition[0].y, 1), altposition[0].z);

        IEnumerator coroutine = buildSystem.SplineGen();
        StartCoroutine(coroutine);

        float2 floatyz = new((float) Math.Round(givenPositions[0].y, 1), givenPositions[0].z);
        swaPositions.Remove(floatyz);
    }

    public string Retriever(Vector3 position)
    {
        string address = GetVal(position, "address");
        string value = GetVal(position, "value");

        if (address == null || address == "") return address + " was freed";
        else return address + " → " + value;
    }

    public string GetVal(Vector3 position, string option)
    {
        Vector2 xzpos = new(position.x, position.z); 
        if (myDictionary.TryGetValue(xzpos, out var innerDictionary))
        {
            foreach (KeyValuePair<(float, string), string> kvp in innerDictionary) 
                if (position.y < kvp.Key.Item1)
                    if (option == "address") return kvp.Key.Item2;
                    else if (option == "value") return kvp.Value;
        }
        return null;
    }

    class AscendingTuple<T> : IComparer<(float, string)> where T : IComparable<(float, string)> 
    {
        public int Compare((float, string) x, (float, string) y) {
            return x.Item1.CompareTo(y.Item1);
        }
    }

}
