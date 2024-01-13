#nullable enable
using UnityEngine;

namespace CraftSharp.Rendering
{
    public class ProjectileEntityRender : EntityRender
    {
        public override void UpdateTransform(float tickMilSec)
        {
            // Update position
            if ((Position.Value - transform.position).sqrMagnitude > MOVE_THRESHOLD) // Treat as teleport
                transform.position = Position.Value;
            else // Smoothly move to current position
                transform.position = Vector3.SmoothDamp(transform.position, Position.Value, ref visualMovementVelocity, tickMilSec);

            // Update rotation
            if (lastYaw != Yaw.Value || lastPitch != Pitch.Value)
            {
                lastPitch = Pitch.Value;
                lastYaw = Yaw.Value;

                transform.localEulerAngles = new(-lastPitch, 180F - lastYaw);
            }
        }
    }
}