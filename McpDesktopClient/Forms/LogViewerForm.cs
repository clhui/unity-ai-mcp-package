using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using McpDesktopClient.Services;

namespace McpDesktopClient.Forms
{
    public partial class LogViewerForm : Form
    {
        private LoggingService _loggingService;
        private ListView _listViewLogs;
        private ComboBox _comboBoxLogLevel;
        private ComboBox _comboBoxCategory;
        private TextBox _textBoxSearch;
        private Button _buttonClear;
        private Button _buttonExport;
        private Button _buttonRefresh;
        private CheckBox _checkBoxAutoScroll;
        private ToolStripStatusLabel _labelStatus;
        private System.Windows.Forms.Timer _refreshTimer;
        
        public LogViewerForm(LoggingService loggingService)
        {
            _loggingService = loggingService ?? throw new ArgumentNullException(nameof(loggingService));
            InitializeComponent();
            
            // 确保所有控件都已初始化后再设置事件处理程序
            SetupEventHandlers();
            
            // 加载日志数据
            LoadLogs();
            
            // 设置自动刷新定时器
            _refreshTimer = new System.Windows.Forms.Timer();
            _refreshTimer.Interval = 2000; // 2秒刷新一次
            _refreshTimer.Tick += RefreshTimer_Tick;
            _refreshTimer.Start();
        }
        
        private void InitializeComponent()
        {
            this.Text = "日志查看器";
            this.Size = new Size(1000, 600);
            this.StartPosition = FormStartPosition.CenterParent;
            this.MinimumSize = new Size(800, 500);
            
            // 创建工具栏
            var toolStrip = new ToolStrip();
            toolStrip.Dock = DockStyle.Top;
            
            // 日志级别过滤
            var labelLevel = new ToolStripLabel("级别:");
            toolStrip.Items.Add(labelLevel);
            
            var comboBoxLevelHost = new ToolStripControlHost(CreateLogLevelComboBox());
            toolStrip.Items.Add(comboBoxLevelHost);
            
            toolStrip.Items.Add(new ToolStripSeparator());
            
            // 类别过滤
            var labelCategory = new ToolStripLabel("类别:");
            toolStrip.Items.Add(labelCategory);
            
            var comboBoxCategoryHost = new ToolStripControlHost(CreateCategoryComboBox());
            toolStrip.Items.Add(comboBoxCategoryHost);
            
            toolStrip.Items.Add(new ToolStripSeparator());
            
            // 搜索框
            var labelSearch = new ToolStripLabel("搜索:");
            toolStrip.Items.Add(labelSearch);
            
            var textBoxSearchHost = new ToolStripControlHost(CreateSearchTextBox());
            toolStrip.Items.Add(textBoxSearchHost);
            
            toolStrip.Items.Add(new ToolStripSeparator());
            
            // 按钮
            var buttonRefreshHost = new ToolStripControlHost(CreateRefreshButton());
            toolStrip.Items.Add(buttonRefreshHost);
            
            var buttonClearHost = new ToolStripControlHost(CreateClearButton());
            toolStrip.Items.Add(buttonClearHost);
            
            var buttonExportHost = new ToolStripControlHost(CreateExportButton());
            toolStrip.Items.Add(buttonExportHost);
            
            toolStrip.Items.Add(new ToolStripSeparator());
            
            // 自动滚动复选框
            var checkBoxAutoScrollHost = new ToolStripControlHost(CreateAutoScrollCheckBox());
            toolStrip.Items.Add(checkBoxAutoScrollHost);
            
            this.Controls.Add(toolStrip);
            
            // 创建日志列表视图
            _listViewLogs = new ListView();
            _listViewLogs.Dock = DockStyle.Fill;
            _listViewLogs.View = View.Details;
            _listViewLogs.FullRowSelect = true;
            _listViewLogs.GridLines = true;
            _listViewLogs.MultiSelect = true;
            _listViewLogs.VirtualMode = false;
            
            // 添加列
            _listViewLogs.Columns.Add("时间", 150);
            _listViewLogs.Columns.Add("级别", 80);
            _listViewLogs.Columns.Add("类别", 100);
            _listViewLogs.Columns.Add("消息", 600);
            
            this.Controls.Add(_listViewLogs);
            
            // 创建状态栏
            var statusStrip = new StatusStrip();
            _labelStatus = new ToolStripStatusLabel("就绪");
            statusStrip.Items.Add(_labelStatus);
            this.Controls.Add(statusStrip);
        }
        
        private ComboBox CreateLogLevelComboBox()
        {
            _comboBoxLogLevel = new ComboBox();
            _comboBoxLogLevel.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboBoxLogLevel.Width = 100;
            _comboBoxLogLevel.Items.Add("全部");
            _comboBoxLogLevel.Items.Add("调试");
            _comboBoxLogLevel.Items.Add("信息");
            _comboBoxLogLevel.Items.Add("警告");
            _comboBoxLogLevel.Items.Add("错误");
            _comboBoxLogLevel.SelectedIndex = 0;
            return _comboBoxLogLevel;
        }
        
        private ComboBox CreateCategoryComboBox()
        {
            _comboBoxCategory = new ComboBox();
            _comboBoxCategory.DropDownStyle = ComboBoxStyle.DropDownList;
            _comboBoxCategory.Width = 120;
            _comboBoxCategory.Items.Add("全部");
            _comboBoxCategory.SelectedIndex = 0;
            return _comboBoxCategory;
        }
        
        private TextBox CreateSearchTextBox()
        {
            _textBoxSearch = new TextBox();
            _textBoxSearch.Width = 200;
            // 移除PlaceholderText以避免兼容性问题
            return _textBoxSearch;
        }
        
        private Button CreateRefreshButton()
        {
            _buttonRefresh = new Button();
            _buttonRefresh.Text = "刷新";
            _buttonRefresh.Width = 60;
            return _buttonRefresh;
        }
        
        private Button CreateClearButton()
        {
            _buttonClear = new Button();
            _buttonClear.Text = "清空";
            _buttonClear.Width = 60;
            return _buttonClear;
        }
        
        private Button CreateExportButton()
        {
            _buttonExport = new Button();
            _buttonExport.Text = "导出";
            _buttonExport.Width = 60;
            return _buttonExport;
        }
        
        private CheckBox CreateAutoScrollCheckBox()
        {
            _checkBoxAutoScroll = new CheckBox();
            _checkBoxAutoScroll.Text = "自动滚动";
            _checkBoxAutoScroll.Checked = true;
            _checkBoxAutoScroll.Width = 80;
            return _checkBoxAutoScroll;
        }
        
        private void SetupEventHandlers()
        {
            _comboBoxLogLevel.SelectedIndexChanged += FilterChanged;
            _comboBoxCategory.SelectedIndexChanged += FilterChanged;
            _textBoxSearch.TextChanged += FilterChanged;
            _buttonRefresh.Click += ButtonRefresh_Click;
            _buttonClear.Click += ButtonClear_Click;
            _buttonExport.Click += ButtonExport_Click;
            _listViewLogs.DoubleClick += ListViewLogs_DoubleClick;
            
            // 订阅日志服务的事件
            _loggingService.LogEntryAdded += LoggingService_LogEntryAdded;
        }
        
        private void LoggingService_LogEntryAdded(object sender, LogEntry e)
        {
            // 在UI线程中更新界面
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateUIWithLogEntry(e)));
                return;
            }
            
            UpdateUIWithLogEntry(e);
        }
        
        private void UpdateUIWithLogEntry(LogEntry e)
        {
            
            // 更新类别下拉框
            if (!_comboBoxCategory.Items.Contains(e.Category) && e.Category != "全部")
            {
                _comboBoxCategory.Items.Add(e.Category);
            }
            
            // 如果当前过滤条件匹配，添加到列表
            if (ShouldShowLogEntry(e))
            {
                AddLogEntryToList(e);
                
                // 自动滚动到底部
                if (_checkBoxAutoScroll.Checked && _listViewLogs.Items.Count > 0)
                {
                    _listViewLogs.EnsureVisible(_listViewLogs.Items.Count - 1);
                }
            }
        }
        
        private void RefreshTimer_Tick(object sender, EventArgs e)
        {
            // 更新状态栏
            var totalLogs = _loggingService.GetLogEntries().Count;
            var displayedLogs = _listViewLogs.Items.Count;
            _labelStatus.Text = $"显示 {displayedLogs} / {totalLogs} 条日志";
        }
        
        private void FilterChanged(object sender, EventArgs e)
        {
            LoadLogs();
        }
        
        private void ButtonRefresh_Click(object sender, EventArgs e)
        {
            LoadLogs();
        }
        
        private void ButtonClear_Click(object sender, EventArgs e)
        {
            var result = MessageBox.Show("确定要清空所有日志吗？", "确认清空", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
            if (result == DialogResult.Yes)
            {
                _loggingService.ClearLogs();
                _listViewLogs.Items.Clear();
                _labelStatus.Text = "日志已清空";
            }
        }
        
        private async void ButtonExport_Click(object sender, EventArgs e)
        {
            var saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "文本文件 (*.txt)|*.txt|所有文件 (*.*)|*.*";
            saveFileDialog.FileName = $"mcp_logs_{DateTime.Now:yyyyMMdd_HHmmss}.txt";
            
            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                try
                {
                    await _loggingService.ExportLogsAsync(saveFileDialog.FileName);
                    MessageBox.Show($"日志已导出到：{saveFileDialog.FileName}", "导出成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"导出失败：{ex.Message}", "导出错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }
        
        private void ListViewLogs_DoubleClick(object sender, EventArgs e)
        {
            if (_listViewLogs.SelectedItems.Count > 0)
            {
                var item = _listViewLogs.SelectedItems[0];
                var logEntry = item.Tag as LogEntry;
                if (logEntry != null)
                {
                    ShowLogDetails(logEntry);
                }
            }
        }
        
        private void ShowLogDetails(LogEntry logEntry)
        {
            var details = $"时间：{logEntry.Timestamp:yyyy-MM-dd HH:mm:ss.fff}\n";
            details += $"级别：{logEntry.Level}\n";
            details += $"类别：{logEntry.Category}\n";
            details += $"消息：{logEntry.Message}\n";
            
            if (logEntry.Exception != null)
            {
                details += $"\n异常信息：\n{logEntry.Exception.Message}\n";
                if (!string.IsNullOrEmpty(logEntry.Exception.StackTrace))
                {
                    details += $"\n堆栈跟踪：\n{logEntry.Exception.StackTrace}";
                }
            }
            
            MessageBox.Show(details, "日志详情", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        
        private void LoadLogs()
        {
            if (_listViewLogs == null) return;
            
            _listViewLogs.Items.Clear();
            
            var logs = _loggingService.GetLogEntries();
            var filteredLogs = logs.Where(ShouldShowLogEntry).ToList();
            
            foreach (var log in filteredLogs)
            {
                AddLogEntryToList(log);
            }
            
            // 更新类别下拉框
            UpdateCategoryComboBox(logs);
            
            // 自动滚动到底部
            if (_checkBoxAutoScroll != null && _checkBoxAutoScroll.Checked && _listViewLogs.Items.Count > 0)
            {
                _listViewLogs.EnsureVisible(_listViewLogs.Items.Count - 1);
            }
        }
        
        private void UpdateCategoryComboBox(List<LogEntry> logs)
        {
            if (_comboBoxCategory == null) return;
            
            var currentSelection = _comboBoxCategory.SelectedItem?.ToString();
            var categories = logs.Select(l => l.Category).Distinct().OrderBy(c => c).ToList();
            
            _comboBoxCategory.Items.Clear();
            _comboBoxCategory.Items.Add("全部");
            
            foreach (var category in categories)
            {
                _comboBoxCategory.Items.Add(category);
            }
            
            // 恢复选择
            if (!string.IsNullOrEmpty(currentSelection) && _comboBoxCategory.Items.Contains(currentSelection))
            {
                _comboBoxCategory.SelectedItem = currentSelection;
            }
            else
            {
                _comboBoxCategory.SelectedIndex = 0;
            }
        }
        
        private bool ShouldShowLogEntry(LogEntry logEntry)
        {
            // 级别过滤
            if (_comboBoxLogLevel != null && _comboBoxLogLevel.SelectedIndex > 0)
            {
                var selectedLevel = (LogLevel)(_comboBoxLogLevel.SelectedIndex - 1);
                if (logEntry.Level != selectedLevel)
                    return false;
            }
            
            // 类别过滤
            if (_comboBoxCategory != null && _comboBoxCategory.SelectedIndex > 0 && _comboBoxCategory.SelectedItem != null)
            {
                var selectedCategory = _comboBoxCategory.SelectedItem.ToString();
                if (!logEntry.Category.Equals(selectedCategory, StringComparison.OrdinalIgnoreCase))
                    return false;
            }
            
            // 搜索过滤
            if (_textBoxSearch != null && !string.IsNullOrWhiteSpace(_textBoxSearch.Text))
            {
                var searchText = _textBoxSearch.Text.ToLower();
                if (!logEntry.Message.ToLower().Contains(searchText) && 
                    !logEntry.Category.ToLower().Contains(searchText))
                    return false;
            }
            
            return true;
        }
        
        private void AddLogEntryToList(LogEntry logEntry)
        {
            var item = new ListViewItem(logEntry.Timestamp.ToString("yyyy-MM-dd HH:mm:ss.fff"));
            item.SubItems.Add(logEntry.Level.ToString());
            item.SubItems.Add(logEntry.Category);
            item.SubItems.Add(logEntry.Message);
            item.Tag = logEntry;
            
            // 根据日志级别设置颜色
            switch (logEntry.Level)
            {
                case LogLevel.Error:
                    item.ForeColor = Color.Red;
                    break;
                case LogLevel.Warning:
                    item.ForeColor = Color.Orange;
                    break;
                case LogLevel.Debug:
                    item.ForeColor = Color.Gray;
                    break;
                default:
                    item.ForeColor = Color.Black;
                    break;
            }
            
            _listViewLogs.Items.Add(item);
        }
        
        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            // 取消订阅事件
            _loggingService.LogEntryAdded -= LoggingService_LogEntryAdded;
            
            // 停止定时器
            _refreshTimer?.Stop();
            _refreshTimer?.Dispose();
            
            base.OnFormClosed(e);
        }
    }
}