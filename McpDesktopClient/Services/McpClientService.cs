using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using McpDesktopClient.Models;

namespace McpDesktopClient.Services
{
    public class McpClientService
    {
        private readonly HttpClient _httpClient;
        private string _serverUrl = "http://localhost:9123/mcp";

        public McpClientService()
        {
            _httpClient = new HttpClient();
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
        }

        public void SetServerUrl(string url)
        {
            _serverUrl = url.TrimEnd('/');
        }

        public async Task<JsonRpcResponse> SendRequestAsync(JsonRpcRequest request)
        {
            try
            {
                var json = JsonConvert.SerializeObject(request, Formatting.Indented);
                var content = new StringContent(json, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync(_serverUrl, content);
                var responseJson = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    return new JsonRpcResponse
                    {
                        Id = request.Id,
                        Error = new JsonRpcError
                        {
                            Code = (int)response.StatusCode,
                            Message = $"HTTP Error: {response.StatusCode}",
                            Data = responseJson
                        }
                    };
                }

                var result = JsonConvert.DeserializeObject<JsonRpcResponse>(responseJson);
                return result ?? new JsonRpcResponse { Id = request.Id };
            }
            catch (Exception ex)
            {
                return new JsonRpcResponse
                {
                    Id = request.Id,
                    Error = new JsonRpcError
                    {
                        Code = -1,
                        Message = ex.Message,
                        Data = ex.StackTrace
                    }
                };
            }
        }

        public async Task<McpToolsListResponse?> GetToolsListAsync()
        {
            var request = new JsonRpcRequest
            {
                Method = "tools/list"
            };

            var response = await SendRequestAsync(request);
            if (response.Error != null || response.Result == null)
            {
                return null;
            }

            try
            {
                var json = JsonConvert.SerializeObject(response.Result);
                return JsonConvert.DeserializeObject<McpToolsListResponse>(json);
            }
            catch
            {
                return null;
            }
        }

        public async Task<McpToolResult?> CallToolAsync(string toolName, Dictionary<string, object> arguments)
        {
            var request = new JsonRpcRequest
            {
                Method = "tools/call",
                Params = new McpToolCallRequest
                {
                    Name = toolName,
                    Arguments = arguments
                }
            };

            var response = await SendRequestAsync(request);
            if (response.Error != null || response.Result == null)
            {
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent
                        {
                            Type = "text",
                            Text = response.Error?.Message ?? "Unknown error"
                        }
                    }
                };
            }

            try
            {
                var json = JsonConvert.SerializeObject(response.Result);
                return JsonConvert.DeserializeObject<McpToolResult>(json);
            }
            catch (Exception ex)
            {
                return new McpToolResult
                {
                    IsError = true,
                    Content = new List<McpContent>
                    {
                        new McpContent
                        {
                            Type = "text",
                            Text = $"Failed to parse result: {ex.Message}"
                        }
                    }
                };
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}