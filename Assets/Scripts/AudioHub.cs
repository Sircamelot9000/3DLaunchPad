using UnityEngine;
using System.Collections.Generic;

public class AudioHub : MonoBehaviour
{
    // Singleton access
    public static AudioHub I { get; private set; }

    // Looping sounds, keyed by clip name
    readonly Dictionary<string, AudioSource> loopers = new();

    // Pool for one-shot sounds
    readonly List<AudioSource> pool = new();
    const int POOL = 8; // Max simultaneous one-shot sounds

    // AudioSources that can be modified by hand gestures (volume, FX, etc.)
    private List<AudioSource> affectableSources = new List<AudioSource>();

    void Awake(){
        // Initialize singleton
        I = this;

        // Create AudioSource pool
        for (int i = 0; i < POOL; i++){
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            s.spatialBlend = 0f; // 2D sound
            pool.Add(s);
        }
    }

    void Update()
    {
        // Remove finished or destroyed sources from affectable list
        for (int i = affectableSources.Count - 1; i >= 0; i--)
        {
            if (affectableSources[i] == null || !affectableSources[i].isPlaying)
            {
                affectableSources.RemoveAt(i);
            }
        }
    }

    // ================= MIXING FUNCTIONS =================

    // Adjust volume of all gesture-affected sounds
    public void AdjustActiveVolume(float delta)
    {
        foreach(var s in affectableSources)
        {
            if(s) s.volume = Mathf.Clamp01(s.volume + delta);
        }
    }

    // Adjust reverb amount on active sounds
    public void AdjustActiveReverb(float delta)
    {
        foreach(var s in affectableSources)
        {
            if(!s) continue;

            // Add reverb filter if missing
            var rev = s.GetComponent<AudioReverbFilter>();
            if(!rev) { 
                rev = s.gameObject.AddComponent<AudioReverbFilter>(); 
                rev.reverbPreset = AudioReverbPreset.Hallway; 
                rev.dryLevel = 0f; 
                rev.room = -10000f; // Start silent
            }

            // Increase / decrease reverb intensity
            float newRoom = rev.room + (delta * 2000f);
            rev.room = Mathf.Clamp(newRoom, -10000f, 0f);
        }
    }

    // Adjust distortion effect on active sounds
    public void AdjustActiveDistortion(float delta)
    {
        foreach(var s in affectableSources)
        {
            if(!s) continue;

            // Add distortion filter if missing
            var dist = s.GetComponent<AudioDistortionFilter>();
            if(!dist) { 
                dist = s.gameObject.AddComponent<AudioDistortionFilter>(); 
                dist.distortionLevel = 0f; 
            }

            dist.distortionLevel = Mathf.Clamp01(dist.distortionLevel + delta);
        }
    }
    // ====================================================

    // Pause / resume all sounds globally
    public void SetGlobalPause(bool pause)
    {
        foreach(var kvp in loopers)
            if (pause) kvp.Value.Pause(); else kvp.Value.UnPause();

        foreach(var s in pool)
            if (pause) s.Pause(); else s.UnPause();
    }

    // Play a one-shot sound (non-looping)
    public void OneShot(AudioClip clip, float gain = 1f, bool affectable = false){
        if (!clip) return;
        if (PauseManager.I && PauseManager.I.isPaused) return;

        // Find a free AudioSource in the pool
        AudioSource chosen = null;
        foreach (var s in pool){
            if (!s.isPlaying){ chosen = s; break; }
        }
        if(chosen == null) chosen = pool[0]; // Fallback

        ResetEffects(chosen);
        chosen.volume = gain;
        chosen.PlayOneShot(clip);

        // Register as gesture-affected if enabled
        if(affectable) affectableSources.Add(chosen);
    }

    // Toggle looping sound on/off
    public void ToggleLoop(AudioClip clip, float gain = 1f, bool affectable = false){
        if (!clip) return;
        if (PauseManager.I && PauseManager.I.isPaused) return;

        string id = clip.name;

        // Stop loop if already playing
        if (loopers.TryGetValue(id, out var src) && src.isPlaying){ 
            src.Stop(); 
            if(affectableSources.Contains(src))
                affectableSources.Remove(src);
            return; 
        }

        // Create AudioSource for loop if missing
        if (!loopers.TryGetValue(id, out src)){
            src = gameObject.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            loopers[id] = src;
        }

        ResetEffects(src);
        src.clip = clip;
        src.volume = gain;
        src.Play();

        // Register as gesture-affected if enabled
        if (affectable && !affectableSources.Contains(src))
            affectableSources.Add(src);
    }

    // Remove audio effects before reuse
    void ResetEffects(AudioSource s)
    {
        var r = s.GetComponent<AudioReverbFilter>();
        if(r) Destroy(r);

        var d = s.GetComponent<AudioDistortionFilter>();
        if(d) Destroy(d);
    }
}
