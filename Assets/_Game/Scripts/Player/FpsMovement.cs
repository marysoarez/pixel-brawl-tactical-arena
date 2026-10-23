using UnityEngine;
using UnityEngine.InputSystem;
using PixelBrawl.Abilities;

namespace PixelBrawl.Player
{
    [RequireComponent(typeof(CharacterController))]
    public sealed class FpsMovement : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField, Min(0f)] private float moveSpeed = 6f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.25f;
        [SerializeField, Min(0f)] private float gravityMultiplier = 2f;

        private CharacterController characterController;
        private DashAbility dashAbility;
        private PlayerActionGate actionGate;
        private float verticalVelocity;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            dashAbility = GetComponent<DashAbility>();
            actionGate = GetComponent<PlayerActionGate>();
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector2 input = ReadMovementInput(keyboard);
            Vector3 horizontalVelocity = (transform.right * input.x + transform.forward * input.y) * moveSpeed;

            dashAbility?.Tick();
            if (dashAbility != null && keyboard.leftShiftKey.wasPressedThisFrame &&
                (actionGate == null || actionGate.CanUseCombat))
            {
                Vector3 requestedDirection = horizontalVelocity.sqrMagnitude > 0.001f
                    ? horizontalVelocity.normalized
                    : transform.forward;
                dashAbility.TryActivate(requestedDirection);
            }

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (characterController.isGrounded && keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * Physics.gravity.y * gravityMultiplier);
            }

            verticalVelocity += Physics.gravity.y * gravityMultiplier * Time.deltaTime;
            Vector3 frameVelocity = dashAbility != null && dashAbility.IsActive
                ? dashAbility.Velocity
                : horizontalVelocity;
            frameVelocity += Vector3.up * verticalVelocity;

            CollisionFlags collisionFlags = characterController.Move(frameVelocity * Time.deltaTime);
            if (dashAbility != null && dashAbility.IsActive && (collisionFlags & CollisionFlags.Sides) != 0)
            {
                dashAbility.StopDash();
            }
        }

        private static Vector2 ReadMovementInput(Keyboard keyboard)
        {
            float horizontal = 0f;
            float vertical = 0f;

            if (keyboard.aKey.isPressed) horizontal -= 1f;
            if (keyboard.dKey.isPressed) horizontal += 1f;
            if (keyboard.sKey.isPressed) vertical -= 1f;
            if (keyboard.wKey.isPressed) vertical += 1f;

            return Vector2.ClampMagnitude(new Vector2(horizontal, vertical), 1f);
        }
    }
}
