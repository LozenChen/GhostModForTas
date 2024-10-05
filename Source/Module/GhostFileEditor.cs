using Celeste.Mod.GhostModForTas.Recorder.Data;
using Celeste.Mod.GhostModForTas.Replayer;
using Celeste.Mod.GhostModForTas.Utils;
using Celeste.Mod.UI;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Celeste.Mod.GhostModForTas.Module;

public class GhostFileEditorContainer : Oui, OuiModOptions.ISubmenu {

    public Dictionary<Guid, List<GhostData>> Dictionary = new();

    public GhostFileEditor Editor;

    public float ease;

    public void Init() {
        if (Dictionary.IsNullOrEmpty()) {
            Dictionary = GhostData.GhostFileEditorHelper.GetGhostFileInfo();
        }
    }

    public override IEnumerator Enter(Oui from) {
        Init();
        Engine.Scene.Add(Editor = new GhostFileEditor(this));
        Editor.Focused = true;
        yield return null;
    }

    public override IEnumerator Leave(Oui next) {
        Engine.Scene.Remove(Editor);
        yield return null;
    }
}
public class GhostFileEditor : TextMenu {

    public GhostFileEditorContainer GrandParent;
    private Dictionary<Guid, List<GhostData>> Dict => GrandParent.Dictionary;

    public int LeftRightIndex = -1;

    public void OnLeftRightPressed(int dir) {
        if (dir > 0) {
            LeftRightIndex = LeftRightIndex switch {
                -1 => 0,
                0 => 1,
                1 => -1,
                _ => -1
            };
        } else if (dir < 0) {
            LeftRightIndex = LeftRightIndex switch {
                -1 => 1,
                1 => 0,
                0 => -1,
                _ => -1
            };
        }
    }

    public class SingleGhostFile : TextMenu.Item {

        public GhostFileEditor Parent;

        public Guid guid;

        public string fileName;

        public float tempHeight;

        public float heightDecreaseSpeed = 10f;

        public string ghostName;

        public bool dark;

        public SingleGhostFile(Guid guid, GhostFileEditor editor) {
            this.guid = guid;
            Parent = editor;
            Selectable = true;
            OnPressed = OnPress;
            Init();
        }

        public override void Update() {
            base.Update();
            if (tempHeight >= heightDecreaseSpeed) {
                tempHeight -= heightDecreaseSpeed;
            } else if (tempHeight > 0) {
                tempHeight = 0;
            }
        }

        private string delayedNameChange;

        public void OnPress() {
            switch (Parent.LeftRightIndex) {
                case 0: {
                    OuiModOptionFileName.DefaultString = ghostName;
                    Audio.Play("event:/ui/main/savefile_rename_start");
                    Parent.SceneAs<Overworld>().Goto<OuiModOptionFileName>()
                        .Init<GhostFileEditorContainer>(ghostName,
                            value => delayedNameChange = value, confirm => {
                                if (confirm) {
                                    SetName(delayedNameChange);
                                };
                                delayedNameChange = null;
                            },
                            ModOptionsMenu.MaxNameLength, 1);
                    break;
                }
                case 1: {
                    Audio.Play(SFX.ui_main_savefile_delete);
                    Delete();
                    break;
                }
                default: {
                    Audio.Play(SFX.ui_main_button_invalid);
                    break;
                }
            }
        }

        public override float Height() {
            return ActiveFont.LineHeight + tempHeight;
        }

        public override float LeftWidth() {
            return ActiveFont.Measure(fileName).X;
        }

        public override float RightWidth() {
            return ActiveFont.Measure(separator + reName + delete).X;
        }

        public override void LeftPressed() {
            Parent.OnLeftRightPressed(-1);
        }

        public override void RightPressed() {
            Parent.OnLeftRightPressed(1);
        }

        private string separator = "    |    ";

        private string reName = "Rename".ToDialogText();

        private string delete = "Delete".ToDialogText();

        public override void Render(Vector2 position, bool highlighted) {
            RenderImpl(position + tempHeight * Vector2.UnitY, highlighted);
        }

        private Color GetColor(int index, bool highlight) {
            return highlight && index == Parent.LeftRightIndex ? Container.HighlightColor : (index < 0 && dark ? Color.SlateGray : Color.White);
        }
        public void RenderImpl(Vector2 position, bool highlighted) {
            float scale = position.X < 0f ? 1860f / Parent.Width : 1f;
            float alpha = Container.Alpha;
            Color strokeColor = Color.Black * (alpha * alpha * alpha);
            Vector2 position2 = position;
            if (scale < 1f) {
                position2.X = 30f;
            }
            Vector2 justify = new Vector2(0f, 1f);
            ActiveFont.DrawOutline(fileName, position2, justify, Vector2.One * scale, GetColor(-1, highlighted), 2f, strokeColor);
            if (highlighted) {
                Monocle.Draw.Line(position2, position2 + (ActiveFont.Measure(fileName).X * scale) * Vector2.UnitX, Color.White, 2f);
            }
            position2.X += (Parent.Width - RightWidth()) * scale;
            ActiveFont.DrawOutline(separator, position2, justify, Vector2.One * scale, Color.White, 2f, strokeColor);
            position2.X += ActiveFont.Measure(separator).X * scale;
            ActiveFont.DrawOutline(reName, position2, justify, Vector2.One * scale, GetColor(0, highlighted), 2f, strokeColor);
            position2.X += ActiveFont.Measure(reName).X * scale;
            ActiveFont.DrawOutline(delete, position2, justify, Vector2.One * scale, GetColor(1, highlighted), 2f, strokeColor);
        }

        public void AddTempHeight(float deltaY) {
            tempHeight = deltaY;
        }
        public void Init() {
            if (Parent is null) {
                return;
            }

            if (Parent.Dict.TryGetValue(guid, out List<GhostData> data) && data.Count > 0) {
                fileName = GhostHud.GhostFileTitleGetter(data.LastOrDefault());
                ghostName = data.LastOrDefault().Name;
                dark = !data.LastOrDefault().IsCompleted;
            } else {
                Parent.RemoveItem(this);
            }
            // update label
        }
        public void SetName(string name) {
            if (Parent.Dict.TryGetValue(guid, out List<GhostData> datas)) {
                foreach (GhostData data in datas) {
                    if (data.Name != name) {
                        data.Name = name;
                        data.Write();
                    }
                }
                Init();
            }
        }

        public void Delete() {
            if (Parent.Dict.TryGetValue(guid, out List<GhostData> datas)) {
                foreach (GhostData data in datas) {
                    data.DeleteFromMemory();
                }
                Parent.RemoveItem(this);
            }
        }
    }

    public GhostFileEditor(GhostFileEditorContainer container) {
        GrandParent = container;
        InnerContent = InnerContentMode.TwoColumn;
        Add(new Header("Ghost File Editor".ToDialogText()));
        Add(new TextMenuExt.SubHeaderExt("") { HeightExtra = 100f });
        Add(noGhostFileWarning = new Header("No Ghost Files".ToDialogText()));
        initialItemsCount = Items.Count;
        noGhostFileWarning.Visible = false;

        OnESC = OnCancel = () => {
            Focused = false;
            SceneAs<Overworld>().Goto<OuiModOptions>();
            Audio.Play(SFX.ui_main_rename_entry_backspace);
        };
        MinWidth = 600f;

        Init();
    }

    private int initialItemsCount;

    private Item noGhostFileWarning;

    public void Init() {
        foreach (KeyValuePair<Guid, List<GhostData>> pair in Dict) {
            Add(new SingleGhostFile(pair.Key, this));
        }
    }

    public void RemoveItem(SingleGhostFile file) {
        int i = Items.IndexOf(file);
        if (i > -1 && i < Items.Count - 1 && Items[i + 1] is SingleGhostFile nextFile) {
            nextFile.AddTempHeight(file.Height() + ItemSpacing);
        }
        if (i > -1) {
            Remove(file);
            if (i >= Items.Count) {
                Selection = Items.Count - 1;
            }
        }
        Dict.Remove(file.guid);
    }

    public override void Update() {
        base.Update();
        noGhostFileWarning.Visible = Items.Count <= initialItemsCount;
    }

    public override void Render() {
        Draw.Rect(-10f, -10f, 1940f, 1100f, Color.Black * 0.8f);
        base.Render();
    }
}