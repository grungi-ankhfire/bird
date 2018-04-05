using UnityEngine;
using System.Collections;

public class PlayerMove : MonoBehaviour {

    private float phase = 0f;
    private float freq = 0.5f;
    public Transform leftShoulder;
    public Transform leftWrist;
    public Transform rightShoulder;
    public Transform rightWrist;
    private bool flapping = false;

    public float flapShoulderAmplitude = 80f;
    public float flapWristAmplitude = 60f;

    public float featheringShoulderAmplitude = 70f;
    public float featheringWristAmplitude = 20f;

    public float maxBankAngle = 45f;


    public float rho = 0.001225f;
    public float mass = 2000;
    public float wingArea = 7200;


    private float above = 0.0f;
    private float new_above = 0.0f;
    private float altitude = 10.0f;

	// Use this for initialization
	void Start () {
	
	}
	
	// Update is called once per frame
	void Update () {
    }

    float LiftCoeff(float alpha) {

        if (alpha >= -90f && alpha < -35f) {
            return - (90f + alpha)/55f;
        } else if (alpha < -25f) {
            return -1.0f;
        } else if (alpha < 25f) {
            return alpha / 25f;
        } else if (alpha < 35f) {
            return 1.0f;
        } else if (alpha <= 90f) {
            return (90 - alpha) / 55f;
        }

        return 0f;
    }

    float DragCoeff(float alpha) {
        return 1f;
    }

    void FixedUpdate() {
        if ((Input.GetKeyDown(KeyCode.Space) || true )&& !flapping) {
            flapping = true;
            phase = 0;
        }


        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        altitude += v * Time.deltaTime * 50f;
        altitude = Mathf.Min(20f, Mathf.Max(altitude, 0.5f));
        //transform.Rotate(transform.forward, - h * Time.deltaTime * 50f, Space.World);
        //transform.Rotate(transform.right, v * Time.deltaTime * 50f, Space.World);


        transform.Translate(0, 0, 44100f/1024f * Time.deltaTime);

        above = new_above;
        RaycastHit hit;
        if(Physics.Raycast(transform.position, -transform.up, out hit, 500f)){
            new_above = hit.distance;
        }

        transform.Translate(0, altitude - hit.distance, 0);



        if (flapping) {
            phase += Time.deltaTime * freq * 2f * Mathf.PI;
            float shoulderX = Mathf.Sin(phase)*featheringShoulderAmplitude;
            float shoulderY = Mathf.Sin(phase)*10f;
            float shoulderZ = Mathf.Sin(phase)*flapShoulderAmplitude;
            float wristX = Mathf.Sin(phase)*featheringWristAmplitude;
            float wristZ;
            if (phase <= Mathf.PI/2f) {
                wristZ = 0f;
            } else if (phase <= Mathf.PI) {
                wristZ = (phase * 2 / Mathf.PI - 1f)*flapWristAmplitude;
            } else if (phase <= 3f / 2f * Mathf.PI) {
                wristZ = flapWristAmplitude;
            } else {
                wristZ = - (phase * 2 / Mathf.PI - 4f)*flapWristAmplitude;
            }
            if (phase >= Mathf.PI) {
                shoulderX *= 0.5f;
                shoulderZ *= 0.75f;
            }
            leftShoulder.localEulerAngles = new Vector3(shoulderX, shoulderY, shoulderZ);
            leftWrist.localEulerAngles = new Vector3(wristX, 0, wristZ);
            rightShoulder.localEulerAngles = new Vector3(shoulderX, -shoulderY, -shoulderZ);
            rightWrist.localEulerAngles = new Vector3(wristX, 0, -wristZ);

            //leftShoulder.Rotate(0, 0, -Mathf.Sin(phase)*80);
        }
        if (phase >= 2f * Mathf.PI) {
            flapping = false;
        }
    }
}
