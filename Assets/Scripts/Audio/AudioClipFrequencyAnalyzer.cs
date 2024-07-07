using UnityEngine;
using MathNet.Numerics.IntegralTransforms;
using System.Numerics;
using Unity.Mathematics;

public class AudioClipFrequencyAnalyzer : MonoBehaviour
{
    public AudioClip _clip;
    public int sampleSegmentMilliseconds = 100;

    public float _threshold = 10f;

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

    [ContextMenu("Low")]
    public void AnalyzeClipLow()
    {
        if (_clip == null)
        {
            // Debug.LogError("AudioClip is not assigned.");
            return;
        }

        int clipLength = (int)math.round(_clip.length * 1000f);
        FunscriptMouseInput.Singleton.AddFunAction(0, 0, false);
        for (int i = 1; i < clipLength; i += sampleSegmentMilliseconds)
        {
            AnalyzeSegment(i, out float lowFreq, out float midFreq, out float highFreq);

            // add point at i
            int pos = (int)math.floor(lowFreq / _threshold);
            FunscriptMouseInput.Singleton.AddFunAction(i, pos, false);
        }

        FunscriptRenderer.Singleton.SortFunscript();
        FunscriptRenderer.Singleton.CleanupExcessPoints();

        TitleBar.MarkLabelDirty();
        FunscriptOverview.Singleton.RenderHaptics();
    }
    
    [ContextMenu("Mid")]
    public void AnalyzeClipMid()
    {
        if (_clip == null)
        {
            // Debug.LogError("AudioClip is not assigned.");
            return;
        }

        int clipLength = (int)math.round(_clip.length * 1000f);
        FunscriptMouseInput.Singleton.AddFunAction(0, 0, false);
        for (int i = 1; i < clipLength; i += sampleSegmentMilliseconds)
        {
            AnalyzeSegment(i, out float lowFreq, out float midFreq, out float highFreq);

            // add point at i
            int pos = (int)math.floor(midFreq / _threshold);
            FunscriptMouseInput.Singleton.AddFunAction(i, pos, false);
        }

        FunscriptRenderer.Singleton.SortFunscript();
        FunscriptRenderer.Singleton.CleanupExcessPoints();

        TitleBar.MarkLabelDirty();
        FunscriptOverview.Singleton.RenderHaptics();
    }
    
    [ContextMenu("High")]
    public void AnalyzeClipHigh()
    {
        if (_clip == null)
        {
            // Debug.LogError("AudioClip is not assigned.");
            return;
        }

        int clipLength = (int)math.round(_clip.length * 1000f);
        FunscriptMouseInput.Singleton.AddFunAction(0, 0, false);
        for (int i = 1; i < clipLength; i += sampleSegmentMilliseconds)
        {
            AnalyzeSegment(i, out float lowFreq, out float midFreq, out float highFreq);

            // add point at i
            int pos = (int)math.floor(highFreq / _threshold);
            FunscriptMouseInput.Singleton.AddFunAction(i, pos, false);
        }

        FunscriptRenderer.Singleton.SortFunscript();
        FunscriptRenderer.Singleton.CleanupExcessPoints();

        TitleBar.MarkLabelDirty();
        FunscriptOverview.Singleton.RenderHaptics();
    }
    void AnalyzeSegment(int timeInMilliseconds, out float lowFreq, out float midFreq, out float highFreq)
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
        ProcessFrequencyBands(spectrumData, numSamples, out lowFreq, out midFreq, out highFreq);
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

        // Get magnitude spectrum
        float[] spectrum = new float[sampleSize];
        for (int i = 0; i < sampleSize; i++)
        {
            spectrum[i] = (float)complexSamples[i].Magnitude;
        }

        return spectrum;
    }

    void ProcessFrequencyBands(float[] spectrumData, int sampleSize, out float lowFreq, out float midFreq, out float highFreq)
    {
        lowFreq = 0f;
        midFreq = 0f;
        highFreq = 0f;

        int lowEnd = sampleSize / 3;
        int midEnd = 2 * sampleSize / 3;

        for (int i = 0; i < lowEnd; i++)
        {
            lowFreq += spectrumData[i];
        }

        for (int i = lowEnd; i < midEnd; i++)
        {
            midFreq += spectrumData[i];
        }

        for (int i = midEnd; i < sampleSize; i++)
        {
            highFreq += spectrumData[i];
        }

        // Debug.Log("Low: " + lowFreq);
        // Debug.Log("Mid: " + midFreq);
        // Debug.Log("High: " + highFreq);
    }
}