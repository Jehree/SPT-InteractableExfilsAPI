﻿using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using HarmonyLib;
using InteractableExfilsAPI.Common;
using InteractableExfilsAPI.Components;
using InteractableExfilsAPI.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace InteractableExfilsAPI.Singletons
{
    /// <summary>
    /// Event result containing actions, etc.<br/>
    /// Return <see cref="null"/> to skip adding actions
    /// </summary>
    public class OnActionsAppliedResult
    {
        public List<CustomExfilAction> Actions { get; private set; } = [];
        public Action OnExitZone { get; set; } = () => { };

        public OnActionsAppliedResult()
        {
        }

        public OnActionsAppliedResult(CustomExfilAction action)
        {
            if (action != null)
            {
                Actions = [action];
            }
        }

        public OnActionsAppliedResult(List<CustomExfilAction> actions)
        {
            Actions = actions;
        }

        public OnActionsAppliedResult(Action onExitZone)
        {
            OnExitZone = onExitZone;
        }

        public OnActionsAppliedResult(CustomExfilAction action, Action onExitZone)
        {
            if (action != null)
            {
                Actions = [action];
            }

            OnExitZone = onExitZone;
        }

        public OnActionsAppliedResult(List<CustomExfilAction> actions, Action onExitZone)
        {
            Actions = actions;
            OnExitZone = onExitZone;
        }
    }

    public class InteractableExfilsService
    {
        public bool DisableVanillaActions { get; set; } = false;
        public delegate OnActionsAppliedResult ActionsAppliedEventHandler(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer);

        // other mods can subscribe to this event and optionally pass ActionsTypesClass(es) back to be added to the interactable objects
        public event ActionsAppliedEventHandler OnActionsAppliedEvent;
        private CustomExfilTrigger LastUsedCustomExfilTrigger { get; set; }

        internal void ResetLastUsedCustomExfilTrigger()
        {
            LastUsedCustomExfilTrigger = null;
        }

        private static CustomExfilAction WrapCustomExfilAction(CustomExfilAction action)
        {
            return new CustomExfilAction(action.GetName, action.GetDisabled, () =>
            {
                if (action.GetDisabled())
                {
                    // this is a guard because it's still possible to select a disabled action (even if the player can't)
                    return;
                }

                action.Action();
                RefreshPrompt(); // automatic prompt re-rendering when an action is performed by the user
            });
        }

        public virtual OnActionsAppliedResult OnActionsApplied(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            OnActionsAppliedResult result = new OnActionsAppliedResult();

            List<Action> onExitZoneActions = [];

            if (OnActionsAppliedEvent != null)
            {
                LastUsedCustomExfilTrigger = customExfilTrigger;

                foreach (ActionsAppliedEventHandler handler in OnActionsAppliedEvent.GetInvocationList().Cast<ActionsAppliedEventHandler>())
                {
                    customExfilTrigger.LockedRefreshPrompt = true;
                    OnActionsAppliedResult handlerResult = handler(exfil, customExfilTrigger, exfilIsAvailableToPlayer);
                    customExfilTrigger.LockedRefreshPrompt = false;

                    if (handlerResult == null) continue;

                    List<CustomExfilAction> decoratedActions = handlerResult.Actions.Select(WrapCustomExfilAction).ToList();
                    result.Actions.AddRange(decoratedActions);

                    if (handlerResult.OnExitZone != null)
                    {
                        onExitZoneActions.Add(handlerResult.OnExitZone);
                    }
                }
            }

            result.OnExitZone = () =>
            {
                onExitZoneActions.ForEach(h => h());
            };

            return result;
        }

        /// <summary>
        /// Retrieve the current InteractableExfilsService
        /// </summary>
        /// <returns></returns>
        public static InteractableExfilsService Instance()
        {
            return Singleton<InteractableExfilsService>.Instance;
        }

        /// <summary>
        /// Check if it's the first render for the given prompt.
        /// This is useful in order to initialize a state when the prompt is displayed for the first time
        /// </summary>
        /// <returns></returns>
        public static bool IsFirstRender()
        {
            var session = GetSession();
            if (session == null)
            {
                return false;
            }

            return session.PlayerOwner.AvailableInteractionState.Value == null;
        }

        private static CustomExfilAction GetDebugAction(ExfiltrationPoint exfil)
        {
            return new CustomExfilAction(
                "Print Debug Info To Console",
                false,
                () =>
                {
                    var gameWorld = Singleton<GameWorld>.Instance;
                    var player = gameWorld.MainPlayer;

                    foreach (var req in exfil.Requirements)
                    {
                        ConsoleScreen.Log($"... {req.Requirement.ToString()}");
                    }
                    ConsoleScreen.Log($"Requirements: ");
                    ConsoleScreen.Log($"Chance: {exfil.Settings.Chance}");
                    ConsoleScreen.Log($"Exfil Id: {exfil.Settings.Name}");
                    ConsoleScreen.Log($"EXFIL INFO:\n");

                    ConsoleScreen.Log($"Map Id: {gameWorld.LocationId}");
                    ConsoleScreen.Log($"WORLD INFO:\n");

                    List<string> exfilNames = new List<string>();
                    foreach (var activeExfil in player.gameObject.GetComponent<InteractableExfilsSession>().ActiveExfils)
                    {
                        exfilNames.Add(activeExfil.Settings.Name);
                    }
                    string combinedString = string.Join(", ", exfilNames);
                    ConsoleScreen.Log(combinedString);
                    ConsoleScreen.Log($"Active Exfils:");
                    ConsoleScreen.Log($"Player Rotation (Quaternion): {player.CameraPosition.rotation}");
                    ConsoleScreen.Log($"Player Rotation (Euler): {player.CameraPosition.rotation.eulerAngles}");
                    ConsoleScreen.Log($"Player Position: {player.gameObject.transform.position}");
                    ConsoleScreen.Log($"Profile Side: {player.Side.ToString()}");
                    ConsoleScreen.Log($"Profile Id: {player.ProfileId}");
                    ConsoleScreen.Log($"PLAYER INFO:\n");

                    Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.TradeOperationComplete);
                }
            );
        }

        private static CustomExfilAction GetExfilToggleAction(CustomExfilTrigger customExfilTrigger)
        {
            string customActionName = customExfilTrigger.ExfilEnabled
              ? "Cancel".Localized()
              : "Extraction point".Localized();

            return new CustomExfilAction(
                customActionName,
                false,
                customExfilTrigger.ToggleExfilZoneEnabled
            );
        }

        /// <summary>
        /// Retrieve the current session
        /// </summary>
        /// <returns></returns>
        public static InteractableExfilsSession GetSession()
        {
            if (!Singleton<GameWorld>.Instantiated)
            {
                string errorMsg = "Tried to get InteractableExfilsSession while GameWorld singleton was not instantiated.";
                Plugin.LogSource.LogError(errorMsg);
                ConsoleScreen.LogError(errorMsg);
                return null;
            }

            var session = Singleton<GameWorld>.Instance.MainPlayer.gameObject.GetComponent<InteractableExfilsSession>();

            if (session == null)
            {
                string errorMsg = "Failed to get InteractableExfilsSession component from player, was it added correctly?";
                Plugin.LogSource.LogError(errorMsg);
                ConsoleScreen.LogError(errorMsg);
                return null;
            }

            return session;
        }

        /// <summary>
        /// Refresh the current prompt if available (if not, you'll see a warning in logs)
        /// </summary>
        public static void RefreshPrompt()
        {
            if (Instance().LastUsedCustomExfilTrigger != null)
            {
                Instance().LastUsedCustomExfilTrigger.RefreshPrompt();
            }
            else
            {
                Plugin.LogSource.LogWarning("Cannot refresh prompt because LastUsedCustomExfilTrigger is not found");
            }
        }

        /// <summary>
        /// Get the current prompt state for current player
        /// </summary>
        /// <returns></returns>
        public static BindableState<ActionsReturnClass> GetAvailableInteractionState()
        {
            var session = GetSession();
            if (session == null)
            {
                return null;
            }

            return session.PlayerOwner.AvailableInteractionState;
        }

        private static bool IsExfilLabElevator(ExfiltrationPoint exfil)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld.LocationId == "laboratory" && exfil.Settings.Name.Contains("Elevator");
        }

        private static bool IsExfilInterchangeSafeRoom(ExfiltrationPoint exfil)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            return gameWorld.LocationId.ToLower() == "interchange" && exfil.Settings.Name == "Saferoom Exfil";
        }

        // mostly useful for car exfils
        internal static bool IsExfilShared(ExfiltrationPoint exfil)
        {
            return exfil.Settings.ExfiltrationType == EExfiltrationType.SharedTimer;
        }

        internal static bool IsExfilSwitchLabElevator(Switch @switch)
        {
            if (@switch == null || @switch.ExfiltrationPoint == null)
            {
                return false;
            }

            return IsExfilLabElevator(@switch.ExfiltrationPoint);
        }

        internal static bool IsExfilSwitchInterchangeSafeRoom(Switch @switch)
        {
            if (@switch == null || @switch.ExfiltrationPoint == null)
            {
                return false;
            }

            return IsExfilInterchangeSafeRoom(@switch.ExfiltrationPoint);
        }

        // An exfil is considered as special when we don't want to create any custom exfil zone for those exits.
        // We want to let the game handle the default behaviour, so in addition the user should not be able to cancel the extract here.
        //
        // - Shared exfils (mostly used for car extracts, but we assume that all exfils with a shared timer should be uncancellable)
        // - Laboratory elevator
        // - Interchange Saferoom
        internal static bool IsSpecialExfil(ExfiltrationPoint exfil)
        {
            if (IsExfilShared(exfil)) return true;
            if (IsExfilLabElevator(exfil)) return true;
            if (IsExfilInterchangeSafeRoom(exfil)) return true;

            return false;
        }

        /// <summary>
        /// Handler for unavailable extracts
        /// </summary>
        /// <param name="exfil"></param>
        /// <param name="customExfilTrigger"></param>
        /// <param name="exfilIsAvailableToPlayer"></param>
        /// <returns></returns>
        public OnActionsAppliedResult ApplyUnavailableExtractAction(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            if (!Settings.InactiveExtractsDisplayUnavailable.Value) return null;
            if (exfilIsAvailableToPlayer) return null;

            CustomExfilAction customExfilAction = new(
                "Extract Unavailable",
                true,
                () => { Plugin.LogSource.LogWarning("\"Extract Unavailable\" internal action has been called, this should never happens. (please report an issue if you see this message in your Player.log)"); }
            );

            return new OnActionsAppliedResult(customExfilAction);
        }

        /// <summary>
        /// Handler for default toggle prompt action
        /// </summary>
        /// <param name="exfil"></param>
        /// <param name="customExfilTrigger"></param>
        /// <param name="exfilIsAvailableToPlayer"></param>
        /// <returns></returns>
        public OnActionsAppliedResult ApplyExtractToggleAction(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            if (!exfilIsAvailableToPlayer) return null;

            if (customExfilTrigger.ExfilEnabled && IsSpecialExfil(exfil))
            {
                return null;
            }

            CustomExfilAction customExfilAction = GetExfilToggleAction(customExfilTrigger);

            return new OnActionsAppliedResult(customExfilAction);
        }

        /// <summary>
        /// Handler for debug prompt action
        /// </summary>
        /// <param name="exfil"></param>
        /// <param name="customExfilTrigger"></param>
        /// <param name="exfilIsAvailableToPlayer"></param>
        /// <returns></returns>
        public OnActionsAppliedResult ApplyDebugAction(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            if (!Settings.DebugMode.Value) return null;

            CustomExfilAction customExfilAction = GetDebugAction(exfil);

            return new OnActionsAppliedResult(customExfilAction);
        }
    }
}
