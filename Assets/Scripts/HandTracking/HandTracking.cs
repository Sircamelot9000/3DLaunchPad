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
    public bool useVerticalGuide = true; 
    public float guideStartHeight = 0.15f; 
    [Range(0.0f, 1.0f)] public float guideStrength = 0.95f; 

    private Vector3 mapScale = new Vector3(-0.02f, -0.02f, -0.05f); 
    private Vector3 mapOffset = new Vector3(0, 5, 0); 

    void Start()
    {
        for (int i = 0; i < handPoints.Length; i++)
        {
            if (handPoints[i] != null)
            {
                GameObject p = handPoints[i];
                bool isTip = (i == 4 || i == 8 || i == 12 || i == 16 || i == 20);
                if (isTip) p.tag = "Finger";
                else p.tag = "Untagged"; 

                if (p.GetComponent<Rigidbody>() == null) { p.AddComponent<Rigidbody>().isKinematic = true; }
                if (p.GetComponent<Collider>() == null) { p.AddComponent<SphereCollider>().isTrigger = true; }
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

        // --- READ PAUSE SIGNAL ---
        if (points.Length > 63)
        {
            float gestureSignal = float.Parse(points[points.Length - 1], CultureInfo.InvariantCulture);
            if (gestureSignal == 1) PauseManager.I.SetPaused(true);
            else if (gestureSignal == 0) PauseManager.I.SetPaused(false);
        }
        // -------------------------

        float wristX = float.Parse(points[0], CultureInfo.InvariantCulture);
        float wristY = float.Parse(points[1], CultureInfo.InvariantCulture);
        float midX = float.Parse(points[27], CultureInfo.InvariantCulture);
        float midY = float.Parse(points[28], CultureInfo.InvariantCulture);

        float currentHandSize = Vector2.Distance(new Vector2(wristX, wristY), new Vector2(midX, midY));
        float zMove = (currentHandSize - handSizeTrigger) * (depthSensitivity * 0.001f);

        for (int i = 0; i < 21; i++)
        {
            if (handPoints[i] == null) continue;

            bool isTip = (i == 4 || i == 8 || i == 12 || i == 16 || i == 20);
            if (showOnlyTips && !isTip) { handPoints[i].SetActive(false); continue; }
            handPoints[i].SetActive(true);

            float rawX = float.Parse(points[i * 3], CultureInfo.InvariantCulture);
            float rawY = float.Parse(points[i * 3 + 1], CultureInfo.InvariantCulture);
            float rawZ = float.Parse(points[i * 3 + 2], CultureInfo.InvariantCulture);

            if (flipY) rawY = -rawY; 

            float x = (rawX * mapScale.x * handGlobalScale) + mapOffset.x;
            float y = (rawY * mapScale.y * handGlobalScale) + mapOffset.y;
            float z = zMove + (rawZ * mapScale.z * handGlobalScale);

            if (i == 4)  y += offsetThumb;
            if (i == 8)  y += offsetIndex;
            if (i == 12) y += offsetMiddle;
            if (i == 16) y += offsetRing;
            if (i == 20) y += offsetPinky;

            Vector3 targetPos = new Vector3(x, y, z) + spawnOffset;
            
            // Wall Logic (Simplified for brevity, logic remains identical to yours)
            if (wallLeft != null) { float l = transform.InverseTransformPoint(wallLeft.position).x; if (targetPos.x < l) targetPos.x = l; }
            if (wallRight != null) { float l = transform.InverseTransformPoint(wallRight.position).x; if (targetPos.x > l) targetPos.x = l; }
            if (wallFront != null) { float l = transform.InverseTransformPoint(wallFront.position).z; if (targetPos.z > l) targetPos.z = l; }
            if (wallBase != null) 
            {
                float limit = transform.InverseTransformPoint(wallBase.position).y;
                if (targetPos.y < limit) targetPos.y = limit;
                if (useVerticalGuide && isTip) {
                    if (Mathf.Abs(targetPos.y - limit) < guideStartHeight) {
                        Vector3 c = handPoints[i].transform.localPosition;
                        targetPos.x = Mathf.Lerp(targetPos.x, c.x, guideStrength);
                        targetPos.z = Mathf.Lerp(targetPos.z, c.z, guideStrength);
                    }
                }
            }

            handPoints[i].transform.localPosition = Vector3.Lerp(handPoints[i].transform.localPosition, targetPos, Time.deltaTime * smoothing);
        }
    }
}