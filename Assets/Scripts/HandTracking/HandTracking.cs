using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class HandTracking : MonoBehaviour
{
    public UDPReceive udpReceive;
    public GameObject[] handPoints = new GameObject[21];

    [Header("1. Spawn Position (Move Hand Here)")]
    // CHANGE THESE NUMBERS to move the hand to your launchpad
    public Vector3 spawnOffset = new Vector3(0, 0, 0);

    [Header("2. Orientation")]
    public bool flipY = false;    // Check this if hand is upside down

    [Header("3. Calibration")]
    public float handSizeTrigger = 100f;   // If hand is bigger than this, it moves forward
    public float depthSensitivity = 15f;   // How fast it moves forward/back
    public float smoothing = 15f;          // Makes it less jittery

    // Hard-coded offsets (Internal math)
    private Vector3 mapScale = new Vector3(-0.02f, -0.02f, -0.05f); 
    private Vector3 mapOffset = new Vector3(0, 5, 0); 

    void Update()
    {
        string data = udpReceive.data;
        if (string.IsNullOrEmpty(data)) return;

        // Clean Data
        data = data.Replace("[", "").Replace("]", "");
        string[] points = data.Split(',');

        if (points.Length < 63) return;

        // --- 1. CALCULATE DISTANCE ---
        float wristX = float.Parse(points[0], CultureInfo.InvariantCulture);
        float wristY = float.Parse(points[1], CultureInfo.InvariantCulture);
        float midX = float.Parse(points[27], CultureInfo.InvariantCulture);
        float midY = float.Parse(points[28], CultureInfo.InvariantCulture);

        float currentHandSize = Vector2.Distance(new Vector2(wristX, wristY), new Vector2(midX, midY));
        float zMove = (currentHandSize - handSizeTrigger) * (depthSensitivity * 0.001f);

        // --- 2. MOVE THE POINTS ---
        for (int i = 0; i < 21; i++)
        {
            float rawX = float.Parse(points[i * 3], CultureInfo.InvariantCulture);
            float rawY = float.Parse(points[i * 3 + 1], CultureInfo.InvariantCulture);
            float rawZ = float.Parse(points[i * 3 + 2], CultureInfo.InvariantCulture);

            // --- FLIP LOGIC ---
            // If the box is checked, we invert the incoming Y pixel data
            if (flipY) rawY = -rawY; 

            // --- MAPPING ---
            float x = (rawX * mapScale.x) + mapOffset.x;
            
            // Note: mapScale.y is negative in your code (-0.02f). 
            // If flipY is true, we inverted rawY above, so the result flips.
            float y = (rawY * mapScale.y) + mapOffset.y;

            float z = zMove + (rawZ * mapScale.z);

            // Add the Spawn Offset here so you can move the whole hand easily
            Vector3 targetPos = new Vector3(x, y, z) + spawnOffset;

            if (handPoints[i] != null)
            {
                handPoints[i].transform.localPosition = Vector3.Lerp(handPoints[i].transform.localPosition, targetPos, Time.deltaTime * smoothing);
            }
        }
    }
}