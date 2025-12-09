using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class HandTracking : MonoBehaviour
{
    public UDPReceive udpReceive;

    [Header("Game Interaction (NEW)")]
    public Transform objectToControl; // Drag the object you want to move here
    public float riseSpeed = 5.0f;    // How fast the object moves up
    public float curlThreshold = 0.05f; // <--- Adjust this if it's too sensitive!

    [Header("Hand Sphere Settings")]
    public GameObject[] handPoints = new GameObject[21];
    public float sphereScale = 0.05f;

    [Header("Position Mapping")]
    public Vector3 startOffset = new Vector3(0, 0, 0);
    
    [Tooltip("Adjust these to stretch movement on specific axes")]
    public float scaleX = 0.01f;
    public float scaleY = 0.01f;
    public float scaleZ = 0.01f;

    [Header("Speed & Movement")]
    [Tooltip("Multiplies all movement.")]
    public float globalMovementSpeed = 1.0f; 

    [Tooltip("Smoothing speed.")]
    public float followSpeed = 30f; 

    [Header("Axis Remapping")]
    public bool flipZ = true;

    // We need this array to do math between fingers
    private Vector3[] currentLandmarks = new Vector3[21];

    void Update()
    {
        string data = udpReceive.data;
        if (string.IsNullOrEmpty(data)) return;

        data = data.Replace("[", "").Replace("]", "");
        string[] points = data.Split(',');

        if (points.Length < 63) return;

        for (int i = 0; i < 21; i++)
        {
            float rawX = float.Parse(points[i * 3], CultureInfo.InvariantCulture);
            float rawY = float.Parse(points[i * 3 + 1], CultureInfo.InvariantCulture);
            float rawZ = float.Parse(points[i * 3 + 2], CultureInfo.InvariantCulture);

            // --- Your Original Calculation ---
            float x = startOffset.x + (7 - rawX) * scaleX * globalMovementSpeed;
            float y = startOffset.y + rawZ * scaleY * globalMovementSpeed;
            float z = startOffset.z + (flipZ ? -rawY : rawY) * scaleZ * globalMovementSpeed;

            // Define the target position
            Vector3 targetPos = new Vector3(x, y, z);

            // --- Store position for Logic ---
            currentLandmarks[i] = targetPos;

            if (handPoints[i] != null)
            {
                // Apply Smoothing
                handPoints[i].transform.localPosition = Vector3.Lerp(handPoints[i].transform.localPosition, targetPos, Time.deltaTime * followSpeed);

                // Update Scale
                handPoints[i].transform.localScale = Vector3.one * sphereScale;
            }
        }

        // --- NEW: Run the Gesture Check ---
        CheckForCurl();
    }

    void CheckForCurl()
    {
        if (objectToControl == null) return;

        // Get Index Tip (8) and Index Base/Knuckle (5)
        Vector3 tip = currentLandmarks[8];
        Vector3 knuckle = currentLandmarks[5];

        // Check distance
        float distance = Vector3.Distance(tip, knuckle);

        // Debug.Log(distance); // Uncomment this line if you need to see the number to tune threshold

        if (distance < curlThreshold)
        {
            // Finger is Curled -> Move Object UP (Y axis)
            objectToControl.Translate(Vector3.up * riseSpeed * Time.deltaTime);
        }
    }
}