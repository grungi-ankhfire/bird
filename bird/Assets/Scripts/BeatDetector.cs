using UnityEngine;
using System.Collections;
using SpeedTest;

[RequireComponent(typeof (AudioSource))]
public class BeatDetector : MonoBehaviour {

    // For the precomputed analysis
    public int maxWindowSize = 1024;
    
    private AudioSource aud;
    private float C = 1.3f;
    private bool[] beats;
    private FFT2 fft;
    private int channels;
    private int numSamples;
    private int numWindows;
    private float[] energies;
    private float energyBuffer;
    private int beatNumber;
    private int windowSize;
    private float variance;
    private float minVariance;
    private float maxVariance;
    private float minEnergy;
    private float maxEnergy;

    // For in-game use
    private int delay = 1;
    private int previousSegment;
    private int segment = 0;

    private float[] rawSamples;
    private float[] samplesFFT;
    private float[] samplesFFTi;


    private float[] trajectory;
    private float[] smoothTrajectory;

    // For road construction
    private float[] elevations;

    [Range(0.0f, 1.0f)]
    public float smoothing;

    private Path path;

    void Start() {

        // Get AudioClip informations
        aud = GetComponent<AudioSource>();
        channels = aud.clip.channels;
        numSamples = aud.clip.samples;
        rawSamples = new float[numSamples * channels];
        aud.clip.GetData(rawSamples, 0);
        numWindows = (int) Mathf.Ceil(numSamples / (float) maxWindowSize);

        
        // Allocate objects for spectrum analysis
        energies = new float[numWindows];
        beats = new bool[numWindows];
        fft = new FFT2();
        fft.init(10);
        minVariance = 10000000000f;
        minEnergy = 10000000000f;
        maxVariance = -10000000000f;
        maxEnergy = -10000000000f;
        samplesFFT = new float[maxWindowSize];
        samplesFFTi = new float[maxWindowSize];
        beatNumber = 0;
        elevations = new float[numWindows];
        trajectory = new float[numWindows];
        smoothTrajectory = new float[numWindows];


        Debug.Log("Number of samples : " + numSamples);
        Debug.Log("Number of windows : " + numWindows);

        for (int w=0; w<numWindows; w++) {

            windowSize = maxWindowSize;
            if (w == numWindows-1) {
                windowSize = numSamples - (numWindows - 1) * maxWindowSize;
            }
            
            rawSamples = new float[windowSize * channels];
            aud.clip.GetData(rawSamples, w*maxWindowSize);

            for (int c = 0; c < channels; c++) {
                for (int s = 0; s < maxWindowSize; s++) {
                    if (windowSize < maxWindowSize && s >= windowSize) {
                        samplesFFT[s] = 0f;
                    }
                    else {
                        samplesFFT[s] = rawSamples[s * channels + c];
                    }
                    elevations[w] += Mathf.Abs(samplesFFT[s]);
                    samplesFFTi[s] = 0f;
                }
                samplesFFT = BlackmanHarris(samplesFFT);
                fft.run(samplesFFT, samplesFFTi, false);

                for (int i = 0; i < maxWindowSize/16; i++){
                    energies[w] += norm(samplesFFT[i], samplesFFTi[i], true);
                } 
                // energies[w] *= 2;
            }
            elevations[w] = -(elevations[w] / (channels * maxWindowSize)) * 2f + 0.5f;
            if (elevations[w] < 0) {
                elevations[w] *= 2f;
            }
            if (w > 0) {
                elevations[w] += elevations[w-1];
            }
            if (w == 0) {
                smoothTrajectory[w] = elevations[w];
            } else {
                smoothTrajectory[w] = smoothing * elevations[w] + (1-smoothing) * smoothTrajectory[w-1];
            }
            minEnergy = Mathf.Min(energies[w], minEnergy);
            maxEnergy = Mathf.Max(energies[w], maxEnergy);

            if (w > 42) {
                energyBuffer = 0f;
                for (int j = w-43; j < w; j++) {
                    energyBuffer += energies[j];
                }
                energyBuffer *= 1f / 43f;

                if (energies[w] > energyBuffer || energies[w] > 0.25*maxEnergy) {
                    trajectory[w] = trajectory[w-1] - 1.0f;    
                } else if (Mathf.Abs(energyBuffer - energies[w]) < 1.0f) {
                    trajectory[w] = trajectory[w-1];
                } else {
                    trajectory[w] = trajectory[w-1] + 1.0f;
                }
                trajectory[w] = trajectory[w-1] + (energyBuffer-energies[w]) / 10f; //maxEnergy * 2f;

                variance = 0f;
                for (int j = w-43; j < w; j++) {
                    variance += (energies[j] - energyBuffer) * (energies[j] - energyBuffer);
                }
                variance /= 43f;
                minVariance = Mathf.Min(variance, minVariance);
                maxVariance = Mathf.Max(variance, maxVariance);

                C = (-0.0005f*variance) +2f;
                //C = 1.4f;
                if (C*energyBuffer < energies[w]) {
                //if (10 < energies[w]) {
                    beats[w] = true;
                    beatNumber += 1;
                } else {
                    beats[w] = false;
                }
            }
        }

        print("Minimum energy : " + minEnergy);
        print("Maximum energy : " + maxEnergy);
        print("Number of beats : " + beatNumber);
        print("Minimum variance : " + minVariance);
        print("Maximum variance : " + maxVariance);
        rawSamples = new float[maxWindowSize*channels];

        path = new Path(5.0f, 1.0f, 2.0f, Color.red, smoothTrajectory);
    }

    void FixedUpdate() {
        previousSegment = segment;
        segment = (int) Mathf.Floor(aud.timeSamples / maxWindowSize);

        int energyStart = (int) Mathf.Max(0, segment-512);
        int energyEnd = (int) Mathf.Min(numWindows, segment + 512);

        for (int w = energyStart; w < energyEnd; w++) {
            if (w == segment) {
                Debug.DrawLine(new Vector3(0.01f*(w-energyStart + 520), 0, 0), new Vector3(0.01f*(w-energyStart + 520), energies[w]/maxEnergy, 0), Color.white);
                Debug.DrawLine(new Vector3(0, smoothTrajectory[w], w-energyStart), new Vector3(0, smoothTrajectory[w+1], (w+1)-energyStart), Color.yellow);
            } else {
                Debug.DrawLine(new Vector3(0.01f*(w-energyStart + 520), 0, 0), new Vector3(0.01f*(w-energyStart + 520), energies[w]/maxEnergy, 0), Color.black);
                Debug.DrawLine(new Vector3(0, smoothTrajectory[w], w-energyStart), new Vector3(0, smoothTrajectory[w+1], (w+1)-energyStart), Color.black);
            }
        }

        if (segment != previousSegment) {
            samplesFFT = new float[maxWindowSize];
            samplesFFTi = new float[maxWindowSize];
            aud.clip.GetData(rawSamples, segment*maxWindowSize);
            for (int c = 1; c < channels; c++) {
                for (int s = 0; s < maxWindowSize; s++) {
                    samplesFFT[s] = rawSamples[s*channels + c];
                    samplesFFTi[s] = 0f;
                }
                samplesFFT = BlackmanHarris(samplesFFT);
                fft.run(samplesFFT, samplesFFTi, false);
            }
        }
        float newNorm = 0f;
        float oldNorm = 0f;
        Color col = Color.red;
        oldNorm += norm(samplesFFT[0], samplesFFTi[0], true);

        float energy = oldNorm;
        for (int i = 0; i < 511; i++) {
            newNorm = 0f;
            newNorm += norm(samplesFFT[i], samplesFFTi[i], true);
            energy += newNorm;

            Debug.DrawLine(new Vector3(0.01f*i, oldNorm, 0), new Vector3(0.01f*(i+1), newNorm, 0), col);
            oldNorm = newNorm;
        }

        Debug.DrawLine(new Vector3(-1, 0, 0), new Vector3(-1, energy, 0), Color.green);

        if (energy > 10) {
            Debug.DrawLine(new Vector3(-1.2f, 0, 0), new Vector3(-1.1f, 1, 0), Color.black);
        }
        if (beats[segment]) {
            Debug.DrawLine(new Vector3(-1.1f, 0, 0), new Vector3(-1.1f, 1, 0), Color.blue);
            transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        } else {
            Debug.DrawLine(new Vector3(-1.1f, 0, 0), new Vector3(-1.1f, 1, 0), Color.yellow);

            if (delay == 0) {
                delay = 1;
                transform.localScale = new Vector3(1f, 1f, 1f);
            } else {
                delay -= 1;
            }
        }
    }

    float[] BlackmanHarris(float[] signal) {
        float N = maxWindowSize;
        for (int n = 0; n < signal.Length; n++) {
            signal[n] *= 0.35875f - (0.48829f * Mathf.Cos(1.0f * n/N)) + (0.14128f * Mathf.Cos(2.0f * n/N)) - (0.01168f * Mathf.Cos(3.0f * n/N));
        }
        return signal;
    }

    float norm(float real, float complex, bool squared=false) {
        if (squared) {
            return real*real + complex*complex;
        } else {
            return Mathf.Sqrt(real*real + complex*complex);
        }
    }


}
