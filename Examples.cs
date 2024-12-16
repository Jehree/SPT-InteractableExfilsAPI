using EFT.Interactive;
using EFT;
using InteractableExfilsAPI.Common;
using InteractableExfilsAPI.Singletons;
using EFT.UI;
using InteractableExfilsAPI.Helpers;
using Comfort.Common;
using InteractableExfilsAPI.Components;
using System.Collections.Generic;

namespace InteractableExfilsAPI
{
    internal static class Examples
    {
        // this example will add an enabled static action to every single extract in the game
        public static OnActionsAppliedResult SimpleExample(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            CustomExfilAction customExfilAction = new CustomExfilAction(
                "Example Interaction",
                false,
                () => { NotificationManagerClass.DisplayMessageNotification("Simple Intercation Example Selected!"); }
            );
            
            return new OnActionsAppliedResult(customExfilAction);
        }



        public static OnActionsAppliedResult ScavGate3OnlyExample(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            // return null to skip adding an action to certain exfils. In this case, we are only adding the action when the player is a scav.
            Player player = Singleton<GameWorld>.Instance.MainPlayer;
            if (player.Side == EPlayerSide.Bear || player.Side == EPlayerSide.Usec) return null;

            // ...and only when it is the exfil with the name "Gate 3"
            if (exfil.Settings.Name != "Gate 3") return null;

            // NOTE: since this code will be running during a raid, you can safely access the Player and the GameWorld to check for additional conditions or acquire info if desired:
            GameWorld gameWorld = Singleton<GameWorld>.Instance;

            if (!gameWorld.LocationId.Contains("factory")) return null;

            CustomExfilAction customExfilAction = new CustomExfilAction(
                "Example Scav Only Gate 3 Interaction",
                false,
                () => { NotificationManagerClass.DisplayMessageNotification($"Simple Intercation Example Selected by profile: {player.ProfileId}"); }
            );

            return new OnActionsAppliedResult(customExfilAction);
        }



        // NOTE: disabled state will only update when an interaction menu first appears, this means if it is updated while the player is inside an exfil area,
        // they will have to either exit and re-enter it, or look at something else like loot on the ground and then away again to get the interaction menu to refresh.
        // for these reasons, I recommend only using this when you know that the player cannot be inside the exfil area when the disabled condition is updated.
        public static OnActionsAppliedResult DynamicDisabledExample(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            CustomExfilAction customExfilAction = new CustomExfilAction(
                "I'm only active when Debug Mode is on (hard disable)",
                () => { return !Settings.DebugMode.Value; }, // passing a function that returns a bool here will allow the action to dynamically update it's disabled state
                () => { NotificationManagerClass.DisplayMessageNotification("Dynamic Disabled Example (hard) Selected!"); }
            );

            return new OnActionsAppliedResult(customExfilAction);
        }



        // this example is the same as above, but the action will entirely be absent from the list if it is "disabled"
        public static OnActionsAppliedResult GoneWhenDisabledExample(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            if (!Settings.DebugMode.Value) return null;

            CustomExfilAction customExfilAction = new CustomExfilAction(
                "I'm only present in the interactions menu when enabled!",
                false, // leave interaction enabled, we just won't add it at all when it's disabled state is met
                () => { NotificationManagerClass.DisplayMessageNotification("Gone When Disabled Selected!"); }
            );

            return new OnActionsAppliedResult(customExfilAction);
        }



        // If you need the interaction to update while the player is inside the exfil, consider doing a "soft" disable behavior like below:
        public static OnActionsAppliedResult SoftDynamicDisabledExample(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            CustomExfilAction customExfilAction = new CustomExfilAction(
                "I'm only active when Debug Mode is on (soft disable)",
                false, // leave the action itself always enabled
                () =>
                {
                    if ( !Settings.DebugMode.Value )
                    {
                        // check your disabled condition inside the action, and display a warning notif and an error sound if it isn't met followed by a return.
                        // this does a decent job of maintaining good player feedback so they know why the interaction didn't work, while allowing you to capture
                        // the timing of the moment when the player selects the interaction.
                        // NOTE: this is exactly how the built in "Extract" toggle action itself is set up.
                        NotificationManagerClass.DisplayWarningNotification("Debug mode not enabled!");
                        Singleton<GUISounds>.Instance.PlayUISound(EUISoundType.ErrorMessage);
                        return;
                    }

                    NotificationManagerClass.DisplayMessageNotification("Dynamic Disabled Example (soft) Selected!");
                }
            );

            return new OnActionsAppliedResult(customExfilAction);
        }

        // this will cause the mod to ignore ExtractAreaStartsEnabled in the config and always require the mod to be enabled manually
        public static OnActionsAppliedResult RequiresManualActivationsGate3Example(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            // only when it is the exfil with the name "Gate 3"
            if (exfil.Settings.Name == "Gate 3")
            {
                customExfilTrigger.RequiresManualActivation = true;
            }

            return null;
        }


        // prompt refreshing example
        public static int Counter = 1;
        public static OnActionsAppliedResult PromptRefreshingExample(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
        {
            CustomExfilAction increaseCounterAction = new CustomExfilAction(
                $"Increase Counter: {Counter}",
                false,
                () =>
                {
                    Counter++;
                    InteractableExfilsService.RefreshPrompt();
                }
            );
            CustomExfilAction decreaseCounterAction = new CustomExfilAction(
                $"Decrease Counter: {Counter}",
                false,
                () =>
                {
                    Counter--;
                    InteractableExfilsService.RefreshPrompt();
                }
            );

            List<CustomExfilAction> actions = new List<CustomExfilAction>();
            actions.Add(increaseCounterAction);
            actions.Add(decreaseCounterAction);

            return new OnActionsAppliedResult(actions);
        }
    }
}
