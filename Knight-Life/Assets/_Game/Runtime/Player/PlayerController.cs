namespace KnightLife.Runtime.Player
{
    using Unity.Netcode;
    using UnityEngine;
    using UnityEngine.InputSystem;
    using UnityEngine.SocialPlatforms;

    /// <summary>
    /// PlayerController is responsible for handling input for the local player
    /// </summary>
    public class PlayerController : NetworkBehaviour
    {
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;

        [SerializeField] private PlayerInputReader inputReader;
        private PlayerInput input;

        [Header("Movement")]
        [SerializeField] private float moveSpeed = 6f;
        [SerializeField] private float sprintMultiplier = 1.5f;

        [Header("Jump / Gravity")]
        [SerializeField] private float gravity = -20f;
        [SerializeField] private float jumpHeight = 1.2f;

        [Header("Look Settings")]
        [SerializeField] private float lookSensitivity = 0.1f;
        [SerializeField] private float maxPitch = 85f;

        private CharacterController controller;
        private float verticalVelocity;

        public override void OnNetworkSpawn()
        {
            input = GetComponent<PlayerInput>();
            playerCamera.gameObject.SetActive(IsOwner);
            inputReader.enabled = IsOwner;
            input.enabled = IsOwner;
            if (!IsOwner)
            {
                return;
            }

            
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        private void Awake()
        {
            controller = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!IsOwner)
            {
                return;
            }

            Debug.Log($"Owner={IsOwner} IsPlayer={IsLocalPlayer} Move={inputReader.Move} Look={inputReader.Look} Jump={inputReader.JumpPressed}");


            Move();
            Look();
        }

        private void Move()
        {
            if (controller.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            Vector3 move = transform.right * inputReader.Move.x + transform.forward * inputReader.Move.y;

            float speed = inputReader.SprintHeld ? moveSpeed * sprintMultiplier : moveSpeed;
            move *= speed;

            if (controller.isGrounded && inputReader.JumpPressed)
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);

            verticalVelocity += gravity * Time.deltaTime;
            move.y = verticalVelocity;

            controller.Move(move * Time.deltaTime);

            inputReader.ClearOneFrameFlags();
        }

        private float pitch;
        private void Look()
        {
            Vector2 lookDelta = inputReader.Look * lookSensitivity;

            // Yaw: rotate player left/right
            gameObject.transform.Rotate(Vector3.up * lookDelta.x);

            // Pitch: rotate camera up/down
            pitch -= lookDelta.y;
            pitch = Mathf.Clamp(pitch, -maxPitch, maxPitch);

            playerCamera.transform.localRotation = Quaternion.Euler(pitch, 0f, 0f);
        }
    }
}
