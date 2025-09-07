#!/usr/bin/env python3
"""
Unity MCP线程信息HTTP测试脚本
通过HTTP接口测试get_thread_info工具
"""

import requests
import json
import time

def test_thread_info_http():
    """通过HTTP接口测试Unity线程信息获取"""
    
    # MCP服务器HTTP端点
    base_url = "http://localhost:9123/mcp"
    
    print("🚀 启动Unity MCP线程信息HTTP测试...")
    print(f"📡 连接到: {base_url}")
    print()
    
    try:
        # 测试服务器连接
        print("🔍 测试服务器连接...")
        response = requests.get(f"{base_url}/health", timeout=5)
        if response.status_code == 200:
            print("✅ 服务器连接成功")
        else:
            print(f"⚠️ 服务器响应异常: {response.status_code}")
    except requests.exceptions.RequestException as e:
        print(f"❌ 无法连接到MCP服务器: {e}")
        print("💡 请确保Unity编辑器已启动并且MCP插件正在运行")
        return
    
    try:
        # 获取工具列表
        print("\n📋 获取可用工具列表...")
        tools_response = requests.post(
            f"{base_url}/tools/list",
            json={},
            headers={"Content-Type": "application/json"},
            timeout=10
        )
        
        if tools_response.status_code == 200:
            tools_data = tools_response.json()
            tools = tools_data.get('tools', [])
            tool_names = [tool.get('name', '') for tool in tools]
            print(f"🔧 可用工具: {', '.join(tool_names)}")
            
            if 'get_thread_info' in tool_names:
                print("✅ get_thread_info工具已找到")
            else:
                print("❌ get_thread_info工具未找到")
                print("💡 请确保已正确添加GetThreadInfo方法到UnityTools.cs")
                return
        else:
            print(f"❌ 获取工具列表失败: {tools_response.status_code}")
            return
            
    except requests.exceptions.RequestException as e:
        print(f"❌ 获取工具列表失败: {e}")
        return
    
    try:
        # 调用get_thread_info工具
        print("\n🧵 调用get_thread_info工具...")
        thread_info_response = requests.post(
            f"{base_url}/tools/call",
            json={
                "name": "get_thread_info",
                "arguments": {}
            },
            headers={"Content-Type": "application/json"},
            timeout=15
        )
        
        if thread_info_response.status_code == 200:
            result_data = thread_info_response.json()
            
            print("\n📊 Unity线程信息:")
            print("=" * 60)
            
            # 解析并显示结果
            if 'content' in result_data:
                for content in result_data['content']:
                    if content.get('type') == 'text':
                        print(content.get('text', ''))
            else:
                print("未找到内容数据")
                
            print("=" * 60)
            
            # 检查是否有错误
            if result_data.get('isError', False):
                print("⚠️ 工具执行时出现错误")
            else:
                print("✅ 线程信息获取成功！")
                
        else:
            print(f"❌ 调用get_thread_info失败: {thread_info_response.status_code}")
            print(f"响应内容: {thread_info_response.text}")
            
    except requests.exceptions.RequestException as e:
        print(f"❌ 调用get_thread_info失败: {e}")
        return
    
    print("\n🎯 测试完成")
    print("\n📝 说明:")
    print("   - 此测试通过HTTP接口调用Unity MCP服务器")
    print("   - 实际的线程信息来自Unity编辑器进程")
    print("   - 可以用于监控Unity的线程状态和性能")

def test_multiple_calls():
    """测试多次调用以观察线程信息变化"""
    
    base_url = "http://localhost:9123/mcp"
    
    print("\n🔄 测试多次调用get_thread_info...")
    
    for i in range(3):
        print(f"\n--- 第 {i+1} 次调用 ---")
        
        try:
            response = requests.post(
                f"{base_url}/tools/call",
                json={
                    "name": "get_thread_info",
                    "arguments": {}
                },
                headers={"Content-Type": "application/json"},
                timeout=10
            )
            
            if response.status_code == 200:
                result_data = response.json()
                if 'content' in result_data and result_data['content']:
                    text = result_data['content'][0].get('text', '')
                    # 只显示关键信息
                    lines = text.split('\n')
                    for line in lines[:8]:  # 只显示前8行
                        if line.strip():
                            print(line)
                print("✅ 调用成功")
            else:
                print(f"❌ 调用失败: {response.status_code}")
                
        except requests.exceptions.RequestException as e:
            print(f"❌ 请求失败: {e}")
            
        if i < 2:  # 不在最后一次调用后等待
            time.sleep(2)

if __name__ == "__main__":
    test_thread_info_http()
    
    # 询问是否进行多次调用测试
    try:
        user_input = input("\n❓ 是否进行多次调用测试？(y/n): ").strip().lower()
        if user_input in ['y', 'yes', '是']:
            test_multiple_calls()
    except KeyboardInterrupt:
        print("\n👋 测试中断")
    
    print("\n🏁 所有测试完成")