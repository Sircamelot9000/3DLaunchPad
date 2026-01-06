using UnityEngine;
using System.Collections.Generic;

public class AudioHub : MonoBehaviour
{
    public static AudioHub I { get; private set; }

    readonly Dictionary<string, AudioSource> loopers = new();
    readonly List<AudioSource> pool = new();
    const int POOL = 8;

    // List of Active Sounds that allow Mixing
    private List<AudioSource> affectableSources = new List<AudioSource>();

    void Awake(){
        I = this;
        for (int i = 0; i < POOL; i++){
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            s.spatialBlend = 0f;
            pool.Add(s);
        }
    }

    void Update()
    {
        // Cleanup stopped sounds
        for (int i = affectableSources.Count - 1; i >= 0; i--)
        {
            if (affectableSources[i] == null || !affectableSources[i].isPlaying)
            {
                affectableSources.RemoveAt(i);
            }
        }
    }

    // --- MIXING FUNCTIONS ---
    public void AdjustActiveVolume(float delta)
    {
        foreach(var s in affectableSources)
        {
            if(s) s.volume = Mathf.Clamp01(s.volume + delta);
        }
    }

    public void AdjustActiveReverb(float delta)
    {
        foreach(var s in affectableSources)
        {
            if(!s) continue;
            var rev = s.GetComponent<AudioReverbFilter>();
            if(!rev) { 
                rev = s.gameObject.AddComponent<AudioReverbFilter>(); 
                rev.reverbPreset = AudioReverbPreset.Hallway; 
                rev.dryLevel = 0f; 
                rev.room = -10000f; // Silent Reverb
            }
            float newRoom = rev.room + (delta * 2000f);
            rev.room = Mathf.Clamp(newRoom, -10000f, 0f);
        }
    }

    public void AdjustActiveDistortion(float delta)
    {
        foreach(var s in affectableSources)
        {
            if(!s) continue;
            var dist = s.GetComponent<AudioDistortionFilter>();
            if(!dist) { dist = s.gameObject.AddComponent<AudioDistortionFilter>(); dist.distortionLevel = 0f; }
            
            dist.distortionLevel = Mathf.Clamp01(dist.distortionLevel + delta);
        }
    }
    // -----------------------

    public void SetGlobalPause(bool pause)
    {
        foreach(var kvp in loopers) { if (pause) kvp.Value.Pause(); else kvp.Value.UnPause(); }
        foreach(var s in pool) { if (pause) s.Pause(); else s.UnPause(); }
    }

    public void OneShot(AudioClip clip, float gain = 1f, bool affectable = false){
        if (!clip) return;
        if (PauseManager.I && PauseManager.I.isPaused) return;

        AudioSource chosen = null;
        foreach (var s in pool){ if (!s.isPlaying){ chosen = s; break; } }
        if(chosen == null) chosen = pool[0];

        ResetEffects(chosen);
        chosen.volume = gain;
        chosen.PlayOneShot(clip);

        if(affectable) affectableSources.Add(chosen);
    }

    public void ToggleLoop(AudioClip clip, float gain = 1f, bool affectable = false){
        if (!clip) return;
        if (PauseManager.I && PauseManager.I.isPaused) return;

        string id = clip.name; 
        if (loopers.TryGetValue(id, out var src) && src.isPlaying){ 
            src.Stop(); 
            if(affectableSources.Contains(src)) affectableSources.Remove(src);
            return; 
        }

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

        if (affectable && !affectableSources.Contains(src)) affectableSources.Add(src);
    }

    void ResetEffects(AudioSource s)
    {
        var r = s.GetComponent<AudioReverbFilter>();
        if(r) Destroy(r);
        var d = s.GetComponent<AudioDistortionFilter>();
        if(d) Destroy(d);
    }
}