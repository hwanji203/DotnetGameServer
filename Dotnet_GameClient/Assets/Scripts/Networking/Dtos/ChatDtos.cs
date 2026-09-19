namespace Networking.Dtos
{
    /// <summary>
    /// SignalR이 기본 JSON으로 직렬화 / 역직렬화를 수행한다. 서버 클라 모두 C#이기 때문에 CamelCase로 매칭한다.
    /// 역직렬화가 될 수 있도록 get; set;의 기본 생성자를 ㅇ가진 클래스로 만든다.
    /// </summary>
    public class ChatMessage
    {
        //general, 또는 guild id형태의 채널이 된다.
           public string Channel { get; set; } //어느 채널로 보낼지
           public string Nickname { get; set; } //발신자 닉네임, 서버가 JWT 클래임에서 읽어서 채워준다.
           public string Text { get; set; } //메시지 본문
    }
}