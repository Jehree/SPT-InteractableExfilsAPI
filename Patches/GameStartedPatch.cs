using Comfort.Common;
using EFT;
using InteractableExfilsAPI.Components;
using SPT.Reflection.Patching;
using System.Reflection;

namespace InteractableExfilsAPI.Patches
{
    internal class GameStartedPatch : ModulePatch
    {
        protected override MethodBase GetTargetMethod()
        {
            return typeof(GameWorld).GetMethod(nameof(GameWorld.OnGameStarted));
        }

        [PatchPrefix]
        protected static bool PatchPrefix()
        {
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            player.gameObject.AddComponent<InteractableExfilsSession>();
            return true;
        }
    }
}
