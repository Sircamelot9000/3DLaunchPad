using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class HandTracking : MonoBehaviour
{
    public UDPReceive udpReceive;
    public GameObject[] handPoints = new GameObject[21];

    [Header("1. Settings")]
    public Vector3 spawnOffset = new Vector3(0, 0, 0); // Position Offset
    [Range(0.1f, 2f)]
    public float handGlobalScale = 1.0f; // <--- SCALE SLIDER
    public bool showOnlyTips = true;     // Checkbox to show/hide knuckles

    [Header("2. Orientation")]
    public bool flipY = false;

    [Header("3. Calibration")]
    public float handSizeTrigger = 100f;
    public float depthSensitivity = 15f;
    public float smoothing = 15f;

    // --- NEW: WALL LIMITS ---
    [Header("4. Invisible Walls (Drag Cubes Here)")]
    public Transform wallLeft;   // Limit Left Movement (Min X)
    public Transform wallRight;  // Limit Right Movement (Max X)
    public Transform wallFront;  // Limit Forward Movement (Max Z)

    // Base values (Internal math)
    private Vector3 mapScale = new Vector3(-0.02f, -0.02f, -0.05f); 
    private Vector3 mapOffset = new Vector3(0, 5, 0); 

    // --- PHYSICS SETUP ---
    void Start()
    {
        // This loop gives your spheres "physical bodies" so they can touch buttons
        for (int i = 0; i < handPoints.Length; i++)
        {
            if (handPoints[i] != null)
            {
                GameObject p = handPoints[i];

                // 1. Tag it (Make sure you created the 'Finger' tag in Unity!)
                p.tag = "Finger";

                // 2. Add Rigidbody (Required for collision detection)
                if (p.GetComponent<Rigidbody>() == null)
                {
                    Rigidbody rb = p.AddComponent<Rigidbody>();
                    rb.useGravity = false; // Floating hand
                    rb.isKinematic = true; // Moved by code, not gravity
                }

                // 3. Add Collider (The "skin" that touches triggers)
                if (p.GetComponent<Collider>() == null)
                {
                    SphereCollider col = p.AddComponent<SphereCollider>();
                    col.isTrigger = true; 
                    col.radius = 0.1f; // Small touch radius
                }
            }
        }
    }

    void Update()
    {
        string data = udpReceive.data;
        if (string.IsNullOrEmpty(data)) return;

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
            if (handPoints[i] == null) continue;

            // --- VISIBILITY LOGIC ---
            bool isTip = (i == 4 || i == 8 || i == 12 || i == 16 || i == 20);

            if (showOnlyTips && !isTip)
            {
                handPoints[i].SetActive(false);
                continue;
            }
            handPoints[i].SetActive(true);

            // --- PARSE DATA ---
            float rawX = float.Parse(points[i * 3], CultureInfo.InvariantCulture);
            float rawY = float.Parse(points[i * 3 + 1], CultureInfo.InvariantCulture);
            float rawZ = float.Parse(points[i * 3 + 2], CultureInfo.InvariantCulture);

            if (flipY) rawY = -rawY; 

            // --- APPLY SCALE ---
            float x = (rawX * mapScale.x * handGlobalScale) + mapOffset.x;
            float y = (rawY * mapScale.y * handGlobalScale) + mapOffset.y;
            float z = zMove + (rawZ * mapScale.z * handGlobalScale);

            Vector3 targetPos = new Vector3(x, y, z) + spawnOffset;

            // --- NEW: WALL CLAMPING LOGIC ---
            // If you assigned a wall, we limit the position so it can't go past it.
            
            if (wallLeft != null)
            {
                // Stop if trying to go further Left than the wall
                if (targetPos.x < wallLeft.position.x) targetPos.x = wallLeft.position.x;
            }

            if (wallRight != null)
            {
                // Stop if trying to go further Right than the wall
                if (targetPos.x > wallRight.position.x) targetPos.x = wallRight.position.x;
            }

            if (wallFront != null)
            {
                // Stop if trying to go deeper (Forward) than the wall
                if (targetPos.z > wallFront.position.z) targetPos.z = wallFront.position.z;
            }
            // --------------------------------

            // Move
            handPoints[i].transform.localPosition = Vector3.Lerp(handPoints[i].transform.localPosition, targetPos, Time.deltaTime * smoothing);
        }
    }
}