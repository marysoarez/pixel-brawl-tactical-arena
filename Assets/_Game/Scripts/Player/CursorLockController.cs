using UnityEngine;
using UnityEngine.InputSystem;

namespace PixelBrawl.Player
{
    public sealed class CursorLockController : MonoBehaviour
    {
        [SerializeField] private bool lockCursorOnStart = true;
        private PlayerActionGate actionGate;

        private void Awake()
        {
            actionGate = GetComponent<PlayerActionGate>();
        }

        private void Start()
        {
            if (lockCursorOnStart)
            {
                SetCursorLocked(true);
            }
        }

        private void Update()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                SetCursorLocked(false);
            }
            else if (mouse != null && mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked &&
                     (actionGate == null || !actionGate.ShopOpen))
            {
                SetCursorLocked(true);
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                SetCursorLocked(false);
            }
        }

        private static void SetCursorLocked(bool isLocked)
        {
            Cursor.lockState = isLocked ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !isLocked;
        }
    }
}
