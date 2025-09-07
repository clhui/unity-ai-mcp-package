#!/usr/bin/env python3
"""
简单的Unity线程信息测试脚本
这个脚本模拟了get_thread_info工具的输出
"""

import threading
import os
import psutil

def get_thread_info():
    """获取当前Python进程的线程信息（模拟Unity线程信息）"""
    
    print("Unity线程信息:")
    print("=" * 50)
    
    # 当前线程信息
    current_thread = threading.current_thread()
    print(f"- 当前线程ID: {current_thread.ident}")
    print(f"- 当前线程名称: {current_thread.name}")
    print(f"- 是否守护线程: {current_thread.daemon}")
    print(f"- 线程是否存活: {current_thread.is_alive()}")
    print()
    
    # 主线程信息
    main_thread = threading.main_thread()
    print("Unity主线程信息:")
    print(f"- 主线程ID: {main_thread.ident}")
    print(f"- 主线程名称: {main_thread.name}")
    print(f"- 是否在主线程: {current_thread.ident == main_thread.ident}")
    print(f"- 编辑器模式: True")
    print(f"- 播放模式: False")
    print()
    
    # 系统进程信息
    process = psutil.Process()
    print("系统进程信息:")
    print(f"- 进程ID: {process.pid}")
    print(f"- 进程名称: {process.name()}")
    print(f"- 线程数量: {process.num_threads()}")
    print(f"- 工作集内存: {process.memory_info().rss / 1024 / 1024:.1f} MB")
    print(f"- CPU使用率: {process.cpu_percent():.1f}%")
    print()
    
    # 活动线程信息
    active_threads = threading.enumerate()
    print("线程池信息:")
    print(f"- 活动线程数量: {len(active_threads)}")
    print(f"- 主线程数量: 1")
    print(f"- 工作线程数量: {len(active_threads) - 1}")
    print()
    
    print("活动线程列表:")
    for i, thread in enumerate(active_threads, 1):
        print(f"  {i}. {thread.name} (ID: {thread.ident}, 守护: {thread.daemon})")
    
    print("=" * 50)

if __name__ == "__main__":
    print("🚀 启动Unity线程信息测试...")
    print()
    
    try:
        get_thread_info()
        print("\n✅ 线程信息获取成功！")
        print("\n📝 说明: 这是模拟的线程信息输出")
        print("   在实际Unity环境中，get_thread_info工具将显示Unity特定的线程信息")
        
    except Exception as e:
        print(f"❌ 获取线程信息失败: {e}")
        import traceback
        traceback.print_exc()
    
    print("\n🎯 测试完成")