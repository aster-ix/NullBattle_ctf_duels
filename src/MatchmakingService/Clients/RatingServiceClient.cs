using NullBattle.Matchmaking.DTOs;
using NullBattle.Matchmaking.Exceptions;
using NullBattle.Matchmaking.Services;
using System.Net.Http.Headers;

namespace NullBattle.Matchmaking.Clients;

public class RatingServiceClient
{
    private readonly HttpClient _http;
    private readonly ServiceTokenProvider _tokens;

    public RatingServiceClient(HttpClient http, ServiceTokenProvider tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    public async Task<int> GetRatingAsync(long userId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/ratings/{userId}");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.GetToken());

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            throw new AppException(502, "RatingService недоступен");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new AppException(502, "RatingService вернул ошибку");
        }

        var result = await response.Content.ReadFromJsonAsync<RatingResponse>();
        if (result == null)
        {
            throw new AppException(502, "RatingService вернул пустой ответ");
        }
        return result.Rating;
    }
}