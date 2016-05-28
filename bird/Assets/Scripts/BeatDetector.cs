using UnityEngine;
using System.Collections;
using SpeedTest;

public class BeatDetector : MonoBehaviour {

    AudioSource aud;
    private float[] spectrum;
    public float C = 1.3f;
    public int sample_window = 1024;
    private bool[] beats;

    private int delay = 1;

    private FFT2 fft;

    void Start() {
        aud = GetComponent<AudioSource>();
        int num_samples = aud.clip.samples * aud.clip.channels;
        float[] samples = new float[num_samples];
        int num_windows = (int) Mathf.Ceil(num_samples / aud.clip.channels / 1024.0f);
        float[] energies = new float[num_windows];
        beats = new bool[num_windows];
        float energy_history;
        float current_sample;

        fft = new FFT2();
        fft.init(10);

        print(num_windows);
        print(num_samples);

        float[] samples_fft = new float[sample_window];
        float[] samples_fft_i = new float[sample_window];
        spectrum = new float[sample_window];
        aud.clip.GetData(samples, 0);
        int number_of_beats = 0;

        float min_variance = 10000000000f;
        float max_variance = -10000000000f;

        for (int w=0; w<num_windows; w++) {

            int window_length = sample_window;
            if (w == num_windows-1) {
                window_length = (num_samples/aud.clip.channels - (num_windows-1) * sample_window);
            }
            
            for (int c = 0; c < aud.clip.channels; c++) {
                for (int s = 0; s < sample_window; s++) {
                    if (window_length < sample_window && s >= window_length) {
                            samples_fft[s] = 0f;
                    }
                    else {
                        samples_fft[s] = samples[w*sample_window + s*aud.clip.channels + c];
                    }
                    samples_fft_i[s] = 0f;
                
                }

                fft.run(samples_fft, samples_fft_i, false);

                for (int i=window_length / 4; i<window_length / 2; i++){
                    current_sample = samples_fft[i];
                    energies[w] +=  current_sample * current_sample;
                }
            }

            //energies[w] = energy_left + energy_right;
            if (w > 42) {
                energy_history = 0f;
                for (int j = w-43; j < w; j++) {
                    energy_history += energies[j];
                }
                energy_history *= 1f/43f;

                float variance = 0f;
                for (int j = w-43; j < w; j++) {
                    variance += (energies[j] - energy_history) * (energies[j] - energy_history);
                }
                variance *= 1f/43f;
                if (variance < min_variance) {
                    min_variance = variance;
                }
                if (variance > max_variance) {
                    max_variance = variance;
                }
                C = 1.4f; // (-0.0025714f*variance) +1.5142857f;

                if (energy_history*C < energies[w]) {
                    beats[w] = true;
                    number_of_beats += 1;
                } else {
                    beats[w] = false;
                }
            }
        }
        print("number of beats : " + number_of_beats);
        print("Min variance : " + min_variance);
        print("Max variance : " + max_variance);
    }

    void FixedUpdate() {
        //print(aud.timeSamples);
        if (beats[(int) Mathf.Floor(aud.timeSamples / 1024.0f)]) {
            transform.localScale = new Vector3(1.5f, 1.5f, 1.5f);
        } else {
            if (delay == 0) {
                delay = 1;
                transform.localScale = new Vector3(1f, 1f, 1f);
            } else {
                delay -= 1;
            }
        }
    }

}
