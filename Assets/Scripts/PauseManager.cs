using UnityEngine;

public class PauseManager : MonoBehaviour
{
    public static PauseManager I;
    public bool isPaused = false;

    void Awake() { I = this; }

    public void SetPaused(bool state)
    {
        if (isPaused == state) return;
        isPaused = state;

        // 1. Tell AudioHub to Pause/Resume all sounds
        if (AudioHub.I) AudioHub.I.SetGlobalPause(isPaused);

        // 2. (LightDirector handles itself by checking 'isPaused' constantly)
        
        Debug.Log(isPaused ? "ACTION PAUSED (Fist)" : "ACTION RESUMED (Open)");
    }
}