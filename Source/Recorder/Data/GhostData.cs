using Celeste.Mod.GhostModForTas.Utils;
using Monocle;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using DynamicData = MonoMod.Utils.DynamicData;

namespace Celeste.Mod.GhostModForTas.Recorder.Data;

public class GhostData {
    public readonly static string Magic = "everest-ghost\r\n";
    public readonly static char[] MagicChars = Magic.ToCharArray();

    public readonly static int Version = 3;
    // increase this int when we change the data structure in some future update
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

    private static bool Match_level => ghostSettings.REPLAYER_CHECK_STARTING_WITH_SAME_ROOM; // if we should check the starting room is same
    public static List<Replayer.Ghost> FindAllGhosts(Session session) {
        string[] filePaths = Match_level ? GetAllGhostFilePaths(session) : GetAllGhostFilePaths_NoLevel(session);
        Dictionary<Guid, List<GhostData>> dictionary = new();
        List<Replayer.Ghost> ghosts = new();
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
        if (dictionary.IsNullOrEmpty()) {
            Logger.Log("GhostModForTas", "No Ghost in this Level!");
        }
        foreach (Guid guid in dictionary.Keys) {
            if (Match_level) {
                GhostData[] ghostDatas = dictionary[guid].OrderBy(x => x.RTASessionTime).ToArray();
                List<GhostData> sortedGhostData = new();
                // ghostDatas.ForEach(x => Logger.Log("GhostModForTas", $"Try Read GhostData {guid}: {x.LevelCount}"));

                int head = 0;
                int tail = ghostDatas.Length;

                LevelCount lc = new LevelCount(session.Level, 1);

                bool found;
                do {
                    found = false;
                    for (int i = head; i < tail; i++) {
                        if (ghostDatas[i].LevelCount == lc) {
                            sortedGhostData.Add(ghostDatas[i]);
                            lc = ghostDatas[i].TargetCount;
                            head = i + 1;
                            found = true;
                            break;
                        }
                    }
                } while (found);

                if (sortedGhostData.Count > 0) {
                    ghosts.Add(new Replayer.Ghost(sortedGhostData));
                }
            } else {
                List<GhostData> sortedGhostData = dictionary[guid].OrderBy(x => x.RTASessionTime).ToList();
                if (sortedGhostData.Count > 0) {
                    ghosts.Add(new Replayer.Ghost(sortedGhostData));
                }
            }

        }

        return ghosts;
    }

    internal class GhostFileEditorHelper {
        private static string[] GetAllGhostFilePaths()
        => Directory.GetFiles(
            PathGhosts,
            "*" + OshiroPostfix
        );

        public static Dictionary<Guid, List<GhostData>> GetGhostFileInfo() {
            Dictionary<Guid, List<GhostData>> dict = new();

            string[] filePaths = GetAllGhostFilePaths();
            Dictionary<Guid, List<GhostData>> dictionary = new();
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
                dict[guid] = dictionary[guid].OrderBy(x => x.RTASessionTime).ToList();
            }

            return dict;
        }
    }

    public string SID;
    public AreaMode Mode;
    public LevelCount LevelCount;
    public LevelCount TargetCount;
    public string Name;
    public DateTime Date;
    public long SessionTime;
    public long RTASessionTime;
    public bool IsCompleted = false;

    public Guid Run;
    public string CustomInfoTemplate;
    public bool IsTas;
    public string TasString;

    protected string _FilePath;

    public string FilePath {
        get {
            if (_FilePath != null) {
                return _FilePath;
            }

            return GetGhostFilePath(SID, Mode, LevelCount.Level, Name, Date);
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
            LevelCount = new(session.Level, 1);
            if (DynamicData.For(session).TryGet(GhostNameInDynData, out string name)) {
                Name = name;
            } else {
                Name = ghostSettings.DefaultName;
            }
        }
    }

    private const string GhostNameInDynData = "GhostModForTas:GhostName";

    [Command("ghost_set_name", "Set the name of the ghost being recorded.")]
    public static void SetGhostName(string name) {
        if (Engine.Scene.GetSession() is { } session) {
            DynamicData.For(session).Set(GhostNameInDynData, name);
        }
        if (GhostRecorder.Recorder?.Data is { } data) {
            data.Name = name;
        }
    }

    public GhostData(string filePath)
        : this() {
        FilePath = filePath;
    }

    public long GetSessionTime() {
        return ghostSettings.IsIGT ? SessionTime : RTASessionTime;
    }

    public GhostData Read() {
        if (FilePath == null)
        // Keep existing frames in-tact.
        {
            return null;
        }

        if (!File.Exists(FilePath)) {
            // File doesn't exist - load nothing.
            Logger.Log("GhostModForTas", $"Ghost doesn't exist: {FilePath}");
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
        // Don't read data from the past.
        if (version < Version) {
            Logger.Log("GhostModForTas", $"Ghost out of date: {FilePath}");
            return null;
        }

        int compression = reader.ReadInt32();

        if (compression != 0) {
            return null; // Compression not supported yet.
        }

        SID = reader.ReadNullTerminatedString();
        Mode = (AreaMode)reader.ReadInt32();
        LevelCount = new(reader.ReadNullTerminatedString(), reader.ReadInt32());
        TargetCount = new(reader.ReadNullTerminatedString(), reader.ReadInt32());
        Name = reader.ReadNullTerminatedString();
        long dateBin = reader.ReadInt64();
        try {
            Date = DateTime.FromBinary(dateBin);
        } catch {
            // The date was invalid. Let's ignore it.
            Date = DateTime.UtcNow;
        }

        SessionTime = reader.ReadInt64();
        RTASessionTime = reader.ReadInt64();
        IsCompleted = reader.ReadBoolean();

        Run = new Guid(reader.ReadBytes(16));
        CustomInfoTemplate = reader.ReadNullTerminatedString();
        IsTas = reader.ReadBoolean();
        TasString = reader.ReadNullTerminatedString();

        int count = reader.ReadInt32();
        if (count > 1E6) {
            throw new Exception("GhostData out of date or corrupted. Please clear this Ghost file.");
        }
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

    public void DeleteFromMemory() {
        if (FilePath != null && File.Exists(FilePath)) {
            File.Delete(FilePath);
        }
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
        Logger.Log("GhostModForTas", $"Write: SID = {SID}, Level = {LevelCount}, NextLevel = {TargetCount}, RunGUID = {Run}");
    }

    public void Write(BinaryWriter writer) {
        writer.Write((short)0x0ade);
        writer.Write(MagicChars);
        writer.Write(Version);

        writer.Write(0); // Uncompressed

        writer.WriteNullTerminatedString(SID);
        writer.Write((int)Mode);
        writer.WriteNullTerminatedString(LevelCount.Level);
        writer.Write(LevelCount.Count);
        writer.WriteNullTerminatedString(TargetCount.Level);
        writer.Write(TargetCount.Count);
        writer.WriteNullTerminatedString(Name);
        writer.Write(Date.ToBinary());
        writer.Write(SessionTime);
        writer.Write(RTASessionTime);
        writer.Write(IsCompleted);

        writer.Write(Run.ToByteArray());
        writer.WriteNullTerminatedString(CustomInfoTemplate);
        writer.Write(IsTas);
        writer.WriteNullTerminatedString(TasString);
        writer.Write(Frames.Count);
        writer.Write('\r');
        writer.Write('\n');
        for (int i = 0; i < Frames.Count; i++) {
            GhostFrame frame = Frames[i];
            frame.Write(writer);
        }
    }
}

public struct LevelCount {
    public string Level;
    public int Count;
    public static readonly LevelCount Exit = new LevelCount("LevelExit", 1);
    public LevelCount(string level, int count) {
        Level = level;
        Count = count;
    }

    public static bool operator ==(LevelCount lc1, LevelCount lc2) {
        return lc1.Count == lc2.Count && lc1.Level == lc2.Level;
    }

    public static bool operator !=(LevelCount lc1, LevelCount lc2) {
        return lc1.Count != lc2.Count || lc1.Level != lc2.Level;
    }
    public override bool Equals(object obj) {
        if (obj is LevelCount lc) {
            return Equals(lc);
        }
        return false;
    }
    public bool Equals(LevelCount lc) {
        return Count == lc.Count && Level == lc.Level;
    }

    public override int GetHashCode() {
        return Level.GetHashCode() + Count;
    }

    public override string ToString() {
        return $"[{Level}]{Count switch { 1 => "", 2 => "@2nd", 3 => "@3rd", _ => $"@{Count}th" }}";
    }
}