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

        // it isn't normally necessary to cache these classes, but we are using them in FixedUpdate() so I feel it is best practice here
        private GameWorld _gameWorld;
        private Player _player;
        private GamePlayerOwner _gamePlayerOwner;

        public void Awake()
        {
            _gameWorld = Singleton<GameWorld>.Instance;
            _player = _gameWorld.MainPlayer;
            _gamePlayerOwner = _player.gameObject.GetComponent<GamePlayerOwner>();
        }

        public void Update()
        {
            if (!_playerInTriggerArea) return;
            if (_gamePlayerOwner.AvailableInteractionState.Value != null) return;

            OnActionsAppliedResult eventResult = Singleton<InteractableExfilsService>.Instance.OnActionsApplied(Exfil, this, ExfilIsActiveToPlayer);
            var returnClass = new ActionsReturnClass { Actions = CustomExfilAction.GetActionsTypesClassList(eventResult.Actions) };
            returnClass.InitSelected();

            _gamePlayerOwner.AvailableInteractionState.Value = returnClass;
        }

        public void OnTriggerEnter(Collider collider)
        {
            Player player = Singleton<GameWorld>.Instance.GetPlayerByCollider(collider);
            if (player == _player)
            {
                _playerInTriggerArea = true;

                if (RequiresManualActivation)
                {
                    SetExfilZoneEnabled(false);
                }
                else
                {
                    SetExfilZoneEnabled(Settings.ExtractAreaStartsEnabled.Value);
                }
            }
        }

        public void OnTriggerExit(Collider collider)
        {
            Player player = Singleton<GameWorld>.Instance.GetPlayerByCollider(collider);
            if (player == _player)
            {
                _playerInTriggerArea = false;
                _gamePlayerOwner.ClearInteractionState();
                SetExfilZoneEnabled(true);
            }
        }

        public void SetExfil(ExfiltrationPoint exfil)
        {
            Exfil = exfil;
        }

        public void SetExfilIsActiveToPlayer(bool exfilIsActiveToPlayer)
        {
            ExfilIsActiveToPlayer = exfilIsActiveToPlayer;
        }

        //-116.9174 -18 169.2975
        // labs ele switch -276.1301 -2.3366 -364.7404
        // labs sewer -131.3852 -5.4804 -266.646
        // labs cargo switch -122.9479 -4 -355.3093
        // labs cargo ele -118.9453 4 -406.0783

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

        public void SetExfilZoneEnabled(bool enabled)
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

        public void ToggleExfilZoneEnabled()
        {
            if (Exfil.HasRequirements && !Exfil.HasMetRequirements(_player.ProfileId))
            {
                if (!Exfil.UnmetRequirements(_player).ToArray<ExfiltrationRequirement>().Any<ExfiltrationRequirement>())
                {
                    Singleton<InteractableExfilsService>.Instance.AddPlayerToPlayersMetAllRequirements(Exfil, _player.ProfileId);
                    ToggleExfilZoneEnabled();
                    return;
                }

                string tips = string.Join(", ", Exfil.GetTips(_player.ProfileId));
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
