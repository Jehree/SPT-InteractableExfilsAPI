# Interactable Exfils API

Adds interaction prompts to exfils, letting you toggle the exfil timer on and off (you can finally loot that body without extracting!) as well as API tools for modders to add their own exfil interaction options.

## API Usage (for modders)

### Initial setup in your project
The only thing you need to do to start development with Interactable Exfils API is to reference the dll in your `.csproj` file: 

```xml
<ItemGroup>
    <Reference Include="InteractableExfilsAPI">
        <HintPath>$(PathToSPT)\BepInEx\plugins\InteractableExfilsAPI.dll</HintPath>
    </Reference>
</ItemGroup>
```

Then you have to create at least one handler and register it with the instance of the `InteractableExfilsService`

### Create your custom handler

```cs
// This example will add an enabled static action to every single extract in the game
public static class Examples
{
    // this static function is an exfil actions handler you can register with the InteractableExfilsService
    public static OnActionsAppliedResult SimpleExample(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
    {
        // This part of the code is ran everytime the prompt is created/refreshed
        // It occurs when:
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
                // This part of the code is ran when the player interact with this prompt item
                NotificationManagerClass.DisplayMessageNotification("Simple Interaction Example Selected!");
            }
        );

        // Here you have control over the ordering of the actions
        List<CustomExfilAction> actions = [customExfilAction];

        return new OnActionsAppliedResult(actions);
    }
}
```

Take a look to the [Examples class](./Examples.cs) for more.

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

## Mods that use Interactable Exfils API
If your mod use Interactable Exfils API, please make a PR here so we can point it as an example.

- [Path To Tarkov](https://hub.sp-tarkov.com/files/file/569-path-to-tarkov/): used in [PTT.Services.ExfilPromptService](https://github.com/guillaumearm/PathToTarkov/blob/cc5a24140ae3acd9e212b9e73729e42b77780a7d/PTT-Plugin/Services/ExfilPromptService.cs)
