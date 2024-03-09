using Microsoft.Xna.Framework;
using Monocle;
using System.IO;

namespace Celeste.Mod.GhostModForTas.Recorder.Data;

public struct GhostChunkData {
    public const string ChunkV1 = "v1";
    public const string ChunkV2 = "v2";
    public const string ChunkV3 = "v3";
    public const string Chunk = ChunkV3;
    public long Time;
    public bool HasPlayer;


    // V1

    public Vector2 Position;
    public Vector2 Speed;

    // V2
    public bool UpdateHair;
    public float Rotation;
    public Vector2 Scale;
    public Color Color;
    public Facings Facing;
    public string CurrentAnimationID;
    public int CurrentAnimationFrame;
    public Color HairColor;
    public bool HairSimulateMotion;
    public int HairCount;

    // V3

    public void Read(BinaryReader reader, int version) {
        Time = reader.ReadInt64();


        if (version < 2) {
            return;
        }


        Position = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        Speed = new Vector2(reader.ReadSingle(), reader.ReadSingle());

        if (version < 3) {
            return;
        }

        UpdateHair = reader.ReadBoolean();
        Rotation = reader.ReadSingle();
        Scale = new Vector2(reader.ReadSingle(), reader.ReadSingle());
        Color = new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        Facing = (Facings)reader.ReadInt32();
        CurrentAnimationID = reader.ReadNullTerminatedString();
        CurrentAnimationFrame = reader.ReadInt32();
        HairColor = new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        HairSimulateMotion = reader.ReadBoolean();
        HairCount = reader.ReadInt32();
    }

    public void Write(BinaryWriter writer) {
        writer.Write(Time);


        writer.Write(Position.X);
        writer.Write(Position.Y);
        writer.Write(Speed.X);
        writer.Write(Speed.Y);


        writer.Write(UpdateHair);
        writer.Write(Rotation);
        writer.Write(Scale.X);
        writer.Write(Scale.Y);
        writer.Write(Color.R);
        writer.Write(Color.G);
        writer.Write(Color.B);
        writer.Write(Color.A);
        writer.Write((int)Facing);
        writer.WriteNullTerminatedString(CurrentAnimationID);
        writer.Write(CurrentAnimationFrame);
        writer.Write(HairColor.R);
        writer.Write(HairColor.G);
        writer.Write(HairColor.B);
        writer.Write(HairColor.A);
        writer.Write(HairSimulateMotion);
        writer.Write(HairCount);
    }
}