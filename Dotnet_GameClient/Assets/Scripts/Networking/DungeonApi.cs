using System.Threading;
using System.Threading.Tasks;
using Networking.Dtos;

namespace Networking
{
    public static class DungeonApi
    {
        public static Task<ApiResult<DungeonEnterResponse>> EnterAsync(int dungeonId, CancellationToken ct = default)
            => ApiClient.Instance.PostAsync<DungeonEnterResponse>($"/api/dungeon/{dungeonId}/enter", null, ct);
        
        public static Task<ApiResult<DungeonResultResponse>> CompleteAsync(int runId, DungeonCompleteRequest body,
            CancellationToken ct = default)
            => ApiClient.Instance.PostAsync<DungeonResultResponse>($"/api/dungeon/{runId}/complete", body, ct);
    }
}