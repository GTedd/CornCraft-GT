#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine;

using MinecraftClient.Event;
using MinecraftClient.Mapping;
using MinecraftClient.Rendering;

namespace MinecraftClient.Control
{
    [RequireComponent(typeof (Rigidbody), typeof (EntityRender), typeof (PlayerStatusUpdater))]
    public class PlayerController : MonoBehaviour, IPlayerController
    {
        [SerializeField] public PlayerAbility? ability;
        public PlayerAbility Ability => ability!;

        [SerializeField] public PlayerMeleeAttack? meleeAttack;
        public PlayerMeleeAttack MeleeAttack => meleeAttack!;
        
        [SerializeField] public Transform? cameraRef;
        [SerializeField] public Transform? visualTransform;
        [SerializeField] public PhysicMaterial? physicMaterial;

        private CornClient? client;
        private CameraController? cameraController;
        private Rigidbody? playerRigidbody;
        public Rigidbody PlayerRigidbody => playerRigidbody!;
        private Collider? playerCollider;
        public Collider PlayerCollider => playerCollider!;

        private readonly PlayerUserInputData inputData = new();
        private PlayerUserInput? userInput;
        private PlayerStatusUpdater? statusUpdater;
        public PlayerStatus? Status => statusUpdater?.Status;

        private PlayerInteractionUpdater? interactionUpdater;

        private IPlayerState CurrentState = PlayerStates.IDLE;
        [HideInInspector] public bool UseRootMotion = false;
        private AnimatorEntityRender? playerRender;
        private PlayerAccessoryWidget? accessoryWidget;
        public PlayerAccessoryWidget AccessoryWidget => accessoryWidget!;

        public void SetClientAndCamera(CornClient client, CameraController camController)
        {
            this.client = client;
            this.cameraController = camController;
        }

        public void DisableEntity()
        {
            // Update components state...
            playerCollider!.enabled = false;
            playerRigidbody!.useGravity = false;

            // Reset player status
            playerRigidbody!.velocity = Vector3.zero;
            Status!.Grounded  = false;
            Status.InLiquid  = false;
            Status.OnWall    = false;
            Status.Sprinting = false;

            Status.Spectating = true;
            Status.GravityDisabled = true;
        }

        public void EnableEntity()
        {
            // Update components state...
            playerCollider!.enabled = true;
            playerRigidbody!.useGravity = true;

            Status!.Spectating = false;
            Status.GravityDisabled = false;
        }

        public void ToggleWalkMode()
        {
            Status!.WalkMode = !Status.WalkMode;
            CornApp.ShowNotification(Status.WalkMode ? "Switched to walk mode" : "Switched to rush mode");
        }

        public void CrossFadeState(string stateName, float time = 0.2F, int layer = 0, float timeOffset = 0F)
        {
            playerRender!.CrossFadeState(stateName, time, layer, timeOffset);
        }

        public void RandomizeMirroredFlag()
        {
            var mirrored = Time.frameCount % 2 == 0;
            playerRender!.SetMirroredFlag(mirrored);
            //Debug.Log($"Animation mirrored: {mirrored}");
        }

        public void SetLocation(Location loc, bool resetVelocity = false, float yaw = 0F)
        {
            if (playerRigidbody is null) return;

            if (resetVelocity) // Reset rigidbody velocity
                playerRigidbody.velocity = Vector3.zero;
            
            playerRigidbody.position = CoordConvert.MC2Unity(loc);
            visualTransform!.eulerAngles = new(0F, yaw + 90F, 0F);
            cameraController?.SetYaw(yaw + 90F);

            Debug.Log($"Position set to {playerRigidbody.position} with yaw {yaw}");
        }

        void Update() => LogicalUpdate(Time.deltaTime);

        void FixedUpdate() => PhysicalUpdate(Time.fixedDeltaTime);

        public void LogicalUpdate(float interval)
        {
            if (!client!.IsMovementReady())
            {
                playerRigidbody!.useGravity = false;
                playerRigidbody!.velocity = Vector3.zero;

                Status!.GravityDisabled = true;

                lock (client.movementLock)
                {
                    transform.position = CoordConvert.MC2Unity(client.PlayerEntity.Location);
                }
            }
            else
            {
                if (Status!.GravityDisabled != Status.Spectating)
                {
                    playerRigidbody!.useGravity = !Status.Spectating;
                    Status.GravityDisabled = !Status.Spectating;
                }
            }

            // Update user input
            userInput!.UpdateInputs(inputData, client.Perspective);

            // Update player status (in water, grounded, etc)
            if (!Status.Spectating)
                statusUpdater!.UpdatePlayerStatus(client!.GetWorld(), visualTransform!.forward);

            // Update target block selection
            interactionUpdater!.UpdateBlockSelection(cameraController!.GetViewportCenterRay());

            // Update player interactions
            interactionUpdater.UpdateInteractions(client!.GetWorld());

            var status = statusUpdater!.Status;

            // Update current player state
            if (CurrentState.ShouldExit(inputData, status))
            {
                // Try to exit current state and enter another one
                foreach (var state in PlayerStates.STATES)
                {
                    if (state != CurrentState && state.ShouldEnter(inputData, status))
                    {
                        CurrentState.OnExit(status, playerRigidbody!, this);

                        // Exit previous state and enter this state
                        CurrentState = state;
                        
                        CurrentState.OnEnter(status, playerRigidbody!, this);
                        break;
                    }
                }
            }

            Status.CurrentVisualYaw = visualTransform!.eulerAngles.y;

            float prevStamina = status.StaminaLeft;

            // Prepare current and target player visual yaw before updating it
            if (inputData.horInputNormalized != Vector2.zero)
            {
                Status.UserInputYaw = getYawFromVector2(inputData.horInputNormalized);
                Status.TargetVisualYaw = cameraController!.GetYaw() + Status.UserInputYaw;
            }
            
            // Update player physics and transform using updated current state
            CurrentState.UpdatePlayer(interval, inputData, status, playerRigidbody!, this);

            // Broadcast current stamina if changed
            if (prevStamina != status.StaminaLeft)
                EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(status.StaminaLeft, status.StaminaLeft >= ability!.MaxStamina));

            // Apply updated visual yaw to visual transform
            visualTransform!.eulerAngles = new(0F, Status.CurrentVisualYaw, 0F);
            
            // Update player render state machine
            playerRender!.UpdateStateMachine(status);
            // Update player render velocity
            playerRender.SetVisualMovementVelocity(playerRigidbody!.velocity);

            // Update render
            playerRender!.UpdateAnimation(client!.GetTickMilSec());

            // Tell server our current position
            Location newLocation;

            if (CurrentState is ForceMoveState)
            // Use move origin as the player location to tell to server, to
            // prevent sending invalid positions during a force move operation
                newLocation = CoordConvert.Unity2MC(((ForceMoveState) CurrentState).GetFakePlayerOffset());
            else
                newLocation = CoordConvert.Unity2MC(transform.position);

            // Preprocess the location before sending it (to avoid position correction from server)
            if ((status.Grounded || status.CenterDownDist < 0.5F) && newLocation.Y - (int)newLocation.Y > 0.9D)
                newLocation.Y = (int)newLocation.Y + 1;

            // Update client player data
            lock (client.movementLock)
            {
                var playerEntity = client.PlayerEntity;

                // Update player location
                playerEntity.Location = newLocation;

                // Update player yaw and pitch
                var newYaw = visualTransform!.eulerAngles.y - 90F;
                var newPitch = 0F;
                if (playerEntity.Yaw != newYaw || playerEntity.Pitch != newPitch)
                {
                    client.YawToSend = newYaw;
                    playerEntity.Yaw = newYaw;
                    client.PitchToSend = newPitch;
                    playerEntity.Pitch = newPitch;
                }
                
                client.Grounded = Status.Grounded;
                
            }
        }

        public void PhysicalUpdate(float interval)
        {
            var info = Status!;

            if (info.MoveVelocity != Vector3.zero)
            {
                // The player is actively moving
                playerRigidbody!.AddForce((info.MoveVelocity - playerRigidbody!.velocity) * interval * 10F, ForceMode.VelocityChange);
            }
            else
            {
                if (info.Spectating)
                    playerRigidbody!.velocity = Vector3.zero;
                
                // Otherwise leave the player rigidbody untouched
            }
        }

        public void StartForceMoveOperation(string name, ForceMoveOperation[] ops)
        {
            CurrentState.OnExit(statusUpdater!.Status, playerRigidbody!, this);
            // Enter a new force move state
            CurrentState = new ForceMoveState(name, ops);
            CurrentState.OnEnter(statusUpdater!.Status, playerRigidbody!, this);
        }

        public bool TryStartAttack()
        {
            if (statusUpdater!.Status.AttackStatus.AttackCooldown <= 0F)
            {
                CurrentState.OnExit(statusUpdater!.Status, playerRigidbody!, this);
                // Enter melee attack state
                CurrentState = PlayerStates.MELEE;

                CurrentState.OnEnter(statusUpdater!.Status, playerRigidbody!, this);

                return true;
            }
            
            return false;
        }

        public void TurnToAttackTarget()
        {
            var entityManager = client!.EntityRenderManager;
            var nearbyEntities = entityManager?.GetNearbyEntities();

            if (nearbyEntities is null || nearbyEntities.Count == 0) // Nothing to do
                return;
            
            float minDist = float.MaxValue;
            Vector3? targetPos = null;

            foreach (var pair in nearbyEntities)
            {
                if (pair.Value < minDist)
                {
                    var render = entityManager?.GetEntityRender(pair.Key);

                    if (render!.Entity.Type.ContainsItem) // Not a valid target
                        continue;

                    var pos = render.transform.position;
                    
                    if (pair.Value <= 16F && pos.y - transform.position.y < 2F)
                        targetPos = pos;
                }
            }

            if (targetPos is null) // Failed to get attack target's position, do nothing
                return;

            var posOffset = targetPos.Value - transform.position;

            var attackYaw = getYawFromVector2(new(posOffset.x, posOffset.z));

            statusUpdater!.Status.TargetVisualYaw = attackYaw;
            statusUpdater!.Status.CurrentVisualYaw = attackYaw;

            visualTransform!.eulerAngles = new(0F, attackYaw, 0F);

        }

        public void AttackDamage(bool enable)
        {
            if (statusUpdater!.Status.Attacking)
            {
                statusUpdater!.Status.AttackStatus.CausingDamage = enable;
            }
            else
            {
                Debug.LogWarning("Trying to toggle attack damage when not attacking!");
            }
        }

        public void DealDamage(List<AttackHitInfo> hitInfos)
        {
            foreach (var hitInfo in hitInfos)
            {
                // Send attack packets to server
                var entityId = hitInfo.EntityRender?.Entity?.ID;

                if (entityId is not null)
                    client!.InteractEntity(entityId.Value, 1);

            }
        }

        public void ClearAttackCooldown()
        {
            if (statusUpdater!.Status.Attacking)
            {
                statusUpdater!.Status.AttackStatus.AttackCooldown = 0F;
            }
            else
            {
                Debug.LogWarning("Trying to clear attack cooldown when not attacking!");
            }
        }

        private float getYawFromVector2(Vector2 direction)
        {
            if (direction.y > 0F)
                return Mathf.Atan(direction.x / direction.y) * Mathf.Rad2Deg;
            else if (direction.y < 0F)
                return Mathf.Atan(direction.x / direction.y) * Mathf.Rad2Deg + 180F;
            else
                return direction.x > 0 ? 90F : 270F;
        }
 
        private Action<PerspectiveUpdateEvent>? perspectiveCallback;
        private Action<GameModeUpdateEvent>? gameModeCallback;

        void Start()
        {
            // Create player entity
            var playerEntity = CornApp.CurrentClient!.PlayerEntity;

            // Initialize player visuals
            playerRender = GetComponent<AnimatorEntityRender>();
            playerRender.Initialize(playerEntity.Type, playerEntity);

            playerRigidbody = GetComponent<Rigidbody>();
            playerRigidbody.useGravity = true;

            statusUpdater = GetComponent<PlayerStatusUpdater>();
            userInput = GetComponent<PlayerUserInput>();
            interactionUpdater = GetComponent<PlayerInteractionUpdater>();
            accessoryWidget = visualTransform!.GetComponent<PlayerAccessoryWidget>();

            perspectiveCallback = (e) => { };

            gameModeCallback = (e) => {
                if (e.GameMode != GameMode.Spectator && client!.LocationReceived)
                    EnableEntity();
                else
                    DisableEntity();
            };

            EventManager.Instance.Register(perspectiveCallback);
            EventManager.Instance.Register(gameModeCallback);

            if (ability is null)
                Debug.LogError("Player ability not assigned!");
            else
            {
                var boxcast = ability.ColliderType == PlayerAbility.PlayerColliderType.Box;

                if (boxcast)
                {
                    // Attach box collider
                    var box = gameObject.AddComponent<BoxCollider>();
                    var sideLength = ability.ColliderRadius * 2F;
                    box.size = new(sideLength, ability.ColliderHeight, sideLength);
                    box.center = new(0F, ability.ColliderHeight / 2F, 0F);

                    statusUpdater.GroundBoxcastHalfSize = new(ability.ColliderRadius, 0.01F, ability.ColliderRadius);

                    playerCollider = box;
                }
                else
                {
                    // Attach capsule collider
                    var capsule = gameObject.AddComponent<CapsuleCollider>();
                    capsule.height = ability.ColliderHeight;
                    capsule.radius = ability.ColliderRadius;
                    capsule.center = new(0F, ability.ColliderHeight / 2F, 0F);

                    statusUpdater.GroundSpherecastRadius = ability.ColliderRadius;
                    statusUpdater.GroundSpherecastCenter = new(0F, ability.ColliderRadius + 0.05F, 0F);

                    playerCollider = capsule;
                }

                playerCollider.material = physicMaterial;

                statusUpdater.UseBoxCastForGroundedCheck = boxcast;

                // Set stamina to max value
                Status!.StaminaLeft = ability!.MaxStamina;
                // And broadcast current stamina
                EventManager.Instance.Broadcast<StaminaUpdateEvent>(new(Status.StaminaLeft, true));
                // Initialize health value
                EventManager.Instance.Broadcast<HealthUpdateEvent>(new(20F, true));
            }

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
            if (client == null) return string.Empty;

            string targetBlockInfo = string.Empty;
            var loc = client.GetCurrentLocation();
            var world = client.GetWorld();

            var target = interactionUpdater?.TargetLocation;

            if (target is not null)
            {
                var targetBlock = world?.GetBlock(target.Value);
                if (targetBlock is not null)
                    targetBlockInfo = targetBlock.ToString();
            }

            var lightInfo = $"sky {world?.GetSkyLight(loc)} block {world?.GetBlockLight(loc)}";

            var velocity = playerRigidbody?.velocity;
            // Visually swap xz velocity to fit vanilla
            var veloInfo = $"Vel:\t{velocity?.z:0.00}\t{velocity?.y:0.00}\t{velocity?.x:0.00}\n({velocity?.magnitude:0.000})";

            string statusInfo;

            if (statusUpdater?.Status.Spectating ?? true)
                statusInfo = string.Empty;
            else
                statusInfo = statusUpdater!.Status.ToString();
            
            return $"Pos:\t{loc}\nState:\t{CurrentState}\n{veloInfo}\nLighting:\t{lightInfo}\n{statusInfo}\nTarget Block:\t{target}\n{targetBlockInfo}\nBiome:\n[{world?.GetBiomeId(loc)}] {world?.GetBiome(loc).GetDescription()}";

        }

    }
}
