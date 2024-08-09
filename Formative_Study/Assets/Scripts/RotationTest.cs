using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RotationTest : MonoBehaviour
{
    // The object that will be rotated, and we are going to monitor its rotation using the Roll, Yaw, Pitch expressions
    public OrientationUtility orientationUtility;
    void Start()
    {
        // Set the object that will be rotated
        orientationUtility = FindObjectOfType<OrientationUtility>();
        if(orientationUtility == null)
        {
            Debug.LogError("OrientationUtility not found in the scene");
            Application.Quit();
        }
    }

    // Update is called once per frame
    void Update()
    {
        Debug.Log("Roll: " + orientationUtility.RollAngle + " Yaw: " + orientationUtility.YawAngle + " Pitch: " + orientationUtility.PitchAngle);
    }
}
