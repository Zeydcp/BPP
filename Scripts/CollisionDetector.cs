using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.ProBuilder;

public class CollisionDetector : MonoBehaviour
{
    private static bool collisionHandled;
    [SerializeField] GameObject mergedInput;

    void Start()
    {
        collisionHandled = false;
    }

    private void OnCollisionEnter(Collision collisionInfo)
    {
        if (!collisionHandled)
        {
            Vector3 midpoint = (transform.position * transform.localScale.x 
                + collisionInfo.transform.position * collisionInfo.transform.localScale.x) 
                / (transform.localScale.x + collisionInfo.transform.localScale.x);

            midpoint = new(midpoint.x, (float) System.Math.Round(midpoint.y, 1, MidpointRounding.AwayFromZero), midpoint.z);

            mergedInput.transform.localScale = new Vector3(
                transform.localScale.x + collisionInfo.transform.localScale.x, 
                mergedInput.transform.localScale.y, mergedInput.transform.localScale.z);

            GameObject newObject = Instantiate(mergedInput, midpoint, Quaternion.identity);

            newObject.name = gameObject.name;
            Destroy(gameObject);
            Destroy(collisionInfo.gameObject);

            Transform parent = GameObject.Find("Parent").transform;
            newObject.transform.SetParent(parent);
            collisionHandled = true;
        }
    }

}
