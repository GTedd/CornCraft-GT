#nullable enable
using System;
using UnityEngine;
using MinecraftClient.Event;
using MinecraftClient.Mapping;
using MinecraftClient.Rendering;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (Rigidbody), typeof (EntityRender))]
    [RequireComponent(typeof (PlayerStatusUpdater), typeof (PlayerUserInput))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] public PlayerAbility? playerAbility;

        private CornClient? game;
        private Rigidbody? playerRigidbody;
        private BoxCollider? boxCollider;

        private readonly PlayerUserInputData inputData = new();
        private PlayerUserInput? userInput;
        private PlayerStatusUpdater? statusUpdater;
        public PlayerStatus Status { get => statusUpdater!.Status; }

        private IPlayerState CurrentState = PlayerStates.IDLE;

        private CameraController? camControl;
        private Transform? visualTransform;
        private EntityRender? playerRender;
        private Entity fakeEntity = new(0, EntityType.Player, new());

        [SerializeField] public bool EntityDisabled = false;

        public void DisableEntity()
        {
            EntityDisabled = true;
            // Update components state...
            boxCollider!.enabled = false;
            playerRigidbody!.velocity = Vector3.zero;
            playerRigidbody!.useGravity = false;

            Status.Spectating = true;
        }

        public void EnableEntity()
        {
            // Update and control...
            EntityDisabled = false;
            // Update components state...
            boxCollider!.enabled = true;
            playerRigidbody!.useGravity = true;

            Status.Spectating = false;
        }

        public void ToggleWalkMode()
        {
            Status.WalkMode = !Status.WalkMode;
            CornClient.ShowNotification(Status.WalkMode ? "Switched to walk mode" : "Switched to rush mode");
        }

        private void CheckEntityEnabled()
        {
            if (game!.GetPlayer().GameMode == GameMode.Spectator) // Spectating
                DisableEntity();
            else
                EnableEntity();
        }

        public void SetEntityId(int entityId)
        {
            fakeEntity.ID = entityId;
            // Reassign this entity to refresh
            playerRender!.Entity = fakeEntity;
        }

        public void SetLocation(Location pos)
        {
            transform.position = CoordConvert.MC2Unity(pos);
            Debug.Log($"Position set to {transform.position}");

            CheckEntityEnabled();
        }

        public Location GetLocation() => CoordConvert.Unity2MC(transform.position);

        void Update() => ManagedUpdate(Time.deltaTime);

        public void ManagedUpdate(float interval)
        {
            // Update user input
            userInput!.UpdateInputs(inputData);

            // Update target block selection
            var viewRay = camControl!.ActiveCamera.ViewportPointToRay(new(0.5F, 0.5F, 0F));
            statusUpdater!.UpdateBlockSelection(viewRay);

            // Update player status (in water, grounded, etc)
            statusUpdater.UpdatePlayerStatus(game!.GetWorld(), visualTransform!.forward);

            var status = statusUpdater.Status;

            // Update current player state
            if (CurrentState.ShouldExit(status))
            {
                // Try to exit current state and enter another one
                foreach (var state in PlayerStates.STATES)
                {
                    if (state != CurrentState && state.ShouldEnter(status))
                    {
                        // Exit previous state and enter this state
                        CurrentState = state;
                        break;
                    }
                }
            }

            // Prepare current and target player visual yaw before updating it
            Status.UserInputYaw = AngleConvert.GetYawFromVector2(inputData.horInputNormalized);
            Status.TargetVisualYaw = camControl!.GetCameraYaw() + Status.UserInputYaw;

            Status.CurrentVisualYaw = visualTransform!.eulerAngles.y;

            // Update player physics and transform using updated current state
            CurrentState.UpdatePlayer(interval, inputData, status, playerAbility!, playerRigidbody!);

            // Apply updated visual yaw to visual transform
            visualTransform!.eulerAngles = new(0F, Status.CurrentVisualYaw, 0F);

            // Apply current horizontal velocity to visual render TODO Check and improve
            var horizontalVelocity = playerRigidbody!.velocity;
            horizontalVelocity.y = 0;
            playerRender!.SetCurrentVelocity(horizontalVelocity);

            // Tell server our current position
            var rawLocation = CoordConvert.Unity2MC(transform.position);

            // Preprocess the location before sending it (to avoid position correction from server)
            if ((status.Grounded || status.CenterDownDist < 0.5F) && rawLocation.Y - (int)rawLocation.Y > 0.9D)
                rawLocation.Y = (int)rawLocation.Y + 1;

            CornClient.Instance.SyncLocation(rawLocation, visualTransform!.eulerAngles.y - 90F, 0F);

            // Update render
            playerRender!.UpdateInfoPlate(game!.GetCameraPosition(), 0F); // Use 0 as distance to camera since it doesn't matter
            playerRender!.UpdateAnimation(game!.GetTickMilSec());

        }

        private Action<PerspectiveUpdateEvent>? perspectiveCallback;
        private Action<GameModeUpdateEvent>? gameModeCallback;

        void Start()
        {
            camControl = GameObject.FindObjectOfType<CameraController>();
            game = CornClient.Instance;
            
            // Initialize player visuals
            visualTransform = transform.Find("Visual");
            playerRender    = GetComponent<EntityRender>();

            fakeEntity.Name = game!.GetUsername();
            fakeEntity.ID   = 0;
            playerRender.Entity = fakeEntity;

            boxCollider = transform.Find("Collider").GetComponent<BoxCollider>();
            playerRigidbody   = GetComponent<Rigidbody>();

            statusUpdater = GetComponent<PlayerStatusUpdater>();
            userInput = GetComponent<PlayerUserInput>();

            perspectiveCallback = (e) => { };

            gameModeCallback = (e) => {
                if (e.newGameMode != GameMode.Spectator && game!.LocationReceived)
                    EnableEntity();
                else
                    DisableEntity();
            };

            EventManager.Instance.Register(perspectiveCallback);
            EventManager.Instance.Register(gameModeCallback);

        }

        void OnDestroy()
        {
            if (perspectiveCallback is not null)
                EventManager.Instance.Unregister(perspectiveCallback);

            if (gameModeCallback is not null)
                EventManager.Instance.Unregister(gameModeCallback);
        }

        public string GetDebugInfo()
        {
            string targetBlockInfo = string.Empty;
            var loc = CoordConvert.Unity2MC(transform.position);
            var world = game!.GetWorld();

            var status = statusUpdater!.Status;

            if (status.TargetBlockPos is not null)
            {
                var targetBlockState = world?.GetBlock(status.TargetBlockPos.Value).State;
                if (targetBlockState is not null)
                    targetBlockInfo = targetBlockState.ToString();
            }

            var velocity = playerRigidbody!.velocity;
            // Visually swap xz velocity to fit vanilla
            var veloInfo = $"Vel:\t{velocity.z:0.00}\t{velocity.y:0.00}\t{velocity.x:0.00}\n({velocity.magnitude:0.000})";

            if (EntityDisabled)
                return $"Position:\t{loc}\nState:\t{CurrentState}\n{veloInfo}\nTarget Block:\t{status.TargetBlockPos}\n{targetBlockInfo}\nBiome:\n[{world?.GetBiomeId(loc)}] {world?.GetBiome(loc).GetDescription()}";
            else
                return $"Position:\t{loc}\nState:\t{CurrentState}\n{veloInfo}\n{status.ToString()}\nTarget Block:\t{status.TargetBlockPos}\n{targetBlockInfo}\nBiome:\n[{world?.GetBiomeId(loc)}] {world?.GetBiome(loc).GetDescription()}";

        }

    }
}
