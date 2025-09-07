using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Unity.MCP
{
    [Serializable]
    public class JsonRpcMessage
    {
        [JsonProperty("jsonrpc")]
        public string JsonRpc { get; set; } = "2.0";
        
        [JsonProperty("id", NullValueHandling = NullValueHandling.Ignore)]
        public object Id { get; set; }
    }
    
    [Serializable]
    public class JsonRpcRequest : JsonRpcMessage
    {
        [JsonProperty("method")]
        public string Method { get; set; }
        
        [JsonProperty("params", NullValueHandling = NullValueHandling.Ignore)]
        public JObject Params { get; set; }
    }
    
    [Serializable]
    public class JsonRpcResponse : JsonRpcMessage
    {
        [JsonProperty("result", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Result { get; set; }
        
        [JsonProperty("error", NullValueHandling = NullValueHandling.Ignore)]
        public JsonRpcError Error { get; set; }
    }
    
    [Serializable]
    public class JsonRpcError
    {
        [JsonProperty("code")]
        public int Code { get; set; }
        
        [JsonProperty("message")]
        public string Message { get; set; }
        
        [JsonProperty("data", NullValueHandling = NullValueHandling.Ignore)]
        public JToken Data { get; set; }
    }
    
    [Serializable]
    public class McpCapabilities
    {
        [JsonProperty("tools", NullValueHandling = NullValueHandling.Ignore)]
        public McpToolsCapability Tools { get; set; }
    }
    
    [Serializable]
    public class McpToolsCapability
    {
        [JsonProperty("listChanged")]
        public bool ListChanged { get; set; } = false;
    }
    
    [Serializable]
    public class McpTool
    {
        [JsonProperty("name")]
        public string Name { get; set; }
        
        [JsonProperty("title", NullValueHandling = NullValueHandling.Ignore)]
        public string Title { get; set; }
        
        [JsonProperty("description")]
        public string Description { get; set; }
        
        [JsonProperty("inputSchema")]
        public JObject InputSchema { get; set; }
    }
    
    [Serializable]
    public class McpToolResult
    {
        [JsonProperty("content")]
        public List<McpContent> Content { get; set; } = new List<McpContent>();
        
        [JsonProperty("isError")]
        public bool IsError { get; set; } = false;
    }
    
    [Serializable]
    public class McpContent
    {
        [JsonProperty("type")]
        public string Type { get; set; }
        
        [JsonProperty("text", NullValueHandling = NullValueHandling.Ignore)]
        public string Text { get; set; }
    }
}