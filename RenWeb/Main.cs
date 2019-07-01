using RenSharp;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RenWeb
{
    public class Main :  RenSharpEventClass
    {
        public static bool GameLog = true; //Cmon it's useful ;)
        public static int Port = 7550;
        public static int MaxPendingConnections = 2000;
        public static string RootHTTPFolder = "RenWebHTML";
        public static string IndexFile = "index.html";
        public static IDictionary<string, string> MimeTypes = new Dictionary<string, string>();
        public static IDictionary<System.Collections.Generic.List<int>, string> ErrorPages = new Dictionary<System.Collections.Generic.List<int>, string>();
        public static WebServer Server;
        public static string Version = "1.0";

        public override void UnmanagedAttach()
        {
            RegisterEvent(DAEventType.SettingsLoaded);
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
