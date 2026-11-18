
using NullBattle.Matchmaking.DTOs;
using NullBattle.Matchmaking.Exceptions;
using NullBattle.Matchmaking.Models;
using NullBattle.Matchmaking.Services;
using System.Net;
using System.Net.Http.Headers;

namespace NullBattle.Matchmaking.Clients;

public class DuelServiceClient
{
    private readonly HttpClient _http;
    private readonly ServiceTokenProvider _tokens;

    public DuelServiceClient(HttpClient http, ServiceTokenProvider tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    public async Task<long> CreateDuelAsync(long player1Id, long player2Id, Difficulty difficulty)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/api/duels");
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.GetToken());
        request.Content = JsonContent.Create(new
        {
            player1Id,
            player2Id,
            difficulty = difficulty.ToString()
        });

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            throw new AppException(502, "DuelService недоступен");
        }

        if (response.StatusCode == HttpStatusCode.Conflict)
        {
            throw new AppException(409, "Один из игроков уже участвует в дуэли");
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new AppException(502, "DuelService вернул ошибку");
        }

        var result = await response.Content.ReadFromJsonAsync<DuelCreatedResponse>();
        if (result == null)
        {
            throw new AppException(502, "DuelService вернул пустой ответ");
        }
        return result.Id;
    }
}