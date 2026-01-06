using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Globalization;

public class HandTracking : MonoBehaviour
{
    public UDPReceive udpReceive;                 // Receive hand landmark data via UDP
    public GameObject[] handPoints = new GameObject[21]; // 21 MediaPipe landmarks

    [Header("1. Settings")]
    public Vector3 spawnOffset = new Vector3(0, 0, 0);   // Global position offset
    [Range(0.1f, 2f)] public float handGlobalScale = 1.0f; // Scale entire hand
    public bool showOnlyTips = true;              // Show only fingertip points

    [Header("Finger Height Offsets")]
    public float offsetThumb = 0.05f;             // Adjust thumb height
    public float offsetIndex = 0.0f;
    public float offsetMiddle = 0.0f;
    public float offsetRing = 0.0f;
    public float offsetPinky = 0.02f;

    [Header("2. Orientation")]
    public bool flipY = false;                    // Flip Y axis if camera is inverted

    [Header("3. Calibration")]
    public float handSizeTrigger = 100f;          // Reference hand size (2D)
    public float depthSensitivity = 15f;          // Z movement sensitivity
    public float smoothing = 15f;                 // Position smoothing factor

    [Header("4. Invisible Walls")]
    public Transform wallLeft;                    // X min limit
    public Transform wallRight;                   // X max limit
    public Transform wallFront;                   // Z max limit
    public Transform wallBase;                    // Y min limit (press surface)

    [Header("5. Vertical Press Guide")]
    public bool useVerticalGuide = true;          // Lock XZ when pressing down
    public float guideStartHeight = 0.15f;        // Activation height above base
    [Range(0.0f, 1.0f)] public float guideStrength = 0.95f; // Lock strength

    // Convert MediaPipe space to Unity space
    private Vector3 mapScale = new Vector3(-0.02f, -0.02f, -0.05f);
    private Vector3 mapOffset = new Vector3(0, 5, 0);

    void Start()
    {
        // Initialize hand point physics & tags
        for (int i = 0; i < handPoints.Length; i++)
        {
            if (handPoints[i] == null) continue;

            GameObject p = handPoints[i];
            bool isTip = (i == 4 || i == 8 || i == 12 || i == 16 || i == 20);

            p.tag = isTip ? "Finger" : "Untagged";

            if (p.GetComponent<Rigidbody>() == null)
                p.AddComponent<Rigidbody>().isKinematic = true;

            if (p.GetComponent<Collider>() == null)
                p.AddComponent<SphereCollider>().isTrigger = true;
        }
    }

    void Update()
    {
        string data = udpReceive.data;
        if (string.IsNullOrEmpty(data)) return;

        // Clean and split incoming data
        data = data.Replace("[", "").Replace("]", "");
        string[] points = data.Split(',');

        if (points.Length < 63) return; // Need at least 21 * 3 values

        // --- Optional pause signal (extra value after landmarks) ---
        if (points.Length > 63)
        {
            float gestureSignal = float.Parse(points[points.Length - 1], CultureInfo.InvariantCulture);
            PauseManager.I.SetPaused(gestureSignal == 1);
        }
        // -----------------------------------------------------------

        // Wrist and middle MCP for hand size estimation
        float wristX = float.Parse(points[0], CultureInfo.InvariantCulture);
        float wristY = float.Parse(points[1], CultureInfo.InvariantCulture);
        float midX   = float.Parse(points[27], CultureInfo.InvariantCulture);
        float midY   = float.Parse(points[28], CultureInfo.InvariantCulture);

        // Approximate hand depth from 2D size
        float currentHandSize = Vector2.Distance(new Vector2(wristX, wristY), new Vector2(midX, midY));
        float zMove = (currentHandSize - handSizeTrigger) * (depthSensitivity * 0.001f);

        for (int i = 0; i < 21; i++)
        {
            if (handPoints[i] == null) continue;

            bool isTip = (i == 4 || i == 8 || i == 12 || i == 16 || i == 20);

            // Hide non-tip points if needed
            if (showOnlyTips && !isTip)
            {
                handPoints[i].SetActive(false);
                continue;
            }
            handPoints[i].SetActive(true);

            // Read MediaPipe landmark
            float rawX = float.Parse(points[i * 3],     CultureInfo.InvariantCulture);
            float rawY = float.Parse(points[i * 3 + 1], CultureInfo.InvariantCulture);
            float rawZ = float.Parse(points[i * 3 + 2], CultureInfo.InvariantCulture);

            if (flipY) rawY = -rawY;

            // Map MediaPipe coordinates to Unity
            float x = (rawX * mapScale.x * handGlobalScale) + mapOffset.x;
            float y = (rawY * mapScale.y * handGlobalScale) + mapOffset.y;
            float z = zMove + (rawZ * mapScale.z * handGlobalScale);

            // Per-finger height adjustment
            if (i == 4)  y += offsetThumb;
            if (i == 8)  y += offsetIndex;
            if (i == 12) y += offsetMiddle;
            if (i == 16) y += offsetRing;
            if (i == 20) y += offsetPinky;

            Vector3 targetPos = new Vector3(x, y, z) + spawnOffset;

            // Clamp position inside invisible walls
            if (wallLeft != null)
            {
                float l = transform.InverseTransformPoint(wallLeft.position).x;
                if (targetPos.x < l) targetPos.x = l;
            }
            if (wallRight != null)
            {
                float l = transform.InverseTransformPoint(wallRight.position).x;
                if (targetPos.x > l) targetPos.x = l;
            }
            if (wallFront != null)
            {
                float l = transform.InverseTransformPoint(wallFront.position).z;
                if (targetPos.z > l) targetPos.z = l;
            }

            // Base plane + vertical press guide
            if (wallBase != null)
            {
                float limit = transform.InverseTransformPoint(wallBase.position).y;

                if (targetPos.y < limit)
                    targetPos.y = limit;

                // Lock XZ when fingertip is close to press surface
                if (useVerticalGuide && isTip &&
                    Mathf.Abs(targetPos.y - limit) < guideStartHeight)
                {
                    Vector3 current = handPoints[i].transform.localPosition;
                    targetPos.x = Mathf.Lerp(targetPos.x, current.x, guideStrength);
                    targetPos.z = Mathf.Lerp(targetPos.z, current.z, guideStrength);
                }
            }

            // Smooth movement
            handPoints[i].transform.localPosition =
                Vector3.Lerp(handPoints[i].transform.localPosition, targetPos, Time.deltaTime * smoothing);
        }
    }
}
