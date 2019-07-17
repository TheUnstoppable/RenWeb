using RenSharp;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace RenWeb
{
    public enum LogSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Connection = 3
    }

    public class ConnectionData
    {
        public IPAddress IP;
        public Uri Request;
        public int Result;
    }

    public class TeamClass
    {
        private string tName = "Unknown";
        private long tScore = 0;
        private long tKill = 0;
        private long tDeath = 0;

        public string Name
        {
            get { return tName; }
            set { tName = value; }
        }
        public long Score
        {
            get { return tScore; }
            set { tScore = value; }
        }
        public long Kills
        {
            get { return tKill; }
            set { tKill = value; }
        }
        public long Deaths
        {
            get { return tDeath; }
            set { tDeath = value; }
        }
    }
    public class ServerDefinitions
    {
        //Private TOP SECRET!!
        private static string pMap = "Unknown";
        private static string pNext = "Unknwon";
        private static int pTLeft = 0;
        private static int pTElapsed = 0;
        private static int pTTotal = 0;
        private static string pServ = "Unknown";
        private static string pGM = "Unknown";
        private static string pSGM = "N/A";
        private static string pPl = "";
        private static int pCPC = 0;
        private static int pMPC = 0;
        private static TeamClass pGDI = new TeamClass() { Name = "Unknown", Kills = 0, Deaths = 0, Score = 0};
        private static TeamClass pNod = new TeamClass() { Name = "Unknown", Kills = 0, Deaths = 0, Score = 0 };


        //Public
        public static string CurrentMap
        {
            get { return pMap; }
            set { pMap = value; }
        }
        public static string NextMap
        {
            get { return pNext; }
            set { pNext = value; }
        }
        public static int TimeLeft
        {
            get { return pTLeft; }
            set { pTLeft = value; }
        }
        public static int TimeElapsed
        {
            get { return pTElapsed; }
            set { pTElapsed = value; }
        }
        public static int TimeTotal
        {
            get { return pTTotal; }
            set { pTTotal = value; }
        }
        public static string GameMode
        {
            get { return pGM; }
            set { pGM = value; }
        }
        public static string ShortGameMode
        {
            get { return pSGM; }
            set { pSGM = value; }
        }
        public static int PlayerCount
        {
            get { return pCPC; }
            set { pCPC = value; }
        }
        public static int MaxPlayerCount
        {
            get { return pMPC; }
            set { pMPC = value; }
        }
        public static string ServerName
        {
            get { return pServ; }
            set { pServ = value; }
        }
        public static string Players
        {
            get { return pPl; }
            set { pPl = value; }
        }
        public static TeamClass GDITeam
        {
            get { return pGDI; }
            set { pGDI = value; }
        }
        public static TeamClass NodTeam
        {
            get { return pNod; }
            set { pNod = value; }
        }
    }

    public class Main :  RenSharpEventClass
    {
        public static bool GameLog = true; //Cmon it's useful ;)
        public static int Port = 7550;
        public static int MaxPendingConnections = 5;
        public static string RootHTTPFolder = "RenWebHTML";
        public static string IndexFile = "index.html";
        public static string LogFile = @"RWLogs\RenWeb.log";
        public static IDictionary<string, string> MimeTypes = new Dictionary<string, string>();
        public static IDictionary<System.Collections.Generic.List<int>, string> ErrorPages = new Dictionary<System.Collections.Generic.List<int>, string>();
        public static WebServer Server;
        public static string Version = "1.0";
        public static bool DoThink = false;
        public static object LockObject = new object();

        public override void UnmanagedAttach()
        {
            RegisterEvent(DAEventType.SettingsLoaded);
            RegisterEvent(DAEventType.Think);
            RegisterEvent(DAEventType.LevelLoaded);
        }

        public override void LevelLoadedEvent()
        {
            DoThink = true;
        }

        public override void Think()
        {
            if (DoThink)
            {
                try
                {
                    lock (LockObject)
                    {
                        //Current Map
                        if (Engine.TheCncGame.MapName != null)
                            ServerDefinitions.CurrentMap = Engine.TheCncGame.MapName;
                        else
                            ServerDefinitions.CurrentMap = "Unknown";

                        //Next Map
                        using (var GameDefinitions = Engine.GetGameDefinitions())
                        {
                            string MapDef = Engine.GetMap(Engine.GetCurrentMapIndex() + (Engine.TheGame.IsIntermission ? 0 : 1));
                            if (String.IsNullOrEmpty(MapDef))
                                MapDef = Engine.GetMap(0);
                            ServerDefinitions.NextMap = GameDefinitions.UnmanagedObject.First(x => x.Value.DisplayName == MapDef).Value.MapName;
                        }

                        //Server Name
                        if (Engine.TheCncGame.GameTitle != null)
                            ServerDefinitions.ServerName = Engine.TheCncGame.GameTitle;
                        else
                            ServerDefinitions.ServerName = "Unknown";

                        //Time Left
                        ServerDefinitions.TimeLeft = Convert.ToInt32(Engine.TheGame.TimeRemainingSeconds);

                        //Time Elapsed
                        ServerDefinitions.TimeElapsed = Convert.ToInt32(Engine.TheGame.GameDurationSeconds);

                        //Time Total
                        ServerDefinitions.TimeTotal = Convert.ToInt32(Engine.TheGame.TimeLimitMinutes * 60);

                        //Game Mode
                        if (DAGameManager.GameModeLongName != null)
                            ServerDefinitions.GameMode = DAGameManager.GameModeLongName;
                        else
                            ServerDefinitions.GameMode = "Unknown";

                        //Short Game Mode
                        if (DAGameManager.GameModeShortName != null)
                            ServerDefinitions.ShortGameMode = DAGameManager.GameModeShortName;
                        else
                            ServerDefinitions.ShortGameMode = "Unknown";

                        //Players
                        ServerDefinitions.PlayerCount = Engine.TheGame.CurrentPlayers;

                        //Max Players
                        ServerDefinitions.MaxPlayerCount = Engine.TheGame.MaxPlayers;

                        //Players
                        string PlayersText = "";
                        if (Engine.GetPlayerList() != null)
                        {
                            if ((Engine.GetPlayerList() as ICollection<IcPlayer>).Count > 0)
                            {

                                foreach (IcPlayer Player in Engine.GetPlayerList())
                                {
                                    if (Player.IsActive)
                                    {
                                        PlayersText += $"{Player.PlayerName},{Engine.GetTeamName(Player.PlayerType)},{Convert.ToInt32(Player.Score)},{Player.Kills},{Player.Deaths},{Player.Ping},{Main.FormatTime(TimeSpan.FromSeconds(Player.GameTime))},";
                                    }
                                }
                                if (!String.IsNullOrEmpty(PlayersText))
                                {
                                    PlayersText = PlayersText.Substring(0, PlayersText.Length - 1);
                                }

                            }
                        }
                        ServerDefinitions.Players = PlayersText;

                        //Team Informati0ns
                        IcTeam Team = Engine.FindTeam(0);
                        if (Team != null)
                        {
                            TeamClass Nod = new TeamClass()
                            {
                                Name = Engine.GetTeamName(0),
                                Score = Convert.ToInt64(Team.Score),
                                Kills = Team.Kills,
                                Deaths = Team.Deaths
                            };
                            ServerDefinitions.NodTeam = Nod;
                        }

                        IcTeam Team2 = Engine.FindTeam(1);
                        if (Team2 != null)
                        {
                            TeamClass GDI = new TeamClass()
                            {
                                Name = Engine.GetTeamName(1),
                                Score = Convert.ToInt64(Team2.Score),
                                Kills = Team2.Kills,
                                Deaths = Team2.Deaths
                            };
                            ServerDefinitions.GDITeam = GDI;
                        }

                        //End of THONK!
                    }
                }
                catch (Exception ex)
                {

                }
            }
        }

        public static void Log(LogSeverity Severity, object Something)
        {
            switch(Severity)
            {
                case LogSeverity.Connection:
                    var Client = (ConnectionData)Something;
                    var ConResult = AppendLog($"[{DateTime.Now.ToString("dd/MM/yyyy-HH:mm:ss")}|{Severity.ToString()}] {Client.IP.ToString()} is requested {Client.Request.AbsoluteUri} and HTTP code is {Client.Result}.");
                    if (ConResult != null)
                        Engine.ConsoleOutput($"[RenWeb] {ConResult}");                        
                    break;
                case LogSeverity.Error:
                    var Exception = (Exception)Something;
                    var ErrResult1 = AppendLog($"[{DateTime.Now.ToString("dd/MM/yyyy-HH:mm:ss")}|{Severity.ToString()}] An error occured while RenWeb doing something.");
                    var ErrResult2 = AppendLog($"[{DateTime.Now.ToString("dd/MM/yyyy-HH:mm:ss")}|{Severity.ToString()}] Error: {Exception.ToString()}");
                    var ErrResult3 = AppendLog($"[{DateTime.Now.ToString("dd/MM/yyyy-HH:mm:ss")}|{Severity.ToString()}] Message: {Exception.Message}");
                    if (ErrResult1 != null)
                        Engine.ConsoleOutput($"[RenWeb] {ErrResult1}");
                    if (ErrResult2 != null)
                        Engine.ConsoleOutput($"[RenWeb] {ErrResult2}");
                    if (ErrResult3 != null)
                        Engine.ConsoleOutput($"[RenWeb] {ErrResult3}");
                    break;
                case LogSeverity.Info:
                    var InfResult = AppendLog($"[{DateTime.Now.ToString("dd/MM/yyyy-HH:mm:ss")}|{Severity.ToString()}] {(string)Something}");
                    if (InfResult != null)
                        Engine.ConsoleOutput($"[RenWeb] {InfResult}");
                    break;
                case LogSeverity.Warning:
                    var WarnResult = AppendLog($"[{DateTime.Now.ToString("dd/MM/yyyy-HH:mm:ss")}|{Severity.ToString()}] {(string)Something}");
                    if (WarnResult != null)
                        Engine.ConsoleOutput($"[RenWeb] {WarnResult}");
                    break;
            }
        }

        private static string AppendLog(string Line)
        {
            try
            {
                if (File.Exists(LogFile))
                {
                    StreamWriter Writer = new StreamWriter(LogFile, true);
                    Writer.WriteLine(Line);
                    Writer.Close();
                }
                else
                {
                    MakeFile(LogFile);
                    return "Could not locate log file.";
                }
                return null;
            }
            catch(IOException)
            {
                return "An I/O exception occured while writing into log file.";
            }
        }

        public override void SettingsLoadedEvent()
        {
            MimeTypes.Clear();
            ErrorPages.Clear();
            IDASettingsClass settings = DASettingsManager.GetSettings("RenWeb.ini");
            if (settings != null)
            {
                IINISection section = settings.GetSection("RenWeb");
                foreach (IINIEntry entry in section.EntryList)
                {
                    switch (entry.Entry)
                    {
                        case "Port":
                            if (int.TryParse(entry.Value, out int port))
                            {
                                Port = port;
                            }
                            else
                            {
                                Engine.ConsoleOutput($"[RenWeb] Port value is invalid! Using default port 7550...\n");
                                Log(LogSeverity.Warning, $"Port value is invalid! Supplied value: \"{port}\". Using default port \"7550\"");
                                Port = 7550;
                            }
                            break;
                        case "RootHTTPFolder":
                            RootHTTPFolder = entry.Value;
                            break;
                        case "IndexFile":
                            IndexFile = entry.Value;
                            break;
                        case "LogFile":
                            LogFile = entry.Value;
                            break;
                        case "MaxPendingConnections":
                            if (int.TryParse(entry.Value, out int max))
                            {
                                MaxPendingConnections = max;
                            }
                            else
                            {
                                Engine.ConsoleOutput($"[RenWeb] Max pending conections value is invalid! Using default 5...\n");
                                Log(LogSeverity.Warning, $"Max pending conections value is invalid! Supplied value: \"{max}\". Using default \"5\"");
                                MaxPendingConnections = 5;
                            }
                            break;
                        case "GameLog":
                            if (bool.TryParse(entry.Value, out bool isit))
                            {
                                GameLog = isit;
                            }
                            else
                            {
                                Engine.ConsoleOutput($"[RenWeb] Game log value is invalid! Using default true...\n");
                                Log(LogSeverity.Warning, $"Game log value is invalid! Supplied value: \"{isit}\". Using default \"true\"");
                                GameLog = true;
                            }
                            break;
                        default:
                            Engine.ConsoleOutput($"[RenWeb] Invalid entry detected under RenWeb section in configuration file!\n" +
                                                 $"[RenWeb] Key: \"{entry.Entry}\" | Value: \"{entry.Value}\"\n");
                            Log(LogSeverity.Warning, $"Invalid config value detected under [RenWeb]. Key: \"{entry.Entry}\" | Value: \"{entry.Value}\"");
                            break;

                    }
                }

                IINISection Mime = settings.GetSection("RenWeb_MimeTypes");
                foreach (IINIEntry entry in Mime.EntryList)
                {
                    MimeTypes.Add(entry.Entry, entry.Value);
                }

                IINISection Page = settings.GetSection("RenWeb_ErrorPages");
                foreach (IINIEntry entry in Page.EntryList)
                {
                    string[] Errors = entry.Entry.Split('|');
                    System.Collections.Generic.List<int> ErrCodes = new System.Collections.Generic.List<int>();
                    foreach (string s in Errors)
                    {
                        if (int.TryParse(s, out int Code))
                        {
                            ErrCodes.Add(Code);
                        }
                        else
                        {
                            Engine.ConsoleOutput($"[RenWeb] Invalid error code detected under RenWeb_ErrorPages! Value: {s}\n");
                            Log(LogSeverity.Warning, $"Invalid error code under RenWeb_ErrorPages! Value: {s}. Skipping this code...\n");
                        }
                    }
                    ErrorPages.Add(ErrCodes, entry.Value);
                }

                if (!Directory.Exists(Main.RootHTTPFolder))
                {
                    Directory.CreateDirectory(Main.RootHTTPFolder);
                    Setup(Main.RootHTTPFolder);
                }
                if (!File.Exists(Main.LogFile))
                {
                    MakeFile(Main.LogFile);
                }
                RestartServer(); //Start server if not running, restart if running.
            }
            else
            {
                string ServerRoot = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "RenWeb.ini");
                if (!File.Exists(ServerRoot))
                {
                    Engine.ConsoleOutput("[RenWeb] Could not find RenWeb.ini in server folder. Creating a one with default settings...\n");
                    File.Create(ServerRoot).Close();
                    File.WriteAllBytes(ServerRoot, FileStorage.Config);
                    DASettingsManager.AddSettings("RenWeb.ini");
                    Engine.ConsoleInput("reload");
                }
                else
                {
                    Engine.ConsoleOutput("[RenWeb] FATAL ERROR! Failed to load RenWeb settings file. Attempting to register configuration file into DA...\n");
                    DASettingsManager.AddSettings("RenWeb.ini");
                    Engine.ConsoleInput("reload");
                }
            }
        }

        public static void Setup(string Root)
        {
            Application.EnableVisualStyles();
            Welcomer w = new Welcomer();
            w = new Welcomer();
            w.Show();

            Thread tr = new Thread(() =>
            {
                //Ehh ¯\_(ツ)_/¯ (According to me, RAM is ok until this is initialized.)
                FileStorage Stor = new FileStorage();

                //Creating every single file.
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (Index.html)"));
                MakeFile(Main.RootHTTPFolder + "\\Index.html");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (Info.html)"));
                MakeFile(Main.RootHTTPFolder + "\\Info.html");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (Players.html)"));
                MakeFile(Main.RootHTTPFolder + "\\Players.html");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (Teams.html)"));
                MakeFile(Main.RootHTTPFolder + "\\Teams.html");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (Renegade.html)"));
                MakeFile(Main.RootHTTPFolder + "\\Renegade.html");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (404.html)"));
                MakeFile(Main.RootHTTPFolder + "\\404.html");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (ServerError.html)"));
                MakeFile(Main.RootHTTPFolder + "\\ServerError.html");

                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (RenSS1.jpg)"));
                MakeFile(Main.RootHTTPFolder + "\\img\\RenSS1.jpg");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (RenSS2.jpg)"));
                MakeFile(Main.RootHTTPFolder + "\\img\\RenSS2.jpg");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (RenSS3.jpg)"));
                MakeFile(Main.RootHTTPFolder + "\\img\\RenSS3.jpg");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (RenSS4.jpg)"));
                MakeFile(Main.RootHTTPFolder + "\\img\\RenSS4.jpg");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (RenSS5.jpg)"));
                MakeFile(Main.RootHTTPFolder + "\\img\\RenSS5.jpg");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (RenSS6.jpg)"));
                MakeFile(Main.RootHTTPFolder + "\\img\\RenSS6.jpg");

                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (GDI.png)"));
                MakeFile(Main.RootHTTPFolder + "\\img\\teams\\GDI.png");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (Nod.png)"));
                MakeFile(Main.RootHTTPFolder + "\\img\\teams\\Nod.png");
                
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (Icon16.png)"));
                MakeFile(Main.RootHTTPFolder + "\\ServerLogos\\Icon16.png");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (EmbedFrame2.png)"));
                MakeFile(Main.RootHTTPFolder + "\\ServerLogos\\EmbedFrame2.png");
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Creating files... (ExampleEmbed.embed)"));
                MakeFile(Main.RootHTTPFolder + "\\ExampleEmbed.embed");

                //Writing...
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Writing files... (Index.html)"));
                File.WriteAllBytes(Main.RootHTTPFolder + "\\Index.html", Stor.Index);
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Writing files... (Info.html)"));
                File.WriteAllBytes(Main.RootHTTPFolder + "\\Info.html", Stor.Info);
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Writing files... (Players.html)"));
                File.WriteAllBytes(Main.RootHTTPFolder + "\\Players.html", Stor.Players);
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Writing files... (Teams.html)"));
                File.WriteAllBytes(Main.RootHTTPFolder + "\\Teams.html", Stor.Teams);
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Writing files... (Renegade.html)"));
                File.WriteAllBytes(Main.RootHTTPFolder + "\\Renegade.html", Stor.AboutRenegade);
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Writing files... (404.html)"));
                File.WriteAllBytes(Main.RootHTTPFolder + "\\404.html", Stor.Page404);
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Writing files... (ServerError.html)"));
                File.WriteAllBytes(Main.RootHTTPFolder + "\\ServerError.html", Stor.PageServerError);

                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Writing files... (Screenshots)"));
                File.WriteAllBytes(Main.RootHTTPFolder + "\\img\\RenSS1.jpg", Stor.RenSS1);
                File.WriteAllBytes(Main.RootHTTPFolder + "\\img\\RenSS2.jpg", Stor.RenSS2);
                File.WriteAllBytes(Main.RootHTTPFolder + "\\img\\RenSS3.jpg", Stor.RenSS3);
                File.WriteAllBytes(Main.RootHTTPFolder + "\\img\\RenSS4.jpg", Stor.RenSS4);
                File.WriteAllBytes(Main.RootHTTPFolder + "\\img\\RenSS5.jpg", Stor.RenSS5);
                File.WriteAllBytes(Main.RootHTTPFolder + "\\img\\RenSS6.jpg", Stor.RenSS6);

                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Writing files... (Team Logo)"));
                File.WriteAllBytes(Main.RootHTTPFolder + "\\img\\teams\\GDI.png", Stor.LogoGDI);
                File.WriteAllBytes(Main.RootHTTPFolder + "\\img\\teams\\Nod.png", Stor.LogoNod);
                
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Writing extras... (Embedding)"));
                File.WriteAllBytes(Main.RootHTTPFolder + "\\ServerLogos\\EmbedFrame2.png", Stor.EmbedFrame);
                File.WriteAllBytes(Main.RootHTTPFolder + "\\ServerLogos\\Icon16.png", Stor.RenIcon16);
                File.WriteAllBytes(Main.RootHTTPFolder + "\\ExampleEmbed.embed", Stor.ExampleEmbed);

                //Clearing all bytes.
                SynchronizedInvoke.Invoke(w, () => w.UpdateLabel("Finalizing..."));
                Thread.Sleep(150);
                Stor.Dispose();
                Stor = null;
                SynchronizedInvoke.Invoke(w, () => w.Done());
            });

            tr.SetApartmentState(ApartmentState.STA);
            tr.Start();
        }

        public static void MakeFile(string FullPath)
        {
            string[] Path = FullPath.Split('\\');
            string Dir = "";
            for(int i = 0; i < Path.Length; i++)
            {
                Dir += Path[i] + "\\";
                if (i + 1 < Path.Length)
                {
                    if (!Directory.Exists(Dir))
                    {
                        Directory.CreateDirectory(Dir);
                    }
                }
                else if(!File.Exists(Dir))
                {
                    Dir = Dir.Remove(Dir.Length - 1, 1);
                    File.Create(Dir).Close();
                }
            }
        }

        public static string FormatTime(DateTime dt)
        {
            return dt.ToString("HH:mm:ss");
        }

        public static string FormatTime(TimeSpan ts)
        {
            return ts.ToString(@"hh\:mm\:ss");
        }

        public void RestartServer()
        {
            if (Server != null)
            {
                Server.Close();
            }

            Server = new WebServer();
        }
    }

    internal static class SynchronizedInvoke
    {
        /// <summary>
        /// Invokes the specified action on the thread that the specified sync object was created on.
        /// </summary>
        public static void Invoke(ISynchronizeInvoke sync, Action action)
        {
            if (!sync.InvokeRequired)
            {
                action();
            }
            else
            {
                object[] args = new object[] { };
                sync.Invoke(action, args);
            }
        }
    }
}
