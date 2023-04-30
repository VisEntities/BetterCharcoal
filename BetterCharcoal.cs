using Newtonsoft.Json;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Oxide.Plugins
{
    [Info("Better Charcoal", "Dana", "2.2.0")]
    [Description("Say goodbye to charcoal shortages, hello to explosives!")]

    public class BetterCharcoal : RustPlugin
    {
        #region Fields

        private static BetterCharcoal _instance;
        private static Configuration _config;

        private CharcoalController _controller;
        private Coroutine _coroutine;

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

            [JsonProperty(PropertyName = "Enable Electric Furnace Charcoal Production")]
            public bool EnableElectricFurnaceCharcoalProduction { get; set; }

            [JsonProperty(PropertyName = "Electric Furnace Charcoal Yield Interval")]
            public float ElectricFurnaceCharcoalYieldInterval { get; set; }
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
                FuelConsumptionRate = 1,
                EnableElectricFurnaceCharcoalProduction = false,
                ElectricFurnaceCharcoalYieldInterval = 2f
            };
        }

        protected override void LoadConfig()
        {
            base.LoadConfig();
            _config = Config.ReadObject<Configuration>();

            if (string.Compare(_config.Version, Version.ToString()) < 0)
                UpdateConfig();

            SaveConfig();
        }

        protected override void LoadDefaultConfig()
        {
            _config = GetDefaultConfig();
        }

        protected override void SaveConfig()
        {
            Config.WriteObject(_config, true);
        }

        private void UpdateConfig()
        {
            PrintWarning("Detected changes in configuration! Updating...");

            Configuration defaultConfig = GetDefaultConfig();

            if (string.Compare(_config.Version, "1.0.0") < 0)
                _config = defaultConfig;

            if (string.Compare(_config.Version, "2.1.0") < 0)
            {
                _config.ElectricFurnaceCharcoalYieldInterval = defaultConfig.ElectricFurnaceCharcoalYieldInterval;
            }

            if (string.Compare(_config.Version, "2.2.0") < 0)
            {
                _config.EnableElectricFurnaceCharcoalProduction = defaultConfig.EnableElectricFurnaceCharcoalProduction;
            }

            PrintWarning("Configuration update complete! Updated from version " + _config.Version + " to " + Version.ToString());
            _config.Version = Version.ToString();
        }

        #endregion Configuration

        #region Oxide Hooks

        /// <summary>
        /// Hook: Called when a plugin is being initialized.
        /// </summary>
        private void Init()
        {
            _instance = this;
            _controller = new CharcoalController();

            Permission.Register();
        }

        /// <summary>
        /// Hook: Called after server startup is complete and awaits connections or when a plugin is hotloaded.
        /// </summary>
        private void OnServerInitialized()
        {
            StartCoroutine();
        }

        /// <summary>
        /// Hook: Called when a plugin is being unloaded.
        /// </summary>
        private void Unload()
        {
            StopCoroutine();
            _controller.CleanupOvens();

            _config = null;
            _instance = null;
        }

        /// <summary>
        /// Hook: Called after any entity has spawned.
        /// </summary>
        /// <param name="oven"> The oven that has spawned. </param>
        private void OnEntitySpawned(BaseOven oven)
        {
            _controller.Setup(oven);
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
            if (!OvenIsEligible(oven))
                return null;

            CharcoalComponent.GetComponent(oven)?.ConsumeFuel(fuel, burnable);
            return true;
        }

        /// <summary>
        /// Called when an oven is turned on or off.
        /// </summary>
        /// <param name="electricOven"> The electric oven being toggled. </param>
        private void OnOvenToggle(ElectricOven electricOven)
        {
            if (!OvenIsEligible(electricOven) || !_config.EnableElectricFurnaceCharcoalProduction)
                return;

            bool ovenTurnedOn = electricOven.IsOn() ? false : true;
            CharcoalComponent.GetComponent(electricOven).StateChanged(ovenTurnedOn);
        }

        #endregion Oxide Hooks

        #region Coroutine

        private void StartCoroutine()
        {
            _coroutine = ServerMgr.Instance.StartCoroutine(_controller.SetupOvens());
        }

        private void StopCoroutine()
        {
            if (!_coroutine.IsUnityNull())
            {
                ServerMgr.Instance.StopCoroutine(_coroutine);
                _coroutine = null;
            }
        }

        #endregion Coroutine

        #region Controller

        private class CharcoalController
        {
            private HashSet<CharcoalComponent> _components = new HashSet<CharcoalComponent>();

            public void Register(CharcoalComponent component)
            {
                _components.Add(component);
            }

            public void Unregister(CharcoalComponent component)
            {
                _components.Remove(component);
            }

            public void Setup(BaseOven oven)
            {
                CharcoalComponent.InstallComponent(oven, this);
            }

            public IEnumerator SetupOvens()
            {
                WaitForSeconds waitDuration = ConVar.FPS.limit > 80 ? CoroutineEx.waitForSeconds(0.01f) : null;

                foreach (var entity in BaseNetworkable.serverEntities)
                {
                    BaseOven oven = entity as BaseOven;
                    if (!oven.IsValid())
                        continue;

                    CharcoalComponent.InstallComponent(oven, this);

                    if (oven is ElectricOven && oven.IsOn() && _config.EnableElectricFurnaceCharcoalProduction)
                        CharcoalComponent.GetComponent(oven).StateChanged(true);

                    yield return waitDuration;
                }
            }

            public void CleanupOvens()
            {
                foreach (CharcoalComponent component in _components.ToList())
                {
                    component.RemoveComponent();
                }
            }
        }

        #endregion Controller

        #region Component

        private class CharcoalComponent : FacepunchBehaviour
        {
            private BaseOven _oven;
            private CharcoalController _controller;

            private void OnDestroy()
            {
                _controller.Unregister(this);
            }

            public CharcoalComponent InitializeComponent(CharcoalController controller)
            {
                _oven = GetComponent<BaseOven>();

                _controller = controller;
                _controller.Register(this);

                return this;
            }

            public static void InstallComponent(BaseOven oven, CharcoalController controller)
            {
                oven.gameObject.AddComponent<CharcoalComponent>().InitializeComponent(controller);
            }

            public static CharcoalComponent GetComponent(BaseOven oven)
            {
                return oven.gameObject.GetComponent<CharcoalComponent>();
            }

            public void RemoveComponent()
            {
                DestroyImmediate(this);
            }

            #region Furnace Functions

            public void StateChanged(bool ovenTurnedOn)
            {
                if (ovenTurnedOn)
                    InvokeRepeating(YieldCharcoal, 1f, _config.ElectricFurnaceCharcoalYieldInterval);
                else
                    CancelInvoke(YieldCharcoal);
            }

            private void YieldCharcoal()
            {
                if (_config.EnableCharcoalProduction && Random.Range(0, 100) < _config.CharcoalYieldChance)
                {
                    Item item = ItemManager.CreateByName("charcoal", Random.Range(_config.LowestCharcoalYield, _config.HighestCharcoalYield) * _config.CharcoalProductionRate);
                    if (!item.MoveToContainer(_oven.inventory, -1, true, false, null))
                    {
                        _oven.OvenFull();
                        item.Drop(_oven.inventory.dropPosition, _oven.inventory.dropVelocity, default(Quaternion));
                        CancelInvoke(YieldCharcoal);
                    }
                }
            }

            public void ConsumeFuel(Item fuel, ItemModBurnable burnable)
            {
                if (_oven.allowByproductCreation && burnable.byproductItem != null)
                    YieldCharcoal();

                if (fuel.amount <= 1)
                {
                    fuel.Remove(0f);
                    return;
                }

                fuel.UseItem(_config.FuelConsumptionRate);
                fuel.fuel = burnable.fuelAmount;
                fuel.MarkDirty();
            }

            #endregion Furnace Functions
        }

        #endregion Component

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

        /// <summary>
        /// Determines if a oven is eligible for altered charcoal yield based on the owner's permission.
        /// </summary>
        /// <param name="oven"> The oven to check eligibility for. </param>
        /// <returns> True if the oven is eligible, false otherwise. </returns>
        private bool OvenIsEligible(BaseOven oven)
        {
            BasePlayer player = FindPlayerById(oven.OwnerID);

            if (!player.IsValid())
                return false;

            if (!Permission.Verify(player))
                return false;

            return true;
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
                _instance.permission.RegisterPermission(USE, _instance);
            }

            public static bool Verify(BasePlayer player, string permissionName = USE)
            {
                if (_instance.permission.UserHasPermission(player.UserIDString, permissionName))
                    return true;

                return false;
            }
        }

        #endregion Permissions
    }
}