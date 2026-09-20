using System.Threading;
using System.Threading.Tasks;
using Networking.Dtos;

namespace Networking
{
    public static class CharacterApi
    {
        //내 캐릭터 정보 조회
        public static Task<ApiResult<CharacterResponse>> MeAsync(CancellationToken ct = default)
            => ApiClient.Instance.GetAsync<CharacterResponse>("/api/character/me", ct);
    }
}