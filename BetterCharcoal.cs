using System.Collections.Generic;
using System;
using Newtonsoft.Json;
using Oxide.Core.Plugins;
using Oxide.Core.Configuration;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Better Charcoal", "Dana", "2.0.0")]
    [Description("Say goodbye to charcoal shortages, hello to explosives!")]

    public class BetterCharcoal : RustPlugin
    {
        #region Fields

        private static BetterCharcoal instance;
        private static Configuration config;

        #endregion Fields

        #region Configuration

        private class Configuration
        {
            [JsonProperty(PropertyName = "Version")]
            public string Version { get; set; }

            [JsonProperty(PropertyName = "Enable Charcoal Production")]
            public bool EnableCharcoalProduction { get; set; }

            [JsonProperty(PropertyName = "Charcoal Yield Chance")]
            public int CharcoalYieldChance { get; set; }

            [JsonProperty(PropertyName = "Lowest Charcoal Yield")]
            public int LowestCharcoalYield { get; set; }

            [JsonProperty(PropertyName = "Highest Charcoal Yield")]
            public int HighestCharcoalYield { get; set; }

            [JsonProperty(PropertyName = "Charcoal Production Rate")]
            public int CharcoalProductionRate { get; set; }

            [JsonProperty(PropertyName = "Fuel Consumption Rate")]
            public int FuelConsumptionRate { get; set; }
        }

        private Configuration GetDefaultConfig()
        {
            return new Configuration
            {
                Version = Version.ToString(),
                EnableCharcoalProduction = true,
                CharcoalYieldChance = 75,
                LowestCharcoalYield = 1,
                HighestCharcoalYield = 1,
                CharcoalProductionRate = 1,
                FuelConsumptionRate = 1
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            config = Config.ReadObject<Configuration>();

            if (string.Compare(config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Detected changes in configuration! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(config.Version, "1.0.0") < 0)
                config = defaultConfig;

            PrintWarning("Configuration update complete! Updated from version " + config.Version + " to " + Version.ToString());
            config.Version = Version.ToString();
        }

        #endregion Configuration

        #region Oxide Hooks

        /// <summary>
        /// Hook: Called when a plugin is being initialized.
        /// </summary>
        private void Init()
        {
            instance = this;
            Permission.Register();
        }

        /// <summary>
        /// Hook: Called when a plugin is being unloaded.
        /// </summary>
        private void Unload()
        {
            config = null;
            instance = null;
        }

        /// <summary>
        /// Hook: Called right before fuel starts being consumed in the oven.
        /// </summary>
        /// <param name="oven"> The oven being used. </param>
        /// <param name="fuel"> The fuel item being consumed. </param>
        /// <param name="burnable"> The burnable component of the fuel item. </param>
        /// <returns> True or false if the fuel consumption should be skipped, null otherwise. </returns>
        private object OnFuelConsume(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            BasePlayer player = FindPlayerById(oven.OwnerID);
            if (!player.IsValid() || !Permission.Verify(player))
                return null;

            ProcessFuelConsumption(oven, fuel, burnable);
            return true;
        }

        #endregion Oxide Hooks

        #region Fuel Consumption

        /// <summary>
        /// Processes the consumption of fuel in the oven and produces charcoal if conditions are met.
        /// </summary>
        /// <param name="oven"> The oven being used. </param>
        /// <param name="fuel"> The fuel item being consumed. </param>
        /// <param name="burnable"> The burnable component of the fuel item. </param>
        /// <remarks> Substantially similar to the ConsumeFuel method in the BaseOven class. </remarks>
        private void ProcessFuelConsumption(BaseOven oven, Item fuel, ItemModBurnable burnable)
        {
            if (oven.allowByproductCreation && burnable.byproductItem != null)
            {
                if (config.EnableCharcoalProduction && Random.Range(0, 100) < config.CharcoalYieldChance)
                {
                    Item item = ItemManager.Create(burnable.byproductItem, Random.Range(config.LowestCharcoalYield, config.HighestCharcoalYield) * config.CharcoalProductionRate);
                    if (!item.MoveToContainer(oven.inventory, -1, true, false, null))
                    {
                        oven.OvenFull();
                        item.Drop(oven.inventory.dropPosition, oven.inventory.dropVelocity, default(Quaternion));
                    }
                }
            }

            if (fuel.amount <= 1)
            {
                fuel.Remove(0f);
                return;
            }

            fuel.UseItem(config.FuelConsumptionRate);
            fuel.fuel = burnable.fuelAmount;
            fuel.MarkDirty();
        }

        #endregion Fuel Consumption

        #region Helper Functions

        /// <summary>
        /// Finds a player by their unique player id and returns the BasePlayer object.
        /// </summary>
        /// <param name="playerId"> The  id of the player to find. </param>
        /// <returns> The BasePlayer object of the player with the specified id, or null if not found. </returns>
        private BasePlayer FindPlayerById(ulong playerId)
        {
            return RelationshipManager.FindByID(playerId) ?? null;
        }

        #endregion Helper Functions

        #region Permissions

        /// <summary>
        /// Contains utility methods for checking and registering plugin permissions.
        /// </summary>
        private static class Permission
        {
            public const string USE = "bettercharcoal.use";

            public static void Register()
            {
                instance.permission.RegisterPermission(USE, instance);
            }

            public static bool Verify(BasePlayer player, string permissionName = USE)
            {
                if (instance.permission.UserHasPermission(player.UserIDString, permissionName))
                    return true;

                return false;
            }
        }

        #endregion Permissions
    }
}
