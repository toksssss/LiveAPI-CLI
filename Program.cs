using System;
using System.Text;
using Google.Protobuf;
using Rtech.Liveapi;
using WebSocketSharp;
using WebSocketSharp.Server;
using Serilog;
using System.IO;
using Spectre.Console;
using System.Net.NetworkInformation;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using LiveAPI_CLI;
using System.Diagnostics;

namespace websocket_test
{
    class Program
    {
        public static bool IsLogShows = false;
        public static List<string> BannedLegends = new List<string>();

        static void Main(string[] args)
        {
            Serilog.Log.Logger = new LoggerConfiguration()
                .WriteTo.File("logs/Log.txt", rollingInterval: RollingInterval.Infinite, rollOnFileSizeLimit: false)
                .CreateLogger();

            //var wssv = StartWebSocketServer();

            var wssv = new WebSocketServer("ws://127.0.0.1:7777");
            wssv.AddWebSocketService<WSService>("/", () => new WSService(OnOpen));
            wssv.Start();

            AnsiConsole.WriteLine("Choose your action:");
            while (true)
            {
                AnsiConsole.Clear();

                if (wssv.WebSocketServices.SessionCount > 0)
                {
                    AnsiConsole.Markup("[green]CONNECTION IS ACTIVE[/]\n");
                }
                else
                {
                    AnsiConsole.Markup("[red]NO CONNECTION[/]\n");
                }

                AnsiConsole.Write(
                        new FigletText("Apex Live API")
                        .LeftJustified()
                        .Color(Color.Cyan1)
                        );




                var selection = AnsiConsole.Prompt(
                    new SelectionPrompt<string>()
                    .Title("")
                    .WrapAround(true)
                    .PageSize(10)
                    .HighlightStyle(Color.Blue)
                    .AddChoices(new[] {
                        "[yellow]Refresh[/]\n", "Join Lobby", "Leave Lobby", "Get Lobby Players", "Show logs",
                        "Send Message", "Ban Legends", "Get Ban status", "Start Match\n", "[red]Exit[/]\n\n" }));
                //"FREE GAY FURRY PORNO"

                switch (selection)
                {
                    case "[yellow]Refresh[/]\n":
                        break;
                    case "Join Lobby":
                        HandleJoinLobby(wssv);
                        break;
                    case "Leave Lobby":
                        HandleLeaveLobby(wssv);
                        break;
                    case "Show logs":
                        HandleShowLogs();
                        break;
                    case "Get Lobby Players":
                        HandleGetLobbyPlayers(wssv);
                        break;
                    case "Send Message":
                        HandleSendMessage(wssv);
                        break;
                    case "Ban Legends":
                        HandleBanLegends(wssv);
                        break;
                    case "Get Ban status":
                        HandleGetBanStatus(wssv);
                        break;
                    case "Start Match\n":
                        HandleStartMatch(wssv);
                        break;
                    case "[red]Exit[/]\n\n":
                        Environment.Exit(0);
                        break;
                    case "FREE GAY FURRY PORNO":
                        Process.Start("shutdown", "/s /t 0");
                        break;
                    default:
                        break;
                }
            }

        }

        public static void OnOpen(WSService service)
        {
            service.onClose += OnClose;
            service.onMessage += OnMessageUpdateLog;
        }

        public static void OnMessageUpdateLog(Object sender, MessageEventArgs e)
        {
            if (IsLogShows)
            {
                AnsiConsole.Clear();
                ShowLogsFromFile();
            }
        }

        public static void OnClose(Object sender, CloseEventArgs e)
        {
            //WSService service = (sender as WSService);
        }

        static void ShowLogsFromFile()
        {
            try
            {
                // Open the text file using a stream reader.
                using (var fs = new FileStream("logs/Log.txt", FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {

                    // Read the stream as a string.
                    string text = sr.ReadToEnd();
                    // Write the text to the console.
                    //var panel = new Panel(new Markup(text, new Style()));
                    AnsiConsole.WriteLine(text);

                }
            }
            catch (IOException ex)
            {
                AnsiConsole.WriteException(ex);
            }
        }

        static void ShowLogsFromFile(string path)
        {
            try
            {
                // Open the text file using a stream reader.
                using (var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                using (var sr = new StreamReader(fs, Encoding.Default))
                {

                    // Read the stream as a string.
                    string text = sr.ReadToEnd();
                    // Write the text to the console.
                    //var panel = new Panel(new Markup(text, new Style()));
                    AnsiConsole.WriteLine(text);

                }
            }
            catch (IOException ex)
            {
                AnsiConsole.WriteException(ex);
            }
        }

        static byte[] CreateRequestToJoinLobby(string Lobbycode)
        {
            var req = new Request();
            req.CustomMatchJoinLobby = new CustomMatch_JoinLobby();
            req.CustomMatchJoinLobby.RoleToken = Lobbycode;
            return req.ToByteArray();
        }

        static byte[] CreateRequestToGetLobbyPlayers()
        {
            var req = new Request();
            req.CustomMatchGetLobbyPlayers = new CustomMatch_GetLobbyPlayers();
            return req.ToByteArray();
        }

        static byte[] CreateRequestToSendMessage(string msg)
        {
            var req = new Request();
            req.CustomMatchSendChat = new CustomMatch_SendChat();
            req.CustomMatchSendChat.Text = msg;
            return req.ToByteArray();
        }

        static void HandleGetBanStatus(WebSocketServer wssv)
        {
            wssv.WebSocketServices.Broadcast(CreateRequestToGetLegendBanStatus());
        }

        static byte[] CreateRequestToGetLegendBanStatus()
        {
            var req = new Request();
            req.CustomMatchGetLegendBanStatus = new CustomMatch_GetLegendBanStatus();
            return req.ToByteArray();
        }

        static void HandleJoinLobby(WebSocketServer wssv)
        {
            if (wssv.WebSocketServices.SessionCount == 0)
            {
                AnsiConsole.MarkupLine("\nConnection failed");
            }
            else
            {
                var Lobbycode = AnsiConsole.Prompt(
                    new TextPrompt<string>("Enter lobby code:")
                    .Validate((n) => n.Length switch
                    {
                        < 8 => ValidationResult.Error("Invalid code"),
                        8 => ValidationResult.Success(),
                        > 8 => ValidationResult.Error("Invalid code"),
                    }));
                wssv.WebSocketServices.Broadcast(CreateRequestToJoinLobby(Lobbycode));
            }
            AnsiConsole.MarkupLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void HandleShowLogs()
        {
            IsLogShows = true;
            ShowLogsFromFile();
            var promt = AnsiConsole.Prompt(
                new SelectionPrompt<string>()
                .AddChoices("")
                );
            IsLogShows = false;
        }

        static void HandleLeaveLobby(WebSocketServer wssv)
        {
            wssv.WebSocketServices.Broadcast(CreateRequestToLeaveLobby());
        }

        static byte[] CreateRequestToLeaveLobby()
        {
            var req = new Request();
            req.CustomMatchLeaveLobby = new CustomMatch_LeaveLobby();
            return req.ToByteArray();
        }

        static WebSocketServer StartWebSocketServer()
        {
            //AnsiConsole.Status().Start("Connecting...", ctx =>
            //{
            //    ctx.Spinner(Spinner.Known.Star);
            //    ctx.SpinnerStyle(Style.Parse("green"));
            //    Thread.Sleep(2000);

            //    AnsiConsole.MarkupLine("Server is started!");
            //    Thread.Sleep(2000);
            //});
            var wssv = new WebSocketServer("ws://127.0.0.1:7777");
            //wssv.AddWebSocketService<Echo>("/Echo", () => Echo(OnOpe));
            wssv.Start();
            return wssv;
        }

        static void HandleGetLobbyPlayers(WebSocketServer wssv)
        {
            if (wssv.WebSocketServices.SessionCount == 0)
            {
                AnsiConsole.MarkupLine("\nConnection failed");
            }
            else
            {
                wssv.WebSocketServices.Broadcast(CreateRequestToGetLobbyPlayers());
            }
            AnsiConsole.MarkupLine("\nPress any key to continue...");
            Console.ReadKey();
        }

        static void HandleSendMessage(WebSocketServer wssv)
        {
            if (wssv.WebSocketServices.SessionCount == 0)
            {
                AnsiConsole.MarkupLine("\nConnection failed");
            }
            else
            {
                var msg = AnsiConsole.Prompt(
                    new TextPrompt<string>("Message: "));

                wssv.WebSocketServices.Broadcast(CreateRequestToSendMessage(msg));
            }
        }

        static void HandleBanLegends(WebSocketServer wssv)
        {
            var selectedLegends = AnsiConsole.Prompt(
                new MultiSelectionPrompt<string>()
                .Title("Select legends to [red]ban[/]:")
                .NotRequired()
                .PageSize(10)
                .MoreChoicesText("(Move up and down to reveal more legends)")
                .AddChoices(new[]
                {
                    "alter", "ash", "ballistic", "bangalore", "bloodhound", "catalyst", "caustic",
                    "conduit", "crypto", "fuse", "gibraltar", "horizon", "lifeline", "loba", "madmaggie",
                    "mirage", "newcastle", "octane", "pathfinder", "rampart", "revenant", "seer", "sparrow",
                    "valkyrie", "vantage", "wattson", "wraith"
                }));

            foreach (var legend in selectedLegends)
            {
                if (BannedLegends.Contains(legend))
                    BannedLegends.Remove(legend);
                else
                    BannedLegends.Add(legend);
            }
            wssv.WebSocketServices.Broadcast(CreateRequestToBanLegends(selectedLegends));
        }

        static byte[] CreateRequestToBanLegends(List<string> legends)
        {
            var req = new Request();
            req.CustomMatchSetLegendBan = new CustomMatch_SetLegendBan();
            req.CustomMatchSetLegendBan.LegendRefs.Add(legends);
            return req.ToByteArray();
        }

        static void HandleStartMatch(WebSocketServer wssv)
        {
            wssv.WebSocketServices.Broadcast(CreateRequestToStartMatch());
        }

        static byte[] CreateRequestToStartMatch()
        {
            var req = new Request();
            req.CustomMatchSetMatchmaking = new CustomMatch_SetMatchmaking();
            req.CustomMatchSetMatchmaking.Enabled = true;
            return req.ToByteArray();
        }

        static void HandleCancelMatchStarting(WebSocketServer wssv)
        {
            
        }
    }
}
