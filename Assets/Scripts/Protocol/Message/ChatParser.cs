﻿#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MinecraftClient.Protocol.Message;

namespace MinecraftClient.Protocol
{
    /// <summary>
    /// This class parses JSON chat data from MC 1.6+ and returns the appropriate string to be printed.
    /// </summary>

    static class ChatParser
    {
        public enum MessageType
        {
            CHAT,
            SAY_COMMAND,
            MSG_COMMAND_INCOMING,
            MSG_COMMAND_OUTGOING,
            TEAM_MSG_COMMAND_INCOMING,
            TEAM_MSG_COMMAND_OUTGOING,
            EMOTE_COMMAND,
            RAW_MSG
        };

        public static Dictionary<int, MessageType>? ChatId2Type;

        /// <summary>
        /// The main function to convert text from MC 1.6+ JSON to MC 1.5.2 formatted text
        /// </summary>
        /// <param name="json">JSON serialized text</param>
        /// <param name="links">Optional container for links from JSON serialized text</param>
        /// <returns>Returns the translated text</returns>
        public static string ParseText(string json, List<string>? links = null)
        {
            return JSONData2String(Json.ParseJson(json), "", links);
        }
        
        /// <summary>
        /// The main function to convert text from MC 1.9+ JSON to MC 1.5.2 formatted text
        /// </summary>
        /// <param name="message">Message received</param>
        /// <param name="links">Optional container for links from JSON serialized text</param>
        /// <returns>Returns the translated text</returns>
        public static string ParseSignedChat(ChatMessage message, List<string>? links = null)
        {
            string chatContent = CornCraft.ShowModifiedChat && message.unsignedContent != null ? message.unsignedContent : message.content;
            string content = message.isJson ? ParseText(chatContent, links) : chatContent;
            string sender = message.displayName!;

            string text;
            List<string> usingData = new();

            MessageType chatType;
            if (message.isSystemChat)
                chatType = MessageType.RAW_MSG;
            else if (!ChatId2Type!.TryGetValue(message.chatTypeId, out chatType))
                chatType = MessageType.CHAT;
            switch (chatType)
            {
                case MessageType.CHAT:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.text", usingData);
                    break;
                case MessageType.SAY_COMMAND:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.announcement", usingData);
                    break;
                case MessageType.MSG_COMMAND_INCOMING:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("commands.message.display.incoming", usingData);
                    break;
                case MessageType.MSG_COMMAND_OUTGOING:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("commands.message.display.outgoing", usingData);
                    break;
                case MessageType.TEAM_MSG_COMMAND_INCOMING:
                    usingData.Add(message.teamName!);
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.team.text", usingData);
                    break;
                case MessageType.TEAM_MSG_COMMAND_OUTGOING:
                    usingData.Add(message.teamName!);
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.team.sent", usingData);
                    break;
                case MessageType.EMOTE_COMMAND:
                    usingData.Add(sender);
                    usingData.Add(content);
                    text = TranslateString("chat.type.emote", usingData);
                    break;
                case MessageType.RAW_MSG:
                    text = content;
                    break;
                default:
                    goto case MessageType.CHAT;
            }
            string color = string.Empty;
            if (message.isSystemChat)
            {
                if (CornCraft.MarkSystemMessage)
                    color = "§z §r "; // Custom color code §z : Background Gray
            }
            else
            {
                if ((bool)message.isSignatureLegal!)
                {
                    if (CornCraft.ShowModifiedChat && message.unsignedContent != null)
                    {
                        if (CornCraft.MarkModifiedMsg)
                            color = "§x §r "; // Custom color code §x : Background Yellow
                    }
                    else
                    {
                        if (CornCraft.MarkLegallySignedMsg)
                            color = "§y §r "; // Custom color code §y : Background Green
                    }
                }
                else
                {
                    if (CornCraft.MarkIllegallySignedMsg)
                        color = "§w §r "; // Custom color code §w : Background Red
                }
            }
            return color + text;
        }

        /// <summary>
        /// Get the classic color tag corresponding to a color name
        /// </summary>
        /// <param name="colorname">Color Name</param>
        /// <returns>Color code</returns>
        private static string Color2tag(string colorname)
        {
            switch (colorname.ToLower())
            {
                /* MC 1.7+ Name           MC 1.6 Name           Classic tag */
                case "black":        /*  Blank if same  */      return "§0";
                case "dark_blue":                               return "§1";
                case "dark_green":                              return "§2";
                case "dark_aqua":       case "dark_cyan":       return "§3";
                case "dark_red":                                return "§4";
                case "dark_purple":     case "dark_magenta":    return "§5";
                case "gold":            case "dark_yellow":     return "§6";
                case "gray":                                    return "§7";
                case "dark_gray":                               return "§8";
                case "blue":                                    return "§9";
                case "green":                                   return "§a";
                case "aqua":            case "cyan":            return "§b";
                case "red":                                     return "§c";
                case "light_purple":    case "magenta":         return "§d";
                case "yellow":                                  return "§e";
                case "white":                                   return "§f";
                default: return "";
            }
        }

        /// <summary>
        /// Specify whether translation rules have been loaded
        /// </summary>
        private static bool RulesInitialized = false;

        /// <summary>
        /// Set of translation rules for formatting text
        /// </summary>
        private static Dictionary<string, string> TranslationRules = new Dictionary<string, string>();

        /// <summary>
        /// Initialize translation rules.
        /// Necessary for properly printing some chat messages.
        /// </summary>
        public static void InitTranslations() { if (!RulesInitialized) { InitRules(); RulesInitialized = true; } }

        /// <summary>
        /// Internal rule initialization method. Looks for local rule file or download it from Mojang asset servers.
        /// </summary>
        private static void InitRules()
        {
            //Small default dictionnary of translation rules
            TranslationRules["chat.type.admin"] = "[%s: %s]";
            TranslationRules["chat.type.announcement"] = "§d[%s] %s";
            TranslationRules["chat.type.emote"] = " * %s %s";
            TranslationRules["chat.type.text"] = "<%s> %s";
            TranslationRules["multiplayer.player.joined"] = "§e%s joined the game.";
            TranslationRules["multiplayer.player.left"] = "§e%s left the game.";
            TranslationRules["commands.message.display.incoming"] = "§7%s whispers to you: %s";
            TranslationRules["commands.message.display.outgoing"] = "§7You whisper to %s: %s";

            //Language file in a subfolder, depending on the language setting
            if (!Directory.Exists("lang"))
                Directory.CreateDirectory("lang");

            string langFile = "lang" + Path.DirectorySeparatorChar + CornCraft.Language + ".lang";

            //File not found? Try downloading language file from Mojang's servers?
            if (!File.Exists(langFile))
            {
                Translations.Log("chat.download", CornCraft.Language);
                try
                {
                    string assets_index = DownloadString(Translations.TranslationsFile_Website_Index);
                    string[] tmp = assets_index.Split(new string[] { "minecraft/lang/" + CornCraft.Language.ToLower() + ".json" }, StringSplitOptions.None);
                    tmp = tmp[1].Split(new string[] { "hash\": \"" }, StringSplitOptions.None);
                    string hash = tmp[1].Split('"')[0]; //Translations file identifier on Mojang's servers
                    string translation_file_location = Translations.TranslationsFile_Website_Download + '/' + hash.Substring(0, 2) + '/' + hash;
                    if (CornCraft.DebugMode)
                        Translations.Log("chat.request", translation_file_location);

                    StringBuilder stringBuilder = new StringBuilder();
                    foreach (KeyValuePair<string, Json.JSONData> entry in Json.ParseJson(DownloadString(translation_file_location)).Properties)
                    {
                        stringBuilder.Append(entry.Key + "=" + entry.Value.StringValue + Environment.NewLine);
                    }

                    File.WriteAllText(langFile, stringBuilder.ToString());
                    Translations.Log("chat.done", langFile);
                }
                catch
                {
                    Translations.LogWarning("chat.fail");
                }
            }

            //Download Failed? Defaulting to en_GB.lang if the game is installed
            if (!File.Exists(langFile) //Try en_GB.lang
              && File.Exists(Translations.TranslationsFile_FromMCDir))
            {
                langFile = Translations.TranslationsFile_FromMCDir;
                Translations.Log("chat.from_dir");
            }

            //Load the external dictionnary of translation rules or display an error message
            if (File.Exists(langFile))
            {
                string[] translations = File.ReadAllLines(langFile);
                foreach (string line in translations)
                {
                    if (line.Length > 0)
                    {
                        string[] splitted = line.Split('=');
                        if (splitted.Length == 2)
                        {
                            TranslationRules[splitted[0]] = splitted[1];
                        }
                    }
                }

                if (CornCraft.DebugMode)
                    Translations.Log("chat.loaded");
            }
            else //No external dictionary found.
            {
                Translations.Log("chat.not_found", langFile);
            }
        }

        /// <summary>
        /// Format text using a specific formatting rule.
        /// Example : * %s %s + ["ORelio", "is doing something"] = * ORelio is doing something
        /// </summary>
        /// <param name="rulename">Name of the rule, chosen by the server</param>
        /// <param name="using_data">Data to be used in the rule</param>
        /// <returns>Returns the formatted text according to the given data</returns>
        private static string TranslateString(string rulename, List<string> using_data)
        {
            if (!RulesInitialized) { InitRules(); RulesInitialized = true; }
            if (TranslationRules.ContainsKey(rulename))
            {
                int using_idx = 0;
                string rule = TranslationRules[rulename];
                StringBuilder result = new StringBuilder();
                for (int i = 0; i < rule.Length; i++)
                {
                    if (rule[i] == '%' && i + 1 < rule.Length)
                    {
                        //Using string or int with %s or %d
                        if (rule[i + 1] == 's' || rule[i + 1] == 'd')
                        {
                            if (using_data.Count > using_idx)
                            {
                                result.Append(using_data[using_idx]);
                                using_idx++;
                                i += 1;
                                continue;
                            }
                        }

                        //Using specified string or int with %1$s, %2$s...
                        else if (char.IsDigit(rule[i + 1])
                            && i + 3 < rule.Length && rule[i + 2] == '$'
                            && (rule[i + 3] == 's' || rule[i + 3] == 'd'))
                        {
                            int specified_idx = rule[i + 1] - '1';
                            if (using_data.Count > specified_idx)
                            {
                                result.Append(using_data[specified_idx]);
                                using_idx++;
                                i += 3;
                                continue;
                            }
                        }
                    }
                    result.Append(rule[i]);
                }
                return result.ToString();
            }
            else return "[" + rulename + "] " + String.Join(" ", using_data);
        }

        /// <summary>
        /// Use a JSON Object to build the corresponding string
        /// </summary>
        /// <param name="data">JSON object to convert</param>
        /// <param name="colorcode">Allow parent color code to affect child elements (set to "" for function init)</param>
        /// <param name="links">Container for links from JSON serialized text</param>
        /// <returns>returns the Minecraft-formatted string</returns>
        private static string JSONData2String(Json.JSONData data, string colorcode, List<string>? links)
        {
            string extra_result = "";
            switch (data.Type)
            {
                case Json.JSONData.DataType.Object:
                    if (data.Properties.ContainsKey("color"))
                    {
                        colorcode = Color2tag(JSONData2String(data.Properties["color"], "", links));
                    }
                    if (data.Properties.ContainsKey("clickEvent") && links != null)
                    {
                        Json.JSONData clickEvent = data.Properties["clickEvent"];
                        if (clickEvent.Properties.ContainsKey("action")
                            && clickEvent.Properties.ContainsKey("value")
                            && clickEvent.Properties["action"].StringValue == "open_url"
                            && !String.IsNullOrEmpty(clickEvent.Properties["value"].StringValue))
                        {
                            links.Add(clickEvent.Properties["value"].StringValue);
                        }
                     }
                    if (data.Properties.ContainsKey("extra"))
                    {
                        Json.JSONData[] extras = data.Properties["extra"].DataArray.ToArray();
                        foreach (Json.JSONData item in extras)
                            extra_result = extra_result + JSONData2String(item, colorcode, links) + "§r";
                    }
                    if (data.Properties.ContainsKey("text"))
                    {
                        return colorcode + JSONData2String(data.Properties["text"], colorcode, links) + extra_result;
                    }
                    else if (data.Properties.ContainsKey("translate"))
                    {
                        List<string> using_data = new List<string>();
                        if (data.Properties.ContainsKey("using") && !data.Properties.ContainsKey("with"))
                            data.Properties["with"] = data.Properties["using"];
                        if (data.Properties.ContainsKey("with"))
                        {
                            Json.JSONData[] array = data.Properties["with"].DataArray.ToArray();
                            for (int i = 0; i < array.Length; i++)
                            {
                                using_data.Add(JSONData2String(array[i], colorcode, links));
                            }
                        }
                        return colorcode + TranslateString(JSONData2String(data.Properties["translate"], "", links), using_data) + extra_result;
                    }
                    else return extra_result;

                case Json.JSONData.DataType.Array:
                    string result = "";
                    foreach (Json.JSONData item in data.DataArray)
                    {
                        result += JSONData2String(item, colorcode, links);
                    }
                    return result;

                case Json.JSONData.DataType.String:
                    return colorcode + data.StringValue;
            }

            return "";
        }

        /// <summary>
        /// Do a HTTP request to get a webpage or text data from a server file
        /// </summary>
        /// <param name="url">URL of resource</param>
        /// <returns>Returns resource data if success, otherwise a WebException is raised</returns>
        private static string DownloadString(string url)
        {
            System.Net.HttpWebRequest myRequest = (System.Net.HttpWebRequest)System.Net.WebRequest.Create(url);
            myRequest.Method = "GET";
            System.Net.WebResponse myResponse = myRequest.GetResponse();
            System.IO.StreamReader sr = new System.IO.StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
            string result = sr.ReadToEnd();
            sr.Close();
            myResponse.Close();
            return result;
        }
    }
}
