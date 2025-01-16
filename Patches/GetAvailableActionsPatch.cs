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
            if (interactive is not Switch) return false;
            Switch switcheroo = interactive as Switch;
            if (switcheroo == null || switcheroo.ExfiltrationPoint == null) return false;
            if (!InteractableExfilsService.ExfilIsInteractable(switcheroo.ExfiltrationPoint)) return false;

            Plugin.LogSource.LogInfo($"Debug switch.Interaction: {switcheroo.Interaction}");
            Plugin.LogSource.LogInfo($"Debug switch.NextSwitches.Length: {switcheroo.NextSwitches.Length}");
            Plugin.LogSource.LogInfo($"Debug switch.ExtractionZoneTip: {switcheroo.ExtractionZoneTip}");
            Plugin.LogSource.LogInfo($"Debug switch.ConditionStatus.Length: {switcheroo.ConditionStatus.Length}");
            Plugin.LogSource.LogInfo($"Debug switch.PreviousSwitch (exist?): {switcheroo.PreviousSwitch != null}");
            Plugin.LogSource.LogInfo($"Debug switch.TargetStatus: {switcheroo.TargetStatus}");
            Plugin.LogSource.LogInfo($"Debug switch.ContextMenuTip: {switcheroo.ContextMenuTip}");

            // This is to avoid overriding intermediate switches (like the interchange power switch for example)
            if (switcheroo.NextSwitches != null && switcheroo.NextSwitches.Length > 1)
            {
                return false;
            }

            return true;
        }

        private static ExfiltrationPoint GetExfilPointFromInteractive(object interactive)
        {
            if (interactive is Switch @switch) return @switch.ExfiltrationPoint;
            return interactive as ExfiltrationPoint;
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
