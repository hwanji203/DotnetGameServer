using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;
using Networking.Dtos;
using UnityEngine;

namespace Networking
{
    public class ChatClient : MonoBehaviour
    {
        public static ChatClient Instance { get; private set; }

        private HubConnection _connection; //소켓 연결 담당.
        private Task<bool> _connectTask; //연결 작업
        
        //소켓연결은 백그라운드에서 이루어지고, 이걸 UI등에 반영할때는 Foreground로 올려야 해.
        private readonly ConcurrentQueue<Action> _mainThread = new ConcurrentQueue<Action>();
        private const int HistoryCap = 100; //채팅창 히스토리 저장.
        private readonly List<ChatMessage> _history = new List<ChatMessage>(HistoryCap);

        public event Action<ChatMessage> OnMessage;
        public event Action OnConnected;
        public event Action<string> OnClosed;

        public bool IsConnected => _connection is { State: HubConnectionState.Connected };
        public IReadOnlyList<ChatMessage> History => _history; //히스토리 메시지에 요소를 추가하거나 삭제하지 못하게 읽기전용

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void BootStrap()
        {
            if(Instance == null)
                new GameObject("ChatClient").AddComponent<ChatClient>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void Update()
        {
            //박민성 피셜 애미 쓰레드에서 등골메시지 뽑기를 시도한다.
            while (_mainThread.TryDequeue(out Action action))
            {
                action(); //수행
            }
        }


        //연결이 되었음을 보장해주는 함수.
        public Task<bool> EnsureConnectedAsync()
        {
            if(IsConnected) return Task.FromResult(true); //이녀석은 쓰레딩작업을 안하고 바로 리턴하는거
            if (_connectTask != null) return _connectTask;
            _connectTask = ConnectAsync();
            
            return _connectTask;
        }

        private async Task<bool> ConnectAsync()
        {
            try
            {
                if (string.IsNullOrEmpty(ApiClient.Instance.Token))
                    return false; //만약에 토큰이 없다면 할 수 있는게 없어. 

                string hubUrl = ApiClient.Instance.ServerUrl.TrimEnd('/') + "/hubs/chat";
                _connection = new HubConnectionBuilder()
                    .WithUrl(hubUrl, options =>
                    {
                        //AccessTokenProvider의 경우 Task<string>을 받는다.(토큰을 읽어오는 비동기 작업이 있을 수도 있으니
                        //하지만 우리는 이미 토큰을 메모리에 올려두었기 때문에 비동기 형태만 맞추어서 Task.FromResult로 보낸다.
                        options.AccessTokenProvider = () => Task.FromResult(ApiClient.Instance.Token);
                    })
                    .WithAutomaticReconnect()
                    .Build();

                _connection.On<ChatMessage>("ReceiveMessage",
                    msg => _mainThread.Enqueue(() => Deliver(msg)));

                _connection.Closed += error =>
                {
                    _mainThread.Enqueue(() => OnClosed?.Invoke(error?.Message));
                    return Task.CompletedTask;
                };

                await _connection.StartAsync();
                _mainThread.Enqueue(() => OnConnected?.Invoke());

                return true;
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Chat client] 연결 실패 {e.Message}");
                _connection = null;
                throw;
            }
            finally
            {
                _connectTask = null; //종료시 무조건 task는 처리
            }
        }

        private void Deliver(ChatMessage msg)
        {
            _history.Add(msg);
            if(_history.Count > HistoryCap)
                _history.RemoveAt(0);
            OnMessage?.Invoke(msg);
        }
        
        //우리가 서버로 송신
        public async Task SendMessageAsync(string channel, string text)
        {
            if (!IsConnected)
                return;

            try
            {
                //서버쪽에 있는 함수를 호출한다.
                await _connection.InvokeAsync("SendMessage", channel, text);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Chat client] 전송실패 : {e.Message}");
                throw;
            }
        }

        //로그아웃이나 유저 교체시에 연결 끊고. 히스토리 비워준다.
        public async Task DisconnectAsync()
        {
            _connectTask = null;
            _history.Clear();
            if (_connection == null)
                return;
            HubConnection conn = _connection;
            _connection = null;
            try
            {
                await conn.DisposeAsync(); //연결관련 내용을 정리
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[Chat client] 종료 예외 발생 : {e.Message}");
            }
        }

        private void OnDestroy()
        {
            if(Instance == this)
                _ = DisconnectAsync(); //파괴시에도 연결종료로 서버에 안전하게 종료를 알린다.
        }
    }
}
