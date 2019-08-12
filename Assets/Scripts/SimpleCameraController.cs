using UnityEngine;

namespace UnityTemplateProjects {
    public class SimpleCameraController : MonoBehaviour {
        class CameraState {
            public float yaw;
            public float pitch;
            public float roll;
            public float x;
            public float y;
            public float z;

            public void SetFromTransform(Transform t) {
                pitch = t.eulerAngles.x;
                yaw = t.eulerAngles.y;
                roll = t.eulerAngles.z;
                x = t.position.x;
                y = t.position.y;
                z = t.position.z;
            }

            public void Translate(Vector3 translation) {
                Vector3 rotatedTranslation = Quaternion.Euler(pitch, yaw, roll) * translation;

                x += rotatedTranslation.x;
                y += rotatedTranslation.y;
                z += rotatedTranslation.z;
            }

            public void LerpTowards(CameraState target, float positionLerpPct, float rotationLerpPct) {
                yaw = Mathf.Lerp(yaw, target.yaw, rotationLerpPct);
                pitch = Mathf.Lerp(pitch, target.pitch, rotationLerpPct);
                roll = Mathf.Lerp(roll, target.roll, rotationLerpPct);
                
                x = Mathf.Lerp(x, target.x, positionLerpPct);
                y = Mathf.Lerp(y, target.y, positionLerpPct);
                z = Mathf.Lerp(z, target.z, positionLerpPct);
            }

            public void UpdateTransform(Transform t) {
                t.eulerAngles = new Vector3(pitch, yaw, roll);
                t.position = new Vector3(x, y, z);
            }
        }
        
        CameraState m_TargetCameraState = new CameraState();
        CameraState m_InterpolatingCameraState = new CameraState();

        [Header("Movement Settings")]
        [Tooltip("Exponential boost factor on translation, controllable by mouse wheel.")]
        public float boost = 3.5f;
        [Tooltip("Adjust the gravity pull force on the player. This will effect fall speed and jump height.")]
        public float gravityForce = 0.8f;
        [Tooltip("Adjust the jump height of the player. Goes hand in hand with Gravity Force.")]
        public float jumpHeight = 2.0f;

        [Tooltip("Time it takes to interpolate camera position 99% of the way to the target."), Range(0.001f, 1f)]
        public float positionLerpTime = 0.2f;

        [Header("Rotation Settings")]
        [Tooltip("X = Change in mouse position.\nY = Multiplicative factor for camera rotation.")]
        public AnimationCurve mouseSensitivityCurve = new AnimationCurve(new Keyframe(0f, 0.5f, 0f, 5f), new Keyframe(1f, 2.5f, 0f, 0f));
        [Tooltip("Adjust the x rotation sensitivity.")]
        public float xSensitivity = 1.75f;
        [Tooltip("Adjust the y rotation sensitivity.")]
        public float ySensitivity = 1.6f;
        [Tooltip("Time it takes to interpolate camera rotation 99% of the way to the target."), Range(0.001f, 1f)]
        public float rotationLerpTime = 0.01f;

        [Tooltip("Whether or not to invert our Y axis for mouse input to rotation.")]
        public bool invertY = false;

        void OnEnable() {
            m_TargetCameraState.SetFromTransform(transform);
            m_InterpolatingCameraState.SetFromTransform(transform);
            Cursor.lockState = CursorLockMode.Locked;
        }

        Vector3 GetInputTranslationDirection() {
            Vector3 direction = new Vector3();
            
            if (Input.GetKey(KeyCode.W)) {
                direction += Vector3.forward;
            }
            if (Input.GetKey(KeyCode.S)) {
                direction += Vector3.back;
            }
            if (Input.GetKey(KeyCode.A)) {
                direction += Vector3.left;
            }
            if (Input.GetKey(KeyCode.D)) {
                direction += Vector3.right;
            }
            if (Input.GetKey(KeyCode.Q)) {
                direction += Vector3.down;
            }
            if (Input.GetKey(KeyCode.E)) {
                direction += Vector3.up;
            }
            if (Input.GetKeyUp(KeyCode.Space)) {
                direction += Vector3.up * jumpHeight;
            }
            return direction;
        }

        Vector3 GetMouseTranslationKeybing() {
            Vector3 rotation = new Vector3();
            float sensitivityMultiplier = 40.0f;
            if (Input.GetKey(KeyCode.I)) {
                rotation += Vector3.up * (ySensitivity * sensitivityMultiplier) * (invertY ? 1 : -1);
            }
            if (Input.GetKey(KeyCode.K)) {
                rotation -= Vector3.up * (ySensitivity * sensitivityMultiplier) * (invertY ? 1 : -1);
            }
            if (Input.GetKey(KeyCode.J)) {
                rotation -= Vector3.left * (xSensitivity * sensitivityMultiplier) * (invertY ? 1 : -1);
            }
            if (Input.GetKey(KeyCode.L)) {
                rotation += Vector3.left * (xSensitivity * sensitivityMultiplier) * (invertY ? 1 : -1);
            }
            return rotation;
        }

        void getMouseRotation() {
            var mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * (invertY ? 1 : -1));

            var mouseSensitivityFactor = mouseSensitivityCurve.Evaluate(mouseMovement.magnitude);

            m_TargetCameraState.yaw += mouseMovement.x * mouseSensitivityFactor;
            m_TargetCameraState.pitch += mouseMovement.y * mouseSensitivityFactor;
        }
        
        void Update() {
            //Put cursor back into lock mode
            if (Cursor.lockState == CursorLockMode.None) {
                if (Input.GetMouseButton(0) || Input.GetMouseButton(1)) {
                    Cursor.lockState = CursorLockMode.Locked;
                    Time.timeScale = 1;
                }
                if (Input.GetKey(KeyCode.Escape)) {
                    Application.Quit();
                    #if UNITY_EDITOR
                        UnityEditor.EditorApplication.isPlaying = false;
                    #endif
                }
            } else {
                getMouseRotation();

                if (Input.GetKey(KeyCode.Escape)) {
                    Cursor.lockState = CursorLockMode.None;
                    Time.timeScale = 0;
                }
            }
            
            // Translation
            var translation = GetInputTranslationDirection() * Time.deltaTime;
            var keyRotation = GetMouseTranslationKeybing() * Time.deltaTime;


            m_TargetCameraState.yaw += keyRotation.x;
            m_TargetCameraState.pitch += keyRotation.y;

            // Speed up movement when shift key held
            if (Input.GetKey(KeyCode.LeftShift)) {
                translation *= 5.0f;
            }
            
            // Modify movement by a boost factor (defined in Inspector and modified in play mode through the mouse scroll wheel)
            boost += Input.mouseScrollDelta.y * 0.2f;
            translation *= Mathf.Pow(2.0f, boost);

            m_TargetCameraState.Translate(translation);

            // Framerate-independent interpolation
            // Calculate the lerp amount, such that we get 99% of the way to our target in the specified time
            var positionLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / positionLerpTime) * Time.deltaTime);
            var rotationLerpPct = 1f - Mathf.Exp((Mathf.Log(1f - 0.99f) / rotationLerpTime) * Time.deltaTime);
            m_InterpolatingCameraState.LerpTowards(m_TargetCameraState, positionLerpPct, rotationLerpPct);

            m_InterpolatingCameraState.UpdateTransform(transform);
        }
    }
}
