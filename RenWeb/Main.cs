﻿using RenSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenWeb
{
    public enum LogSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Connection = 3
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

        public void Log(LogSeverity Severity, object Something)
        {

        }

        public override void SettingsLoadedEvent()
        {
            MimeTypes.Clear();
            ErrorPages.Clear();
            IDASettingsClass settings = DASettingsManager.GetSettings("da.ini");
            IINISection section = settings.GetSection("RenWeb");
            foreach (IINIEntry entry in section.EntryList)
            {
                switch (entry.Entry)
                {
                    case "Port":
                        if(int.TryParse(entry.Value, out int port))
                        {
                            Port = port;
                        }
                        else
                        {
                            Engine.ConsoleOutput($"[RenWeb] Port value is invalid! Using default port 7550...\n");
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
                        IndexFile = entry.Value;
                        break;
                    case "MaxPendingConnections":
                        if (int.TryParse(entry.Value, out int max))
                        {
                             MaxPendingConnections = max;
                        }
                        else
                        {
                            Engine.ConsoleOutput($"[RenWeb] Max pending conections value is invalid! Using default 5...\n");
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
                            GameLog = true;
                        }
                        break;
                    default:
                        Engine.ConsoleOutput($"[RenWeb] Invalid entry detected under RenWeb section in configuration file!\n" +
                                             $"[RenWeb] Key: \"{entry.Entry}\" | Value: \"{entry.Value}\"\n");
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
                foreach(string s in Errors)
                {
                    if(int.TryParse(s, out int Code))
                    {
                        ErrCodes.Add(Code);
                    }
                    else
                    {
                        Engine.ConsoleOutput($"[RenWeb] Invalid error code detected under RenWeb_ErrorPages! Value: {s}\n");
                    }
                }
                ErrorPages.Add(ErrCodes, entry.Value);
            }

            if (!Directory.Exists(Main.RootHTTPFolder))
            {
                Directory.CreateDirectory(Main.RootHTTPFolder);
            }
            RestartServer(); //Start server if not running, restart if running.
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
            if(Server != null)
                Server.Close();

            Server = new WebServer();
        }
    }
}
