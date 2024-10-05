using Celeste.Mod.GravityHelper.Components;
using MonoMod.ModInterop;
using System;

namespace Celeste.Mod.GhostModForTas.ModInterop;

public static class ModImports {

    public static bool GravityHelperInstalled;

    public static bool ExtendedVariantInstalled;
    public static bool IsPlayerInverted => GravityHelperInstalled && GravityHelperImport.IsPlayerInverted.Invoke();

    public static bool IsActorInverted(Actor actor) => GravityHelperInstalled && GravityHelperImport.IsActorInverted.Invoke(actor);

    public static int InvertedType;

    public static bool UpsideDown => ExtendedVariantInstalled && (bool)ExtendedVariantImport.GetCurrentVariantValue("UpsideDown");

    public static void SetActorGravity(Actor actor, bool inverted) {
        if (GravityHelperInstalled) {
            GravityHelperImport.SetActorGravity.Invoke(actor, inverted ? InvertedType : 0, 1f);
        }
    }

    public static void AddGravityComponent(Actor actor) {
        if (GravityHelperInstalled) {
            actor.Add(CreateGravityComponent());
        }
    }

    private static Monocle.Component CreateGravityComponent() {
        GravityComponent component = new GravityComponent();
        component.UpdatePosition = (_) => { };
        component.UpdateColliders = (_) => { };
        component.UpdateVisuals = (_) => { };
        component.UpdateSpeed = (_) => { };
        return component;
    }

    [Initialize]
    private static void Initialize() {
        typeof(GravityHelperImport).ModInterop();
        GravityHelperInstalled = GravityHelperImport.IsPlayerInverted is not null;
        InvertedType = GravityHelperImport.GravityTypeToInt?.Invoke("Inverted") ?? 0;

        typeof(ExtendedVariantImport).ModInterop();
        ExtendedVariantInstalled = ExtendedVariantImport.GetCurrentVariantValue is not null;
    }
}


[ModImportName("GravityHelper")]
internal static class GravityHelperImport {
    public static Func<bool> IsPlayerInverted;

    public static Action<Actor, int, float> SetActorGravity;

    public static Func<string, int> GravityTypeToInt;

    public static Func<Actor, bool> IsActorInverted;
}

[ModImportName("ExtendedVariantMode")]
internal static class ExtendedVariantImport {
    public static Func<string, object> GetCurrentVariantValue;
}