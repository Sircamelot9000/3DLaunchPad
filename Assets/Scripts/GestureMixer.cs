using UnityEngine;

public class GestureMixer : MonoBehaviour
{
    [Header("Setup")]
    public HandTracking handTracking; 
    
    [Header("Visual Feedback")]
    public Material matNormal; // Red
    public Material matActive; // Green

    [Header("Settings")]
    public float sensitivity = 3.0f;
    public float pinchDistance = 0.08f; 

    // CHANGED: We now track X (Horizontal) instead of Y
    private float lastX = 0f; 
    private bool isMixing = false;
    private Renderer thumbRend;

    void Update()
    {
        // 1. Safety Checks
        if (handTracking == null || PauseManager.I == null || PauseManager.I.isPaused) return;
        if (handTracking.handPoints.Length < 17) return;

        // 2. Get Fingers
        GameObject thumbObj = handTracking.handPoints[4];
        GameObject indexObj = handTracking.handPoints[8];
        GameObject middleObj = handTracking.handPoints[12];
        GameObject ringObj = handTracking.handPoints[16];

        if (thumbObj == null || !thumbObj.activeInHierarchy) {
            isMixing = false;
            if(DisplayManager.I) DisplayManager.I.SetActive(false);
            return;
        }

        if(thumbRend == null) thumbRend = thumbObj.GetComponent<Renderer>();

        // 3. Measure Pinch Distances
        Vector3 thumbPos = thumbObj.transform.position;
        float distIndex = Vector3.Distance(thumbPos, indexObj.transform.position);
        float distMiddle = Vector3.Distance(thumbPos, middleObj.transform.position);
        float distRing = Vector3.Distance(thumbPos, ringObj.transform.position);

        // 4. Calculate Movement (LEFT / RIGHT)
        // ---------------------------------------------------------
        float currentX = thumbPos.x; // Use X axis now
        
        if (!isMixing) lastX = currentX;
        
        // Calculate difference. 
        // Move Right (+X) = Positive Delta (Volume Up)
        // Move Left (-X) = Negative Delta (Volume Down)
        float delta = (currentX - lastX) * sensitivity;
        // ---------------------------------------------------------

        // 5. Logic
        bool isPinchingAny = false;
        int activeMode = -1;

        // Ring (Distortion)
        if (distRing < pinchDistance)
        {
            isMixing = true; isPinchingAny = true; activeMode = 2;
            AudioHub.I.AdjustActiveDistortion(delta);
        }
        // Middle (Reverb)
        else if (distMiddle < pinchDistance)
        {
            isMixing = true; isPinchingAny = true; activeMode = 1;
            AudioHub.I.AdjustActiveReverb(delta);
        }
        // Index (Volume)
        else if (distIndex < pinchDistance)
        {
            isMixing = true; isPinchingAny = true; activeMode = 0;
            AudioHub.I.AdjustActiveVolume(delta);
        }
        else
        {
            isMixing = false;
            isPinchingAny = false;
        }

        // 6. Visuals & UI
        if (thumbRend != null && matNormal != null && matActive != null)
        {
            if (isPinchingAny && thumbRend.sharedMaterial != matActive) thumbRend.material = matActive;
            else if (!isPinchingAny && thumbRend.sharedMaterial != matNormal) thumbRend.material = matNormal;
        }

        if (DisplayManager.I)
        {
            if (isPinchingAny)
            {
                DisplayManager.I.SetActive(true);
                DisplayManager.I.UpdateDisplay(delta, activeMode);
            }
            // Optional: Hide UI immediately when letting go
            // else DisplayManager.I.SetActive(false); 
        }

        lastX = currentX; // Remember position for next frame
    }
}