using UnityEngine;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (CameraController))]
    public class CameraUserControl : MonoBehaviour
    {
        private CornClient game;
        CameraController camControl;
        private bool scrollLocked = false;

        void Start()
        {
            game = CornClient.Instance;
            camControl = GetComponent<CameraController>();
        }

        void Update()
        {
            var paused = game.IsPaused();

            float x = paused ? 0F : Input.GetAxis("Mouse X");
            float y = paused ? 0F : Input.GetAxis("Mouse Y");
            camControl.Tick(Time.deltaTime, x, y);

            if (!scrollLocked)
            {
                ScrollCamera(Input.GetAxis("Mouse ScrollWheel"));
            }

            if (Input.GetKeyDown(KeyCode.F5))
            {
                camControl.ToggleFixedMode();
            }
        }

        void LateUpdate()
        {
            var paused = game.IsPaused();

            float x = paused ? 0F : Input.GetAxis("Mouse X");
            float y = paused ? 0F : Input.GetAxis("Mouse Y");
            camControl.LateTick(Time.deltaTime, x, y);
        }

        public void ScrollCamera(float scroll)
        {
            if (!camControl) return;
            camControl.Scroll(scroll);
        }
    }
}
