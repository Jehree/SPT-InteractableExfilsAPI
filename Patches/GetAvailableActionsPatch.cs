using EFT;
using EFT.Interactive;
using EFT.UI;
using HarmonyLib;
using InteractableExfilsAPI.Components;
using InteractableExfilsAPI.Singletons;
using SPT.Reflection.Patching;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace InteractableExfilsAPI.Patches
{
    internal class GetAvailableActionsPatch : ModulePatch
    {
        private static MethodInfo _getExfiltrationActions;
        private static MethodInfo _getSwitchActions;

        protected override MethodBase GetTargetMethod()
        {
            _getExfiltrationActions = AccessTools.FirstMethod(
                typeof(GetActionsClass),
                method =>
                method.GetParameters()[0].Name == "owner" &&
                method.GetParameters()[1].ParameterType == typeof(ExfiltrationPoint)
            );

            _getSwitchActions = AccessTools.FirstMethod(
                typeof(GetActionsClass),
                method =>
                method.GetParameters()[0].Name == "owner" &&
                method.GetParameters()[1].ParameterType == typeof(Switch)
            );

            return AccessTools.FirstMethod(typeof(GetActionsClass), method => method.Name == nameof(GetActionsClass.GetAvailableActions) && method.GetParameters()[0].Name == "owner");
        }

        [PatchPrefix]
        public static bool PatchPrefix(object[] __args, ref ActionsReturnClass __result)
        {
            var owner = __args[0] as GamePlayerOwner;
            var interactive = __args[1]; // as GInterface139 as of SPT 3.10.3

            if (IsCarExtract(interactive) || IsInteractableExfil(interactive))
            {
                ExfiltrationPoint exfil = GetExfilPointFromInteractive(interactive);
                if (exfil == null)
                {
                    Plugin.LogSource.LogError("Cannot retrieve exfil point from interactive");
                    return true;
                }

                List<ActionsTypesClass> vanillaActions = GetVanillaInteractionActions(owner, interactive);
                CustomExfilTrigger customTrigger = CreateCustomExfilTrigger(exfil, vanillaActions);
                ActionsReturnClass prompt = customTrigger.CreateExfilPrompt();

                __result = prompt;
                return false;
            }

            return true;
        }

        private static bool IsCarExtract(object interactive)
        {
            if (interactive is not ExfiltrationPoint) return false;
            if (InteractableExfilsService.ExfilIsCar((ExfiltrationPoint)interactive)) return true;
            return false;
        }

        // vanilla interactable exfils (elevator exfils and saferoom exfil)
        private static bool IsInteractableExfil(object interactive)
        {
            // 1. check for car exfils
            if (interactive is ExfiltrationPoint point)
            {
                return InteractableExfilsService.ExfilIsCar(point);
            }

            // 2. check for other exfils (based on a switch)
            if (interactive is Switch interactiveSwitch)
            {
                if (interactiveSwitch == null || interactiveSwitch.ExfiltrationPoint == null)
                {
                    return false;
                }

                if (InteractableExfilsService.ExfilIsLabElevator(interactiveSwitch.ExfiltrationPoint))
                {
                    return true;
                }

                if (InteractableExfilsService.ExfilIsInterchangeSafeRoom(interactiveSwitch.ExfiltrationPoint))
                {
                    // This is to avoid override intermediate switches (like the interchange power switch for example)
                    if (interactiveSwitch.NextSwitches != null && interactiveSwitch.NextSwitches.Length <= 1)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private static ExfiltrationPoint GetExfilPointFromInteractive(object interactive)
        {
            if (interactive is Switch interactiveSwitch) return @interactiveSwitch.ExfiltrationPoint;
            if (interactive is ExfiltrationPoint point) return point;

            return null;
        }

        private static List<ActionsTypesClass> GetVanillaInteractionActions(GamePlayerOwner gamePlayerOwner, object interactive)
        {
            if (InteractableExfilsService.Instance().DisableVanillaActions)
            {
                return [];
            }

            object[] args = [gamePlayerOwner, interactive];

            MethodInfo methodInfo = null;
            if (interactive is ExfiltrationPoint)
            {
                methodInfo = _getExfiltrationActions;
            }
            if (interactive is Switch)
            {
                methodInfo = _getSwitchActions;
            }

            List<ActionsTypesClass> vanillaExfilActions = ((ActionsReturnClass)methodInfo.Invoke(null, args))?.Actions;
            return vanillaExfilActions ?? [];
        }

        private static CustomExfilTrigger CreateCustomExfilTrigger(ExfiltrationPoint exfil, List<ActionsTypesClass> vanillaActions)
        {
            // Create a new GameObject to attach the MonoBehaviour
            GameObject customTriggerObject = new GameObject("CustomExfilTrigger");

            // Add the CustomExfilTrigger component
            CustomExfilTrigger customTrigger = customTriggerObject.AddComponent<CustomExfilTrigger>();

            bool exfilIsActiveToPlayer = true;
            customTrigger.Init(exfil, exfilIsActiveToPlayer, vanillaActions);

            string message = $"GetActionsClassWithCustomActions called for exfil {exfil.Settings.Name}!\n";
            ConsoleScreen.Log(message);
            Plugin.LogSource.LogInfo(message);

            return customTrigger;
        }
    }
}
