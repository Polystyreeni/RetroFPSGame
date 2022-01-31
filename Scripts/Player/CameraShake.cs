using UnityEngine;
using System.Collections;
public class CameraShake : MonoBehaviour
{ 
	public bool debugMode = false;

	public float shakeAmount;		// The amount to shake this frame.
	public float shakeDuration;     // The duration this frame.
	public float maxAmount = 100f;

	private Transform parentTransform = null;

	//Readonly values...
	float shakePercentage;			// A percentage (0-1) representing the amount of shake to be applied when setting rotation.
	float startAmount;				// The initial shake amount (to determine percentage), set when ShakeCamera is called.
	float startDuration;			// The initial shake duration, set when ShakeCamera is called.

	bool isRunning = false;			// Is the coroutine running right now?

	public bool smooth;				// Smooth rotation?
	public float smoothAmount = 5f;	// Amount to smooth

	void Start()
	{
		if (debugMode) ShakeCamera();
		parentTransform = transform;
	}

	void ShakeCamera()
	{
		startAmount = shakeAmount;
		startDuration = shakeDuration;

		if (!isRunning) StartCoroutine(Shake(smoothAmount));	// Only call the coroutine if it isn't currently running. Otherwise, just set the variables.
	}

    public void ShakeCamera(float amount, float duration, float smoothSpeed = 0f)
	{
		if (smoothSpeed <= 0)
			smoothSpeed = smoothAmount;

		if (amount > maxAmount)
			amount = maxAmount;

		shakeAmount += amount;			// Add to the current amount.
		startAmount = shakeAmount;		// Reset the start amount, to determine percentage.
		shakeDuration += duration;		// Add to the current time.
		startDuration = shakeDuration;	// Reset the start time.

		if (!isRunning) StartCoroutine(Shake(smoothSpeed));	// Only call the coroutine if it isn't currently running. Otherwise, just set the variables.
	}
	IEnumerator Shake(float smoothSpeed)
	{
		isRunning = true;

		while (shakeDuration > 0.01f)
		{
			Vector3 rotationAmount = Random.insideUnitSphere * shakeAmount;	// A Vector3 to add to the Local Rotation
			rotationAmount.z = 0;                                           // Don't change the Z; it looks funny.
			rotationAmount.y = 0;

			shakePercentage = shakeDuration / startDuration;	// Used to set the amount of shake (% * startAmount).

			shakeAmount = startAmount * shakePercentage;//Set the amount of shake (% * startAmount).
			shakeDuration = Mathf.Lerp(shakeDuration, 0, Time.deltaTime);//Lerp the time, so it is less and tapers off towards the end.

			if (smooth)
				transform.localRotation = Quaternion.Lerp(parentTransform.localRotation, Quaternion.Euler(rotationAmount), Time.deltaTime * smoothSpeed);
			else
				parentTransform.localRotation = Quaternion.Euler(rotationAmount);//Set the local rotation the be the rotation amount.

			yield return null;
		}

		transform.localRotation = Quaternion.identity;
		parentTransform.localRotation = Quaternion.identity;	//Set the local rotation to 0 when done, just to get rid of any fudging stuff.
		isRunning = false;
	}

	public void ShakeAmountOfTimes(float amount, int numberOfShakes, float smoothSpeed = 0f)
    {
		if (isRunning) return;

		if (smoothSpeed <= 0)
			smoothSpeed = smoothAmount;

		if (amount > maxAmount)
			amount = maxAmount;

		shakeAmount += amount;          // Add to the current amount.
		startAmount = shakeAmount;      // Reset the start amount, to determine percentage.

		if (!isRunning) StartCoroutine(ShakeNAmount(numberOfShakes, smoothSpeed)); // Only call the coroutine if it isn't currently running. Otherwise, just set the variables.
	}

	IEnumerator ShakeNAmount(int numberOfShakes, float smoothSpeed)
    {
		isRunning = true;
		int passed = numberOfShakes;
		float interval = 1 / (float)numberOfShakes;
		Debug.Log("Interval: " + interval);
		shakeDuration = interval * passed;

		Debug.Log("Camera shake: Shake" + numberOfShakes + " Times, shake duration: " + shakeDuration);

		while (passed > 0)
        {
			Vector3 rotationAmount = Random.insideUnitSphere * shakeAmount; // A Vector3 to add to the Local Rotation
			rotationAmount.z = 0;                                           // Don't change the Z; it looks funny.
			rotationAmount.y = 0;

			shakeAmount = startAmount * interval * passed;

			while (shakeDuration > 0f)
            {
				if (smooth)
					transform.localRotation = Quaternion.Lerp(parentTransform.localRotation, Quaternion.Euler(rotationAmount), Time.deltaTime * smoothSpeed);

				else
					parentTransform.localRotation = Quaternion.Euler(rotationAmount);   // Set the local rotation the be the rotation amount.

				shakeDuration -= Time.deltaTime;
				Debug.Log("Shake moving camera");
			}
			
			passed--;
			shakeDuration = interval * passed;
			yield return null;
		}

		transform.localRotation = Quaternion.identity;
		parentTransform.localRotation = Quaternion.identity;    //Set the local rotation to 0 when done, just to get rid of any fudging stuff.
		shakeDuration = 0;
		isRunning = false;
	}
}
