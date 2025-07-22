using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Rtech.Liveapi;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace LiveAPI_CLI
{
    public class WSService : WebSocketBehavior
    {
        public event EventHandler<CloseEventArgs> onClose;
        public event EventHandler<MessageEventArgs> onMessage;

        private Action<WSService> clientCallBack;

        public WSService(Action<WSService> _clientCallBack)
        {
            clientCallBack = _clientCallBack;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            onMessage?.Invoke(this, e);

            LiveAPIEvent ApiEvent = new LiveAPIEvent();
            ApiEvent.MergeFrom(e.RawData);

            var msg = ApiEvent.GameMessage;

            if (msg.Is(Init.Descriptor))
            {
                var init = msg.Unpack<Init>();
                Serilog.Log.Information("Init message:\n {@_init}\n", init);
            }
            else if (msg.Is(CustomMatch_LobbyPlayers.Descriptor))
            {
                var lobbyPlayers = msg.Unpack<CustomMatch_LobbyPlayers>();
                Serilog.Log.Information("CustomMatchGetLobbyPlayers Response:\nTeams: {@teams}\nPlayers: {@players}", lobbyPlayers.Teams, lobbyPlayers.Players);
            }
            else if (msg.Is(MatchSetup.Descriptor))
            {
                var matchSetup = msg.Unpack<MatchSetup>();
                Serilog.Log.Information("MatchSetup:\n {@matchSetup}", matchSetup);
            }
            else if(msg.Is(CustomMatch_LegendBanStatus.Descriptor))
            {
                var status = msg.Unpack<CustomMatch_LegendBanStatus>();
                Serilog.Log.Information("CustomMatch_LegendBanStatus:\n{@status}", status.Legends);
            }
            else
                Serilog.Log.Information("Other:\n {@_other}\n", msg);
        }
        protected override void OnOpen()
        {
            clientCallBack?.Invoke(this);
            //AnsiConsole.WriteLine("Connection is opened");
        }
        protected override void OnClose(CloseEventArgs e)
        {
            onClose?.Invoke(this, e);
            //AnsiConsole.WriteLine($"Connection is closed. Reason: {e.Reason}");
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            base.OnError(e);
        }
    }
}
