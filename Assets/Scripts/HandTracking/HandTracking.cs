using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class HandTracking : MonoBehaviour
{
    public UDPReceive udpReceive;
    public GameObject[] handPoints = new GameObject[21];

    [Header("1. Settings")]
    public Vector3 spawnOffset = new Vector3(0, 0, 0); 
    [Range(0.1f, 2f)] public float handGlobalScale = 1.0f; 
    public bool showOnlyTips = true;    

    // --- INDIVIDUAL FINGER OFFSETS ---
    [Header("Finger Height Offsets")]
    public float offsetThumb = 0.05f;  
    public float offsetIndex = 0.0f;   
    public float offsetMiddle = 0.0f;
    public float offsetRing = 0.0f;
    public float offsetPinky = 0.02f;  

    [Header("2. Orientation")]
    public bool flipY = false;

    [Header("3. Calibration")]
    public float handSizeTrigger = 100f;
    public float depthSensitivity = 15f;
    public float smoothing = 15f;

    [Header("4. Invisible Walls")]
    public Transform wallLeft;   
    public Transform wallRight;  
    public Transform wallFront;  
    public Transform wallBase;   

    [Header("5. Vertical Press Guide")]
    [Tooltip("If true, locks horizontal movement when pressing down (Anti-Drift).")]
    public bool useVerticalGuide = true; 
    
    [Tooltip("The height where the Lock activates.")]
    public float guideStartHeight = 0.15f; 

    [Range(0.0f, 1.0f)] public float guideStrength = 0.95f; 

    // Internal math
    private Vector3 mapScale = new Vector3(-0.02f, -0.02f, -0.05f); 
    private Vector3 mapOffset = new Vector3(0, 5, 0); 

    void Start()
    {
        for (int i = 0; i < handPoints.Length; i++)
        {
            if (handPoints[i] != null)
            {
                GameObject p = handPoints[i];

                // 1. Identify if this point is a fingertip
                bool isTip = (i == 4 || i == 8 || i == 12 || i == 16 || i == 20);
                if (isTip) 
                {
                    p.tag = "Finger";
                }
                else 
                {
                    p.tag = "Untagged"; 
                }
                // -----------------------

                if (p.GetComponent<Rigidbody>() == null)
                {
                    Rigidbody rb = p.AddComponent<Rigidbody>();
                    rb.useGravity = false; 
                    rb.isKinematic = true; 
                }
                if (p.GetComponent<Collider>() == null)
                {
                    SphereCollider col = p.AddComponent<SphereCollider>();
                    col.isTrigger = true; 
                    col.radius = 0.1f; 
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

        // --- 1. CALCULATE ---
        float wristX = float.Parse(points[0], CultureInfo.InvariantCulture);
        float wristY = float.Parse(points[1], CultureInfo.InvariantCulture);
        float midX = float.Parse(points[27], CultureInfo.InvariantCulture);
        float midY = float.Parse(points[28], CultureInfo.InvariantCulture);

        float currentHandSize = Vector2.Distance(new Vector2(wristX, wristY), new Vector2(midX, midY));
        float zMove = (currentHandSize - handSizeTrigger) * (depthSensitivity * 0.001f);

        // --- 2. MOVE POINTS ---
        for (int i = 0; i < 21; i++)
        {
            if (handPoints[i] == null) continue;

            bool isTip = (i == 4 || i == 8 || i == 12 || i == 16 || i == 20);
            if (showOnlyTips && !isTip) 
            { 
                handPoints[i].SetActive(false); 
                continue; 
            }
            handPoints[i].SetActive(true);

            float rawX = float.Parse(points[i * 3], CultureInfo.InvariantCulture);
            float rawY = float.Parse(points[i * 3 + 1], CultureInfo.InvariantCulture);
            float rawZ = float.Parse(points[i * 3 + 2], CultureInfo.InvariantCulture);

            if (flipY) rawY = -rawY; 

            float x = (rawX * mapScale.x * handGlobalScale) + mapOffset.x;
            float y = (rawY * mapScale.y * handGlobalScale) + mapOffset.y;
            float z = zMove + (rawZ * mapScale.z * handGlobalScale);

            // --- APPLY INDIVIDUAL OFFSETS ---
            if (i == 4)  y += offsetThumb;
            if (i == 8)  y += offsetIndex;
            if (i == 12) y += offsetMiddle;
            if (i == 16) y += offsetRing;
            if (i == 20) y += offsetPinky;

            Vector3 targetPos = new Vector3(x, y, z) + spawnOffset;
            
            // --- WALLS ---
            if (wallLeft != null)
            {
                float limit = transform.InverseTransformPoint(wallLeft.position).x;
                if (targetPos.x < limit) targetPos.x = limit;
            }

            if (wallRight != null)
            {
                float limit = transform.InverseTransformPoint(wallRight.position).x;
                if (targetPos.x > limit) targetPos.x = limit;
            }

            if (wallFront != null)
            {
                float limit = transform.InverseTransformPoint(wallFront.position).z;
                if (targetPos.z > limit) targetPos.z = limit;
            }

            if (wallBase != null)
            {
                float limit = transform.InverseTransformPoint(wallBase.position).y;
                if (targetPos.y < limit) targetPos.y = limit;

                // --- VERTICAL GUIDE (ANTI-DRIFT ONLY) ---
                if (useVerticalGuide && isTip)
                {
                    float distToFloor = Mathf.Abs(targetPos.y - limit);

                    if (distToFloor < guideStartHeight)
                    {
                        Vector3 currentPos = handPoints[i].transform.localPosition;
                        targetPos.x = Mathf.Lerp(targetPos.x, currentPos.x, guideStrength);
                        targetPos.z = Mathf.Lerp(targetPos.z, currentPos.z, guideStrength);
                    }
                }
            }

            handPoints[i].transform.localPosition = Vector3.Lerp(handPoints[i].transform.localPosition, targetPos, Time.deltaTime * smoothing);
        }
    }
}