# Interactable Exfils API

Adds interaction prompts to exfils, letting you toggle the exfil timer on and off (you can finally loot that body without extracting!) as well as API tools for modders to add their own exfil interaction options.

## API Usage (for modders)


### Create your own custom actions

```cs
public static class Examples
{
    // this example will add an enabled static action to every single extract in the game
    public static OnActionsAppliedResult SimpleExample(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
    {
        // this part of the code is ran everytime the prompt is created/refreshed
        // it occurs when:
        // 1. the player enter the exfil zone
        // 2. the player interact with the prompt (i.e. press the "F" key)
        // 3. the player changed a BepInEx config in InteractableExfilsAPI
        // 4. the customExfilTrigger.RefreshPrompt() method has been invoked
        // 5. the InteractableExfilsService.RefreshPrompt() method has been invoked

        bool isDisabled = false;

        // This represent the definition of 1 prompt item
        CustomExfilAction customExfilAction = new CustomExfilAction(
            "Example Interaction",
            isDisabled,
            () => {
                // this part of the code is ran when the player interact with this prompt item
                NotificationManagerClass.DisplayMessageNotification("Simple Interaction Example Selected!");
            }
        );

        // here you have control over the ordering of the actions
        List<CustomExfilAction> actions = [customExfilAction];

        return new OnActionsAppliedResult(actions);
    }
}
```

Check the [Examples](./Examples.cs) class to see more examples

### Register your actions

You can do this whenever you want but the recommended way for doing it as early as possible is in the `Start()` method of your plugin class

```cs
public class Plugin : BaseUnityPlugin {
    private void Awake() {
        // enable your patches here
    }
    private void Start() {
        // retrieve the interactable exfil singleton service
        InteractableExfilsService ieService = InteractableExfilsService.Instance();

        // register SimpleExample handler
        ieService.OnActionsAppliedEvent += Examples.SimpleExample;
    }
}
```

### Disable vanilla actions
If you don't want to let Interactable Exfils API show the car exfils and labs elevator exfils prompts, it's possible to disable them. Be aware that in this situation your mod should handle the extraction logic by itself otherwise the player couldn't extract.

```cs
public static void DisableVanillaAction()
{
    // e.g. disable vanilla action (for cars and labs elevator)
    InteractableExfilsService.Instance().DisableVanillaActions = true;
}
```

### Retrieve all active exfils

If you need a list of all the active exfils in raid, you can get it via the `InteractableExfilsSession` component

```cs
public static List<ExfiltrationPoint> ExampleGetExfils() {
    InteractableExfilsSession session = InteractableExfilsService.GetSession();
    return session.ActiveExfils;
}
```