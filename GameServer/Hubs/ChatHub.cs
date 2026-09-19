using System.Security.Claims;
using GameServer.DTOs;
using Microsoft.AspNetCore.SignalR;

namespace GameServer.Hubs;

public class ChatHub : Hub
{
    //여기는 다음시간에 작성할 거고.
    private const string GeneralGroup = "general"; //일반채팅 그룹
    
    //현재는 길드시스템이 없으니까 단일 demo 길드를 사용해서 시연할 예정
    private const string GuildGroup = "guild:demo";

    /// <summary>
    /// 연결 시작시에 호출되는 메서드로 클라이언트를 general과 demo길드에 추가해준다.
    /// </summary>
    /// <returns></returns>
    public override async Task OnConnectedAsync()
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GeneralGroup);
        await Groups.AddToGroupAsync(Context.ConnectionId, GuildGroup);
        await base.OnConnectedAsync();
    }

    public async Task SendMessage(string channelCode, string text)
    {
        if (string.IsNullOrEmpty(text))
            return; //공백 메시지는 무시한다.
        
        //토큰 클레임에서 닉네임을 가져온다. 없으면 unknown처리
        string nickname = Context.User?.FindFirstValue("nickname") ?? "Unknown";

        bool isGuild = channelCode == "guild";
        string group = isGuild ? GuildGroup : GeneralGroup;
        string channelName = isGuild ? "guild" : "general";
        
        //모든 클라이언트에게 (해당 그룹에 속해있는) 메시지 전송
        await Clients.Group(group).SendAsync("ReceiveMessage", new ChatMessage(channelName, nickname, text));
    }
}