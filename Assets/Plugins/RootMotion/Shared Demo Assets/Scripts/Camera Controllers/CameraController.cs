using UnityEngine;
using System.Collections;

namespace RootMotion {

	/// <summary>
	/// 3rd person camera controller.
	/// </summary>
	public class CameraController : MonoBehaviour {

		// When to update the camera?
		[System.Serializable]
		public enum UpdateMode {
			Update,
			FixedUpdate,
			LateUpdate,
			FixedLateUpdate
		}

		public Transform target; // The target Transform to follow
		public Transform rotationSpace; // If assigned, will use this Transform's rotation as the rotation space instead of the world space. Useful with spherical planets.
		public UpdateMode updateMode = UpdateMode.LateUpdate; // When to update the camera?
		public bool lockCursor = true; // If true, the mouse will be locked to screen center and hidden

		[Header("Position")]
		public bool smoothFollow; // If > 0, camera will smoothly interpolate towards the target
		public Vector3 offset = new Vector3(0, 1.5f, 0.5f); // The offset from target relative to camera rotation
		public float smoothFollowSpeed = 10f; // Smooth follow speed

		[Header("Rotation")]
		public float rotationSensitivity = 3.5f; // The sensitivity of rotation
		public float yMinLimit = -20; // Min vertical angle
		public float yMaxLimit = 80; // Max vertical angle
		public bool rotateAlways = true; // Always rotate to mouse?
		public bool rotateOnLeftButton; // Rotate to mouse when left button is pressed?
		public bool rotateOnRightButton; // Rotate to mouse when right button is pressed?
		public bool rotateOnMiddleButton; // Rotate to mouse when middle button is pressed?
		public bool dontRotateOnLeftAlt; // Do not rotate when holding left Alt button?
		public bool rotateWithJoystick; // Rotate with x/y Joy Axes 
		public string xJoyAxis; // x Joy Axis
		public string yJoyAxis; // y Joy Axis
		public float xJoystickRotationSensitivity; // The sensitivity of rotation with x Joy Axis 
		public float yJoystickRotationSensitivity; // The sensitivity of rotation with y Joy Axis 

		[Header("Following")]
		public bool follow = false; // Snap back to default rotation when no rotation from user
		public float timeBeforeFollow; // Time without user input after which we snap back to default rotation
		public float followAngleSpeed; // Speed of coming back to default rotation
		public float yDefautRotationOffset; // Offset of default y rotation
		public float followDistance; // Distance to character camera tries to keep
		public float followDistanceSpeed; // Speed of coming to default distance
		[HideInInspector]
		public bool secondaryFollow; // For temporary suspending camera via script when needed

		[Header("Distance")]
		public float distance = 10.0f; // The current distance to target
		public float minDistance = 4; // The minimum distance to target
		public float maxDistance = 10; // The maximum distance to target
		public float zoomSpeed = 10f; // The speed of interpolating the distance
		public float zoomSensitivity = 1f; // The sensitivity of mouse zoom

		[Header("Blocking")]
		public LayerMask blockingLayers;
		public float blockingRadius = 1f;
		public float blockingSmoothTime = 0.1f;
        public float blockingOriginOffset;
		[Range(0f, 1f)] public float blockedOffset = 0.5f;

		public float x { get; private set; } // The current x rotation of the camera
		public float y { get; private set; } // The current y rotation of the camera
		public float distanceTarget { get; private set; } // Get/set distance

		private Vector3 targetDistance, position;
		private Quaternion rotation = Quaternion.identity;
		private Vector3 smoothPosition;
		private Camera cam;
		private bool fixedFrame;
		private float fixedDeltaTime;
		private Quaternion r = Quaternion.identity;
		private Vector3 lastUp;
		private float blockedDistance = 10f, blockedDistanceV;
		private float lastInputTime;

        public void SetAngles(Quaternion rotation)
        {
            Vector3 euler = rotation.eulerAngles;
            this.x = euler.y;
            this.y = euler.x;
        }

        public void SetAngles(float yaw, float pitch)
        {
            this.x = yaw;
            this.y = pitch;
        }

		// Initiate, set the params to the current transformation of the camera relative to the target
		protected virtual void Awake () {
			Vector3 angles = transform.eulerAngles;
			x = angles.y;
			y = angles.x;
			
			distanceTarget = distance;
			smoothPosition = transform.position;

			cam = GetComponent<Camera>();

			lastUp = rotationSpace != null? rotationSpace.up: Vector3.up;
			lastInputTime = Time.time;
		}

		protected virtual void Update() {
			if (updateMode == UpdateMode.Update) UpdateTransform();
		}

		protected virtual void FixedUpdate() {
			fixedFrame = true;
			fixedDeltaTime += Time.deltaTime;
			if (updateMode == UpdateMode.FixedUpdate) UpdateTransform();
		}

		protected virtual void LateUpdate() {
			UpdateInput();

			if (updateMode == UpdateMode.LateUpdate) UpdateTransform();

			if (updateMode == UpdateMode.FixedLateUpdate && fixedFrame) {
				UpdateTransform(fixedDeltaTime);
				fixedDeltaTime = 0f;
				fixedFrame = false;
			}
		}

		// Read the user input
		public void UpdateInput() {
			if (!cam.enabled) return;

			float epsilon = 0.01f;

			// Cursors
			Cursor.lockState = lockCursor? CursorLockMode.Locked: CursorLockMode.None;
			Cursor.visible = lockCursor? false: true;

			// Should we rotate the camera?
			bool rotate = (!dontRotateOnLeftAlt || !Input.GetKey(KeyCode.LeftAlt)) && ( rotateAlways || (rotateOnLeftButton && Input.GetMouseButton(0)) || (rotateOnRightButton && Input.GetMouseButton(1)) || (rotateOnMiddleButton && Input.GetMouseButton(2)));

			// delta rotation
			if (rotate) {
				x += Input.GetAxis("Mouse X") * rotationSensitivity;
				y = ClampAngle(y - Input.GetAxis("Mouse Y") * rotationSensitivity, yMinLimit, yMaxLimit);
				if (Mathf.Abs(Input.GetAxis("Mouse X")) > epsilon || Mathf.Abs(Input.GetAxis("Mouse Y")) > epsilon) lastInputTime = Time.time;
			}

			if (rotateWithJoystick)
			{
				x += Input.GetAxis(xJoyAxis) * xJoystickRotationSensitivity * Time.unscaledDeltaTime;
				y = ClampAngle(y + Input.GetAxis(yJoyAxis) * yJoystickRotationSensitivity * Time.unscaledDeltaTime, yMinLimit, yMaxLimit);
				if (Mathf.Abs(Input.GetAxis(xJoyAxis)) > epsilon || Mathf.Abs(Input.GetAxis(yJoyAxis)) > epsilon) lastInputTime = Time.time;
			}

			// Distance
			distanceTarget = Mathf.Clamp(distanceTarget + zoomAdd, minDistance, maxDistance);
			if (Mathf.Abs(Input.GetAxis("Mouse ScrollWheel")) > epsilon) lastInputTime = Time.time;
		}

		// Update the camera transform
		public void UpdateTransform() {
			UpdateTransform(Time.deltaTime);
		}

		public void UpdateTransform(float deltaTime) {
			if (!cam.enabled) return;

			// In case of pause or slow-mo
			if (deltaTime < 0.02f) deltaTime = 0.02f; // TODO: есть независимое от тикрейта дельта
			
			// Following
			if (follow && secondaryFollow && Time.time - lastInputTime > timeBeforeFollow)
			{
				// Angle
				float targetAngle = target.rotation.eulerAngles.y;

				if (!(Mathf.Abs(x - targetAngle) < deltaTime * followAngleSpeed))
				{
					if (Mathf.Abs(x - targetAngle) < 180)
					{
						// Rotate current directly towards target.
						if (x < targetAngle) x += deltaTime * followAngleSpeed;
						else x -= deltaTime * followAngleSpeed;
					}
					else
					{
						// Rotate the other direction towards target.
						if (x < targetAngle) x -= deltaTime * followAngleSpeed;
						else x += deltaTime * followAngleSpeed;
					}
					x = ((x % 360) + 360) % 360;

					if (y - yDefautRotationOffset > 0) y -= deltaTime * followAngleSpeed;
					if (y - yDefautRotationOffset < 0) y += deltaTime * followAngleSpeed;
				}

				// Distance
				if (distanceTarget < followDistance) distanceTarget += deltaTime * followDistanceSpeed;
				if (distanceTarget > followDistance) distanceTarget -= deltaTime * followDistanceSpeed;
			}

			// Rotation
			rotation = Quaternion.AngleAxis(x, Vector3.up) * Quaternion.AngleAxis(y, Vector3.right);

			if (rotationSpace != null) {
				r = Quaternion.FromToRotation(lastUp, rotationSpace.up) * r;
				rotation = r * rotation;

				lastUp = rotationSpace.up;

			}

			if (target != null) {
				// Distance
				distance += (distanceTarget - distance) * zoomSpeed * deltaTime;

				// Smooth follow
				if (!smoothFollow) smoothPosition = target.position;
				else smoothPosition = Vector3.Lerp(smoothPosition, target.position, deltaTime * smoothFollowSpeed);

				// Position
				Vector3 t = smoothPosition + rotation * offset;
				Vector3 f = rotation * -Vector3.forward;

                if (blockingLayers != -1)
                {
                    RaycastHit hit;
                    if (Physics.SphereCast(t - f * blockingOriginOffset, blockingRadius, f, out hit, blockingOriginOffset + distanceTarget - blockingRadius, blockingLayers))
                    {
                        blockedDistance = Mathf.SmoothDamp(blockedDistance, hit.distance + blockingRadius * (1f - blockedOffset) - blockingOriginOffset, ref blockedDistanceV, blockingSmoothTime);
                    }
                    else blockedDistance = distanceTarget;

                    distance = Mathf.Min(distance, blockedDistance);
                }

                position = t + f * distance;

				// Translating the camera
				transform.position = position;
			}

			transform.rotation = rotation;
		}

		// Zoom input
		private float zoomAdd {
			get {
				float scrollAxis = Input.GetAxis("Mouse ScrollWheel");
				if (scrollAxis > 0) return -zoomSensitivity;
				if (scrollAxis < 0) return zoomSensitivity;
				return 0;
			}
		}

		// Clamping Euler angles
		private float ClampAngle (float angle, float min, float max) {
			if (angle < -360) angle += 360;
			if (angle > 360) angle -= 360;
			return Mathf.Clamp (angle, min, max);
		}
		
	}
}

