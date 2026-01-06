using UnityEngine;
using UnityEngine.UI;

public class DisplayManager : MonoBehaviour
{
    public static DisplayManager I;

    [Header("UI References")]
    public Slider sliderVolume;
    public Slider sliderReverb;
    public Slider sliderDistortion;
    
    [Header("Container")]
    public CanvasGroup uiCanvasGroup; // Assign the Canvas or a Panel holding the sliders

    // Internal "Virtual" values to display on screen
    private float dispVol = 0.5f;
    private float dispRev = 0f;
    private float dispDist = 0f;

    void Awake()
    {
        I = this;
        if(uiCanvasGroup) uiCanvasGroup.alpha = 0f; // Hide on start
    }

    public void SetActive(bool active)
    {
        if(uiCanvasGroup) uiCanvasGroup.alpha = active ? 1f : 0f;
    }

    public void UpdateDisplay(float delta, int mode)
    {
        // Mode: 0=Volume, 1=Reverb, 2=Distortion
        
        if (mode == 0) // Volume
        {
            dispVol = Mathf.Clamp01(dispVol + delta);
            if(sliderVolume) sliderVolume.value = dispVol;
        }
        else if (mode == 1) // Reverb
        {
            dispRev = Mathf.Clamp01(dispRev + delta);
            if(sliderReverb) sliderReverb.value = dispRev;
        }
        else if (mode == 2) // Distortion
        {
            dispDist = Mathf.Clamp01(dispDist + delta);
            if(sliderDistortion) sliderDistortion.value = dispDist;
        }
    }
}