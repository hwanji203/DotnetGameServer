using System.Collections.Generic;
using System.Threading.Tasks;
using Networking;
using Networking.Dtos;
using UnityEngine;
using UnityEngine.UIElements;

namespace UI
{
    public class GameHudUIController : AbstractUIScreenController
    {
        private readonly struct ChatLine
        {
            public readonly string Text;
            public readonly bool IsSystem;
            public ChatLine(string text, bool isSystem)
            {
                Text = text;
                IsSystem = isSystem;
            }
        }

        private const int ChatLogCap = 200;
        private string ActiveChannel => _isGuildChannel ? "guild" : "general";
        private readonly Dictionary<string, List<ChatLine>> _logs = new Dictionary<string, List<ChatLine>>()
        {
            { "general", new List<ChatLine>(ChatLogCap) },
            { "guild", new List<ChatLine>(ChatLogCap) },
        };
        
        private Label _level;
        private Label _nickname;
        private VisualElement _hpFill;
        private Label _hpText;
        private VisualElement _expFill;
        private Label _expText;

        private ScrollView _chatLog;
        private TextField _chatInput;
        private Button _tabNormal;
        private Button _tabGuild;
        private bool _isGuildChannel;

        private void OnDisable()
        {
            UnBindChat();
        }

        protected override void Bind()
        {
            _level = Lbl("hud-level");
            _nickname = Lbl("hud-nickname");
            _hpFill = Q<VisualElement>("hp-fill");
            _hpText = Lbl("hp-text");
            _expFill = Q<VisualElement>("exp-fill");
            _expText = Lbl("exp-text");
            
            _chatLog = Q<ScrollView>("chat-log");
            _chatInput = Q<TextField>("chat-input");
            _tabNormal = Btn("chat-tab-normal");
            _tabGuild = Btn("chat-tab-guild");

            _tabNormal.clicked += () => SetGuildChannel(false);
            _tabGuild.clicked += () => SetGuildChannel(true);

            // Btn("leave-btn").clicked += () => SceneRouter.Go(SceneRouter.MainScene);
            Btn("chat-send").clicked += OnSend;
            // Btn("enter-dungeon-btn").clicked += () => SceneRouter.Go(SceneRouter.DungeonScene);
            
            _chatInput.RegisterCallback<KeyDownEvent>(evt =>
            {
                if (evt.keyCode == KeyCode.Return || evt.keyCode == KeyCode.KeypadEnter)
                {
                    OnSend();
                    evt.StopPropagation();
                }
            }, TrickleDown.TrickleDown);

            SetHp(100, 100);
            SetGuildChannel(false);
            AppendSystem("마을에 입장했습니다.");
            _ = LoadCharacter();

            BindChat();
        }

        private void SetHp(int current, int max)
        {
            float percent = max <= 0 ? 0 : Mathf.Clamp01(current / (float)max) * 100f;
            _hpFill.style.width = Length.Percent(percent);
            SetMessage(_hpText, $"{current} / {max}");
        }

        private async Task LoadCharacter()
        {
            if (Session.CurrentUser != null)
                _nickname.text = Session.CurrentUser.Nickname;
            
            //세션에 넣어도 된다.
            ApiResult<CharacterResponse> meResult = await CharacterApi.MeAsync();
            
            if (meResult.IsSuccess && meResult.Data != null)
                ApplyCharacterToUI(meResult.Data);
            else 
                AppendSystem("캐릭터 정보를 불러오지 못했습니다.");
        }

        private void ApplyCharacterToUI(CharacterResponse characterData)
        {
            SetMessage(_level, $"LV. {characterData.Level}");
            long requiredExp = characterData.ExpToNextLevel <= 0 ? 1 : characterData.ExpToNextLevel;
            float percent = Mathf.Clamp01(characterData.Exp / (float)requiredExp) * 100f;
            
            _expFill.style.width = Length.Percent(percent);
            SetMessage(_expText, $"{characterData.Exp} / {requiredExp}");
        }
        
        #region Chat api
        
        private void BindChat()
        {
            ChatClient chat = ChatClient.Instance;

            foreach (ChatMessage msg in chat.History)
            {
                AppendChat(msg.Channel, FormatChat(msg));
            }

            chat.OnMessage += OnChatMessage;
            chat.OnConnected += OnChatConnected;
            chat.OnClosed += OnChatClosed;

            _ = EnsureChatConnected(chat);
        }

        private void UnBindChat() //=>이거 OnDisable에서 꼭 호출.
        {
            ChatClient chat = ChatClient.Instance;
            if (chat == null)
                return;
            chat.OnMessage -= OnChatMessage;
            chat.OnConnected -= OnChatConnected;
            chat.OnClosed -= OnChatClosed;
        }

        private void OnChatMessage(ChatMessage m) => AppendChat(m.Channel, FormatChat(m));

        private void OnChatConnected() => AppendSystem("채팅 서버에 연결되었습니다.");

        private void OnChatClosed(string result) => AppendSystem($"채팅 연결이 끊겼습니다. {result}");

        private async Task EnsureChatConnected(ChatClient chat)
        {
            if (!await chat.EnsureConnectedAsync())
                AppendSystem("채팅 서버 연길에 실패했습니다.");
        }

        private string FormatChat(ChatMessage msg) => $"{msg.Nickname} : {msg.Text}";

        private void AppendChat(string channel, string text) => Record(channel, new ChatLine(text, false));
        private void AppendSystem(string message)
        {
            Label label = new Label(message);
            Record("general", new ChatLine(message, true));
            Record("guild", new ChatLine(message, true));
        }

        private void Record(string channel, ChatLine line)
        {
            if (!_logs.TryGetValue(channel, out List<ChatLine> buffer))
                return;

            buffer.Add(line);
            if (buffer.Count > ChatLogCap)
                buffer.RemoveAt(0);
            if (channel == ActiveChannel)
                AddLabel(line);
        }

        private void AddLabel(ChatLine line)
        {
            Label label = new Label(line.Text);
            label.AddToClassList("chat-line");
            if (line.IsSystem)
                label.AddToClassList("chat-line--system");
            _chatLog.Add(label);

            //새로운 줄이 추가되면 스크롤을 맨 아래로
            _chatLog.schedule
                .Execute(() => _chatLog.scrollOffset = new Vector2(0f, float.MaxValue))
                .ExecuteLater(1);
            //1ms 이후에 수행하게 한다.
        }
        //
        // private void AppendLine(string line, bool isSystem)
        // {
        //     Label chatLabel = new Label(line);
        //     chatLabel.AddToClassList("chat-line");
        //     if(isSystem)
        //         chatLabel.AddToClassList("chat-line--system");
        //     _chatLog.Add(chatLabel);
        //     //나중에 자동 스크롤 다운까지 만들어볼게.
        //     //UI 툴킷 스케줄링
        // }

        //데이터 전송
        private void OnSend()
        {
            string msg = _chatInput.value?.Trim();
            if (string.IsNullOrEmpty(msg)) return;
            
            //로컬에서 창에 띄울필요 없이 그냥 서버로 보내면 서버가 브로드캐스트해서 나에게 뜬다.
            // string channel = _isGuildChannel ? "guild" : "general";
            _ = ChatClient.Instance.SendMessageAsync(ActiveChannel, msg);
            
            _chatInput.value = string.Empty;
            _chatInput.Focus();
        }

        private void SetGuildChannel(bool isGuildChannel)
        {
            _isGuildChannel = isGuildChannel;
            _tabNormal.EnableInClassList("chat-tab--active", !isGuildChannel);
            _tabGuild.EnableInClassList("chat-tab--active", isGuildChannel);

            RebuildLog();
        }

        private void RebuildLog()
        {
            _chatLog.Clear(); //기존 채팅 삭제 후
            foreach (ChatLine line in _logs[ActiveChannel])
                AddLabel(line);
        }

        #endregion
    }
}