using UnityEngine;
using System.Collections;

[RequireComponent(typeof(CharacterController))]
public class CameraController : MonoBehaviour {

    [Header("Player Movement Mechanics")]
        [Tooltip("Walking Speed adjusts the base speed at which the player will move.")]
        [SerializeField] private float player_walkSpeed = 4.0f;
        [Tooltip("Running Speed adjusts the max speed at which the player will move.")]
        [SerializeField] private float player_runSpeed = 6.0f;
        [Tooltip("If true, diagonal speed (when strafing + moving forward or back) can't exceed normal move speed; If false, it's about 1.4 times faster.")]
        [SerializeField] public bool player_limitDiagonalSpeed = true;
        [Tooltip("If checked, the run key toggles between running and walking. Otherwise player runs if the key is held down.")]
        [SerializeField] private bool player_ToggleRun = false;
        [Tooltip("If the player ends up on a slope which is >= the Slope Limit as set on the character controller, then the player will slide down.")]
        [SerializeField] private bool player_slideWhenOverSlopeLimit = false;
        [Tooltip("If checked and the player is on an object tagged \"Slide\", he will slide down it regardless of the slope limit.")]
        [SerializeField] private bool player_slideOnTaggedObjects = false;   //this will let us do cool slide areas for puzzles
        [Tooltip("How fast the player slides when on slopes as defined above.")]
        [SerializeField] private float player_slideSpeed = 12.0f;

    [Header("Player Physics Adjustsments")]
        [Tooltip("How high the player can jump")]
        [SerializeField] private float player_jumpSpeed = 8.0f;
        [Tooltip("How fast the player falls when not standing on anything.")]
        [SerializeField] private float player_gravity = 20.0f;
        [Tooltip("Units that player can fall before a falling function is run. To disable, type \"infinity\" in the inspector.")]
        [SerializeField] private float player_fallingThreshold = 10.0f;
        [Tooltip("If checked, then the player can change direction while in the air.")]
        [SerializeField] private bool player_airControl = false;
        [Tooltip("Small amounts of this results in bumping when walking down slopes, but large amounts results in falling too fast.")]
        [SerializeField] private float player_AntiBumpFactor = .75f;
        [Tooltip("Player jump cooldown time. Set to 0 to allow for bunny hopping.")]
        [SerializeField] private int player_jumpCooldownTime = 0;

    [Header("Camera Movement Mechanics")]
        [Tooltip("Camera Horizontal rotation sensitivity")]
        [SerializeField] private float camera_xSensitivity = 0.75f;
        [Tooltip("Camera Vertizal rotation sensitivity")]
        [SerializeField] private float camera_ySensitivity = 0.6f;
        [Tooltip("Camera Sensitivity multiplier")]
        [SerializeField] private float camera_sensitivityMul = 2.0f;
        [Tooltip("Camera minimum pitch rotation (vertical-down)")]
        [SerializeField] private float camera_pitchMin;
        [Tooltip("Camera maximum pitch rotation (vertical-up)")]
        [SerializeField] private float camera_pitchMax;
        [Tooltip("Flip the rotation calculations (up goes down, down goes up)")]
        [SerializeField] private bool invertY = false;
        [Tooltip("Force cursor render")]
        [SerializeField] private bool forceCursorRender = false;
        [Tooltip("Force Camera No Clip mode")]
        [SerializeField] private bool forceNoClip = false;

    private Vector3 player_moveDirection = Vector3.zero;
    private bool player_isGrounded = false;
    private CharacterController playerController;
    private Transform playerTransform;
    private Vector2 cameraRotation;
    private float player_moveSpeed;
    private float player_fallStartLevel;
    private bool player_isFalling;
    private float player_SlideLimit;
    private RaycastHit camera_RaycastHit;
    private float camera_RaycastDistance;
    private Vector3 player_ContactPoint;
    private bool playerControl = false;
    private int player_JumpTimer;
    private CameraState m_TargetCameraState = new CameraState();

    private void Awake() {
        m_TargetCameraState.SetFromTransform(transform);
        Cursor.lockState = (forceCursorRender == false ? CursorLockMode.Locked : CursorLockMode.None);
    }

    private void Start() {
        // Saving component references to improve performance.
        playerTransform = GetComponent<Transform>();
        playerController = GetComponent<CharacterController>();

        // Setting initial values.
        player_moveSpeed = player_walkSpeed;
        camera_RaycastDistance = playerController.height * .5f + playerController.radius;
        player_SlideLimit = playerController.slopeLimit - .1f;
        player_JumpTimer = player_jumpCooldownTime;
    }

    private void Update() {
        // If the run button is set to toggle, then switch between walk/run speed. (We use Update for this...
        // FixedUpdate is a poor place to use GetButtonDown, since it doesn't necessarily run every frame and can miss the event)
        if (player_ToggleRun && player_isGrounded && Input.GetButtonDown("Run")) {
            player_moveSpeed = (player_moveSpeed == player_walkSpeed ? player_runSpeed : player_walkSpeed);
        }

        if (forceCursorRender == true) { //if cursor is unlocked
            if (Input.GetMouseButton(0) || Input.GetMouseButton(1)) {   //if the cursor is unlocked, click on screen to relock
                Cursor.lockState = CursorLockMode.Locked;
                forceCursorRender = false;
                Time.timeScale = 1; //unpause game
            }
            if (Input.GetKeyUp(KeyCode.Escape)) {
                Application.Quit();
                #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
                #endif
            }
        } else {    //if cursor is locked
            getMouseRotation(); //calculate pitch roll and yaw

            if (Input.GetKeyUp(KeyCode.Escape)) { //escape from locked cursor
                Cursor.lockState = CursorLockMode.None;
                forceCursorRender = true;
                Time.timeScale = 0; //pause the game too
            }
        }
    }

    private void getMouseRotation() {
        var mouseMovement = new Vector2(Input.GetAxis("Mouse X"), Input.GetAxis("Mouse Y") * (invertY ? 1 : -1));

        cameraRotation.x += mouseMovement.x * camera_sensitivityMul;
        cameraRotation.y += mouseMovement.y * camera_sensitivityMul;
    }

    private void FixedUpdate() {
        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxis("Vertical");

        // If both horizontal and vertical are used simultaneously, limit speed (if allowed), so the total doesn't exceed normal move speed
        float inputModifyFactor = (inputX != 0.0f && inputY != 0.0f && player_limitDiagonalSpeed) ? .7071f : 1.0f;

        if (player_isGrounded) {
            bool sliding = false;
            // See if surface immediately below should be slid down. We use this normally rather than a ControllerColliderHit point,
            // because that interferes with step climbing amongst other annoyances
            if (Physics.Raycast(playerTransform.position, -Vector3.up, out camera_RaycastHit, camera_RaycastDistance)) {
                if (Vector3.Angle(camera_RaycastHit.normal, Vector3.up) > player_SlideLimit) {
                    sliding = true;
                }
            }
            // However, just raycasting straight down from the center can fail when on steep slopes
            // So if the above raycast didn't catch anything, raycast down from the stored ControllerColliderHit point instead
            else {
                Physics.Raycast(player_ContactPoint + Vector3.up, -Vector3.up, out camera_RaycastHit);
                if (Vector3.Angle(camera_RaycastHit.normal, Vector3.up) > player_SlideLimit) {
                    sliding = true;
                }
            }

            // If we were falling, and we fell a vertical distance greater than the threshold, run a falling damage routine
            if (player_isFalling) {
                player_isFalling = false;
                if (playerTransform.position.y < player_fallStartLevel - player_fallingThreshold) {
                    OnFell(player_fallStartLevel - playerTransform.position.y);
                }
            }

            // If running isn't on a toggle, then use the appropriate speed depending on whether the run button is down
            if (!player_ToggleRun) {
                player_moveSpeed = Input.GetKey(KeyCode.LeftShift) ? player_runSpeed : player_walkSpeed;
            }

            // If sliding (and it's allowed), or if we're on an object tagged "Slide", get a vector pointing down the slope we're on
            if ((sliding && player_slideWhenOverSlopeLimit) || (player_slideOnTaggedObjects && camera_RaycastHit.collider.tag == "Slide")) {
                Vector3 hitNormal = camera_RaycastHit.normal;
                player_moveDirection = new Vector3(hitNormal.x, -hitNormal.y, hitNormal.z);
                Vector3.OrthoNormalize(ref hitNormal, ref player_moveDirection);
                player_moveDirection *= player_slideSpeed;
                playerControl = false;
            }
            // Otherwise recalculate moveDirection directly from axes, adding a bit of -y to avoid bumping down inclines
            else {
                player_moveDirection = new Vector3(inputX * inputModifyFactor, -player_AntiBumpFactor, inputY * inputModifyFactor);
                player_moveDirection = playerTransform.TransformDirection(player_moveDirection) * player_moveSpeed;
                playerControl = true;
            }

            // Jump! But only if the jump button has been released and player has been grounded for a given number of frames
            if (!Input.GetButton("Jump")) {
                player_JumpTimer++;
            } else if (player_JumpTimer >= player_jumpCooldownTime) {
                player_moveDirection.y = player_jumpSpeed;
                player_JumpTimer = 0;
            }
        } else {
            // If we stepped over a cliff or something, set the height at which we started falling
            if (!player_isFalling) {
                player_isFalling = true;
                player_fallStartLevel = playerTransform.position.y;
            }

            // If air control is allowed, check movement but don't touch the y component
            if (player_airControl && playerControl) {
                player_moveDirection.x = inputX * player_moveSpeed * inputModifyFactor;
                player_moveDirection.z = inputY * player_moveSpeed * inputModifyFactor;
                player_moveDirection = playerTransform.TransformDirection(player_moveDirection);
            }
        }

        // Apply gravity
        player_moveDirection.y -= player_gravity * Time.deltaTime;

        // Move the controller, and set grounded true or false depending on whether we're standing on something
        player_isGrounded = (playerController.Move(player_moveDirection * Time.deltaTime) & CollisionFlags.Below) != 0;
    }

    // Store point that we're in contact with for use in FixedUpdate if needed
    private void OnControllerColliderHit(ControllerColliderHit hit) {
        player_ContactPoint = hit.point;
    }

    // This is the place to apply things like fall damage. You can give the player hitpoints and remove some
    // of them based on the distance fallen, play sound effects, etc.
    private void OnFell(float fallDistance) {
        print("Ouch! You fell " + fallDistance + " units!");
    }
}

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
