using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CriAudioDataPlayer : MonoBehaviour
{
    [SerializeField]
    public CriAudioData data;
    public bool autoPlay;

    private AudioSource introSource;
    private AudioSource loopSource;

    // Start is called before the first frame update
    void Start()
    {
        if (autoPlay)
        {
            Initialise(data);
            Play();
        }
    }

    internal void Initialise(CriAudioData data)
    {
        this.data = data;

        if (data.introClip != null)
        {
            introSource = gameObject.AddComponent<AudioSource>();
            introSource.playOnAwake = false;
            introSource.volume = data.mainVolume;
            introSource.clip = data.introClip;
        }

        if (data.loopClip != null)
        {
            loopSource = gameObject.AddComponent<AudioSource>();
            loopSource.playOnAwake = false;
            loopSource.clip = data.loopClip;
            loopSource.volume = data.mainVolume;
            loopSource.loop = true;
        }
    }

    internal void Play()
    {
        if (data.introClip != null && data.loopClip != null)
        {
            var t0 = AudioSettings.dspTime + 0.1f;
            double clipTime1 = data.introClip.samples;
            clipTime1 /= data.introClip.frequency;
            introSource.PlayScheduled(t0);
            introSource.SetScheduledEndTime(t0 + clipTime1);
            loopSource.PlayScheduled(t0 + clipTime1);
        }
        else if (data.introClip != null)
        {
            introSource.Play();
        }
        else if (data.loopClip != null)
        {
            loopSource.Play();
        }
    }

    internal void Stop()
    {

    }
}
