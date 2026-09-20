using System.Collections.Generic;
using CoreSystem;
using Networking;
using Networking.Dtos;
using UnityEngine;

namespace Test
{
    public class DungeonTestHarness : MonoBehaviour
    {
        [SerializeField] private int[] dungeonIds = { 1, 2 };

        private int _dungeonId = 1;
        private DungeonEnterResponse _entered;
        private readonly Dictionary<int, int> _tally = new Dictionary<int, int>();   // monsterId → 로컬 처치 수
        private DungeonResultResponse _lastResult;
        private string _status = "던전을 선택하고 입장하세요.";
        private bool _busy;
        private GUIStyle _rich;
        
        private void SetKill(int monsterId, int value, int cap)
            => _tally[monsterId] = Mathf.Clamp(value, 0, cap);

        // async void: OnGUI 버튼 콜백에서 호출하는 "발화 후 잊기". UnityWebRequest는 메인스레드에서
        // 완료되므로 이어지는 필드 대입도 메인스레드라 안전. _busy로 중복 클릭을 막는다.
        private async void Enter()
        {
            _busy = true;
            _status = "입장 요청 중...";
            var res = await DungeonApi.EnterAsync(_dungeonId);
            _busy = false;

            if (res.IsSuccess)
            {
                _entered = res.Data;
                _tally.Clear();
                _lastResult = null;
                _status = $"입장 성공: {_entered.Name}";
            }
            else
            {
                _status = $"입장 실패({res.StatusCode}): {res.Error?.ToUserMessage()}";
            }
        }

        private async void Complete()
        {
            if (_entered == null)
                return;

            _busy = true;
            _status = "결과 제출 중...";

            var body = new DungeonCompleteRequest();
            foreach (var kv in _tally)
                if (kv.Value > 0)
                    body.Kills.Add(new MonsterKill { MonsterId = kv.Key, Count = kv.Value });

            var res = await DungeonApi.CompleteAsync(_entered.RunId, body);
            _busy = false;

            if (res.IsSuccess)
            {
                _lastResult = res.Data;
                _status = "정산 완료. 다시 입장하려면 던전을 선택하세요.";
                _entered = null;   // 런 소진(중복 제출은 서버가 409로 막음) — 재입장 필요
                _tally.Clear();
            }
            else
            {
                _status = $"제출 실패({res.StatusCode}): {res.Error?.ToUserMessage()}";
            }
        }
        
        private void OnGUI()
        {
            _rich ??= new GUIStyle(GUI.skin.label) { richText = true };

            GUILayout.BeginArea(new Rect(Screen.width - 500, 20, 480, Screen.height - 40), GUI.skin.box);

            GUILayout.Label("<b>던전 테스트 하네스</b>", _rich);
            bool hasToken = ApiClient.Instance != null && ApiClient.Instance.HasToken;
            GUILayout.Label($"로그인: {(hasToken ? "OK" : "토큰 없음 — 로그인 후 이용")}");
            GUILayout.Space(6);

            // ── 던전 선택 + 입장 ──
            GUILayout.BeginHorizontal();
            GUILayout.Label("던전:", GUILayout.Width(40));
            foreach (var id in dungeonIds)
            {
                bool on = _dungeonId == id;
                if (GUILayout.Toggle(on, $"#{id}", GUI.skin.button, GUILayout.Width(50)) && !on)
                    _dungeonId = id;
            }
            GUILayout.FlexibleSpace();
            GUI.enabled = !_busy && hasToken;
            if (GUILayout.Button("입장(enter)", GUILayout.Width(120)))
                Enter();
            GUI.enabled = true;
            GUILayout.EndHorizontal();

            GUILayout.Space(6);
            GUILayout.Label($"상태: {_status}");

            // ── 입장한 던전 구성 → 종류별 처치 집계 ──
            if (_entered != null)
            {
                GUILayout.Space(8);
                GUILayout.Label($"<b>{_entered.Name}</b>  (runId={_entered.RunId})", _rich);

                foreach (var s in _entered.Spawns)
                {
                    int killed = _tally.TryGetValue(s.MonsterId, out var v) ? v : 0;
                    GUILayout.BeginHorizontal();
                    GUILayout.Label($"{s.MonsterName}  (exp {s.Exp}/gold {s.Gold})   {killed}/{s.Count}",
                        GUILayout.Width(280));
                    if (GUILayout.Button("-", GUILayout.Width(28))) SetKill(s.MonsterId, killed - 1, s.Count);
                    if (GUILayout.Button("+", GUILayout.Width(28))) SetKill(s.MonsterId, killed + 1, s.Count);
                    if (GUILayout.Button("MAX", GUILayout.Width(48))) SetKill(s.MonsterId, s.Count, s.Count);
                    GUILayout.EndHorizontal();
                }

                GUILayout.Space(8);
                GUI.enabled = !_busy;
                if (GUILayout.Button("클리어(결과 제출 complete)"))
                    Complete();
                GUI.enabled = true;
            }

            // ── 마지막 정산 결과(서버 인정치) ──
            if (_lastResult != null)
            {
                GUILayout.Space(12);
                GUILayout.Label("<b>정산 결과 (서버 인정치)</b>", _rich);
                GUILayout.Label($"처치 {_lastResult.MonsterKilled}  ·  획득 골드 {_lastResult.GoldGained}  ·  경험치 {_lastResult.ExpGained}");
                GUILayout.Label($"총 골드 {_lastResult.TotalGold}  ·  Lv {_lastResult.Character.Level} " +
                                $"(exp {_lastResult.Character.Exp}/{_lastResult.Character.ExpToNextLevel})");
            }

            GUILayout.FlexibleSpace();
            if (GUILayout.Button("마을로 돌아가기"))
                SceneRouter.Go(SceneRouter.TownScene);

            GUILayout.EndArea();
        }
    }

}