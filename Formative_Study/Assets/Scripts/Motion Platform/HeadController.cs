using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeadController : MonoBehaviour
{
    // Using the CenterEyeAnchor or OptiTrack Rigidbody as the head anchor
    public Transform headAnchor;
    Quaternion startRotation;
    List<Quaternion> rotations = new List<Quaternion>();
    public int averagingFrames = 200;
    // Actuate the yawing angle according to the head rotation
    public DOFCommunication dOFCommunication;
    float error;
    float targetAngle;

    // Start is called before the first frame update
    void Start()
    {
        //Find the DOFCommunication script in the scene
        dOFCommunication = FindObjectOfType<DOFCommunication>();
        if (headAnchor == null || dOFCommunication == null)
        {
            Debug.LogError("Head anchor or DOFCommunication is not set");
            // Quit
            Application.Quit();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (averagingFrames > 0)
        {
            rotations.Add(headAnchor.rotation);
            averagingFrames -= 1;
        }
        else if (averagingFrames == 0)
        {
            startRotation = AverageRotation(rotations, rotations.Count);
            averagingFrames -= 1;
        }
        else
        {
            // The error is the angle between the current viewing direction and the neutral viewing direction, relative in the yaw axis
            Quaternion currentRotation = headAnchor.rotation;
            Quaternion diff = currentRotation * Quaternion.Inverse(startRotation);
            // Extract the Roll, Yaw, Pitch values from the difference
            // Yaw-Y。Pitch-X。Roll-Z
            float pitch = diff.eulerAngles.x, yaw = diff.eulerAngles.y, roll = diff.eulerAngles.z;
            Debug.Log("Pitch: " + pitch + "Yaw: " + yaw + "Roll: " + roll);
            error = yaw < 180 ? -yaw : 360 - yaw;
            // The yawing range is (0,0) and (1,1), the neutral position is (0.5,0.5)
            // The error is normalized to the range (-0.5,0.5), then shifted to the range (0,1)
            // targetAngle is mapped from the error of (-90,90) to (0,1)
            Debug.Log(error); // -180 ~ +180
            targetAngle = (error / 90 + 0.5f);
            // Clamp the angle between 0 an 1
            targetAngle = Mathf.Clamp(targetAngle, 0, 1);
            // Now the problem is that we don't want the angle to change too fast

            // Set the yaw angle of the motors
            dOFCommunication.SetMotorPos(targetAngle, targetAngle);
            // visualize the yaw axis (green) and neutral direction (blue) during the game
            Debug.DrawRay(Vector3.zero, startRotation*Vector3.up * 500, Color.green);
            Debug.DrawRay(Vector3.zero, startRotation*Vector3.forward * 500, Color.blue);
        }
    }
    public Quaternion AverageRotation(List<Quaternion> multipleRotations, int totalAmount)
    {
        int addAmount = 0;

        //Global variable which represents the additive quaternion
        Quaternion addedRotation = Quaternion.identity;

        //The averaged rotational value
        Quaternion averageRotation = Quaternion.identity;

        //Loop through all the rotational values.
        foreach (Quaternion singleRotation in multipleRotations)
        {
            //Temporary values
            float w;
            float x;
            float y;
            float z;

            //Amount of separate rotational values so far
            addAmount++;

            float addDet = 1.0f / (float)addAmount;
            addedRotation.w += singleRotation.w;
            w = addedRotation.w * addDet;
            addedRotation.x += singleRotation.x;
            x = addedRotation.x * addDet;
            addedRotation.y += singleRotation.y;
            y = addedRotation.y * addDet;
            addedRotation.z += singleRotation.z;
            z = addedRotation.z * addDet;

            //Normalize. Note: experiment to see whether you
            //can skip this step.
            float D = 1.0f / (w * w + x * x + y * y + z * z);
            w *= D;
            x *= D;
            y *= D;
            z *= D;

            //The result is valid right away, without
            //first going through the entire array.
            averageRotation = new Quaternion(x, y, z, w);
        }
        return averageRotation;
    }
}
