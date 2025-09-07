using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Newtonsoft.Json;
using McpDesktopClient.Services;

namespace McpDesktopClient
{
    public partial class PresetManagementForm : Form
    {
        private TestPresetManager _presetManager;
        private List<TestPreset> _presets;

        public PresetManagementForm(TestPresetManager presetManager)
        {
            _presetManager = presetManager;
            _presets = _presetManager.GetAllPresets();
            InitializeComponent();
            LoadPresets();
        }

        private void InitializeComponent()
        {
            this.listBoxPresets = new ListBox();
            this.buttonDelete = new Button();
            this.buttonClose = new Button();
            this.groupBoxDetails = new GroupBox();
            this.labelName = new Label();
            this.textBoxName = new TextBox();
            this.labelTool = new Label();
            this.textBoxTool = new TextBox();
            this.labelDescription = new Label();
            this.textBoxDescription = new TextBox();
            this.labelArguments = new Label();
            this.textBoxArguments = new TextBox();
            this.buttonSave = new Button();
            this.groupBoxDetails.SuspendLayout();
            this.SuspendLayout();
            
            // listBoxPresets
            this.listBoxPresets.FormattingEnabled = true;
            this.listBoxPresets.ItemHeight = 17;
            this.listBoxPresets.Location = new System.Drawing.Point(12, 12);
            this.listBoxPresets.Name = "listBoxPresets";
            this.listBoxPresets.Size = new System.Drawing.Size(250, 350);
            this.listBoxPresets.TabIndex = 0;
            this.listBoxPresets.SelectedIndexChanged += ListBoxPresets_SelectedIndexChanged;
            
            // buttonDelete
            this.buttonDelete.Enabled = false;
            this.buttonDelete.Location = new System.Drawing.Point(12, 375);
            this.buttonDelete.Name = "buttonDelete";
            this.buttonDelete.Size = new System.Drawing.Size(75, 25);
            this.buttonDelete.TabIndex = 1;
            this.buttonDelete.Text = "删除";
            this.buttonDelete.UseVisualStyleBackColor = true;
            this.buttonDelete.Click += ButtonDelete_Click;
            
            // groupBoxDetails
            this.groupBoxDetails.Controls.Add(this.buttonSave);
            this.groupBoxDetails.Controls.Add(this.textBoxArguments);
            this.groupBoxDetails.Controls.Add(this.labelArguments);
            this.groupBoxDetails.Controls.Add(this.textBoxDescription);
            this.groupBoxDetails.Controls.Add(this.labelDescription);
            this.groupBoxDetails.Controls.Add(this.textBoxTool);
            this.groupBoxDetails.Controls.Add(this.labelTool);
            this.groupBoxDetails.Controls.Add(this.textBoxName);
            this.groupBoxDetails.Controls.Add(this.labelName);
            this.groupBoxDetails.Location = new System.Drawing.Point(280, 12);
            this.groupBoxDetails.Name = "groupBoxDetails";
            this.groupBoxDetails.Size = new System.Drawing.Size(400, 350);
            this.groupBoxDetails.TabIndex = 2;
            this.groupBoxDetails.TabStop = false;
            this.groupBoxDetails.Text = "预设详情";
            
            // labelName
            this.labelName.AutoSize = true;
            this.labelName.Location = new System.Drawing.Point(15, 25);
            this.labelName.Name = "labelName";
            this.labelName.Size = new System.Drawing.Size(44, 17);
            this.labelName.TabIndex = 0;
            this.labelName.Text = "名称:";
            
            // textBoxName
            this.textBoxName.Location = new System.Drawing.Point(15, 45);
            this.textBoxName.Name = "textBoxName";
            this.textBoxName.Size = new System.Drawing.Size(370, 23);
            this.textBoxName.TabIndex = 1;
            
            // labelTool
            this.labelTool.AutoSize = true;
            this.labelTool.Location = new System.Drawing.Point(15, 75);
            this.labelTool.Name = "labelTool";
            this.labelTool.Size = new System.Drawing.Size(44, 17);
            this.labelTool.TabIndex = 2;
            this.labelTool.Text = "工具:";
            
            // textBoxTool
            this.textBoxTool.Location = new System.Drawing.Point(15, 95);
            this.textBoxTool.Name = "textBoxTool";
            this.textBoxTool.ReadOnly = true;
            this.textBoxTool.Size = new System.Drawing.Size(370, 23);
            this.textBoxTool.TabIndex = 3;
            
            // labelDescription
            this.labelDescription.AutoSize = true;
            this.labelDescription.Location = new System.Drawing.Point(15, 125);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(44, 17);
            this.labelDescription.TabIndex = 4;
            this.labelDescription.Text = "描述:";
            
            // textBoxDescription
            this.textBoxDescription.Location = new System.Drawing.Point(15, 145);
            this.textBoxDescription.Multiline = true;
            this.textBoxDescription.Name = "textBoxDescription";
            this.textBoxDescription.Size = new System.Drawing.Size(370, 40);
            this.textBoxDescription.TabIndex = 5;
            
            // labelArguments
            this.labelArguments.AutoSize = true;
            this.labelArguments.Location = new System.Drawing.Point(15, 195);
            this.labelArguments.Name = "labelArguments";
            this.labelArguments.Size = new System.Drawing.Size(44, 17);
            this.labelArguments.TabIndex = 6;
            this.labelArguments.Text = "参数:";
            
            // textBoxArguments
            this.textBoxArguments.Location = new System.Drawing.Point(15, 215);
            this.textBoxArguments.Multiline = true;
            this.textBoxArguments.Name = "textBoxArguments";
            this.textBoxArguments.ScrollBars = ScrollBars.Vertical;
            this.textBoxArguments.Size = new System.Drawing.Size(370, 100);
            this.textBoxArguments.TabIndex = 7;
            
            // buttonSave
            this.buttonSave.Enabled = false;
            this.buttonSave.Location = new System.Drawing.Point(310, 320);
            this.buttonSave.Name = "buttonSave";
            this.buttonSave.Size = new System.Drawing.Size(75, 25);
            this.buttonSave.TabIndex = 8;
            this.buttonSave.Text = "保存";
            this.buttonSave.UseVisualStyleBackColor = true;
            this.buttonSave.Click += ButtonSave_Click;
            
            // buttonClose
            this.buttonClose.DialogResult = DialogResult.OK;
            this.buttonClose.Location = new System.Drawing.Point(605, 375);
            this.buttonClose.Name = "buttonClose";
            this.buttonClose.Size = new System.Drawing.Size(75, 25);
            this.buttonClose.TabIndex = 3;
            this.buttonClose.Text = "关闭";
            this.buttonClose.UseVisualStyleBackColor = true;
            
            // PresetManagementForm
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(692, 412);
            this.Controls.Add(this.buttonClose);
            this.Controls.Add(this.groupBoxDetails);
            this.Controls.Add(this.buttonDelete);
            this.Controls.Add(this.listBoxPresets);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PresetManagementForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "预设管理";
            this.groupBoxDetails.ResumeLayout(false);
            this.groupBoxDetails.PerformLayout();
            this.ResumeLayout(false);
        }

        private void LoadPresets()
        {
            listBoxPresets.Items.Clear();
            _presets = _presetManager.GetAllPresets();
            foreach (var preset in _presets)
            {
                listBoxPresets.Items.Add(preset);
            }
        }

        private void ListBoxPresets_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (listBoxPresets.SelectedIndex >= 0)
            {
                var preset = _presets[listBoxPresets.SelectedIndex];
                textBoxName.Text = preset.Name;
                textBoxTool.Text = preset.Tool;
                textBoxDescription.Text = preset.Description;
                textBoxArguments.Text = JsonConvert.SerializeObject(preset.Arguments, Formatting.Indented);
                
                buttonDelete.Enabled = true;
                buttonSave.Enabled = true;
            }
            else
            {
                ClearDetails();
                buttonDelete.Enabled = false;
                buttonSave.Enabled = false;
            }
        }

        private void ClearDetails()
        {
            textBoxName.Text = "";
            textBoxTool.Text = "";
            textBoxDescription.Text = "";
            textBoxArguments.Text = "";
        }

        private void ButtonDelete_Click(object sender, EventArgs e)
        {
            if (listBoxPresets.SelectedIndex >= 0)
            {
                var preset = _presets[listBoxPresets.SelectedIndex];
                var result = MessageBox.Show($"确定要删除预设 '{preset.Name}' 吗？", "确认删除", 
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                
                if (result == DialogResult.Yes)
                {
                    _presetManager.RemovePreset(preset.Name);
                    LoadPresets();
                    ClearDetails();
                    MessageBox.Show("预设已删除", "删除成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
            }
        }

        private void ButtonSave_Click(object sender, EventArgs e)
        {
            if (listBoxPresets.SelectedIndex >= 0)
            {
                try
                {
                    var arguments = JsonConvert.DeserializeObject<Dictionary<string, object>>(textBoxArguments.Text) 
                                   ?? new Dictionary<string, object>();
                    
                    var preset = new TestPreset
                    {
                        Name = textBoxName.Text.Trim(),
                        Tool = textBoxTool.Text.Trim(),
                        Description = textBoxDescription.Text.Trim(),
                        Arguments = arguments
                    };
                    
                    if (string.IsNullOrEmpty(preset.Name))
                    {
                        MessageBox.Show("预设名称不能为空", "验证错误", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        return;
                    }
                    
                    _presetManager.AddPreset(preset);
                    LoadPresets();
                    MessageBox.Show("预设已保存", "保存成功", MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (JsonException ex)
                {
                    MessageBox.Show($"参数JSON格式错误：{ex.Message}", "格式错误", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        private ListBox listBoxPresets;
        private Button buttonDelete;
        private Button buttonClose;
        private GroupBox groupBoxDetails;
        private Label labelName;
        private TextBox textBoxName;
        private Label labelTool;
        private TextBox textBoxTool;
        private Label labelDescription;
        private TextBox textBoxDescription;
        private Label labelArguments;
        private TextBox textBoxArguments;
        private Button buttonSave;
    }
}