﻿using Aki.Reflection.Patching;
using EFT.Vehicle;
using HarmonyLib;
using System.Reflection;
using UnityEngine;

namespace Aki.Debugging.BTR.Patches
{
    public class BTRTurretDefaultAimingPositionPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return AccessTools.Method(typeof(BTRTurretServer), "Start");
        }

        [PatchPrefix]
        public static bool PatchPrefix(BTRTurretServer __instance)
        {
            __instance.defaultAimingPosition = Vector3.zero;

            return false;
        }
    }
}