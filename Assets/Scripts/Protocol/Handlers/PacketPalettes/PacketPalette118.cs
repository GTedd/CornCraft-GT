using System.Collections.Generic;

namespace CraftSharp.Protocol.Handlers.PacketPalettes
{
    public class PacketPalette118 : PacketTypePalette
    {
        private Dictionary<int, PacketTypesIn> typeIn = new Dictionary<int, PacketTypesIn>()
        {
            { 0x00, PacketTypesIn.SpawnEntity },
            { 0x01, PacketTypesIn.SpawnExperienceOrb },
            { 0x02, PacketTypesIn.SpawnLivingEntity },
            { 0x03, PacketTypesIn.SpawnPainting },
            { 0x04, PacketTypesIn.SpawnPlayer },
            { 0x05, PacketTypesIn.SkulkVibrationSignal },
            { 0x06, PacketTypesIn.EntityAnimation },
            { 0x07, PacketTypesIn.Statistics },
            { 0x08, PacketTypesIn.AcknowledgePlayerDigging },
            { 0x09, PacketTypesIn.BlockBreakAnimation },
            { 0x0A, PacketTypesIn.BlockEntityData },
            { 0x0B, PacketTypesIn.BlockAction },
            { 0x0C, PacketTypesIn.BlockChange },
            { 0x0D, PacketTypesIn.BossBar },
            { 0x0E, PacketTypesIn.ServerDifficulty },
            { 0x0F, PacketTypesIn.ChatMessage },
            { 0x10, PacketTypesIn.ClearTiles },
            { 0x11, PacketTypesIn.TabComplete },
            { 0x12, PacketTypesIn.DeclareCommands },
            { 0x13, PacketTypesIn.CloseWindow },
            { 0x14, PacketTypesIn.WindowItems },
            { 0x15, PacketTypesIn.WindowProperty },
            { 0x16, PacketTypesIn.SetSlot },
            { 0x17, PacketTypesIn.SetCooldown },
            { 0x18, PacketTypesIn.PluginMessage },
            { 0x19, PacketTypesIn.NamedSoundEffect },
            { 0x1A, PacketTypesIn.Disconnect },
            { 0x1B, PacketTypesIn.EntityStatus },
            { 0x1C, PacketTypesIn.Explosion },
            { 0x1D, PacketTypesIn.UnloadChunk },
            { 0x1E, PacketTypesIn.ChangeGameState },
            { 0x1F, PacketTypesIn.OpenHorseWindow },
            { 0x20, PacketTypesIn.InitializeWorldBorder },
            { 0x21, PacketTypesIn.KeepAlive },
            { 0x22, PacketTypesIn.ChunkData },
            { 0x23, PacketTypesIn.Effect },
            { 0x24, PacketTypesIn.Particle },
            { 0x25, PacketTypesIn.UpdateLight },
            { 0x26, PacketTypesIn.JoinGame },
            { 0x27, PacketTypesIn.MapData },
            { 0x28, PacketTypesIn.TradeList },
            { 0x29, PacketTypesIn.EntityPosition },
            { 0x2A, PacketTypesIn.EntityPositionAndRotation },
            { 0x2B, PacketTypesIn.EntityRotation },
            { 0x2C, PacketTypesIn.VehicleMove },
            { 0x2D, PacketTypesIn.OpenBook },
            { 0x2E, PacketTypesIn.OpenWindow },
            { 0x2F, PacketTypesIn.OpenSignEditor },
            { 0x30, PacketTypesIn.Ping },
            { 0x31, PacketTypesIn.CraftRecipeResponse },
            { 0x32, PacketTypesIn.PlayerAbilities },
            { 0x33, PacketTypesIn.EndCombatEvent },
            { 0x34, PacketTypesIn.EnterCombatEvent },
            { 0x35, PacketTypesIn.DeathCombatEvent },
            { 0x36, PacketTypesIn.PlayerInfo },
            { 0x37, PacketTypesIn.FacePlayer },
            { 0x38, PacketTypesIn.PlayerPositionAndLook },
            { 0x39, PacketTypesIn.UnlockRecipes },
            { 0x3A, PacketTypesIn.DestroyEntities },
            { 0x3B, PacketTypesIn.RemoveEntityEffect },
            { 0x3C, PacketTypesIn.ResourcePackSend },
            { 0x3D, PacketTypesIn.Respawn },
            { 0x3E, PacketTypesIn.EntityHeadLook },
            { 0x3F, PacketTypesIn.MultiBlockChange },
            { 0x40, PacketTypesIn.SelectAdvancementTab },
            { 0x41, PacketTypesIn.ActionBar },
            { 0x42, PacketTypesIn.WorldBorderCenter },
            { 0x43, PacketTypesIn.WorldBorderLerpSize },
            { 0x44, PacketTypesIn.WorldBorderSize },
            { 0x45, PacketTypesIn.WorldBorderWarningDelay },
            { 0x46, PacketTypesIn.WorldBorderWarningReach },
            { 0x47, PacketTypesIn.Camera },
            { 0x48, PacketTypesIn.HeldItemChange },
            { 0x49, PacketTypesIn.UpdateViewPosition },
            { 0x4A, PacketTypesIn.UpdateViewDistance },
            { 0x4B, PacketTypesIn.SpawnPosition },
            { 0x4C, PacketTypesIn.DisplayScoreboard },
            { 0x4D, PacketTypesIn.EntityMetadata },
            { 0x4E, PacketTypesIn.AttachEntity },
            { 0x4F, PacketTypesIn.EntityVelocity },
            { 0x50, PacketTypesIn.EntityEquipment },
            { 0x51, PacketTypesIn.SetExperience },
            { 0x52, PacketTypesIn.UpdateHealth },
            { 0x53, PacketTypesIn.ScoreboardObjective },
            { 0x54, PacketTypesIn.SetPassengers },
            { 0x55, PacketTypesIn.Teams },
            { 0x56, PacketTypesIn.UpdateScore },
            { 0x57, PacketTypesIn.UpdateSimulationDistance },
            { 0x58, PacketTypesIn.SetTitleSubTitle },
            { 0x59, PacketTypesIn.TimeUpdate },
            { 0x5A, PacketTypesIn.SetTitleText },
            { 0x5B, PacketTypesIn.SetTitleTime },
            { 0x5C, PacketTypesIn.EntitySoundEffect },
            { 0x5D, PacketTypesIn.SoundEffect },
            { 0x5E, PacketTypesIn.StopSound },
            { 0x5F, PacketTypesIn.PlayerListHeaderAndFooter },
            { 0x60, PacketTypesIn.NBTQueryResponse },
            { 0x61, PacketTypesIn.CollectItem },
            { 0x62, PacketTypesIn.EntityTeleport },
            { 0x63, PacketTypesIn.Advancements },
            { 0x64, PacketTypesIn.EntityProperties },
            { 0x65, PacketTypesIn.EntityEffect },
            { 0x66, PacketTypesIn.DeclareRecipes },
            { 0x67, PacketTypesIn.Tags },
        };

        private Dictionary<int, PacketTypesOut> typeOut = new Dictionary<int, PacketTypesOut>()
        {
            { 0x00, PacketTypesOut.TeleportConfirm },
            { 0x01, PacketTypesOut.QueryBlockNBT },
            { 0x02, PacketTypesOut.SetDifficulty },
            { 0x03, PacketTypesOut.ChatMessage },
            { 0x04, PacketTypesOut.ClientStatus },
            { 0x05, PacketTypesOut.ClientSettings },
            { 0x06, PacketTypesOut.TabComplete },
            { 0x07, PacketTypesOut.ClickWindowButton },
            { 0x08, PacketTypesOut.ClickWindow },
            { 0x09, PacketTypesOut.CloseWindow },
            { 0x0A, PacketTypesOut.PluginMessage },
            { 0x0B, PacketTypesOut.EditBook },
            { 0x0C, PacketTypesOut.EntityNBTRequest },
            { 0x0D, PacketTypesOut.InteractEntity },
            { 0x0E, PacketTypesOut.GenerateStructure },
            { 0x0F, PacketTypesOut.KeepAlive },
            { 0x10, PacketTypesOut.LockDifficulty },
            { 0x11, PacketTypesOut.PlayerPosition },
            { 0x12, PacketTypesOut.PlayerPositionAndRotation },
            { 0x13, PacketTypesOut.PlayerRotation },
            { 0x14, PacketTypesOut.PlayerMovement },
            { 0x15, PacketTypesOut.VehicleMove },
            { 0x16, PacketTypesOut.SteerBoat },
            { 0x17, PacketTypesOut.PickItem },
            { 0x18, PacketTypesOut.CraftRecipeRequest },
            { 0x19, PacketTypesOut.PlayerAbilities },
            { 0x1A, PacketTypesOut.PlayerDigging },
            { 0x1B, PacketTypesOut.EntityAction },
            { 0x1C, PacketTypesOut.SteerVehicle },
            { 0x1D, PacketTypesOut.Pong },
            { 0x1E, PacketTypesOut.SetDisplayedRecipe },
            { 0x1F, PacketTypesOut.SetRecipeBookState },
            { 0x20, PacketTypesOut.NameItem },
            { 0x21, PacketTypesOut.ResourcePackStatus },
            { 0x22, PacketTypesOut.AdvancementTab },
            { 0x23, PacketTypesOut.SelectTrade },
            { 0x24, PacketTypesOut.SetBeaconEffect },
            { 0x25, PacketTypesOut.HeldItemChange },
            { 0x26, PacketTypesOut.UpdateCommandBlock },
            { 0x27, PacketTypesOut.UpdateCommandBlockMinecart },
            { 0x28, PacketTypesOut.CreativeInventoryAction },
            { 0x29, PacketTypesOut.UpdateJigsawBlock },
            { 0x2A, PacketTypesOut.UpdateStructureBlock },
            { 0x2B, PacketTypesOut.UpdateSign },
            { 0x2C, PacketTypesOut.Animation },
            { 0x2D, PacketTypesOut.Spectate },
            { 0x2E, PacketTypesOut.PlayerBlockPlacement },
            { 0x2F, PacketTypesOut.UseItem },
        };

        protected override Dictionary<int, PacketTypesIn> GetListIn()
        {
            return typeIn;
        }

        protected override Dictionary<int, PacketTypesOut> GetListOut()
        {
            return typeOut;
        }
    }
}