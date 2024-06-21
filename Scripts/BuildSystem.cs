using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.Splines;
using Unity.Mathematics;
using System;
using System.Linq;

public class BuildSystem : MonoBehaviour
{
    [SerializeField] Block[] availableBuildingBlocks;
    int currentBlockIndex = 0;
    Block currentBlock;
    [SerializeField] TMP_Text blockNameText;
    [SerializeField] Transform shootingPoint, parent;
    [SerializeField] TextAdder textAdder;
    [SerializeField] Dataset dataset; 
    [SerializeField] OutlineSelection outlineSelection;
    [SerializeField] Generate generate;
    private SplineExtrude spl;
    public SplineContainer Spl2
    {get; set;}

    private void Update()
    {
        if (!outlineSelection.Selection && !generate.Freeze)
        {
            if (Input.GetKeyDown(KeyCode.Space)) BuildBlock(currentBlock.BlockObject);
            if (Input.GetKeyDown(KeyCode.D)) DestroyBlock();
        }

        ChangeCurrentBlock();
    }

    public IEnumerator SplineGen()
    {
        textAdder.Turnoffcanvas();

        spl = dataset.SplineObject.GetComponent<SplineExtrude>();
        Spl2 = dataset.SplineObject.GetComponent<SplineContainer>();
        
        while (spl.Range[1] < 1) 
        {
            spl.Range += new Vector2(0, Time.deltaTime / 3);
            yield return null;
        }

        spl.Range = new Vector2(0, 1);

        MeshFilter meshFilter = dataset.SplineObject.GetComponent<MeshFilter>();

        // Get the original mesh from the MeshFilter
        Mesh originalMesh = meshFilter.mesh;

        // Create a new mesh and copy data from the original mesh
        Mesh standaloneMesh = new()
        {
            vertices = originalMesh.vertices,
            triangles = originalMesh.triangles,
            normals = originalMesh.normals,
            colors = originalMesh.colors,
            tangents = originalMesh.tangents,
            bounds = originalMesh.bounds,

            // Optionally copy other mesh data (uv2, uv3, uv4, etc.) if needed
            uv = originalMesh.uv,
            uv2 = originalMesh.uv2,
            uv3 = originalMesh.uv3,
            uv4 = originalMesh.uv4,
            uv5 = originalMesh.uv5,
            uv6 = originalMesh.uv6,
            uv7 = originalMesh.uv7,
            uv8 = originalMesh.uv8
        };

        // Assign the new mesh to the MeshFilter
        meshFilter.mesh = standaloneMesh;

        // // Remove Extrude component
        Destroy(Spl2);
    }

    void ChangeCurrentBlock()
    {
        float scroll = Input.mouseScrollDelta.y;
        if (scroll == 1)
        {
            currentBlockIndex++;
            if (currentBlockIndex > availableBuildingBlocks.Length - 1)
                currentBlockIndex = 0;
        }
        else if (scroll == -1)
        {
            currentBlockIndex--;
            if (currentBlockIndex < 0)
                currentBlockIndex = availableBuildingBlocks.Length - 1;
        }
        currentBlock = availableBuildingBlocks[currentBlockIndex];
        SetText();
    }
 
    void SetText()
    {
        blockNameText.text = currentBlock.BlockName;
    }
 
    void BuildBlock(GameObject block)
    {
        if (Physics.Raycast(shootingPoint.position, shootingPoint.forward, out RaycastHit hitInfo))
        {
            float valueToRound = (float) Math.Round((hitInfo.point.y + hitInfo.normal.y/5) / 0.4f, MidpointRounding.AwayFromZero) * 0.4f;
            float rounded = (float) Math.Round(valueToRound, 1);
            Vector3 spawnPosition = new (
                Mathf.RoundToInt(hitInfo.point.x + hitInfo.normal.x/2), 
                rounded,
                Mathf.RoundToInt(hitInfo.point.z + hitInfo.normal.z/2));
            GameObject newObject = Instantiate(block, spawnPosition, Quaternion.identity, parent);
            newObject.name = block.name;
        }
    }
 
    void DestroyBlock()
    {
        if (Physics.Raycast(shootingPoint.position, shootingPoint.forward, out RaycastHit hitInfo))
        {
            if (hitInfo.transform.parent.CompareTag("box"))
                Destroy(hitInfo.transform.parent.gameObject);

            else if (!hitInfo.transform.CompareTag("Base"))
                Destroy(hitInfo.transform.gameObject);
        }
    }
}
