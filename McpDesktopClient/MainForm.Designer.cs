namespace McpDesktopClient
{
    partial class MainForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.logViewerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.groupBoxConnection = new System.Windows.Forms.GroupBox();
            this.buttonConnect = new System.Windows.Forms.Button();
            this.textBoxServerUrl = new System.Windows.Forms.TextBox();
            this.labelServerUrl = new System.Windows.Forms.Label();
            this.groupBoxTools = new System.Windows.Forms.GroupBox();
            this.buttonRefreshTools = new System.Windows.Forms.Button();
            this.buttonClearSearch = new System.Windows.Forms.Button();
            this.textBoxSearch = new System.Windows.Forms.TextBox();
            this.labelSearch = new System.Windows.Forms.Label();
            this.listBoxTools = new System.Windows.Forms.ListBox();
            this.groupBoxToolCall = new System.Windows.Forms.GroupBox();
            this.buttonGenerateSample = new System.Windows.Forms.Button();
            this.buttonCallTool = new System.Windows.Forms.Button();
            this.buttonThreadStackInfo = new System.Windows.Forms.Button();
            this.textBoxArguments = new System.Windows.Forms.TextBox();
            this.labelArguments = new System.Windows.Forms.Label();
            this.textBoxSelectedTool = new System.Windows.Forms.TextBox();
            this.labelSelectedTool = new System.Windows.Forms.Label();
            this.groupBoxResults = new System.Windows.Forms.GroupBox();
            this.textBoxResults = new System.Windows.Forms.TextBox();
            this.statusStrip = new System.Windows.Forms.StatusStrip();
            this.toolStripStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
            this.menuStrip.SuspendLayout();
            this.groupBoxConnection.SuspendLayout();
            this.groupBoxTools.SuspendLayout();
            this.groupBoxToolCall.SuspendLayout();
            this.groupBoxResults.SuspendLayout();
            this.statusStrip.SuspendLayout();
            this.SuspendLayout();
            // 
            // menuStrip
            // 
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolsToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(784, 24);
            this.menuStrip.TabIndex = 0;
            this.menuStrip.Text = "menuStrip";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.logViewerToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(44, 20);
            this.toolsToolStripMenuItem.Text = "工具";
            // 
            // logViewerToolStripMenuItem
            // 
            this.logViewerToolStripMenuItem.Name = "logViewerToolStripMenuItem";
            this.logViewerToolStripMenuItem.Size = new System.Drawing.Size(124, 22);
            this.logViewerToolStripMenuItem.Text = "日志查看器";
            this.logViewerToolStripMenuItem.Click += new System.EventHandler(this.LogViewerToolStripMenuItem_Click);
            // 
            // groupBoxConnection
            // 
            this.groupBoxConnection.Controls.Add(this.buttonConnect);
            this.groupBoxConnection.Controls.Add(this.textBoxServerUrl);
            this.groupBoxConnection.Controls.Add(this.labelServerUrl);
            this.groupBoxConnection.Location = new System.Drawing.Point(12, 12);
            this.groupBoxConnection.Name = "groupBoxConnection";
            this.groupBoxConnection.Size = new System.Drawing.Size(760, 60);
            this.groupBoxConnection.TabIndex = 0;
            this.groupBoxConnection.TabStop = false;
            this.groupBoxConnection.Text = "连接设置";
            // 
            // buttonConnect
            // 
            this.buttonConnect.Location = new System.Drawing.Point(650, 20);
            this.buttonConnect.Name = "buttonConnect";
            this.buttonConnect.Size = new System.Drawing.Size(100, 25);
            this.buttonConnect.TabIndex = 2;
            this.buttonConnect.Text = "连接";
            this.buttonConnect.UseVisualStyleBackColor = true;
            this.buttonConnect.Click += new System.EventHandler(this.ButtonConnect_Click);
            // 
            // textBoxServerUrl
            // 
            this.textBoxServerUrl.Location = new System.Drawing.Point(100, 22);
            this.textBoxServerUrl.Name = "textBoxServerUrl";
            this.textBoxServerUrl.Size = new System.Drawing.Size(540, 23);
            this.textBoxServerUrl.TabIndex = 1;
            this.textBoxServerUrl.Text = "http://localhost:9123/mcp";
            // 
            // labelServerUrl
            // 
            this.labelServerUrl.AutoSize = true;
            this.labelServerUrl.Location = new System.Drawing.Point(15, 25);
            this.labelServerUrl.Name = "labelServerUrl";
            this.labelServerUrl.Size = new System.Drawing.Size(79, 17);
            this.labelServerUrl.TabIndex = 0;
            this.labelServerUrl.Text = "服务器地址:";
            // 
            // groupBoxTools
            // 
            this.groupBoxTools.Controls.Add(this.buttonRefreshTools);
            this.groupBoxTools.Controls.Add(this.buttonClearSearch);
            this.groupBoxTools.Controls.Add(this.textBoxSearch);
            this.groupBoxTools.Controls.Add(this.labelSearch);
            this.groupBoxTools.Controls.Add(this.listBoxTools);
            this.groupBoxTools.Location = new System.Drawing.Point(12, 78);
            this.groupBoxTools.Name = "groupBoxTools";
            this.groupBoxTools.Size = new System.Drawing.Size(370, 300);
            this.groupBoxTools.TabIndex = 1;
            this.groupBoxTools.TabStop = false;
            this.groupBoxTools.Text = "可用工具";
            // 
            // buttonRefreshTools
            // 
            this.buttonRefreshTools.Location = new System.Drawing.Point(15, 260);
            this.buttonRefreshTools.Name = "buttonRefreshTools";
            this.buttonRefreshTools.Size = new System.Drawing.Size(100, 25);
            this.buttonRefreshTools.TabIndex = 3;
            this.buttonRefreshTools.Text = "刷新工具";
            this.buttonRefreshTools.UseVisualStyleBackColor = true;
            this.buttonRefreshTools.Click += new System.EventHandler(this.ButtonRefreshTools_Click);
            // 
            // buttonClearSearch
            // 
            this.buttonClearSearch.Location = new System.Drawing.Point(280, 50);
            this.buttonClearSearch.Name = "buttonClearSearch";
            this.buttonClearSearch.Size = new System.Drawing.Size(75, 23);
            this.buttonClearSearch.TabIndex = 2;
            this.buttonClearSearch.Text = "清除";
            this.buttonClearSearch.UseVisualStyleBackColor = true;
            this.buttonClearSearch.Click += new System.EventHandler(this.ButtonClearSearch_Click);
            // 
            // textBoxSearch
            // 
            this.textBoxSearch.Location = new System.Drawing.Point(60, 50);
            this.textBoxSearch.Name = "textBoxSearch";
            this.textBoxSearch.Size = new System.Drawing.Size(210, 23);
            this.textBoxSearch.TabIndex = 1;
            this.textBoxSearch.TextChanged += new System.EventHandler(this.TextBoxSearch_TextChanged);
            // 
            // labelSearch
            // 
            this.labelSearch.AutoSize = true;
            this.labelSearch.Location = new System.Drawing.Point(15, 53);
            this.labelSearch.Name = "labelSearch";
            this.labelSearch.Size = new System.Drawing.Size(39, 17);
            this.labelSearch.TabIndex = 0;
            this.labelSearch.Text = "搜索:";
            // 
            // listBoxTools
            // 
            this.listBoxTools.FormattingEnabled = true;
            this.listBoxTools.ItemHeight = 17;
            this.listBoxTools.Location = new System.Drawing.Point(15, 80);
            this.listBoxTools.Name = "listBoxTools";
            this.listBoxTools.Size = new System.Drawing.Size(340, 170);
            this.listBoxTools.TabIndex = 4;
            this.listBoxTools.SelectedIndexChanged += new System.EventHandler(this.ListBoxTools_SelectedIndexChanged);
            // 
            // groupBoxToolCall
            // 
            this.groupBoxToolCall.Controls.Add(this.buttonGenerateSample);
            this.groupBoxToolCall.Controls.Add(this.buttonCallTool);
            this.groupBoxToolCall.Controls.Add(this.buttonThreadStackInfo);
            this.groupBoxToolCall.Controls.Add(this.textBoxArguments);
            this.groupBoxToolCall.Controls.Add(this.labelArguments);
            this.groupBoxToolCall.Controls.Add(this.textBoxSelectedTool);
            this.groupBoxToolCall.Controls.Add(this.labelSelectedTool);
            this.groupBoxToolCall.Location = new System.Drawing.Point(402, 78);
            this.groupBoxToolCall.Name = "groupBoxToolCall";
            this.groupBoxToolCall.Size = new System.Drawing.Size(370, 300);
            this.groupBoxToolCall.TabIndex = 2;
            this.groupBoxToolCall.TabStop = false;
            this.groupBoxToolCall.Text = "工具调用";
            // 
            // buttonGenerateSample
            // 
            this.buttonGenerateSample.Location = new System.Drawing.Point(255, 260);
            this.buttonGenerateSample.Name = "buttonGenerateSample";
            this.buttonGenerateSample.Size = new System.Drawing.Size(100, 25);
            this.buttonGenerateSample.TabIndex = 6;
            this.buttonGenerateSample.Text = "生成样例";
            this.buttonGenerateSample.UseVisualStyleBackColor = true;
            this.buttonGenerateSample.Click += new System.EventHandler(this.ButtonGenerateSample_Click);
            // 
            // buttonCallTool
            // 
            this.buttonCallTool.Location = new System.Drawing.Point(15, 260);
            this.buttonCallTool.Name = "buttonCallTool";
            this.buttonCallTool.Size = new System.Drawing.Size(100, 25);
            this.buttonCallTool.TabIndex = 4;
            this.buttonCallTool.Text = "调用工具";
            this.buttonCallTool.UseVisualStyleBackColor = true;
            this.buttonCallTool.Click += new System.EventHandler(this.ButtonCallTool_Click);
            // 
            // buttonThreadStackInfo
            // 
            this.buttonThreadStackInfo.Location = new System.Drawing.Point(125, 260);
            this.buttonThreadStackInfo.Name = "buttonThreadStackInfo";
            this.buttonThreadStackInfo.Size = new System.Drawing.Size(120, 25);
            this.buttonThreadStackInfo.TabIndex = 5;
            this.buttonThreadStackInfo.Text = "线程栈信息";
            this.buttonThreadStackInfo.UseVisualStyleBackColor = true;
            this.buttonThreadStackInfo.Click += new System.EventHandler(this.ButtonThreadStackInfo_Click);
            // 
            // textBoxArguments
            // 
            this.textBoxArguments.Location = new System.Drawing.Point(15, 80);
            this.textBoxArguments.Multiline = true;
            this.textBoxArguments.Name = "textBoxArguments";
            this.textBoxArguments.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxArguments.Size = new System.Drawing.Size(340, 170);
            this.textBoxArguments.TabIndex = 3;
            this.textBoxArguments.Text = "{\r\n  \"example_param\": \"example_value\"\r\n}";
            // 
            // labelArguments
            // 
            this.labelArguments.AutoSize = true;
            this.labelArguments.Location = new System.Drawing.Point(15, 60);
            this.labelArguments.Name = "labelArguments";
            this.labelArguments.Size = new System.Drawing.Size(127, 17);
            this.labelArguments.TabIndex = 2;
            this.labelArguments.Text = "参数 (JSON格式):";
            // 
            // textBoxSelectedTool
            // 
            this.textBoxSelectedTool.Location = new System.Drawing.Point(80, 25);
            this.textBoxSelectedTool.Name = "textBoxSelectedTool";
            this.textBoxSelectedTool.ReadOnly = true;
            this.textBoxSelectedTool.Size = new System.Drawing.Size(275, 23);
            this.textBoxSelectedTool.TabIndex = 1;
            // 
            // labelSelectedTool
            // 
            this.labelSelectedTool.AutoSize = true;
            this.labelSelectedTool.Location = new System.Drawing.Point(15, 28);
            this.labelSelectedTool.Name = "labelSelectedTool";
            this.labelSelectedTool.Size = new System.Drawing.Size(59, 17);
            this.labelSelectedTool.TabIndex = 0;
            this.labelSelectedTool.Text = "选中工具:";
            // 
            // groupBoxResults
            // 
            this.groupBoxResults.Controls.Add(this.textBoxResults);
            this.groupBoxResults.Location = new System.Drawing.Point(12, 384);
            this.groupBoxResults.Name = "groupBoxResults";
            this.groupBoxResults.Size = new System.Drawing.Size(760, 200);
            this.groupBoxResults.TabIndex = 3;
            this.groupBoxResults.TabStop = false;
            this.groupBoxResults.Text = "执行结果";
            // 
            // textBoxResults
            // 
            this.textBoxResults.Location = new System.Drawing.Point(15, 25);
            this.textBoxResults.Multiline = true;
            this.textBoxResults.Name = "textBoxResults";
            this.textBoxResults.ReadOnly = true;
            this.textBoxResults.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBoxResults.Size = new System.Drawing.Size(730, 160);
            this.textBoxResults.TabIndex = 0;
            // 
            // statusStrip
            // 
            this.statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripStatusLabel});
            this.statusStrip.Location = new System.Drawing.Point(0, 595);
            this.statusStrip.Name = "statusStrip";
            this.statusStrip.Size = new System.Drawing.Size(784, 22);
            this.statusStrip.TabIndex = 4;
            this.statusStrip.Text = "statusStrip1";
            // 
            // toolStripStatusLabel
            // 
            this.toolStripStatusLabel.Name = "toolStripStatusLabel";
            this.toolStripStatusLabel.Size = new System.Drawing.Size(32, 17);
            this.toolStripStatusLabel.Text = "就绪";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(784, 617);
            this.Controls.Add(this.statusStrip);
            this.Controls.Add(this.groupBoxResults);
            this.Controls.Add(this.groupBoxToolCall);
            this.Controls.Add(this.groupBoxTools);
            this.Controls.Add(this.groupBoxConnection);
            this.Controls.Add(this.menuStrip);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MainMenuStrip = this.menuStrip;
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "MCP Desktop Client - Unity MCP服务器测试工具";
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.groupBoxConnection.ResumeLayout(false);
            this.groupBoxConnection.PerformLayout();
            this.groupBoxTools.ResumeLayout(false);
            this.groupBoxToolCall.ResumeLayout(false);
            this.groupBoxToolCall.PerformLayout();
            this.groupBoxResults.ResumeLayout(false);
            this.groupBoxResults.PerformLayout();
            this.statusStrip.ResumeLayout(false);
            this.statusStrip.PerformLayout();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.GroupBox groupBoxConnection;
        private System.Windows.Forms.Button buttonConnect;
        private System.Windows.Forms.TextBox textBoxServerUrl;
        private System.Windows.Forms.Label labelServerUrl;
        private System.Windows.Forms.GroupBox groupBoxTools;
        private System.Windows.Forms.Button buttonRefreshTools;
        private System.Windows.Forms.Button buttonClearSearch;
        private System.Windows.Forms.TextBox textBoxSearch;
        private System.Windows.Forms.Label labelSearch;
        private System.Windows.Forms.ListBox listBoxTools;
        private System.Windows.Forms.GroupBox groupBoxToolCall;
        private System.Windows.Forms.Button buttonGenerateSample;
        private System.Windows.Forms.Button buttonCallTool;
        private System.Windows.Forms.Button buttonThreadStackInfo;
        private System.Windows.Forms.TextBox textBoxArguments;
        private System.Windows.Forms.Label labelArguments;
        private System.Windows.Forms.TextBox textBoxSelectedTool;
        private System.Windows.Forms.Label labelSelectedTool;
        private System.Windows.Forms.GroupBox groupBoxResults;
        private System.Windows.Forms.TextBox textBoxResults;
        private System.Windows.Forms.StatusStrip statusStrip;
        private System.Windows.Forms.ToolStripStatusLabel toolStripStatusLabel;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem logViewerToolStripMenuItem;
    }
}