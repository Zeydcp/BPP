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
        Transform object1 = transform.parent;
        Transform object2 = collisionInfo.transform.parent;
        if (transform.CompareTag("Input") && collisionInfo.transform.CompareTag("Input"))
        {
            if (!collisionHandled)
            {
                Vector3 midpoint = (object1.position * object1.localScale.x 
                    + object2.position * object2.localScale.x) 
                    / (object1.localScale.x + object2.localScale.x);

                midpoint = new(midpoint.x, (float) System.Math.Round(midpoint.y, 1, MidpointRounding.AwayFromZero), midpoint.z);

                mergedInput.transform.localScale = new Vector3(
                    object1.localScale.x + object2.localScale.x, 
                    mergedInput.transform.localScale.y, mergedInput.transform.localScale.z);


                GameObject newObject = Instantiate(mergedInput, midpoint, Quaternion.identity);

                newObject.name = gameObject.name + " dad";
                Destroy(object1.gameObject);
                Destroy(object2.gameObject);

                Transform parent = GameObject.Find("Parent").transform;
                newObject.transform.SetParent(parent);
                collisionHandled = true;
            }
        }
    }

}
