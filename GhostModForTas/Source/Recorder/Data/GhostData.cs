using Monocle;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Celeste.Mod.GhostModForTas.Recorder.Data;

public class GhostData {
    public readonly static string Magic = "everest-ghost\r\n";
    public readonly static char[] MagicChars = Magic.ToCharArray();

    public readonly static int Version = 1;
    public readonly static string OshiroPostfix = ".oshiro";

    public readonly static Regex PathVerifyRegex =
        new Regex("[\"`?* #" + Regex.Escape(new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars())) + "]",
            RegexOptions.Compiled);

    public static string GetGhostFilePrefix_NoLevel(Session session)
        => PathVerifyRegex.Replace($"{session.Area.GetSID()}-{(char)('A' + (int)session.Area.Mode)}", "-");
    public static string GetGhostFilePrefix(Session session)
        => GetGhostFilePrefix(session.Area.GetSID(), session.Area.Mode, session.Level);

    public static string GetGhostFilePrefix(string sid, AreaMode mode, string level)
        => PathVerifyRegex.Replace($"{sid}-{(char)('A' + (int)mode)}-{level}", "-");

    public static string GetGhostFilePath(Session session, string name, DateTime date)
        => GetGhostFilePath(session.Area.GetSID(), session.Area.Mode, session.Level, name, date);

    public static string GetGhostFilePath(string sid, AreaMode mode, string level, string name, DateTime date)
        => Path.Combine(
            PathGhosts,
            GetGhostFilePrefix(sid, mode, level) +
            PathVerifyRegex.Replace($"-{name}-{date.ToString("yyyy-MM-dd-HH-mm-ss-fff", CultureInfo.InvariantCulture)}", "-") + OshiroPostfix
        );

    public static string[] GetAllGhostFilePaths_NoLevel(Session session)
        => Directory.GetFiles(
            PathGhosts,
            GetGhostFilePrefix_NoLevel(session) + "*" + OshiroPostfix
        );

    public static string[] GetAllGhostFilePaths(Session session) // those belong to this level
        => Directory.GetFiles(
            PathGhosts,
            GetGhostFilePrefix(session) + "*" + OshiroPostfix
        );

    public static List<Entities.Ghost> FindAllGhosts(Session session) {
        string[] filePaths = GetAllGhostFilePaths_NoLevel(session);
        Dictionary<Guid, List<GhostData>> dictionary = new();
        List<Entities.Ghost> ghosts = new();
        for (int i = 0; i < filePaths.Length; i++) {
            GhostData ghostData = new GhostData(filePaths[i]).Read();
            if (ghostData?.Run is null) {
                continue;
            }
            if (dictionary.TryGetValue(ghostData.Run, out List<GhostData> list)) {
                list.Add(ghostData);
            } else {
                dictionary.Add(ghostData.Run, new List<GhostData>() { ghostData });
            }
        }
        foreach (Guid guid in dictionary.Keys) {
            List<GhostData> ghostDatas = dictionary[guid];
            List<GhostData> sortedGhostData = new();
            string nextLevel = session.Level;
            int revisitCount = 1;
            bool found;
            do {
                found = false;
                foreach (GhostData data in ghostDatas) {
                    if (data.Level == nextLevel && data.LevelVisitCount == revisitCount) {
                        sortedGhostData.Add(data);
                        nextLevel = data.Target;
                        revisitCount = data.TargetVisitCount;
                        ghostDatas.Remove(data);
                        found = true;
                        break;
                    }
                }
            } while (found);
            if (sortedGhostData.Count > 0) {
                ghosts.Add(new Entities.Ghost(sortedGhostData));
            }
            Logger.Log("GhostModForTas", $"Add Ghost: RunGuid = {guid}, RoomCount = {sortedGhostData.Count}");
        }

        return ghosts;
    }

    public string SID;
    public AreaMode Mode;
    public string From;
    public string Level;
    public string Target;
    public int LevelVisitCount = 1;
    public int TargetVisitCount = 1;
    public string Name;
    public DateTime Date;
    public long SessionTime;

    public float? Opacity;

    public Guid Run;

    protected string _FilePath;

    public string FilePath {
        get {
            if (_FilePath != null) {
                return _FilePath;
            }

            return GetGhostFilePath(SID, Mode, Level, Name, Date);
        }
        set { _FilePath = value; }
    }

    public List<GhostFrame> Frames = new List<GhostFrame>();

    public GhostFrame this[int i] {
        get {
            if (i < 0 || i >= Frames.Count) {
                return default;
            }

            return Frames[i];
        }
    }

    public GhostData() {
        Date = DateTime.UtcNow;
        Run = Guid.NewGuid();
    }

    public GhostData(Session session)
        : this() {
        if (session != null) {
            SID = session.Area.GetSID();
            Mode = session.Area.Mode;
            Level = session.Level;
        }
    }

    public GhostData(string filePath)
        : this() {
        FilePath = filePath;
    }

    public GhostData Read() {
        if (FilePath == null)
        // Keep existing frames in-tact.
        {
            return null;
        }

        if (!File.Exists(FilePath)) {
            // File doesn't exist - load nothing.
            Logger.Log("ghost", $"Ghost doesn't exist: {FilePath}");
            Frames = new List<GhostFrame>();
            return null;
        }

        using (Stream stream = File.OpenRead(FilePath))
        using (BinaryReader reader = new BinaryReader(stream, Encoding.UTF8)) {
            return Read(reader);
        }
    }

    public GhostData Read(BinaryReader reader) {
        if (reader.ReadInt16() != 0x0ade) {
            return null; // Endianness mismatch.
            // Shout out to 0x0ade!
        }

        char[] magic = reader.ReadChars(MagicChars.Length);
        if (magic.Length != MagicChars.Length) {
            return null; // Didn't read as much as we wanted to read.
        }

        for (int i = 0; i < MagicChars.Length; i++) {
            if (magic[i] != MagicChars[i]) {
                return null; // Magic mismatch.
            }
        }

        int version = reader.ReadInt32();
        // Don't read data from the future, but try to read data from the past.
        if (version > Version) {
            return null;
        }

        int compression = reader.ReadInt32();

        if (compression != 0) {
            return null; // Compression not supported yet.
        }

        SID = reader.ReadNullTerminatedString();
        Mode = (AreaMode)reader.ReadInt32();
        Level = reader.ReadNullTerminatedString();
        Target = reader.ReadNullTerminatedString();
        LevelVisitCount = reader.ReadInt32();
        TargetVisitCount = reader.ReadInt32();
        Name = reader.ReadNullTerminatedString();
        long dateBin = reader.ReadInt64();
        try {
            Date = DateTime.FromBinary(dateBin);
        } catch {
            // The date was invalid. Let's ignore it.
            Date = DateTime.UtcNow;
        }

        SessionTime = reader.ReadInt64();

        if (version >= 1) {
            Run = new Guid(reader.ReadBytes(16));
        } else {
            Run = Guid.Empty;
        }

        int count = reader.ReadInt32();
        reader.ReadChar(); // \r
        reader.ReadChar(); // \n
        Frames = new List<GhostFrame>(count);
        for (int i = 0; i < count; i++) {
            GhostFrame frame = new GhostFrame();
            frame.Read(reader);
            Frames.Add(frame);
        }

        return this;
    }

    public void Write() {
        if (FilePath == null) {
            return;
        }

        if (FilePath != null && File.Exists(FilePath)) {
            // Force ourselves onto the set filepath.
            File.Delete(FilePath);
        }

        if (!Directory.Exists(Path.GetDirectoryName(FilePath))) {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath));
        }

        using (Stream stream = File.OpenWrite(FilePath))
        using (BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8)) {
            Write(writer);
        }
        Logger.Log("GhostModForTas", $"Write: SID = {SID}, Level = [{Level}], Target = [{Target}], RunGUID = {Run}");
    }

    public void Write(BinaryWriter writer) {
        writer.Write((short)0x0ade);
        writer.Write(MagicChars);
        writer.Write(Version);

        writer.Write(0); // Uncompressed

        writer.WriteNullTerminatedString(SID);
        writer.Write((int)Mode);
        writer.WriteNullTerminatedString(Level);
        writer.WriteNullTerminatedString(Target);
        writer.Write(LevelVisitCount);
        writer.Write(TargetVisitCount);
        writer.WriteNullTerminatedString(Name);
        writer.Write(Date.ToBinary());
        writer.Write(SessionTime);

        writer.Write(Run.ToByteArray());
        writer.Write(Frames.Count);
        writer.Write('\r');
        writer.Write('\n');
        for (int i = 0; i < Frames.Count; i++) {
            GhostFrame frame = Frames[i];
            frame.Write(writer);
        }
    }
}