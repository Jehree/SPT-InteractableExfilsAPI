using Comfort.Common;
using EFT;
using EFT.Interactive;
using EFT.UI;
using InteractableExfilsAPI.Common;
using InteractableExfilsAPI.Helpers;
using InteractableExfilsAPI.Singletons;
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

            UpdateExfilPrompt();
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
            InteractableExfilsService.LatestCustomExfilTrigger = null;
            Player player = Singleton<GameWorld>.Instance.GetPlayerByCollider(collider);
            if (player == _session.MainPlayer)
            {
                _playerInTriggerArea = false;
                _session.PlayerOwner.ClearInteractionState();
                ForceSetExfilZoneEnabled(true);
            }
        }

        internal void Init(ExfiltrationPoint exfil, bool exfilIsActiveToPlayer)
        {
            Exfil = exfil;
            ExfilIsActiveToPlayer = exfilIsActiveToPlayer;
        }

        internal ActionsReturnClass CreateExfilPrompt()
        {
            InteractableExfilsService.LatestCustomExfilTrigger = this;
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
            var actions = CustomExfilAction.GetActionsTypesClassList(eventResult.Actions);
            if (actions.Count == 0) return new ActionsReturnClass { Actions = [] };

            actionsReturn = new ActionsReturnClass { Actions = actions };

            if (selectedActionIndex >= actions.Count)
            {
                selectedActionIndex = actions.Count - 1;
            }

            var selectedAction = actions[selectedActionIndex];
            actionsReturn.SelectAction(selectedAction);

            return actionsReturn;
        }

        internal void UpdateExfilPrompt()
        {
            ActionsReturnClass exfilPrompt = CreateExfilPrompt();
            _session.PlayerOwner.AvailableInteractionState.Value = exfilPrompt;
        }

        private void EnableExfilZone()
        {
            Exfil.gameObject.GetComponent<BoxCollider>().enabled = true;
            ExfilEnabled = true;
        }

        private void DisableExfilZone()
        {
            Exfil.gameObject.GetComponent<BoxCollider>().enabled = false;
            ExfilEnabled = false;

        }

        /// <summary>
        /// Force enables or disables a zone, does not do any exfil requirement checks.
        /// </summary>
        public void ForceSetExfilZoneEnabled(bool enabled)
        {
            if (enabled)
            {
                EnableExfilZone();
            }
            else
            {
                DisableExfilZone();
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
                DisableExfilZone();
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.GeneratorTurnOff);
            }
            else
            {
                EnableExfilZone();
                Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.GeneratorTurnOn);
            }
        }
    }
}
