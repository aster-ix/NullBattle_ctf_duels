using NullBattle.Duels.DTOs;
using NullBattle.Duels.Exceptions;
using NullBattle.Duels.Models;
using NullBattle.Duels.Services;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace NullBattle.Duels.Clients;

public class TaskServiceClient
{
    private static readonly JsonSerializerOptions JsonOptions =
        new JsonSerializerOptions(JsonSerializerDefaults.Web)
        {
            Converters = { new JsonStringEnumConverter() }
        };

    private readonly HttpClient _http;
    private readonly ServiceTokenProvider _tokens;

    public TaskServiceClient(HttpClient http, ServiceTokenProvider tokens)
    {
        _http = http;
        _tokens = tokens;
    }

    public async Task<List<CategoryDto>> GetRandomCategoriesAsync(int count)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/categories/random?count={count}");
        return await SendAsync<List<CategoryDto>>(request);
    }

    public async Task<TaskResponse> GetTaskAsync(long taskId)
    {
        var request = new HttpRequestMessage(HttpMethod.Get, $"/api/tasks/{taskId}");
        return await SendAsync<TaskResponse>(request);
    }

    public async Task<TaskResponse> GetRandomTaskAsync(long categoryId, Difficulty difficulty)
    {
        var request = new HttpRequestMessage(HttpMethod.Get,
            $"/api/tasks/random?categoryId={categoryId}&difficulty={difficulty}");
        return await SendAsync<TaskResponse>(request);
    }

    public async Task<bool> VerifyFlagAsync(long taskId, string flag)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, $"/api/tasks/{taskId}/verify-flag");
        request.Content = JsonContent.Create(new { flag });

        var result = await SendAsync<VerifyFlagResponse>(request);
        return result.IsCorrect;
    }

    private async Task<T> SendAsync<T>(HttpRequestMessage request)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _tokens.GetToken());

        HttpResponseMessage response;
        try
        {
            response = await _http.SendAsync(request);
        }
        catch (Exception ex) when (ex is HttpRequestException || ex is TaskCanceledException)
        {
            throw new AppException(502, "TaskService недоступен");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new AppException(502, "TaskService вернул ошибку");
        }

        var result = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        if (result == null)
        {
            throw new AppException(502, "TaskService вернул пустой ответ");
        }
        return result;
    }
}