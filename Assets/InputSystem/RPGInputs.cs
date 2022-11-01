using UnityEngine;
using UnityEngine.InputSystem;

namespace Kolman_Freecss.Krodun
{
    public class RPGInputs : MonoBehaviour
    {
        [Header("Character Input Values")] public bool click;
        public Vector2 move;
        public Vector2 look;
        public bool jump;
        public bool sprint;

        [Header("Movement Settings")] public bool analogMovement;

        [Header("Mouse Cursor Settings")] public bool cursorLocked = true;
        public bool cursorInputForLook = true;

        private MenuManager _menuManager;

        private void Awake()
        {
            if (_menuManager == null)
            {
                _menuManager = FindObjectOfType<MenuManager>();
            }
        }

        public void OnMove(InputValue value)
        {
            MoveInput(value.Get<Vector2>());
        }

        public void OnLook(InputValue value)
        {
            if (cursorInputForLook)
            {
                LookInput(value.Get<Vector2>());
            }
        }

        public void OnJump(InputValue value)
        {
            JumpInput(value.isPressed);
        }

        public void OnSprint(InputValue value)
        {
            SprintInput(value.isPressed);
        }


        public void MoveInput(Vector2 newMoveDirection)
        {
            move = newMoveDirection;
        }

        public void LookInput(Vector2 newLookDirection)
        {
            look = newLookDirection;
        }

        public void JumpInput(bool newJumpState)
        {
            jump = newJumpState;
        }

        public void SprintInput(bool newSprintState)
        {
            sprint = newSprintState;
        }

        public void OnMenu(InputValue value)
        {
            if (value.isPressed)
            {
                _menuManager.ToggleMenu();
            }
        }

        public void OnFire(InputValue value)
        {
            FireInput(value.isPressed);
        }

        public void FireInput(bool newFireState)
        {
            click = newFireState;
        }

        /*private void OnApplicationFocus(bool hasFocus)
        {
            SetCursorState(cursorLocked);
        }*/

        /*private void SetCursorState(bool newState)
        {
            Cursor.lockState = newState ? CursorLockMode.Locked : CursorLockMode.None;
        }*/
    }
}