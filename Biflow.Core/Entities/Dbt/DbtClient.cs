using System.Net.Http.Json;
using System.Text.Json;

namespace Biflow.Core.Entities;

public class DbtClient
{
    private readonly HttpClient _httpClient;
    private readonly DbtAccount _account;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public DbtClient(DbtAccount account, IHttpClientFactory httpClientFactory)
    {
        _account = account;
        _httpClient = httpClientFactory.CreateClient();
        _httpClient.DefaultRequestHeaders.Add("Accept", "application/json");
        _httpClient.DefaultRequestHeaders.Authorization = new("Token", account.ApiToken);
        var url = account.ApiBaseUrl.EndsWith('/')
            ? account.ApiBaseUrl
            : $"{account.ApiBaseUrl}/";
        _httpClient.BaseAddress = new Uri(url);
    }

    public async Task TestConnectionAsync(CancellationToken cancellationToken = default)
    {
        var url = $"api/v2/accounts/{_account.AccountId}/jobs/?limit=1";
        var response = await _httpClient.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();
    }

    public async Task<IEnumerable<DbtProject>> GetProjectsAsync(CancellationToken cancellationToken = default)
    {
        var projects = new List<DbtProject>();
        var offset = 0;
        bool go;
        do
        {
            var url = $"api/v2/accounts/{_account.AccountId}/projects/?limit=100&order_by=id&offset={offset}";
            var response = await _httpClient.GetFromJsonAsync<ProjectsResponse>(url, JsonOptions, cancellationToken);
            ArgumentNullException.ThrowIfNull(response);
            projects.AddRange(response.Data);
            offset += response.Data.Length;
            go = response.Data.Length > 0 && response.Extra.Pagination.TotalCount > projects.Count;
        } while (go);
        projects.SortBy(p => p.Name);
        return projects;
    }

    public async Task<IEnumerable<DbtEnvironment>> GetEnvironmentsAsync(long? projectId, CancellationToken cancellationToken = default)
    {
        var environments = new List<DbtEnvironment>();
        var offset = 0;
        bool go;
        do
        {
            var url = projectId switch
            {
                { } id => $"api/v2/accounts/{_account.AccountId}/environments/?limit=100&project_id={id}&order_by=id&offset={offset}",
                _ => $"api/v2/accounts/{_account.AccountId}/environments/?limit=100&order_by=id&offset={offset}"
            };
            var response = await _httpClient.GetFromJsonAsync<EnvironmentsResponse>(url, JsonOptions, cancellationToken);
            ArgumentNullException.ThrowIfNull(response);
            environments.AddRange(response.Data);
            offset += response.Data.Length;
            go = response.Data.Length > 0 && response.Extra.Pagination.TotalCount > environments.Count;
        } while (go);
        environments.SortBy(p => p.Name);
        return environments;
    }

    public async Task<IEnumerable<DbtJob>> GetJobsAsync(long? environmentId, CancellationToken cancellationToken = default)
    {
        var jobs = new List<DbtJob>();
        var offset = 0;
        bool go;
        do
        {
            var url = environmentId switch
            {
                { } id => $"api/v2/accounts/{_account.AccountId}/jobs/?limit=100&environment_id={id}&order_by=id&offset={offset}",
                _ => $"api/v2/accounts/{_account.AccountId}/jobs/?limit=100&order_by=id&offset={offset}"
            };
            var response = await _httpClient.GetFromJsonAsync<JobsResponse>(url, JsonOptions, cancellationToken);
            ArgumentNullException.ThrowIfNull(response);
            jobs.AddRange(response.Data);
            offset += response.Data.Length;
            go = response.Data.Length > 0 && response.Extra.Pagination.TotalCount > jobs.Count;
        } while (go);
        jobs.SortBy(p => p.Name);
        return jobs;
    }

    public async Task<DbtJob?> GetJobAsync(long jobId, CancellationToken cancellationToken = default)
    {
        var url = $"api/v2/accounts/{_account.AccountId}/jobs/{jobId}/";
        var response = await _httpClient.GetFromJsonAsync<JobResponse?>(url, JsonOptions, cancellationToken);
        return response?.Data;
    }

    public async Task<DbtJobRun> TriggerJobRunAsync(long jobId, CancellationToken cancellationToken = default)
    {
        var url = $"api/v2/accounts/{_account.AccountId}/jobs/{jobId}/run/";
        var request = new TriggerJobRunRequest("Triggered via API");
        var response = await _httpClient.PostAsJsonAsync(url, request, JsonOptions, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var runResponse = JsonSerializer.Deserialize<JobRunResponse>(stream, JsonOptions);
        ArgumentNullException.ThrowIfNull(runResponse);
        return runResponse.Data;
    }

    public async Task<DbtJobRun> GetJobRunAsync(long runId, CancellationToken cancellationToken = default)
    {
        var url = $"api/v2/accounts/{_account.AccountId}/runs/{runId}/";
        var response = await _httpClient.GetFromJsonAsync<JobRunResponse>(url, JsonOptions, cancellationToken);
        ArgumentNullException.ThrowIfNull(response);
        return response.Data;
    }

    public async Task<DbtJobRun> CancelJobRunAsync(long runId, CancellationToken cancellationToken = default)
    {
        var url = $"api/v2/accounts/{_account.AccountId}/runs/{runId}/cancel/";
        var response = await _httpClient.PostAsync(url, null, cancellationToken);
        response.EnsureSuccessStatusCode();
        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
        var runResponse = JsonSerializer.Deserialize<JobRunResponse>(stream, JsonOptions);
        ArgumentNullException.ThrowIfNull(runResponse);
        return runResponse.Data;
    }
}

file record ProjectsResponse(DbtProject[] Data, Extra Extra);

file record EnvironmentsResponse(DbtEnvironment[] Data, Extra Extra);

file record JobsResponse(DbtJob[] Data, Extra Extra);

file record JobResponse(DbtJob? Data);

file record TriggerJobRunRequest(string Cause);

file record JobRunResponse(DbtJobRun Data);

file record Extra(Pagination Pagination);

file record Pagination(int TotalCount);