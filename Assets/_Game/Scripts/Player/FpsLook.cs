using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelBrawl.Player
{
    public sealed class FpsLook : MonoBehaviour
    {
        [SerializeField] private Transform cameraTransform;
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Range(1f, 90f)] private float verticalLookLimit = 85f;

        [Header("Recoil")]
        [SerializeField, Min(0f)] private float recoilRecoverySpeed = 12f;

        private float pitch;
        private float recoilPitch;
        private float sensitivityMultiplier = 1f;

        private void Awake()
        {
            if (cameraTransform == null)
            {
                Camera childCamera = GetComponentInChildren<Camera>();
                cameraTransform = childCamera != null ? childCamera.transform : null;
            }
        }

        private void Update()
        {
            Mouse mouse = Mouse.current;
            if (mouse == null || cameraTransform == null || Cursor.lockState != CursorLockMode.Locked)
            {
                return;
            }

            recoilPitch = Mathf.MoveTowards(recoilPitch, 0f, recoilRecoverySpeed * Time.deltaTime);

            Vector2 mouseDelta = mouse.delta.ReadValue() * mouseSensitivity * sensitivityMultiplier;
            pitch = Mathf.Clamp(pitch - mouseDelta.y, -verticalLookLimit, verticalLookLimit);

            cameraTransform.localRotation = Quaternion.Euler(pitch - recoilPitch, 0f, 0f);
            transform.Rotate(Vector3.up, mouseDelta.x, Space.Self);
        }

        public void AddRecoil(float amount)
        {
            recoilPitch = Mathf.Max(0f, recoilPitch + amount);
        }

        public void SetSensitivityMultiplier(float multiplier)
        {
            sensitivityMultiplier = Mathf.Max(0f, multiplier);
        }

        private void OnValidate()
        {
            mouseSensitivity = Mathf.Max(0f, mouseSensitivity);
            verticalLookLimit = Mathf.Clamp(verticalLookLimit, 1f, 90f);
            recoilRecoverySpeed = Mathf.Max(0f, recoilRecoverySpeed);
        }
    }
}
