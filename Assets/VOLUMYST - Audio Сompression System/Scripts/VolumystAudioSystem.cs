using UnityEngine;
using System;
using MathNet.Numerics;
using MathNet.Numerics.IntegralTransforms;

namespace Volumyst.Namespace
{	
    [RequireComponent(typeof(AudioSource))]
    public class VolumystAudioSystem : MonoBehaviour
    {   
        [Header("Audio Source")]
        [Tooltip("Attach here the AudioSource component that will be affected by the VOLUMYST effects")]
        public AudioSource audioSource;

        [Header("Audio Settings")]
        [Tooltip("Volume of the audio")]
        [Range(0.0f, 1.0f)]
        public float volume = 1.0f;

        [Tooltip("Pitch of the audio")]
        [Range(-4.0f, 4.0f)]
        public float pitch = 1.0f;

        [Header("Freeze Effect")]
        [Tooltip("Toggle freeze effect")]
        public bool isFreezeActive = false;

        [Tooltip("Length of frozen audio capture")]
        [Range(50, 2000)]
        public int freezeCaptureLength = 2000;

        [Header("Echo Effect")]
        [Tooltip("Toggle echo effect")]
        public bool isEchoActive = false;

        [Tooltip("Delay time for echo in milliseconds")]
        [Range(0, 500)]
        public int echoDelayMilliseconds = 150;

        [Tooltip("Feedback strength of echo")]
        [Range(0.0f, 1.0f)]
        public float echoFeedback = 0.4f;

        [Header("Compression")]
        [Tooltip("Toggle audio compression")]
        public bool isCompressionActive = false;

        [Tooltip("Threshold for audio compression")]
        [Range(0.01f, 1f)]
        public float compressionThreshold = 0.5f;

        [Tooltip("Ratio for audio compression")]
        [Min(0f)]
        public float compressionRatio = 2.0f;

        [Header("Phaser Effect")]
        [Tooltip("Toggle phaser effect")]
        public bool isPhaserActive = false;

        [Tooltip("Frequency of phaser effect")]
        [Range(0.1f, 10.0f)]
        public float phaserFrequency = 1.0f;

        [Tooltip("Depth of phaser effect")]
        [Range(0.1f, 1.0f)]
        public float phaserDepth = 0.5f;

        [Header("Tremolo Effect")]
        [Tooltip("Toggle tremolo effect")]
        public bool isTremoloActive = false;

        [Tooltip("Frequency of tremolo effect")]
        [Range(0.1f, 10.0f)]
        public float tremoloFrequency = 5.0f;

        [Tooltip("Depth of tremolo effect")]
        [Range(0.0f, 1.0f)]
        public float tremoloDepth = 0.5f;

        [Header("Robotization Effect")]
        [Tooltip("Enable the robotization sound effect")]
        public bool isRobotizationActive = false;

        [Tooltip("Cutoff frequency for robotization effect")]
        [Range(100f, 10000f)]
        public float robotizationCutoffFrequency = 1000f;

        private float[] lowPassFilterBuffer;
        private int sampleRate;

        [Header("Gain Effect")]
        [Tooltip("Toggle gain effect")]
        public bool isGainActive = false;

        [Tooltip("Gain factor")]
        [Range(0.1f, 10.0f)]
        public float gainFactor = 1.0f;

        [Header("FFT Settings")]
        [Tooltip("Toggle FFT (Fast Fourier Transform) effect")]
        public bool isFFTActive = false;

        [Tooltip("Enable handling of audio signal clipping")]
        public bool handleClipping = false;

        public enum FFTWindowType { Rectangular, Hanning, Hamming, Blackman }
        [Tooltip("Type of window function for FFT")]
        public FFTWindowType fftWindowType = FFTWindowType.Rectangular;

        [Tooltip("Lerp value for FFT")]
        [Range(0f, 1f)]
        public float lerpVal = 0.5f;

        [Tooltip("Precision value for FFT")]
        [Range(0f, 1f)]
        public float precisionValue = 0.1f;

        [Tooltip("Multiplier for FFT")]
        [Range(0f, 10f)]
        public float multi = 1.0f;

        private int sampleCount;
        private int lastPlayedSample = 0;
        private int phaserOffset;
        private int echoDelaySamples;
        private int echoBufferPos;

        private float phaserIncrement;
        private float tremoloAmplitude;
        private float tremoloIncrement;
        private float tremoloPhase;

        private float[] audioData;
        private float[] frozenAudioData;
        private float[] echoBuffer;

        private void Start()
        {
            if (audioSource == null)
            {
                audioSource = GetComponent<AudioSource>();
            }

            echoDelaySamples = (int)((float)echoDelayMilliseconds / 1000 * AudioSettings.outputSampleRate);
            echoBuffer = new float[echoDelaySamples];

            phaserIncrement = (2 * Mathf.PI * phaserFrequency) / AudioSettings.outputSampleRate;

            lowPassFilterBuffer = new float[AudioSettings.outputSampleRate];
            sampleRate = AudioSettings.outputSampleRate;

            tremoloIncrement = (2 * Mathf.PI * tremoloFrequency) / AudioSettings.outputSampleRate;
            tremoloPhase = 0f;
        }

        
        private void Update()
        {
            if (!isFreezeActive)
            {
                audioSource.pitch = pitch;
            }

            if (isFreezeActive)
            {
                if (frozenAudioData == null || frozenAudioData.Length != freezeCaptureLength)
                {
                    lastPlayedSample = audioSource.timeSamples;
                    frozenAudioData = new float[freezeCaptureLength];
                    System.Array.Copy(audioData, frozenAudioData, freezeCaptureLength);
                }
            }
            else
            {
                frozenAudioData = null;

                if (lastPlayedSample != 0)
                {
                    audioSource.timeSamples = lastPlayedSample;
                    lastPlayedSample = 0;
                }
            }
        }

        private void OnAudioFilterRead(float[] data, int channels)
        {
            if (audioData == null || audioData.Length != data.Length)
            {
                audioData = new float[data.Length];
                sampleCount = data.Length / channels;
            }

            if (frozenAudioData == null)
            {
                frozenAudioData = new float[1];
            }

            System.Array.Copy(data, audioData, data.Length);

            for (int i = 0; i < data.Length; i++)
            {
                if (!isFreezeActive && !isEchoActive)
                {
                    data[i] = audioData[i] * volume;
                    audioData[i] *= pitch;
                }
                else if (isFreezeActive)
                {
                    data[i] = frozenAudioData[i % frozenAudioData.Length] * volume * (pitch);
                }
                else if (isEchoActive)
                {
                    float echoSample = audioData[i] + (echoBuffer[echoBufferPos] * echoFeedback);
                    echoBuffer[echoBufferPos] = audioData[i];
                    echoBufferPos = (echoBufferPos + 1) % echoDelaySamples;

                    data[i] = Mathf.Clamp(echoSample * volume, -1f, 1f);
                }
            }

            if (isCompressionActive)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    float compressedSample = Mathf.Sign(data[i]) * Mathf.Pow(Mathf.Abs(data[i]), compressionRatio);
                    if (Mathf.Abs(data[i]) > compressionThreshold)
                    {
                        data[i] = compressedSample;
                    }
                }
            }

            if (isPhaserActive)
            {
                float[] delayedSignal = new float[data.Length];
                System.Array.Copy(data, delayedSignal, data.Length);

                for (int i = 0; i < data.Length; i++)
                {
                    phaserOffset = (int)(phaserDepth * Mathf.Sin(phaserIncrement * i));
                    int index = Mathf.Clamp(i + phaserOffset, 0, data.Length - 1);
                    data[i] += delayedSignal[index];
                }
            }

            if (isTremoloActive)
            {
                tremoloAmplitude = Mathf.Lerp(1.0f - tremoloDepth, 1.0f + tremoloDepth, Mathf.Sin(tremoloPhase));
                tremoloPhase += tremoloIncrement;

                for (int i = 0; i < data.Length; i++)
                {
                    data[i] *= tremoloAmplitude;
                }
            }

            if (isGainActive)
            {
                for (int i = 0; i < data.Length; i++)
                {
                    data[i] *= gainFactor;
                }
            }

            if (isFFTActive)
            {
                Complex32[] complexData = new Complex32[data.Length];
                
                for (int i = 0; i < data.Length; i++)
                {
                    complexData[i] = new Complex32(data[i], 0);
                }

                switch (fftWindowType)
                {
                    case FFTWindowType.Rectangular:
                        break;
                    case FFTWindowType.Hanning:
                        ApplyHanningWindow(complexData);
                        break;
                    case FFTWindowType.Hamming:
                        ApplyHammingWindow(complexData);
                        break;
                    case FFTWindowType.Blackman:
                        ApplyBlackmanWindow(complexData);
                        break;
                    default:
                        break;
                }

                Fourier.Forward(complexData, FourierOptions.Default);

                for (int i = 0; i < complexData.Length; i++)
                {
                    float real = complexData[i].Real;
                    float imag = complexData[i].Imaginary;

                    real = Mathf.Lerp(real, lerpVal, precisionValue);
                    imag = Mathf.Pow(1f * multi, 1f);
                    real = Mathf.Pow(real, 2f) * multi;
                    imag = (Mathf.Pow(imag, 2f) * multi);

                    if (handleClipping)
                    {
                        if (real > 1.0f)
                            real = 1.0f;
                        else if (real < -1.0f)
                            real = -1.0f;

                        if (imag > 1.0f)
                            imag = 1.0f;
                        else if (imag < -1.0f)
                            imag = -1.0f;
                    }
                    
                    complexData[i] = new Complex32(real, imag);
                }

                Fourier.Inverse(complexData, FourierOptions.Default);

                for (int i = 0; i < data.Length; i++)
                {
                    data[i] = complexData[i].Real;
                }
            }

            if (isRobotizationActive)
            {
                ApplyRobotizationEffect(data, channels);
            }
        }

        private void ApplyRobotizationEffect(float[] data, int channels)
        {
            if (sampleRate == 0)
                return;

            float dt = 1f / sampleRate;
            float RC = 1f / (robotizationCutoffFrequency * Mathf.PI * 2);
            float alpha = dt / (RC + dt);

            for (int i = 0; i < data.Length; i += channels)
            {
                for (int j = 0; j < channels; j++)
                {
                    lowPassFilterBuffer[i / channels] = lowPassFilterBuffer[i / channels] + alpha * (data[i + j] - lowPassFilterBuffer[i / channels]);
                    data[i + j] = lowPassFilterBuffer[i / channels];
                }
            }
        }

        private void ApplyHanningWindow(Complex32[] data)
        {
            double factor = 2 * Math.PI / data.Length;
            for (int i = 0; i < data.Length; i++)
            {
                double multiplier = 0.5 * (1 - Math.Cos(factor * i));
                data[i] = new Complex32((float)(data[i].Real * multiplier), 0);
            }
        }

        private void ApplyHammingWindow(Complex32[] data)
        {
            double factor = 2 * Math.PI / data.Length;
            for (int i = 0; i < data.Length; i++)
            {
                double multiplier = 0.54 - 0.46 * Math.Cos(factor * i);
                data[i] = new Complex32((float)(data[i].Real * multiplier), 0);
            }
        }

        private void ApplyBlackmanWindow(Complex32[] data)
        {
            double alpha = 0.16;
            double a0 = (1 - alpha) / 2;
            double a1 = 0.5;
            double a2 = alpha / 2;
            double factor = 2 * Math.PI / data.Length;
            
            for (int i = 0; i < data.Length; i++)
            {
                double multiplier = a0 - a1 * Math.Cos(factor * i) + a2 * Math.Cos(2 * factor * i);
                data[i] = new Complex32((float)(data[i].Real * multiplier), 0);
            }
        }

        public void SetVolume(float newVolume)
        {
            volume = Mathf.Clamp01(newVolume);
        }

        public void SetPitch(float newPitch)
        {
            pitch = Mathf.Clamp(newPitch, -3.0f, 3.0f);
        }

        public void ToggleFreeze()
        {
            isFreezeActive = !isFreezeActive;
        }

        public void ToggleEcho()
        {
            isEchoActive = !isEchoActive;
        }

            public void ToggleTremolo()
        {
            isTremoloActive = !isTremoloActive;
        }

        public void TogglePhaser()
        {
            isPhaserActive = !isPhaserActive;
        }

        public void ToggleGain()
        {
            isGainActive = !isGainActive;
        }
    }
}