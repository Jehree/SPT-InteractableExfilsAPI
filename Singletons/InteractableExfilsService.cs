using Comfort.Common;
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
        private readonly FieldInfo _exfilPlayersMetAllRequirementsFieldInfo = AccessTools.Field(typeof(ExfiltrationPoint), "_playersMetAllRequirements");
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

        public static InteractableExfilsService Instance()
        {
            return Singleton<InteractableExfilsService>.Instance;
        }

        public static bool IsFirstRender()
        {
            var session = GetSession();
            if (session == null)
            {
                return false;
            }

            return session.PlayerOwner.AvailableInteractionState.Value == null;
        }

        public static CustomExfilAction GetDebugAction(ExfiltrationPoint exfil)
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

        public static CustomExfilAction GetExfilToggleAction(CustomExfilTrigger customExfilTrigger)
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

        public static void RefreshPrompt()
        {
            if (Instance().LastUsedCustomExfilTrigger != null)
            {
                Instance().LastUsedCustomExfilTrigger.RefreshPrompt();
            }
            else
            {
                Plugin.LogSource.LogError("Cannot refresh prompt because LastUsedCustomExfilTrigger is not found");
            }
        }

        public static BindableState<ActionsReturnClass> GetAvailableInteractionState()
        {
            var session = GetSession();
            if (session == null)
            {
                return null;
            }

            return session.PlayerOwner.AvailableInteractionState;
        }

        internal static bool ExfilIsLabElevator(ExfiltrationPoint exfil)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld.LocationId == "laboratory" && exfil.Settings.Name.Contains("Elevator"))
            {
                return true;
            }

            return false;
        }

        internal static bool ExfilIsInterchangeSafeRoom(ExfiltrationPoint exfil)
        {
            GameWorld gameWorld = Singleton<GameWorld>.Instance;
            if (gameWorld.LocationId.ToLower() == "interchange" && exfil.Settings.Name == "Saferoom Exfil")
            {
                return true;
            }

            return false;
        }

        // mostly useful for car exfils
        internal static bool ExfilIsShared(ExfiltrationPoint exfil)
        {
            return exfil.Settings.ExfiltrationType == EExfiltrationType.SharedTimer;
        }

        // A special exfil is an exfil that don't need any custom exfil trigger
        internal static bool IsSpecialExfil(ExfiltrationPoint exfil)
        {
            if (ExfilIsShared(exfil)) return true;
            if (ExfilIsLabElevator(exfil)) return true;
            if (ExfilIsInterchangeSafeRoom(exfil)) return true;

            return false;
        }

        internal void AddPlayerToPlayersMetAllRequirements(ExfiltrationPoint exfil, string profileId)
        {
            List<string> playerIdList = _exfilPlayersMetAllRequirementsFieldInfo.GetValue(exfil) as List<string>;
            if (playerIdList.Contains(profileId)) return;
            playerIdList.Add(profileId);
            _exfilPlayersMetAllRequirementsFieldInfo.SetValue(exfil, playerIdList);
        }

        public OnActionsAppliedResult ApplyUnavailableExtractAction(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            if (!Settings.InactiveExtractsDisplayUnavailable.Value) return null;
            if (exfilIsAvailableToPlayer) return null;

            CustomExfilAction customExfilAction = new(
                "Extract Unavailable",
                true,
                () => { Plugin.LogSource.LogInfo("this won't ever run"); }
            );

            return new OnActionsAppliedResult(customExfilAction);
        }

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

        public OnActionsAppliedResult ApplyDebugAction(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            if (!Settings.DebugMode.Value) return null;

            CustomExfilAction customExfilAction = GetDebugAction(exfil);

            return new OnActionsAppliedResult(customExfilAction);
        }
    }
}
