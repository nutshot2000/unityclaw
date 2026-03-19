# Unity Audio System

## AudioSource Component

### Basic Setup
```csharp
AudioSource audioSource = GetComponent<AudioSource>();

// Assign clip
audioSource.clip = myAudioClip;

// Play
audioSource.Play();
audioSource.PlayDelayed(2f); // Play after 2 seconds
audioSource.PlayScheduled(AudioSettings.dspTime + 1f); // Precise timing

// Stop
audioSource.Stop();
audioSource.Pause();
audioSource.UnPause();

// Properties
audioSource.volume = 0.5f; // 0 to 1
audioSource.pitch = 1f; // 1 = normal, 2 = double speed, 0.5 = half speed
audioSource.loop = true;
audioSource.mute = false;
audioSource.playOnAwake = true;
```

### 3D Audio Settings
```csharp
// Spatial blend (0 = 2D, 1 = 3D)
audioSource.spatialBlend = 1f;

// 3D settings
audioSource.minDistance = 1f; // Full volume
audioSource.maxDistance = 500f; // Fade to silence
audioSource.rolloffMode = AudioRolloffMode.Linear;
audioSource.rolloffMode = AudioRolloffMode.Logarithmic;
audioSource.rolloffMode = AudioRolloffMode.Custom;

// Spread (0 = mono, 360 = full stereo)
audioSource.spread = 0f;

// Doppler effect
audioSource.dopplerLevel = 1f;

// Velocity
audioSource.velocityUpdateMode = AudioVelocityUpdateMode.Fixed;
audioSource.velocityUpdateMode = AudioVelocityUpdateMode.Dynamic;
```

### Playing One-Shot Sounds
```csharp
// Play without interrupting current clip
audioSource.PlayOneShot(soundEffectClip, volumeScale);
audioSource.PlayOneShot(explosionSound, 0.8f);

// Multiple clips
audioSource.PlayOneShot(clip1);
audioSource.PlayOneShot(clip2); // Plays simultaneously
```

## AudioClip

### Loading
```csharp
// From Resources
AudioClip clip = Resources.Load<AudioClip>("Audio/Explosion");

// From file (runtime)
using UnityEngine.Networking;

IEnumerator LoadAudio() {
    using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip(
        "file:///C:/path/to/audio.mp3", AudioType.MPEG)) {
        yield return www.SendWebRequest();
        
        if (www.result == UnityWebRequest.Result.Success) {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
            audioSource.clip = clip;
        }
    }
}
```

### Clip Properties
```csharp
float length = clip.length; // Duration in seconds
int samples = clip.samples;
int channels = clip.channels;
int frequency = clip.frequency;

// Preload
clip.LoadAudioData();
clip.UnloadAudioData();

// Preload async
clip.PreloadAudioData();
```

## AudioMixer

### Setup
```csharp
// Create AudioMixer asset: Assets -> Create -> Audio -> AudioMixer
// Expose parameters in Inspector for scripting
```

### Controlling Mixer
```csharp
using UnityEngine.Audio;

public AudioMixer mixer;

// Set exposed parameter
mixer.SetFloat("MasterVolume", -10f); // dB
mixer.SetFloat("MusicVolume", -20f);
mixer.SetFloat("SFXVolume", 0f);

// Get parameter
mixer.GetFloat("MasterVolume", out float value);

// Transition to snapshot
AudioMixerSnapshot[] snapshots = new AudioMixerSnapshot[2];
snapshots[0] = normalSnapshot;
snapshots[1] = pausedSnapshot;
float[] weights = { 0.5f, 0.5f };
mixer.TransitionToSnapshots(snapshots, weights, 0.5f);

// Transition single snapshot
pausedSnapshot.TransitionTo(0.5f); // 0.5 second transition
```

### Snapshots
```csharp
// Create snapshots in AudioMixer window
// Use for different game states (Normal, Paused, LowHealth, etc.)

public AudioMixerSnapshot normalSnapshot;
public AudioMixerSnapshot pausedSnapshot;

void PauseGame() {
    pausedSnapshot.TransitionTo(0.5f);
}

void ResumeGame() {
    normalSnapshot.TransitionTo(0.5f);
}
```

## Audio Effects

### Reverb Zones
```csharp
AudioReverbZone reverb = GetComponent<AudioReverbZone>();
reverb.reverbPreset = AudioReverbPreset.Cave;
reverb.reverbPreset = AudioReverbPreset.Arena;
reverb.reverbPreset = AudioReverbPreset.Underwater;

// Custom settings
reverb.reverbPreset = AudioReverbPreset.User;
reverb.reverb = 1000;
reverb.room = -1000;
reverb.roomHF = -100;
```

### Audio Filters
```csharp
// Low Pass Filter
AudioLowPassFilter lowPass = GetComponent<AudioLowPassFilter>();
lowPass.cutoffFrequency = 5000f; // Muffle sound
lowPass.cutoffFrequency = 22000f; // Full range

// High Pass Filter
AudioHighPassFilter highPass = GetComponent<AudioHighPassFilter>();
highPass.cutoffFrequency = 500f;

// Echo
AudioEchoFilter echo = GetComponent<AudioEchoFilter>();
echo.delay = 500f; // ms
echo.decayRatio = 0.5f;
echo.wetMix = 0.5f;
echo.dryMix = 1f;

// Distortion
AudioDistortionFilter distortion = GetComponent<AudioDistortionFilter>();
distortion.distortionLevel = 0.5f;

// Chorus
AudioChorusFilter chorus = GetComponent<AudioChorusFilter>();
```

## Microphone Input

```csharp
// Start recording
string device = Microphone.devices[0]; // First device
AudioClip recordedClip = Microphone.Start(device, false, 10, 44100); // 10 seconds, 44.1kHz

// Check if recording
bool isRecording = Microphone.IsRecording(device);

// Get position
int position = Microphone.GetPosition(device);

// Stop recording
Microphone.End(device);

// Play recording
audioSource.clip = recordedClip;
audioSource.Play();
```

## Common Patterns

### Sound Manager
```csharp
public class SoundManager : MonoBehaviour {
    public static SoundManager Instance;
    
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioMixer mixer;
    
    void Awake() {
        if (Instance == null) {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        } else {
            Destroy(gameObject);
        }
    }
    
    public void PlayMusic(AudioClip clip, float fadeTime = 0.5f) {
        StartCoroutine(FadeMusic(clip, fadeTime));
    }
    
    public void PlaySFX(AudioClip clip, float volume = 1f) {
        sfxSource.PlayOneShot(clip, volume);
    }
    
    public void SetMasterVolume(float volume) {
        // Convert 0-1 to dB (-80 to 0)
        float dB = volume > 0 ? Mathf.Log10(volume) * 20 : -80f;
        mixer.SetFloat("MasterVolume", dB);
    }
    
    IEnumerator FadeMusic(AudioClip newClip, float fadeTime) {
        float startVolume = musicSource.volume;
        
        // Fade out
        while (musicSource.volume > 0.01f) {
            musicSource.volume -= startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }
        
        musicSource.Stop();
        musicSource.clip = newClip;
        musicSource.Play();
        
        // Fade in
        while (musicSource.volume < startVolume) {
            musicSource.volume += startVolume * Time.deltaTime / fadeTime;
            yield return null;
        }
    }
}
```

### Footstep Sounds
```csharp
public class Footsteps : MonoBehaviour {
    [SerializeField] private AudioClip[] footstepSounds;
    [SerializeField] private float stepInterval = 0.5f;
    
    private AudioSource audioSource;
    private float stepTimer;
    
    void Start() {
        audioSource = GetComponent<AudioSource>();
    }
    
    void Update() {
        if (IsMoving()) {
            stepTimer += Time.deltaTime;
            
            if (stepTimer >= stepInterval) {
                PlayFootstep();
                stepTimer = 0;
            }
        }
    }
    
    void PlayFootstep() {
        if (footstepSounds.Length == 0) return;
        
        int index = Random.Range(0, footstepSounds.Length);
        audioSource.PlayOneShot(footstepSounds[index], 0.5f);
    }
}
```

### Dynamic Music System
```csharp
public class DynamicMusic : MonoBehaviour {
    [SerializeField] private AudioClip calmMusic;
    [SerializeField] private AudioClip combatMusic;
    
    private AudioSource source;
    private AudioClip targetClip;
    
    void Start() {
        source = GetComponent<AudioSource>();
        targetClip = calmMusic;
        source.clip = calmMusic;
        source.Play();
    }
    
    void Update() {
        // Check if in combat
        bool inCombat = CheckIfInCombat();
        AudioClip desiredClip = inCombat ? combatMusic : calmMusic;
        
        if (desiredClip != targetClip) {
            targetClip = desiredClip;
            StartCoroutine(TransitionMusic(targetClip));
        }
    }
    
    IEnumerator TransitionMusic(AudioClip newClip) {
        // Fade out
        while (source.volume > 0.01f) {
            source.volume -= Time.deltaTime;
            yield return null;
        }
        
        source.clip = newClip;
        source.Play();
        
        // Fade in
        while (source.volume < 1f) {
            source.volume += Time.deltaTime;
            yield return null;
        }
    }
}
```

### Audio Pooling
```csharp
public class AudioPool : MonoBehaviour {
    [SerializeField] private int poolSize = 10;
    [SerializeField] private AudioSource audioSourcePrefab;
    
    private Queue<AudioSource> pool = new Queue<AudioSource>();
    
    void Start() {
        for (int i = 0; i < poolSize; i++) {
            AudioSource source = Instantiate(audioSourcePrefab, transform);
            source.gameObject.SetActive(false);
            pool.Enqueue(source);
        }
    }
    
    public void PlaySound(AudioClip clip, Vector3 position, float volume = 1f) {
        if (pool.Count == 0) return;
        
        AudioSource source = pool.Dequeue();
        source.transform.position = position;
        source.clip = clip;
        source.volume = volume;
        source.gameObject.SetActive(true);
        source.Play();
        
        StartCoroutine(ReturnToPool(source, clip.length));
    }
    
    IEnumerator ReturnToPool(AudioSource source, float delay) {
        yield return new WaitForSeconds(delay);
        source.gameObject.SetActive(false);
        pool.Enqueue(source);
    }
}
```
