#!/usr/bin/env python3
# -*- coding: utf-8 -*-
"""
Unity MCP 线程栈信息工具测试脚本
测试新添加的 get_thread_stack_info 工具功能
"""

import json
import requests
import time
from datetime import datetime

def test_thread_stack_info_tool():
    """
    测试线程栈信息工具
    """
    print("=" * 60)
    print("Unity MCP 线程栈信息工具测试")
    print("=" * 60)
    print(f"测试时间: {datetime.now().strftime('%Y-%m-%d %H:%M:%S')}")
    print()
    
    # 服务器配置
    server_url = "http://localhost:3000"
    
    try:
        # 1. 测试服务器连接
        print("1. 测试服务器连接...")
        response = requests.get(f"{server_url}/health", timeout=5)
        if response.status_code == 200:
            print("   ✓ 服务器连接成功")
        else:
            print(f"   ✗ 服务器连接失败: {response.status_code}")
            return
    except Exception as e:
        print(f"   ✗ 服务器连接异常: {e}")
        print("   请确保Unity MCP服务器正在运行")
        return
    
    try:
        # 2. 获取工具列表
        print("\n2. 获取可用工具列表...")
        tools_response = requests.post(
            f"{server_url}/tools/list",
            headers={"Content-Type": "application/json"},
            json={},
            timeout=10
        )
        
        if tools_response.status_code == 200:
            tools_data = tools_response.json()
            tools = tools_data.get('tools', [])
            
            # 查找线程栈信息工具
            thread_stack_tool = None
            for tool in tools:
                if tool.get('name') == 'get_thread_stack_info':
                    thread_stack_tool = tool
                    break
            
            if thread_stack_tool:
                print("   ✓ 找到线程栈信息工具")
                print(f"   工具名称: {thread_stack_tool['name']}")
                print(f"   工具描述: {thread_stack_tool['description']}")
            else:
                print("   ✗ 未找到线程栈信息工具")
                print("   可用工具列表:")
                for tool in tools:
                    print(f"     - {tool.get('name', 'Unknown')}: {tool.get('description', 'No description')}")
                return
        else:
            print(f"   ✗ 获取工具列表失败: {tools_response.status_code}")
            return
            
    except Exception as e:
        print(f"   ✗ 获取工具列表异常: {e}")
        return
    
    try:
        # 3. 调用线程栈信息工具
        print("\n3. 调用线程栈信息工具...")
        call_response = requests.post(
            f"{server_url}/tools/call",
            headers={"Content-Type": "application/json"},
            json={
                "name": "get_thread_stack_info",
                "arguments": {}
            },
            timeout=15
        )
        
        if call_response.status_code == 200:
            result_data = call_response.json()
            print("   ✓ 工具调用成功")
            
            # 解析结果
            if result_data.get('isError', False):
                print("   ⚠ 工具执行出现错误:")
                for content in result_data.get('content', []):
                    print(f"     [{content.get('type', 'unknown')}] {content.get('text', '')}")
            else:
                print("   ✓ 线程栈信息获取成功:")
                for content in result_data.get('content', []):
                    content_text = content.get('text', '')
                    content_type = content.get('type', 'text')
                    print(f"\n   [{content_type.upper()}]")
                    
                    # 格式化输出
                    if content_type == 'text':
                        lines = content_text.split('\n')
                        for line in lines:
                            if line.strip():
                                print(f"     {line}")
                    else:
                        print(f"     {content_text}")
        else:
            print(f"   ✗ 工具调用失败: {call_response.status_code}")
            try:
                error_data = call_response.json()
                print(f"   错误信息: {error_data}")
            except:
                print(f"   响应内容: {call_response.text}")
            return
            
    except Exception as e:
        print(f"   ✗ 工具调用异常: {e}")
        return
    
    # 4. 多次调用测试
    print("\n4. 多次调用测试 (检测一致性)...")
    for i in range(3):
        try:
            print(f"   第 {i+1} 次调用...")
            call_response = requests.post(
                f"{server_url}/tools/call",
                headers={"Content-Type": "application/json"},
                json={
                    "name": "get_thread_stack_info",
                    "arguments": {}
                },
                timeout=10
            )
            
            if call_response.status_code == 200:
                result_data = call_response.json()
                if not result_data.get('isError', False):
                    print(f"     ✓ 第 {i+1} 次调用成功")
                else:
                    print(f"     ⚠ 第 {i+1} 次调用有错误")
            else:
                print(f"     ✗ 第 {i+1} 次调用失败: {call_response.status_code}")
                
            time.sleep(1)  # 间隔1秒
            
        except Exception as e:
            print(f"     ✗ 第 {i+1} 次调用异常: {e}")
    
    print("\n" + "=" * 60)
    print("测试完成!")
    print("\n使用说明:")
    print("1. 在桌面客户端中点击'线程栈信息'按钮可快速查看Unity线程状态")
    print("2. 该工具会分析所有活动线程的调用栈信息")
    print("3. 自动检测潜在的死锁风险并给出警告")
    print("4. 在Unity编辑器模式下提供更详细的线程信息")
    print("=" * 60)

if __name__ == "__main__":
    test_thread_stack_info_tool()