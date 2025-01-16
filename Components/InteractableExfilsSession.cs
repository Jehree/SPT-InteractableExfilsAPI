using Comfort.Common;
using EFT;
using EFT.Interactive;
using InteractableExfilsAPI.Singletons;
using System.Collections.Generic;
using UnityEngine;

namespace InteractableExfilsAPI.Components
{
    public class InteractableExfilsSession : MonoBehaviour
    {
        public List<ExfiltrationPoint> ActiveExfils { get; private set; } = new List<ExfiltrationPoint>();
        public List<ExfiltrationPoint> InactiveExfils { get; private set; } = new List<ExfiltrationPoint>();
        public List<CustomExfilTrigger> CustomExfilTriggers { get; private set; } = new List<CustomExfilTrigger>();
        public GameWorld World { get; private set; }
        public Player MainPlayer { get; private set; }
        public GamePlayerOwner PlayerOwner { get; private set; }


        internal InteractableExfilsSession()
        {
            InteractableExfilsService.Instance().ResetLastUsedCustomExfilTrigger();
            FillExfilLists();
            CreateAllCustomExfilTriggers();
            World = Singleton<GameWorld>.Instance;
            MainPlayer = World.MainPlayer;
            PlayerOwner = MainPlayer.gameObject.GetComponent<GamePlayerOwner>();
        }

        public void OnDestroy()
        {
            // 1. destroy all triggers to avoid end of raid null refs
            foreach (var trigger in CustomExfilTriggers)
            {
                if (!trigger.IsNullOrDestroyed())
                {

                    GameObject.Destroy(trigger.gameObject);
                }
            }

            // 2. clear the LastUsedCustomExfilTrigger
            InteractableExfilsService.Instance().ResetLastUsedCustomExfilTrigger();
        }

        private void CreateAllCustomExfilTriggers()
        {
            foreach (var exfil in ActiveExfils)
            {
                if (!InteractableExfilsService.IsSpecialExfil(exfil))
                {
                    CreateCustomExfilTriggerObject(exfil, true);
                }
            }
            foreach (var exfil in InactiveExfils)
            {
                if (!InteractableExfilsService.IsSpecialExfil(exfil))
                {
                    CreateCustomExfilTriggerObject(exfil, false);
                }
            }
        }

        private void CreateCustomExfilTriggerObject(ExfiltrationPoint exfil, bool exfilIsActiveToPlayer)
        {
            BoxCollider sourceCollider = exfil.gameObject.GetComponent<BoxCollider>();

            GameObject customExfilTriggerObject = new GameObject();
            customExfilTriggerObject.name = exfil.Settings.Name + "_custom_trigger";
            customExfilTriggerObject.layer = LayerMask.NameToLayer("Triggers");

            BoxCollider targetCollider = customExfilTriggerObject.AddComponent<BoxCollider>();
            targetCollider.center = sourceCollider.center;
            targetCollider.size = sourceCollider.size;
            targetCollider.isTrigger = sourceCollider.isTrigger;

            customExfilTriggerObject.transform.position = exfil.gameObject.transform.position;
            customExfilTriggerObject.transform.rotation = exfil.gameObject.transform.rotation;
            customExfilTriggerObject.transform.localScale = exfil.gameObject.transform.localScale;
            CustomExfilTrigger customExfilTrigger = customExfilTriggerObject.AddComponent<CustomExfilTrigger>();

            customExfilTrigger.Init(exfil, exfilIsActiveToPlayer, []);
            CustomExfilTriggers.Add(customExfilTrigger);
        }

        private void FillExfilLists()
        {
            var gameWorld = Singleton<GameWorld>.Instance;
            var player = gameWorld.MainPlayer;

            ExfiltrationPoint[] exfils = player.Side == EPlayerSide.Savage
                ? gameWorld.ExfiltrationController.ScavExfiltrationPoints
                : gameWorld.ExfiltrationController.ExfiltrationPoints;

            ExfiltrationPoint[] pmcExfils = gameWorld.ExfiltrationController.ExfiltrationPoints;
            ExfiltrationPoint[] scavExfils = gameWorld.ExfiltrationController.ScavExfiltrationPoints;


            if (player.Side == EPlayerSide.Savage)
            {
                AddExfils(scavExfils, pmcExfils);
            }
            else
            {
                AddExfils(pmcExfils, scavExfils);
            }
        }

        private void AddExfils(ExfiltrationPoint[] sameSideExfils, ExfiltrationPoint[] oppositeSideExfils)
        {
            foreach (var exfil in sameSideExfils)
            {
                if (exfil.InfiltrationMatch(Singleton<GameWorld>.Instance.MainPlayer))
                {
                    ActiveExfils.Add(exfil);
                }
                else
                {
                    InactiveExfils.Add(exfil);
                }
            }

            foreach (var exfil in oppositeSideExfils)
            {
                InactiveExfils.Add(exfil);
            }
        }
    }
}
