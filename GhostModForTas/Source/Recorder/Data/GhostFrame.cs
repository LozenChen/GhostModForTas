using Monocle;
using System;
using System.IO;

namespace Celeste.Mod.GhostModForTas.Recorder.Data;
public struct GhostFrame {
    public const string End = "\r\n";

    public GhostChunkData ChunkData;

    public void Read(BinaryReader reader) {
        string chunk;
        // The last "chunk" type, \r\n (Windows linebreak), doesn't contain a length.
        ChunkData.HasPlayer = reader.ReadBoolean();
        if (!ChunkData.HasPlayer) {
            while (reader.ReadNullTerminatedString() != End) {
                // do nothing
            }
            return;
        }

        while ((chunk = reader.ReadNullTerminatedString()) != End) {
            uint length = reader.ReadUInt32();
            switch (chunk) {
                case GhostChunkData.ChunkV1:
                    ChunkData.Read(reader, 1);
                    break;
                case GhostChunkData.ChunkV2:
                    ChunkData.Read(reader, 2);
                    break;
                case GhostChunkData.ChunkV3:
                    ChunkData.Read(reader, 3);
                    break;

                default:
                    // Skip any unknown chunks.
                    reader.BaseStream.Seek(length, SeekOrigin.Current);
                    break;
            }
        }
    }

    public void Write(BinaryWriter writer) {
        writer.Write(ChunkData.HasPlayer);

        if (ChunkData.HasPlayer) {
            WriteChunk(writer, ChunkData.Write, GhostChunkData.Chunk);
        }

        writer.WriteNullTerminatedString(End);
    }

    public static void WriteChunk(BinaryWriter writer, Action<BinaryWriter> method, string name) {
        long start = WriteChunkStart(writer, name);
        method(writer);
        WriteChunkEnd(writer, start);
    }

    public static long WriteChunkStart(BinaryWriter writer, string name) {
        writer.WriteNullTerminatedString(name);
        writer.Write(0U); // Filled in later.
        long start = writer.BaseStream.Position;
        return start;
    }

    public static void WriteChunkEnd(BinaryWriter writer, long start) {
        long pos = writer.BaseStream.Position;
        long length = pos - start;

        // Update the chunk length, which consists of the 4 bytes before the chunk data.
        writer.Flush();
        writer.BaseStream.Seek(start - 4, SeekOrigin.Begin);
        writer.Write((int)length);

        writer.Flush();
        writer.BaseStream.Seek(pos, SeekOrigin.Begin);
    }
}
