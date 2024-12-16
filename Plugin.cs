using BepInEx;
using BepInEx.Logging;
using Comfort.Common;
using InteractableExfilsAPI.Helpers;
using InteractableExfilsAPI.Patches;
using InteractableExfilsAPI.Singletons;
using System.Reflection;

namespace InteractableExfilsAPI
{
    [BepInPlugin("Jehree.InteractableExfilsAPI", "InteractableExfilsAPI", "1.3.1")]
    public class Plugin : BaseUnityPlugin
    {
        public static ManualLogSource LogSource;
        public static string AssemblyPath { get; private set; } = Assembly.GetExecutingAssembly().Location;
        public const string MOD_NAME = "Interactable Exfils API";

        private void Awake()
        {
            LogSource = Logger;
            Settings.Init(Config);

            Singleton<InteractableExfilsService>.Create(new InteractableExfilsService());
            InteractableExfilsService service = Singleton<InteractableExfilsService>.Instance;
            service.OnActionsAppliedEvent += service.ApplyUnavailableExtractAction;
            service.OnActionsAppliedEvent += service.ApplyExtractToggleAction;
            service.OnActionsAppliedEvent += service.ApplyDebugAction;

            new GameStartedPatch().Enable();
            new GetAvailableActionsPatch().Enable();
        }

        private void Start()
        {
            //Singleton<InteractableExfilsService>.Instance.OnActionsAppliedEvent += Examples.SimpleExample;
            //Singleton<InteractableExfilsService>.Instance.OnActionsAppliedEvent += Examples.GoneWhenDisabledExample;
            //Singleton<InteractableExfilsService>.Instance.OnActionsAppliedEvent += Examples.DynamicDisabledExample;
            //Singleton<InteractableExfilsService>.Instance.OnActionsAppliedEvent += Examples.SoftDynamicDisabledExample;
            //Singleton<InteractableExfilsService>.Instance.OnActionsAppliedEvent += Examples.ScavGate3OnlyExample;
            //Singleton<InteractableExfilsService>.Instance.OnActionsAppliedEvent += Examples.RequiresManualActivationsGate3Example;
            //Singleton<InteractableExfilsService>.Instance.OnActionsAppliedEvent += Examples.PromptRefreshingExample;
        }


    }
}
