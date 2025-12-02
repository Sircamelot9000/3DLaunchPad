using UnityEngine;
using System.Collections.Generic;

public class AudioHub : MonoBehaviour
{
    public static AudioHub I { get; private set; }

    AudioSource music;
    readonly Dictionary<string, AudioClip> cache = new();
    readonly Dictionary<string, AudioSource> loopers = new();
    readonly List<AudioSource> pool = new();
    const int POOL = 8;

    void Awake(){
        I = this;

        // Main music player (expects Assets/Resources/Audio/song.wav)
        music = gameObject.AddComponent<AudioSource>();
        var song = Load("song"); 
        if(song) music.clip = song;
        music.playOnAwake = false;
        music.loop = false;
        music.spatialBlend = 0f;

        // One-shot pool
        for (int i = 0; i < POOL; i++){
            var s = gameObject.AddComponent<AudioSource>();
            s.playOnAwake = false;
            s.loop = false;
            s.spatialBlend = 0f;
            pool.Add(s);
        }
    }

    AudioClip Load(string name){
        if (cache.TryGetValue(name, out var c)) return c;
        var clip = Resources.Load<AudioClip>("Audio/" + name);
        if (clip) cache[name] = clip;
        return clip;
    }

    // Transport: togglePlay | play | pause | stop | restart
    public void Transport(string cmd){
        switch (cmd){
            case "togglePlay": if (music.isPlaying) music.Pause(); else music.Play(); break;
            case "play":       if (!music.isPlaying) music.Play(); break;
            case "pause":      if (music.isPlaying) music.Pause(); break;
            case "stop":       music.Stop(); break;
            case "restart":    music.Stop(); music.time = 0f; music.Play(); break;
        }
    }

    // [UPDATED] Direct AudioClip support (for Drag & Drop)
    public void OneShot(AudioClip clip, float gain = 1f){
        if (!clip) return;
        foreach (var s in pool){
            if (!s.isPlaying){ s.volume = gain; s.PlayOneShot(clip); return; }
        }
        pool[0].PlayOneShot(clip, gain);
    }

    // [UPDATED] Overload for string (backwards compatibility)
    public void OneShot(string clipName, float gain = 1f){
        var clip = Load(clipName); 
        if (clip) OneShot(clip, gain);
    }

    // [UPDATED] Direct AudioClip support for loops
    public void ToggleLoop(AudioClip clip, float gain = 1f){
        if (!clip) return;
        string id = clip.name; // Use clip name as ID

        if (loopers.TryGetValue(id, out var src) && src.isPlaying){ 
            src.Stop(); 
            return; 
        }

        if (!loopers.TryGetValue(id, out src)){
            src = gameObject.AddComponent<AudioSource>();
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f;
            loopers[id] = src;
        }
        src.clip = clip;
        src.volume = gain;
        src.Play();
    }
    
    // [UPDATED] Overload for string
    public void ToggleLoop(string clipName, float gain = 1f){
        var clip = Load(clipName);
        if (clip) ToggleLoop(clip, gain);
    }
}