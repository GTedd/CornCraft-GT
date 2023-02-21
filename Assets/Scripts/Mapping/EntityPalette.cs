﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

namespace MinecraftClient.Mapping
{
    public class EntityPalette
    {
        public static readonly EntityPalette INSTANCE = new();

        private readonly Dictionary<int, EntityType> entityTypeTable = new();
        private readonly Dictionary<ResourceLocation, int> dictId = new();

        /// <summary>
        /// Get entity type from numeral id
        /// </summary>
        /// <param name="id">Entity type ID</param>
        /// <returns>EntityType corresponding to the specified ID</returns>
        public EntityType FromNumId(int id)
        {
            //1.14+ entities have the same set of IDs regardless of living status
            if (entityTypeTable.ContainsKey(id))
                return entityTypeTable[id];

            throw new System.IO.InvalidDataException($"Unknown Entity ID {id} in palette {GetType()}");
        }

        /// <summary>
        /// Get numeral id from entity type identifier
        /// </summary>
        public int ToNumId(ResourceLocation identifier)
        {
            if (dictId.ContainsKey(identifier))
                return dictId[identifier];
            
            throw new System.IO.InvalidDataException($"Unknown Entity Type {identifier}");
        }

        /// <summary>
        /// Get entity type from entity type identifier
        /// </summary>
        public EntityType FromId(ResourceLocation identifier)
        {
            return FromNumId(ToNumId(identifier));
        }

        public IEnumerator PrepareData(string entityVersion, DataLoadFlag flag, LoadStateInfo loadStateInfo)
        {
            // Clear loaded stuff...
            entityTypeTable.Clear();
            dictId.Clear();

            loadStateInfo.infoText = $"Loading entity definitions";

            var entityTypeListPath = PathHelper.GetExtraDataFile($"entity_types-{entityVersion}.json");
            string listsPath  = PathHelper.GetExtraDataFile("entity_lists.json");

            if (!File.Exists(entityTypeListPath) || !File.Exists(listsPath))
            {
                loadStateInfo.infoText = "Entity data not complete!";
                flag.Finished = true;
                flag.Failed = true;
                yield break;
            }

            // First read special item lists...
            var lists = new Dictionary<string, HashSet<ResourceLocation>>();
            lists.Add("contains_item", new());

            Json.JSONData spLists = Json.ParseJson(File.ReadAllText(listsPath, Encoding.UTF8));
            loadStateInfo.infoText = $"Reading special lists from {listsPath}";

            int count = 0, yieldCount = 200;

            foreach (var pair in lists)
            {
                if (spLists.Properties.ContainsKey(pair.Key))
                {
                    foreach (var block in spLists.Properties[pair.Key].DataArray)
                    {
                        pair.Value.Add(ResourceLocation.fromString(block.StringValue));
                        count++;
                        if (count % yieldCount == 0)
                            yield return null;
                    }
                }
            }

            // References for later use
            var containsItem = lists["contains_item"];

            try
            {
                var entityTypeList = Json.ParseJson(File.ReadAllText(entityTypeListPath, Encoding.UTF8));

                foreach (var entityType in entityTypeList.Properties)
                {
                    int numId;
                    if (int.TryParse(entityType.Key, out numId))
                    {
                        var entityTypeId = ResourceLocation.fromString(entityType.Value.StringValue);

                        entityTypeTable.TryAdd(numId, new EntityType(numId,
                                entityTypeId, containsItem.Contains(entityTypeId)));
                        
                        dictId.TryAdd(entityTypeId, numId);
                    }
                    else
                        Debug.LogWarning($"Invalid numeral entity type key [{entityType.Key}]");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error loading entity types: {e.Message}");
                loadStateInfo.infoText = $"Error loading entity types: {e.Message}";
                flag.Failed = true;
            }

            flag.Finished = true;
        }
    }
}
