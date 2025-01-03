﻿using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using InteractableExfilsAPI.Common;
using InteractableExfilsAPI.Helpers;
using InteractableExfilsAPI.Singletons;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace InteractableExfilsAPI.Components
{
    public class CustomExfilTrigger : MonoBehaviour, IPhysicsTrigger
    {
        public ExfiltrationPoint Exfil { get; private set; }
        public string Description { get; } = "Custom Exfil Trigger";
        public bool ExfilEnabled { get; private set; } = true;
        public bool RequiresManualActivation { get; set; } = false;
        public bool ExfilIsActiveToPlayer { get; private set; }

        // this is used to forbid the usage of the RefreshPrompt feature in the handler (to avoid infinite loop)
        internal bool LockedRefreshPrompt { get; set; } = false;

        private List<ActionsTypesClass> VanillaBaseActions { get; set; } = [];
        private bool _playerInTriggerArea = false;

        private InteractableExfilsSession _session;

        public void Awake()
        {
            _session = InteractableExfilsService.GetSession();
        }

        public void Update()
        {
            if (!_playerInTriggerArea) return;
            if (_session.PlayerOwner.AvailableInteractionState.Value != null) return;

            UpdateExfilPrompt(true);
        }

        public void OnTriggerEnter(Collider collider)
        {
            Player player = Singleton<GameWorld>.Instance.GetPlayerByCollider(collider);
            if (player == _session.MainPlayer)
            {
                _playerInTriggerArea = true;

                if (RequiresManualActivation)
                {
                    ForceSetExfilZoneEnabled(false);
                }
                else
                {
                    ForceSetExfilZoneEnabled(Settings.ExtractAreaStartsEnabled.Value);
                }
            }
        }

        public void OnTriggerExit(Collider collider)
        {
            Player player = Singleton<GameWorld>.Instance.GetPlayerByCollider(collider);
            if (player == _session.MainPlayer)
            {
                _playerInTriggerArea = false;
                _session.PlayerOwner.ClearInteractionState();
                ForceSetExfilZoneEnabled(true);
            }
        }

        internal void Init(ExfiltrationPoint exfil, bool exfilIsActiveToPlayer, List<ActionsTypesClass> vanillaBaseActions)
        {
            Exfil = exfil;
            ExfilIsActiveToPlayer = exfilIsActiveToPlayer;
            VanillaBaseActions = vanillaBaseActions;
        }

        internal ActionsReturnClass CreateExfilPrompt()
        {
            ActionsReturnClass actionsReturn = _session.PlayerOwner.AvailableInteractionState.Value;

            var selectedActionIndex = 0;
            if (actionsReturn != null)
            {
                selectedActionIndex = actionsReturn.Actions.IndexOf(actionsReturn.SelectedAction);
                if (selectedActionIndex == -1)
                {
                    selectedActionIndex = 0;
                }
            }

            OnActionsAppliedResult eventResult = Singleton<InteractableExfilsService>.Instance.OnActionsApplied(Exfil, this, ExfilIsActiveToPlayer);
            if (RequiresManualActivation) // this is needed to be checked after the handler has been applied since the handled can modify this prop
            {
                ForceSetExfilZoneEnabled(false);
            }

            var actions = VanillaBaseActions.Concat(CustomExfilAction.GetActionsTypesClassList(eventResult.Actions)).ToList();

            var newActionsReturn = new ActionsReturnClass { Actions = actions };

            if (selectedActionIndex >= actions.Count)
            {
                selectedActionIndex = actions.Count - 1;
            }

            var selectedAction = actions[selectedActionIndex];
            newActionsReturn.SelectAction(selectedAction);

            return newActionsReturn;
        }

        internal void UpdateExfilPrompt(bool forceCreation)
        {
            if (forceCreation || _session.PlayerOwner.AvailableInteractionState.Value != null)
            {
                ActionsReturnClass exfilPrompt = CreateExfilPrompt();
                _session.PlayerOwner.AvailableInteractionState.Value = exfilPrompt;
            }
        }


        /// <summary>
        /// Force enables or disables a zone, does not do any exfil requirement checks.
        /// </summary>
        public void ForceSetExfilZoneEnabled(bool enabled)
        {
            ExfilEnabled = enabled;

            var collider = Exfil.gameObject?.GetComponent<BoxCollider>();
            if (collider != null)
            {
                collider.enabled = enabled;
            }
        }

        /// <summary>
        /// Toggles exfil zone enabled normally. Does exfil requirement checks and gives the player tips on missing requirements if they are not met.
        /// </summary>
        public void ToggleExfilZoneEnabled()
        {
            if (Exfil.HasRequirements && !Exfil.HasMetRequirements(_session.MainPlayer.ProfileId))
            {
                if (!Exfil.UnmetRequirements(_session.MainPlayer).ToArray<ExfiltrationRequirement>().Any<ExfiltrationRequirement>())
                {
                    Singleton<InteractableExfilsService>.Instance.AddPlayerToPlayersMetAllRequirements(Exfil, _session.MainPlayer.ProfileId);
                    ToggleExfilZoneEnabled();
                    return;
                }

                string tips = string.Join(", ", Exfil.GetTips(_session.MainPlayer.ProfileId));
                ConsoleScreen.Log($"You have not met the extract requirements for {Exfil.Settings.Name}!");
                NotificationManagerClass.DisplayWarningNotification($"{tips}");
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ErrorMessage);
                return;
            }

            if (ExfilEnabled)
            {
                ForceSetExfilZoneEnabled(false);
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.GeneratorTurnOff);
            }
            else
            {
                ForceSetExfilZoneEnabled(true);
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.GeneratorTurnOn);
            }
        }

        public void RefreshPrompt()
        {
            if (LockedRefreshPrompt)
            {
                Plugin.LogSource.LogError("RefreshPrompt cannot be called inside the handler");
            }
            else
            {

                UpdateExfilPrompt(false);
            }
        }
    }
}
