using Newtonsoft.Json;
using System.Collections.Generic;

namespace McpDesktopClient.Models
{
    public class JsonRpcRequest
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";

        [JsonProperty("id")]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [JsonProperty("method")]
        public string Method { get; set; } = string.Empty;

        [JsonProperty("params")]
        public object? Params { get; set; }
    }

    public class JsonRpcResponse
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = string.Empty;

        [JsonProperty("id")]
        public string Id { get; set; } = string.Empty;

        [JsonProperty("result")]
        public object? Result { get; set; }

        [JsonProperty("error")]
        public JsonRpcError? Error { get; set; }
    }

    public class JsonRpcError
    {
        [JsonProperty("code")]
        public int Code { get; set; }

        [JsonProperty("message")]
        public string Message { get; set; } = string.Empty;

        [JsonProperty("data")]
        public object? Data { get; set; }
    }

    public class McpTool
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("description")]
        public string Description { get; set; } = string.Empty;

        [JsonProperty("inputSchema")]
        public object? InputSchema { get; set; }
    }

    public class McpToolsListResponse
    {
        [JsonProperty("tools")]
        public List<McpTool> Tools { get; set; } = new List<McpTool>();
    }

    public class McpToolCallRequest
    {
        [JsonProperty("name")]
        public string Name { get; set; } = string.Empty;

        [JsonProperty("arguments")]
        public Dictionary<string, object> Arguments { get; set; } = new Dictionary<string, object>();
    }

    public class McpToolResult
    {
        [JsonProperty("content")]
        public List<McpContent> Content { get; set; } = new List<McpContent>();

        [JsonProperty("isError")]
        public bool IsError { get; set; }
    }

    public class McpContent
    {
        [JsonProperty("type")]
        public string Type { get; set; } = string.Empty;

        [JsonProperty("text")]
        public string Text { get; set; } = string.Empty;
    }
}