#nullable enable
using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using System.IO;
using UnityEngine;

using CraftSharp.Control;
using CraftSharp.Event;
using CraftSharp.Protocol;
using CraftSharp.Protocol.ProfileKey;
using CraftSharp.Protocol.Handlers.Forge;
using CraftSharp.Protocol.Session;
using CraftSharp.Rendering;
using CraftSharp.UI;
using CraftSharp.Inventory;

namespace CraftSharp
{
    [RequireComponent(typeof (PlayerUserInput), typeof (InteractionUpdater))]
    public class CornClientOffline : BaseCornClient
    {
        #region Login Information
        private const string username = "OfflinePlayer";
        private Guid uuid = Guid.Empty;
        #endregion

        #region Client Control

        private readonly Queue<string> chatQueue = new();
        private DateTime nextMessageSendTime = DateTime.MinValue;
        private bool canSendMessage = false;

        #endregion

        #region Players and Entities
        private bool locationReceived = false;
        private readonly Entity clientEntity = new(0, EntityType.DUMMY_ENTITY_TYPE, Location.Zero);
        private float? yawToSend = null, pitchToSend = null;
        private bool grounded = false;
        private int clientSequenceId;
        private int foodSaturation, level, totalExperience;
        private readonly Dictionary<int, Container> inventories = new();
        private readonly object movementLock = new();
        private PlayerUserInput? playerUserInput;
        private InteractionUpdater? interactionUpdater;
        private readonly Dictionary<Guid, PlayerInfo> onlinePlayers = new();
        private readonly Dictionary<int, Entity> entities = new();
        #endregion

        void Awake() // In case where the client wasn't properly assigned before
        {
            if (CornApp.CurrentClient == null)
            {
                CornApp.SetCurrentClient(this);
            }
        }

        void Start()
        {
            MaterialManager!.LoadPlayerSkins();

            // Push HUD Screen on start
            ScreenControl.PushScreen(HUDScreen!);

            // Setup chunk render manager
            ChunkRenderManager!.SetClient(this);

            // Get player user input
            playerUserInput = GetComponent<PlayerUserInput>();

            // Set up camera controller
            CameraController.SetTarget(playerController!.cameraRef!);

            // Set up interaction updater
            interactionUpdater = GetComponent<InteractionUpdater>();
            interactionUpdater!.Initialize(this, CameraController);
        }

        public override bool StartClient(SessionToken session, PlayerKeyPair? playerKeyPair, string serverIp,
                ushort port, int protocol, ForgeInfo? forgeInfo, string accountLower)
        {
            // Start up client

            // Update entity type for dummy client entity
            clientEntity.Type = EntityPalette.INSTANCE.FromId(EntityType.PLAYER_ID);
            // Update client entity name
            clientEntity.Name = session.PlayerName;
            clientEntity.UUID = uuid;
            clientEntity.SetHeadYawFromByte(127);
            clientEntity.MaxHealth = 20F;

            if (playerRenderPrefab != null)
            {
                GameObject renderObj;
                if (playerRenderPrefab.GetComponent<Animator>() != null) // Model prefab, wrap it up
                {
                    renderObj = AnimatorEntityRender.CreateFromModel(playerRenderPrefab);
                }
                else // Player render prefab, just instantiate
                {
                    renderObj = GameObject.Instantiate(playerRenderPrefab);
                }
                renderObj!.name = $"Player Entity ({playerRenderPrefab.name})";

                playerController!.UpdatePlayerRender(clientEntity, renderObj);
                // Subscribe movement events
                playerController!.OnMovementUpdate += UpdatePlayerStatus;
            }
            else
            {
                throw new Exception("Player render prefab is not assigned for game client!");
            }

            return true; // Client successfully started
        }

        public override void Disconnect()
        {
            // Return to login scene
            CornApp.Instance.BackToLogin();
        }

        #region Getters: Retrieve data for use in other methods

        // Retrieve client connection info
        public override string GetServerHost() => string.Empty;
        public override int GetServerPort() => 0;
        public override string GetUsername() => username!;
        public override Guid GetUserUuid() => uuid;
        public override string GetUserUuidStr() => uuid.ToString().Replace("-", string.Empty);
        public override string GetSessionID() => string.Empty;
        public override double GetServerTPS() => 20;
        public override float GetTickMilSec() => 0.05F;

        /// <summary>
        /// Get current world
        /// </summary>
        public override World GetWorld()
        {
            return ChunkRenderManager!.World;
        }

        /// <summary>
        /// Get player inventory with a given id
        /// </summary>
        public override Container? GetInventory(int inventoryId)
        {
            if (inventories.ContainsKey(inventoryId))
                return inventories[inventoryId];
            return null;
        }

        /// <summary>
        /// Get current player location (in Minecraft world)
        /// </summary>
        public override Location GetLocation() => clientEntity.Location;

        /// <summary>
        /// Get current player position (in Unity scene)
        /// </summary>
        public override Vector3 GetPosition() => CoordConvert.MC2Unity(clientEntity.Location);

        /// <summary>
        /// Get current status about the client
        /// </summary>
        /// <returns>Status info string</returns>
        public override string GetInfoString(bool withDebugInfo)
        {
            string baseString = $"FPS: {(int)(1F / Time.deltaTime), 4}\n{GameMode}\nTime: {EnvironmentManager!.GetTimeString()}";

            if (withDebugInfo)
            {
                var targetLoc = interactionUpdater?.TargetLocation;
                var loc = GetLocation();
                var world = GetWorld();

                string targetInfo;

                if (targetLoc is not null)
                {
                    var targetBlock = world.GetBlock(targetLoc.Value);
                    targetInfo = $"Target: {targetLoc}\n{targetBlock}";
                }
                else
                {
                    targetInfo = "\n";
                }
                
                return baseString + $"\nLoc: {loc}\nBiome:\t{world.GetBiome(loc)}\n{targetInfo}\n{playerController?.GetDebugInfo()}" +
                        $"\n{ChunkRenderManager!.GetDebugInfo()}\n{EntityRenderManager!.GetDebugInfo()}\nSvr TPS: {GetServerTPS():00.00}";
            }
            
            return baseString;
        }

        /// <summary>
        /// Get all Entities
        /// </summary>
        /// <returns>All Entities</returns>
        public override Dictionary<int, Entity> GetEntities() => entities;

        /// <summary>
        /// Get target entity for initiating an attack
        /// </summary>
        /// <returns>The current position of target</returns>
        public override Vector3? GetAttackTarget()
        {
            return EntityRenderManager!.GetAttackTarget(GetPosition());
        }

        /// <summary>
        /// Get all players latency
        /// </summary>
        public override Dictionary<string, int> GetPlayersLatency()
        {
            Dictionary<string, int> playersLatency = new();
            foreach (var player in onlinePlayers)
                playersLatency.Add(player.Value.Name, player.Value.Ping);
            return playersLatency;
        }

        /// <summary>
        /// Get latency for current player
        /// </summary>
        public override int GetOwnLatency() => onlinePlayers.ContainsKey(uuid) ? onlinePlayers[uuid].Ping : 0;

        /// <summary>
        /// Get player info from uuid
        /// </summary>
        /// <param name="uuid">Player's UUID</param>
        public override PlayerInfo? GetPlayerInfo(Guid uuid)
        {
            lock (onlinePlayers)
            {
                if (onlinePlayers.ContainsKey(uuid))
                    return onlinePlayers[uuid];
                else
                    return null;
            }
        }
        
        /// <summary>
        /// Get a set of online player names
        /// </summary>
        /// <returns>Online player names</returns>
        public override string[] GetOnlinePlayers()
        {
            lock (onlinePlayers)
            {
                string[] playerNames = new string[onlinePlayers.Count];
                int idx = 0;
                foreach (var player in onlinePlayers)
                    playerNames[idx++] = player.Value.Name;
                return playerNames;
            }
        }

        /// <summary>
        /// Get a dictionary of online player names and their corresponding UUID
        /// </summary>
        /// <returns>Dictionay of online players, key is UUID, value is Player name</returns>
        public override Dictionary<string, string> GetOnlinePlayersWithUUID()
        {
            Dictionary<string, string> uuid2Player = new Dictionary<string, string>();
            lock (onlinePlayers)
            {
                foreach (Guid key in onlinePlayers.Keys)
                {
                    uuid2Player.Add(key.ToString(), onlinePlayers[key].Name);
                }
            }
            return uuid2Player;
        }

        #endregion

        #region Action methods: Perform an action on the Server

        public override void UpdatePlayerStatus(Vector3 newPosition, float newYaw, float newPitch, bool newGrounded)
        {
            lock (movementLock)
            {
                // Update player location
                clientEntity.Location = CoordConvert.Unity2MC(newPosition);

                // Update player yaw and pitch
                
                if (clientEntity.Yaw != newYaw || clientEntity.Pitch != newPitch)
                {
                    yawToSend = newYaw;
                    clientEntity.Yaw = newYaw;
                    pitchToSend = newPitch;
                    clientEntity.Pitch = newPitch;
                }
                
                grounded = newGrounded;
            }
        }

        /// <summary>
        /// Allows the user to send chat messages, commands, and leave the server.
        /// </summary>
        public override void TrySendChat(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            
            Debug.Log($"Sending chat: {text}");
        }

        /// <summary>
        /// Allow to respawn after death
        /// </summary>
        /// <returns>True if packet successfully sent</returns>
        public override bool SendRespawnPacket()
        {
            // Reset location received flag
            locationReceived = false;

            return true;
        }

        /// <summary>
        /// Send the Entity Action packet with the Specified ID
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public override bool SendEntityAction(EntityActionType entityAction)
        {
            return false;
        }

        /// <summary>
        /// Allows the user to send requests to complete current command
        /// </summary>
        public override void SendAutoCompleteRequest(string text) { }

        /// <summary>
        /// Use the item currently in the player's hand
        /// </summary>
        /// <returns>TRUE if the item was successfully used</returns>
        public override bool UseItemOnHand()
        {
            return false;
        }

        /// <summary>
        /// Place the block at hand in the Minecraft world
        /// </summary>
        /// <param name="location">Location to place block to</param>
        /// <param name="blockFace">Block face (e.g. Direction.Down when clicking on the block below to place this block)</param>
        /// <returns>TRUE if successfully placed</returns>
        public override bool PlaceBlock(Location location, Direction blockFace, Hand hand = Hand.MainHand)
        {
            return false;
        }

        /// <summary>
        /// Attempt to dig a block at the specified location
        /// </summary>
        /// <param name="location">Location of block to dig</param>
        /// <param name="swingArms">Also perform the "arm swing" animation</param>
        /// <param name="lookAtBlock">Also look at the block before digging</param>
        public override bool DigBlock(Location location, bool swingArms = true, bool lookAtBlock = true)
        {
            return false;
        }

        /// <summary>
        /// Change active slot in the player inventory
        /// </summary>
        /// <param name="slot">Slot to activate (0 to 8)</param>
        /// <returns>TRUE if the slot was changed</returns>
        public override bool ChangeSlot(short slot)
        {
            if (slot < 0 || slot > 8)
                return false;

            CurrentSlot = Convert.ToByte(slot);
            // Broad cast hotbar selection change
            EventManager.Instance.BroadcastOnUnityThread(new HeldItemChangeEvent(CurrentSlot));

            return true;
        }

        #endregion

    }
}
