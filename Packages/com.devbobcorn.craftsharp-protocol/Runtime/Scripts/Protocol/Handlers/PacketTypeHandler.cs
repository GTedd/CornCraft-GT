﻿using System;
using CraftSharp.Protocol.Handlers.PacketPalettes;

namespace CraftSharp.Protocol.Handlers
{
    public class PacketTypeHandler
    {
        private int protocol;
        private bool forgeEnabled = false;

        /// <summary>
        /// Initialize the handler
        /// </summary>
        /// <param name="protocol">Protocol version to use</param>
        public PacketTypeHandler(int protocol)
        {
            this.protocol = protocol;
        }
        /// <summary>
        /// Initialize the handler
        /// </summary>
        /// <param name="protocol">Protocol version to use</param>
        /// <param name="forgeEnabled">Is forge enabled or not</param>
        public PacketTypeHandler(int protocol, bool forgeEnabled)
        {
            this.protocol = protocol;
            this.forgeEnabled = forgeEnabled;
        }
        /// <summary>
        /// Initialize the handler
        /// </summary>
        public PacketTypeHandler() { }

        /// <summary>
        /// Get the packet type palette
        /// </summary>
        /// <returns></returns>
        public PacketTypePalette GetTypeHandler()
        {
            return GetTypeHandler(this.protocol);
        }
        /// <summary>
        /// Get the packet type palette
        /// </summary>
        /// <param name="protocol">Protocol version to use</param>
        /// <returns></returns>
        public PacketTypePalette GetTypeHandler(int protocol)
        {
            PacketTypePalette p;
            
            if (protocol > ProtocolMinecraft.MC_1_20_4_Version)
                throw new NotImplementedException(Translations.Get("exception.palette.packet"));
            
            if (protocol <= ProtocolMinecraft.MC_1_16_1_Version)
                p = new PacketPalette116();
            else if (protocol <= ProtocolMinecraft.MC_1_16_5_Version)
                p = new PacketPalette1162();
            else if (protocol <= ProtocolMinecraft.MC_1_17_1_Version)
                p = new PacketPalette117();
            else if (protocol <= ProtocolMinecraft.MC_1_18_2_Version)
                p = new PacketPalette118();
            else if (protocol <= ProtocolMinecraft.MC_1_19_Version)
                p = new PacketPalette119();
            else if (protocol <= ProtocolMinecraft.MC_1_19_2_Version)
                p = new PacketPalette1192();
            else if (protocol <= ProtocolMinecraft.MC_1_19_3_Version)
                p = new PacketPalette1193();
            else if (protocol <= ProtocolMinecraft.MC_1_19_4_Version)
                p = new PacketPalette1194();
            else if (protocol <= ProtocolMinecraft.MC_1_20_Version)
                p = new PacketPalette1194();
            else if (protocol <= ProtocolMinecraft.MC_1_20_2_Version)
                p = new PacketPalette1202();
            else //if (protocol <= ProtocolMinecraft.MC_1_20_4_Version)
                p = new PacketPalette1204();

            p.SetForgeEnabled(this.forgeEnabled);
            return p;
        }
    }
}
