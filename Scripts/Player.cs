using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Transform cam;
    public float walkSpeed = 3f;
    private float horizontal;
    private float verticalZ;
    private float verticalY;
    private float mouseHorizontal;
    private float mouseVertical;
    private Vector3 velocity;
    [SerializeField] OutlineSelection outlineSelection;
    [SerializeField] Generate generate;
    // Start is called before the first frame update
    private void Start()
    {
        cam = GameObject.Find("Main Camera").transform;
    }

    // Update is called once per frame
    private void Update()
    {
        if (!outlineSelection.Selection && !generate.Freeze) GetPlayerInputs();

        velocity = Time.deltaTime * walkSpeed * ((transform.forward * verticalZ) + (transform.right * horizontal) + (transform.up * verticalY));
        transform.Rotate(Vector3.up * mouseHorizontal);
        cam.Rotate(Vector3.right * -mouseVertical);
        transform.Translate(velocity, Space.World);
    }

    private void GetPlayerInputs() 
    {
        horizontal = Input.GetAxis("Horizontal");
        verticalZ = Input.GetAxis("VerticalZ");
        verticalY = Input.GetAxis("VerticalY");
        mouseHorizontal = Input.GetAxis("Mouse X");
        mouseVertical = Input.GetAxis("Mouse Y");
    }
}
