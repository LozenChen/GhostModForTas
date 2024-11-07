
using Monocle;
using System;
using System.Threading;

namespace Celeste.Mod.GhostModForTas.GhostEditor;

public static class OuiCommand {
    // taken from tas helper

    [Command("Oui_Ghost_Editor", "Goto Ghost File Editor")]
    public static void GotoOuiGhostFileEditor() {
        Engine.Scene = OverworldLoaderExt.FastGoto<GhostFileEditorContainer>();
    }
}


internal class OverworldLoaderExt : OverworldLoader {

    private Action<Overworld> overworldFirstAction;
    public OverworldLoaderExt(Overworld.StartMode startMode, HiresSnow snow = null) : base(startMode, snow) {
        Snow = null;
        fadeIn = false;
    }

    public static OverworldLoaderExt FastGoto<T>() where T : Oui {
        return new OverworldLoaderExt(Overworld.StartMode.MainMenu, null).SetOverworldAction(x => x.Goto<T>());
    }

    public override void Begin() {
        Add(new HudRenderer());
        base.RendererList.UpdateLists();
        Session session = null;
        if (SaveData.Instance != null) {
            session = SaveData.Instance.CurrentSession_Safe;
        }
        Entity entity = new Entity {
            new Coroutine(Routine(session))
        };
        Add(entity);
        activeThread = Thread.CurrentThread;
        activeThread.Priority = ThreadPriority.Lowest;
        RunThread.Start(LoadThreadExt, "OVERWORLD_LOADER_EXT", highPriority: true);
    }

    private void LoadThreadExt() {
        base.LoadThread();
        overworldFirstAction?.Invoke(overworld);
    }

    public OverworldLoaderExt SetOverworldAction(Action<Overworld> action) {
        overworldFirstAction = action;
        return this;
    }
}