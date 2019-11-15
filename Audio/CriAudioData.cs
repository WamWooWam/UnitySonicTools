using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using UnityEngine;

[Serializable]
public class CriAudioData
{
    public string cueName;
    public int cueIndex;

    // the fuck are these used for?
    public Vector3 translation;
    public Quaternion rotation;

    public float mainVolume;

    public AudioClip introClip;
    public AudioClip loopClip;
    public CriAudioDataPlayer source;
}