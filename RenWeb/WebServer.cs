﻿using System;
using System.Net;
using System.Threading;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.IO;
using RenSharp;
using HtmlAgilityPack;
using System.Collections;
using System.Collections.Generic;

namespace RenWeb
{
    public class WebServer
    {
        private HttpListener listener;
        public System.Collections.Generic.List<Thread> handlers;
        public WebServer()
        {
            listener = new HttpListener()
            {
                IgnoreWriteExceptions = true
            };
            listener.Prefixes.Add($"http://*:{Main.Port}/");
            listener.Start();
            handlers = new System.Collections.Generic.List<Thread>();
            for (int count = 0; count < Main.MaxPendingConnections; count++)
            {
                Thread tr = new Thread(RequestHandler);
                handlers.Add(tr);
                tr.Start();
            }
            Engine.ConsoleOutput($"[RenWeb] The RenWeb Server is now listening port {Main.Port}.\n");
        }

        public string GetPhysicalPath(Uri u)
        {
            return u.AbsolutePath.Replace('/', '\\');
        }

        public string GetMimeType(string ext)
        {
            if(Main.MimeTypes.ContainsKey(ext))
            {
                return Main.MimeTypes[ext];
            }
            return null;
        }

        public int FileExists(string Path)
        {
            //0: Nope
            //1: Append index file, then yep
            //2: Yep
            if(Path == "\\")
            {
                if (!File.Exists(Main.IndexFile))
                {
                    return 0;
                }
            }


            if (!File.Exists(Path))
            {
                if (!File.Exists(Path + "\\" + Main.IndexFile))
                {
                    return 0;
                }
                else
                {
                    return 1;
                }
            }
            else
            {
                return 2;
            }
        }

        public string GetHtmlErrorPage(int Error)
        {
            try
            {
                return Main.ErrorPages.First(x => x.Key.Contains(Error)).Value;
            }
            catch(Exception)
            {
                return null;
            }
        }

        private void RequestHandler()
        {
            HttpListenerContext context = null;
            byte[] Stream = new byte[4];
            string mime = null;
            try
            {
                while (listener.IsListening)
                {
                    //Getting context.
                    context = listener.GetContext();
                    context.Response.StatusCode = (int)HttpStatusCode.OK;
                    try
                    {
                        string Path = Main.RootHTTPFolder + "\\" + GetPhysicalPath(context.Request.Url);
                        int Exist = FileExists(Path);
                        if (Exist == 0)
                        {
                            context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                        }
                        else if (Exist == 1)
                        {
                            Path += "\\" + Main.IndexFile;
                        }
                        //Response parameters.
                        if (context.Response.StatusCode == 200)
                        {
                            mime = GetMimeType(System.IO.Path.GetExtension(Path));
                            if (mime == "text/html")
                            {
                                Stream = Encoding.UTF8.GetBytes(ProcessHTML(File.ReadAllText(Path)));
                            }
                            else if (mime != null)
                            {
                                Stream = File.ReadAllBytes(Path);
                            }
                            else
                            {
                                mime = "text/html";
                                context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (context != null)
                        {
                            Engine.ConsoleOutput($"[RenWeb] Failed to send website to {context.Request.RemoteEndPoint.Address.ToString()}: {ex.ToString()} - {ex.Message}\n");
                            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        }
                    }


                    if (context.Response.StatusCode != 200)
                    {
                        string ErrorFile = Main.RootHTTPFolder + "\\" + GetHtmlErrorPage(context.Response.StatusCode);
                        if (ErrorFile != null)
                        {
                            int Exist = FileExists(ErrorFile);
                            if (Exist == 2)
                            {
                                if (File.Exists(ErrorFile))
                                {
                                    Stream = File.ReadAllBytes(ErrorFile);
                                }
                                else
                                {
                                    Stream = null;
                                }
                            }
                            else
                            {
                                Stream = null;
                            }
                        }
                        else
                        {
                            Stream = null;
                        }
                    }

                    context.Response.ContentType = mime;
                    context.Response.ContentEncoding = Encoding.UTF8;
                    context.Response.AppendHeader("RenWeb-Version", "1.0");

                    //Content.
                    if (Stream != null)
                    {
                        Stream s = context.Response.OutputStream;
                        s.Write(Stream, 0, Stream.Length);
                        s.Close();
                    }

                    //Closing connection.
                    context.Response.Close();
                }

            }
            catch(ObjectDisposedException)
            {
                
            }
            catch (Exception ex)
            {
                if (context != null)
                {
                    Engine.ConsoleOutput($"[RenWeb] Failed to send website to {context.Request.RemoteEndPoint.Address.ToString()}: {ex.ToString()} - {ex.Message}\n");
                    context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                }
                else
                    Engine.ConsoleOutput($"[RenWeb] Failed to send website to a unknown client. {ex.ToString()} - {ex.Message}\n");
            }
        }

        public string ProcessHTML(string Text)
        {
            string HTML = Text;
            //Version
            HTML = HTML.Replace("$RenWebHTML_Version", Main.Version);

            //Server Name
            HTML = HTML.Replace("$RenWebHTML_ServerName", ServerDefinitions.ServerName);

            //Current Map
            HTML = HTML.Replace("$RenWebHTML_CurrentMap", ServerDefinitions.CurrentMap);

            //Next Map
            HTML = HTML.Replace("$RenWebHTML_NextMap", ServerDefinitions.NextMap);

            //Time Left as Formatted String
            HTML = HTML.Replace("$RenWebHTML_TimeLeftF", Main.FormatTime(TimeSpan.FromSeconds(ServerDefinitions.TimeLeft)));

            //Time Left as Seconds
            HTML = HTML.Replace("$RenWebHTML_TimeLeft", ServerDefinitions.TimeLeft.ToString());

            //Time Elapsed as Formatted String
            HTML = HTML.Replace("$RenWebHTML_TimeElapsedF", Main.FormatTime(TimeSpan.FromSeconds(ServerDefinitions.TimeElapsed)));

            //Time Elapsed as Seconds
            HTML = HTML.Replace("$RenWebHTML_TimeElapsed", (ServerDefinitions.TimeElapsed).ToString());

            //Time Limit as Formatted String
            HTML = HTML.Replace("$RenWebHTML_TimeLimitF", Main.FormatTime(TimeSpan.FromSeconds(ServerDefinitions.TimeTotal)));

            //Time Limit as Seconds
            HTML = HTML.Replace("$RenWebHTML_TimeLimit", (ServerDefinitions.TimeTotal * 60).ToString());

            //Long Gamemode Name
            HTML = HTML.Replace("$RenWebHTML_GameMode", ServerDefinitions.GameMode);

            //Short Gamemode Name
            HTML = HTML.Replace("$RenWebHTML_SGameMode", ServerDefinitions.ShortGameMode);

            //Player Count
            HTML = HTML.Replace("$RenWebHTML_CurrentPlayerCount", ServerDefinitions.PlayerCount.ToString());

            //Max Player Count
            HTML = HTML.Replace("$RenWebHTML_MaxPlayerCount", ServerDefinitions.MaxPlayerCount.ToString());

            //Players :O
            HTML = HTML.Replace("$RenWebHTML_Players", ServerDefinitions.Players);

            //Finally -_-
            return HTML;
        }

        public void Close()
        {
            listener.Close();
            listener.Abort();
        }
    }
}