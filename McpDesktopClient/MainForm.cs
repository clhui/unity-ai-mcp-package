using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json;
using McpDesktopClient.Models;
using McpDesktopClient.Services;

namespace McpDesktopClient
{
    public partial class MainForm : Form
    {
        private McpClientService _mcpClient;
        private TestPresetManager _presetManager;
        private SampleDataGenerator _sampleGenerator;
        private List<McpTool> _availableTools = new List<McpTool>();
        private McpTool? _selectedTool;

        public MainForm()
        {
            InitializeComponent();
            _mcpClient = new McpClientService();
            _presetManager = new TestPresetManager();
            _sampleGenerator = new SampleDataGenerator();
            InitializeForm();
        }

        private void InitializeForm()
        {
            // 设置默认参数模板
            UpdateArgumentsTemplate();
            
            // 添加预设功能的右键菜单
            AddPresetContextMenu();
        }
        
        private void AddPresetContextMenu()
        {
            var contextMenu = new ContextMenuStrip();
            
            var loadPresetItem = new ToolStripMenuItem("加载预设");
            loadPresetItem.Click += LoadPreset_Click;
            contextMenu.Items.Add(loadPresetItem);
            
            var savePresetItem = new ToolStripMenuItem("保存为预设");
            savePresetItem.Click += SavePreset_Click;
            contextMenu.Items.Add(savePresetItem);
            
            contextMenu.Items.Add(new ToolStripSeparator());
            
            var showPresetsItem = new ToolStripMenuItem("显示所有预设");
            showPresetsItem.Click += ShowPresets_Click;
            contextMenu.Items.Add(showPresetsItem);
            
            textBoxArguments.ContextMenuStrip = contextMenu;
        }
        
        private void LoadPreset_Click(object sender, EventArgs e)
        {
            var presets = _presetManager.GetAllPresets();
            if (presets.Count == 0)
            {
                MessageBox.Show("没有可用的预设", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            
            var presetForm = new PresetSelectionForm(presets);
            if (presetForm.ShowDialog() == DialogResult.OK && presetForm.SelectedPreset != null)
            {
                var preset = presetForm.SelectedPreset;
                
                // 查找对应的工具
                var tool = _availableTools.FirstOrDefault(t => t.Name == preset.Tool);
                if (tool != null)
                {
                    // 选中工具
                    var index = _availableTools.IndexOf(tool);
                    listBoxTools.SelectedIndex = index;
                    
                    // 设置参数
                    textBoxArguments.Text = JsonConvert.SerializeObject(preset.Arguments, Formatting.Indented);
                    
                    toolStripStatusLabel.Text = $"已加载预设: {preset.Name}";
                }
                else
                {
                    MessageBox.Show($"未找到工具 '{preset.Tool}'，请先连接到服务器并刷新工具列表。", "工具未找到", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        
        private void SavePreset_Click(object sender, EventArgs e)
        {
            if (_selectedTool == null)
            {
                MessageBox.Show("请先选择一个工具", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            
            var name = Microsoft.VisualBasic.Interaction.InputBox("请输入预设名称:", "保存预设", $"{_selectedTool.Name}_预设");
            if (string.IsNullOrWhiteSpace(name))
            {
                return;
            }
            
            try
            {
                var arguments = JsonConvert.DeserializeObject<Dictionary<string, object>>(textBoxArguments.Text) 
                               ?? new Dictionary<string, object>();
                
                var preset = new TestPreset
                {
                    Name = name.Trim(),
                    Tool = _selectedTool.Name,
                    Description = _selectedTool.Description,
                    Arguments = arguments
                };
                
                _presetManager.AddPreset(preset);
                toolStripStatusLabel.Text = $"预设 '{name}' 已保存";
                MessageBox.Show($"预设 '{name}' 保存成功！", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (JsonException ex)
            {
                MessageBox.Show($"参数JSON格式错误，无法保存预设：{ex.Message}", "格式错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
        
        private void ShowPresets_Click(object sender, EventArgs e)
        {
            var presets = _presetManager.GetAllPresets();
            var presetForm = new PresetManagementForm(_presetManager);
            presetForm.ShowDialog();
        }

        private async void ButtonConnect_Click(object sender, EventArgs e)
        {
            try
            {
                buttonConnect.Enabled = false;
                toolStripStatusLabel.Text = "正在连接...";

                var serverUrl = textBoxServerUrl.Text.Trim();
                if (string.IsNullOrEmpty(serverUrl))
                {
                    MessageBox.Show("请输入服务器地址", "错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                _mcpClient.SetServerUrl(serverUrl);

                // 测试连接并获取工具列表
                var toolsList = await _mcpClient.GetToolsListAsync();
                if (toolsList != null)
                {
                    _availableTools = toolsList.Tools;
                    UpdateToolsList();
                    toolStripStatusLabel.Text = $"已连接 - 发现 {_availableTools.Count} 个工具";
                    MessageBox.Show($"连接成功！发现 {_availableTools.Count} 个可用工具。", "连接成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                else
                {
                    toolStripStatusLabel.Text = "连接失败";
                    MessageBox.Show("无法连接到MCP服务器，请检查服务器地址和状态。", "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel.Text = "连接失败";
                MessageBox.Show($"连接失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                buttonConnect.Enabled = true;
            }
        }

        private async void ButtonRefreshTools_Click(object sender, EventArgs e)
        {
            try
            {
                buttonRefreshTools.Enabled = false;
                toolStripStatusLabel.Text = "正在刷新工具列表...";

                var toolsList = await _mcpClient.GetToolsListAsync();
                if (toolsList != null)
                {
                    _availableTools = toolsList.Tools;
                    UpdateToolsList();
                    toolStripStatusLabel.Text = $"工具列表已刷新 - {_availableTools.Count} 个工具";
                }
                else
                {
                    toolStripStatusLabel.Text = "刷新失败";
                    MessageBox.Show("无法获取工具列表，请检查连接状态。", "刷新失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            catch (Exception ex)
            {
                toolStripStatusLabel.Text = "刷新失败";
                MessageBox.Show($"刷新失败：{ex.Message}", "错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                buttonRefreshTools.Enabled = true;
            }
        }

        private void UpdateToolsList()
        {
            listBoxTools.Items.Clear();
            foreach (var tool in _availableTools)
            {
                listBoxTools.Items.Add($"{tool.Name} - {tool.Description}");
            }
        }

        private void ListBoxTools_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxTools.SelectedIndex >= 0 && listBoxTools.SelectedIndex < _availableTools.Count)
            {
                _selectedTool = _availableTools[listBoxTools.SelectedIndex];
                textBoxSelectedTool.Text = _selectedTool.Name;
                UpdateArgumentsTemplate();
            }
        }

        private void UpdateArgumentsTemplate()
        {
            if (_selectedTool != null)
            {
                try
                {
                    // 使用带依赖项检查的样例生成
                    var sampleArguments = _sampleGenerator.GenerateArgumentsWithDependencyCheck(_selectedTool);
                    textBoxArguments.Text = JsonConvert.SerializeObject(sampleArguments, Formatting.Indented);
                    
                    // 显示依赖项信息
                    if (sampleArguments.ContainsKey("_dependency_info"))
                    {
                        toolStripStatusLabel.Text = $"已为工具 '{_selectedTool.Name}' 生成样例参数 (含依赖项检查)";
                    }
                    else
                    {
                        toolStripStatusLabel.Text = $"已为工具 '{_selectedTool.Name}' 生成样例参数";
                    }
                }
                catch (Exception ex)
                {
                    // 如果生成失败，使用默认模板
                    textBoxArguments.Text = "{\r\n  \"example_param\": \"example_value\"\r\n}";
                    toolStripStatusLabel.Text = $"生成样例参数失败: {ex.Message}";
                }
            }
            else
            {
                textBoxArguments.Text = "{\r\n  \"example_param\": \"example_value\"\r\n}";
                toolStripStatusLabel.Text = "请选择一个工具";
            }
        }

        private void ButtonGenerateSample_Click(object sender, EventArgs e)
        {
            if (_selectedTool == null)
            {
                MessageBox.Show("请先选择一个工具", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                // 手动重新生成样例参数
                var sampleArguments = _sampleGenerator.GenerateSampleArguments(_selectedTool);
                textBoxArguments.Text = JsonConvert.SerializeObject(sampleArguments, Formatting.Indented);
                
                toolStripStatusLabel.Text = $"已重新生成工具 '{_selectedTool.Name}' 的样例参数";
                MessageBox.Show($"已为工具 '{_selectedTool.Name}' 重新生成样例参数！", "生成成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                toolStripStatusLabel.Text = $"生成样例参数失败: {ex.Message}";
                MessageBox.Show($"生成样例参数失败：{ex.Message}", "生成失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private async void ButtonCallTool_Click(object sender, EventArgs e)
        {
            if (_selectedTool == null)
            {
                MessageBox.Show("请先选择一个工具", "提示", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            try
            {
                buttonCallTool.Enabled = false;
                toolStripStatusLabel.Text = "正在调用工具...";

                // 解析参数
                var argumentsText = textBoxArguments.Text.Trim();
                Dictionary<string, object> arguments;
                
                if (string.IsNullOrEmpty(argumentsText) || argumentsText == "{}")
                {
                    arguments = new Dictionary<string, object>();
                }
                else
                {
                    try
                    {
                        arguments = JsonConvert.DeserializeObject<Dictionary<string, object>>(argumentsText) 
                                   ?? new Dictionary<string, object>();
                    }
                    catch (JsonException ex)
                    {
                        MessageBox.Show($"参数JSON格式错误：{ex.Message}", "参数错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return;
                    }
                }

                // 调用工具
                var result = await _mcpClient.CallToolAsync(_selectedTool.Name, arguments);
                
                // 显示结果
                if (result != null)
                {
                    var resultText = "";
                    if (result.IsError)
                    {
                        resultText = "错误：\r\n";
                    }
                    else
                    {
                        resultText = "成功：\r\n";
                    }

                    foreach (var content in result.Content)
                    {
                        resultText += $"[{content.Type}] {content.Text}\r\n";
                    }

                    textBoxResults.Text = resultText;
                    toolStripStatusLabel.Text = result.IsError ? "工具调用失败" : "工具调用成功";
                }
                else
                {
                    textBoxResults.Text = "调用失败：未收到响应";
                    toolStripStatusLabel.Text = "工具调用失败";
                }
            }
            catch (Exception ex)
            {
                textBoxResults.Text = $"调用异常：{ex.Message}";
                toolStripStatusLabel.Text = "工具调用异常";
                MessageBox.Show($"调用工具时发生异常：{ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                buttonCallTool.Enabled = true;
            }
        }

        private async void ButtonThreadStackInfo_Click(object sender, EventArgs e)
        {
            try
            {
                buttonThreadStackInfo.Enabled = false;
                toolStripStatusLabel.Text = "正在获取线程栈信息...";

                // 直接调用线程栈信息工具
                var result = await _mcpClient.CallToolAsync("get_thread_stack_info", new Dictionary<string, object>());
                
                // 显示结果
                if (result != null)
                {
                    var resultText = "";
                    if (result.IsError)
                    {
                        resultText = "线程栈信息获取失败：\r\n";
                    }
                    else
                    {
                        resultText = "Unity线程栈信息：\r\n";
                    }

                    foreach (var content in result.Content)
                    {
                        resultText += $"[{content.Type}] {content.Text}\r\n";
                    }

                    textBoxResults.Text = resultText;
                    toolStripStatusLabel.Text = result.IsError ? "线程栈信息获取失败" : "线程栈信息获取成功";
                }
                else
                {
                    textBoxResults.Text = "获取失败：未收到响应";
                    toolStripStatusLabel.Text = "线程栈信息获取失败";
                }
            }
            catch (Exception ex)
            {
                textBoxResults.Text = $"获取异常：{ex.Message}";
                toolStripStatusLabel.Text = "线程栈信息获取异常";
                MessageBox.Show($"获取线程栈信息时发生异常：{ex.Message}", "异常", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                buttonThreadStackInfo.Enabled = true;
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _mcpClient?.Dispose();
            base.OnFormClosed(e);
        }
    }
}