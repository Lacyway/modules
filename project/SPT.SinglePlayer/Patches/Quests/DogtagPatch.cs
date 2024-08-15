using SPT.Reflection.Patching;
using EFT;
using EFT.InventoryLogic;
using System;
using System.Reflection;
using HarmonyLib;

namespace SPT.SinglePlayer.Patches.Quests
{
    public class DogtagPatch : ModulePatch
    {
        static DogtagPatch()
        {
            _ = nameof(EquipmentClass.GetSlot);
            _ = nameof(DamageInfo.Weapon);
        }

        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(Player), nameof(Player.OnBeenKilledByAggressor));
        }

        /// <summary>
        /// Patch OnBeenKilledByAggressor()
        /// </summary>
        /// <param name="__instance"></param>
        /// <param name="aggressor">Player who killed this individuak</param>
        /// <param name="damageInfo">Data on how they died</param>
        [PatchPostfix]
        private static void PatchPostfix(Player __instance, Player aggressor, DamageInfo damageInfo)
        {
            if (__instance.Profile?.Info?.Side == EPlayerSide.Savage)
            {
                // Scav died, we don't care
                return;
            }

            Item dogtagItem = GetDogTagItemFromPlayerWhoDied(__instance);
            if (dogtagItem == null)
            {
                if (__instance.IsYourPlayer)
                {
                    // Human player, expected behaviour
                    return;
                }

                Logger.LogError($"DogtagPatch error > DogTag slot item on: {__instance.Profile?.Info?.Nickname} is null somehow.");
                return;
            }

            var itemComponent = dogtagItem.GetItemComponent<DogtagComponent>();
            if (itemComponent == null)
            {
                Logger.LogError("DogtagPatch error > DogTagComponent on dog tag slot is null. Something went horrifically wrong!");
                return;
            }

            UpdateDogtagItemWithDeathDetails(__instance, aggressor, damageInfo, itemComponent);
        }

        private static Item GetDogTagItemFromPlayerWhoDied(Player __instance)
        {
            var equipment = __instance.Equipment;
            if (equipment == null)
            {
                Logger.LogError("DogtagPatch error > Player has no equipment");

                return null;
            }

            var dogtagSlot = equipment.GetSlot(EquipmentSlot.Dogtag);
            if (dogtagSlot == null)
            {
                Logger.LogError("DogtagPatch error > Player has no dogtag slot");

                return null;
            }

            var dogtagItem = dogtagSlot?.ContainedItem;

            return dogtagItem;
        }

        private static void UpdateDogtagItemWithDeathDetails(Player __instance, Player aggressor, DamageInfo damageInfo, DogtagComponent itemComponent)
        {
            var victimProfileInfo = __instance.Profile.Info;

            itemComponent.AccountId = __instance.Profile.AccountId;
            itemComponent.ProfileId = __instance.Profile.Id;
            itemComponent.Nickname = victimProfileInfo.Nickname;
            itemComponent.Side = victimProfileInfo.Side;
            itemComponent.KillerName = aggressor.Profile.Info.Nickname;
            itemComponent.Time = DateTime.Now;
            itemComponent.Status = "Killed by ";
            itemComponent.KillerAccountId = aggressor.Profile.AccountId;
            itemComponent.KillerProfileId = aggressor.Profile.Id;

            string weaponName = damageInfo.Weapon?.Name;
            if (weaponName == null)
            {
                Logger.LogWarning($"DogtagPatch error > The weapon used by {itemComponent.KillerName} to kill {itemComponent.Nickname} is null");
                weaponName = "???";
            }

            itemComponent.WeaponName = weaponName;

            if (__instance.Profile.Info.Experience > 0)
            {
                itemComponent.Level = victimProfileInfo.Level;
            }
        }
    }
}