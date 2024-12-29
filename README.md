# Interactable Exfils API

Adds interaction prompts to exfils, letting you toggle the exfil timer on and off (you can finally loot that body without extracting!) as well as API tools for modders to add their own exfil interaction options.

## API Usage (for modders)


### 1. Create your own custom actions

```cs
public static class Examples
{
    // this example will add an enabled static action to every single extract in the game
    public static OnActionsAppliedResult SimpleExample(ExfiltrationPoint exfil, CustomExfilTrigger customExfilTrigger, bool exfilIsAvailableToPlayer)
    {
        bool isDisabled = false;

        CustomExfilAction customExfilAction = new CustomExfilAction(
            "Example Interaction",
            isDisabled,
            () => { NotificationManagerClass.DisplayMessageNotification("Simple Intercation Example Selected!"); }
        );

        return new OnActionsAppliedResult(customExfilAction);
    }
}
```

Check the [Examples](./Examples.cs) class to see more examples

### 2. register your actions with the InteractableExfilsService

You can retrieve the `InteractableExfilsService` in the `Start` method of your `BaseUnityPlugin`

```cs
InteractableExfilsService exfilsService = InteractableExfilsService.Instance();
```

and use it to register your own actions: 

```cs
exfilsService.OnActionsAppliedEvent += Examples.SimpleExample;
```


