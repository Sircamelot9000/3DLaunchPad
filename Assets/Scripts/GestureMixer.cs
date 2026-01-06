using UnityEngine;

public class GestureMixer : MonoBehaviour
{
    [Header("Setup")]
    public HandTracking handTracking; 
    
    [Header("Visual Feedback")]
    public Material matNormal; // Red Material
    public Material matActive; // Green Material

    [Header("Settings")]
    public float sensitivity = 3.0f;
    public float pinchDistance = 0.08f; 

    private float lastY = 0f;
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
            if(DisplayManager.I) DisplayManager.I.SetActive(false); // Hide UI if hand lost
            return;
        }

        if(thumbRend == null) thumbRend = thumbObj.GetComponent<Renderer>();

        // 3. Measure Pinch
        Vector3 thumbPos = thumbObj.transform.position;
        float distIndex = Vector3.Distance(thumbPos, indexObj.transform.position);
        float distMiddle = Vector3.Distance(thumbPos, middleObj.transform.position);
        float distRing = Vector3.Distance(thumbPos, ringObj.transform.position);

        // 4. Calculate Delta
        float currentY = thumbPos.y;
        if (!isMixing) lastY = currentY;
        float delta = (currentY - lastY) * sensitivity;

        // 5. Logic
        bool isPinchingAny = false;
        int activeMode = -1; // -1:None, 0:Vol, 1:Rev, 2:Dist

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

        // 6. UPDATE VISUALS & UI
        
        // A. Update Material Color
        if (thumbRend != null && matNormal != null && matActive != null)
        {
            if (isPinchingAny && thumbRend.sharedMaterial != matActive) thumbRend.material = matActive;
            else if (!isPinchingAny && thumbRend.sharedMaterial != matNormal) thumbRend.material = matNormal;
        }

        // B. Update Screen UI
        if (DisplayManager.I)
        {
            if (isPinchingAny)
            {
                DisplayManager.I.SetActive(true); // Show UI
                DisplayManager.I.UpdateDisplay(delta, activeMode); // Move Slider
            }
            else
            {
                // Optional: Keep UI visible for a moment or hide immediately
                // DisplayManager.I.SetActive(false); 
            }
        }

        lastY = currentY;
    }
}