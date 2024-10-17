using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

namespace CraftSharp
{
    public class Viewer : MonoBehaviour
    {
        public float sensitivityX = 5F;
        public float sensitivityY = 5F;
        public float moveSpeed = 5F;
        [SerializeField] private TMP_Text viewerText;

        void Update()
        {
            float x = Mouse.current.delta.x.value;
            float y = Mouse.current.delta.y.value;

            Vector3 orgPivotEuler = transform.rotation.eulerAngles;

            float camYaw   = orgPivotEuler.y + x * sensitivityX;
            float camPitch = orgPivotEuler.x - y * sensitivityY;

            while (camPitch < 0F)
                camPitch += 360F;

            transform.rotation = Quaternion.Euler(
                camPitch > 180F ? Mathf.Clamp(camPitch, 271F, 360F) : Mathf.Clamp(camPitch, 0F, 89F),
                camYaw,
                0F
            );

            Vector3 moveLocal = Vector3.zero;

            if (Keyboard.current.wKey.isPressed)
                moveLocal += Vector3.forward;

            if (Keyboard.current.aKey.isPressed)
                moveLocal += Vector3.left;

            if (Keyboard.current.sKey.isPressed)
                moveLocal += Vector3.back;

            if (Keyboard.current.dKey.isPressed)
                moveLocal += Vector3.right;
            
            var moveGlobal = transform.TransformVector(moveLocal);

            if (Keyboard.current.spaceKey.isPressed)
                moveGlobal.y =  1F;
            else if (Keyboard.current.shiftKey.isPressed)
                moveGlobal.y = -1F;
            else
                moveGlobal.y =  0F;
            
            if (moveGlobal.magnitude > 0F)
            {
                moveGlobal = moveGlobal.normalized;
                transform.position += moveGlobal * moveSpeed * Time.deltaTime;
            }

            if (viewerText is not null)
            {
                var viewRay = Camera.main.ViewportPointToRay(new(0.5F, 0.5F, 0F));

                RaycastHit viewHit;
                string blockStateInfo;
                if (Physics.Raycast(viewRay.origin, viewRay.direction, out viewHit, 10F))
                    blockStateInfo = viewHit.collider.gameObject.name;
                else
                    blockStateInfo = string.Empty;

                viewerText.text = $"FPS: {(int)(1 / Time.unscaledDeltaTime)}\n{blockStateInfo}";
            }
        }
    }
}