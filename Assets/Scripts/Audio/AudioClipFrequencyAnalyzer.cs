using UnityEngine;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
using Unity.Mathematics;

public class AudioClipFrequencyAnalyzer : MonoBehaviour
{
    public static AudioClipFrequencyAnalyzer Singleton;

    public AudioClip _clip;
    public int sampleSegmentMilliseconds = 100;

    private void Awake()
    {
        if (Singleton == null) Singleton = this;
        else if (Singleton != this) Destroy(this);
    }

    private void OnEnable()
    {
        AudioLoader.ClipLoaded += OnAudioClipLoaded;
    }

    private void OnDisable()
    {
        AudioLoader.ClipLoaded -= OnAudioClipLoaded;
    }

    private void OnAudioClipLoaded(AudioSource audioSource)
    {
        _clip = audioSource.clip;
    }

    public void AnalyzeClip(float strength, float min, float max)
    {
        if (_clip == null)
        {
            // Debug.LogError("AudioClip is not assigned.");
            return;
        }

        // clear underlying haptics
        foreach (var haptic in FunscriptRenderer.Singleton.Haptics)
        {
            if (haptic.Selected && haptic.Visible) haptic.Funscript.actions.Clear();
        }

        int clipLength = (int)math.round(_clip.length * 1000f);
        FunscriptMouseInput.Singleton.AddFunAction(0, 0, false);
        for (int i = 1; i < clipLength; i += sampleSegmentMilliseconds)
        {
            AnalyzeSegment(i, min, max, out float freq);

            // add point at i
            int pos = (int)math.floor(freq * strength);
            FunscriptMouseInput.Singleton.AddFunAction(i, pos, false);
        }

        RefreshFunscript();
    }

    void RefreshFunscript()
    {
        FunscriptRenderer.Singleton.SortFunscript();
        FunscriptRenderer.Singleton.CleanupExcessPoints();

        TitleBar.MarkLabelDirty();
        FunscriptOverview.Singleton.RenderHaptics();
    }


    void AnalyzeSegment(int timeInMilliseconds, float min, float max, out float freq)
    {
        int sampleRate = _clip.frequency;
        int numSamples = (sampleRate * sampleSegmentMilliseconds) / 1000;

        // Ensure numSamples is a power of 2 for optimal FFT performance
        numSamples = Mathf.ClosestPowerOfTwo(numSamples);

        int startSampleIndex = (int)(sampleRate * (timeInMilliseconds / 1000f));
        startSampleIndex = Mathf.Clamp(startSampleIndex, 0, _clip.samples - numSamples); // Ensure the segment is within bounds

        float[] samples = new float[numSamples];
        _clip.GetData(samples, startSampleIndex);

        float[] spectrumData = AnalyzeFrequency(samples, numSamples);
        ProcessFrequencyBands(spectrumData, numSamples, min, max, out freq);
    }

    float[] AnalyzeFrequency(float[] samples, int sampleSize)
    {
        // Convert float array to Complex array required by FFT
        Complex[] complexSamples = new Complex[sampleSize];
        for (int i = 0; i < sampleSize; i++)
        {
            complexSamples[i] = new Complex(samples[i], 0);
        }

        // Apply FFT
        Fourier.Forward(complexSamples, FourierOptions.Matlab);

        // Get magnitude spectrum (only first half)
        int halfSize = sampleSize / 2;
        float[] spectrum = new float[halfSize];
        for (int i = 0; i < halfSize; i++)
        {
            spectrum[i] = (float)complexSamples[i].Magnitude;
        }

        return spectrum;
    }

    private static void ProcessFrequencyBands(float[] spectrumData, int sampleSize, float min, float max, out float freq)
    {
        freq = 0f;

        int halfSize = sampleSize / 2;
        int start = (int)math.floor(halfSize * min);
        int end = (int)math.ceil(halfSize * max);

        // Debug.Log(start);
        // Debug.Log(end);
        // Debug.Log(spectrumData.Length);
        //
        // return;
        
        if (math.abs(start - end) < 100)
        {
            start = end - 100; // expect at least 100hz range

            if (start < 100)
            {
                start = 0;
                end = 100;
            }
        }

        // int lowEnd = halfSize / 3;
        // int midEnd = 2 * halfSize / 3;

        for (int i = start; i < end; i++)
        {
            freq += spectrumData[i];
        }

        // Optional: Normalize the frequency bands by the number of elements in each band
        //freq /= (end - start);
    }
}