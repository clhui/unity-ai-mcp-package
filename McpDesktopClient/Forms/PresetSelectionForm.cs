using System;
using System.Collections.Generic;
using System.Windows.Forms;
using McpDesktopClient.Services;

namespace McpDesktopClient
{
    public partial class PresetSelectionForm : Form
    {
        public TestPreset? SelectedPreset { get; private set; }
        private List<TestPreset> _presets;

        public PresetSelectionForm(List<TestPreset> presets)
        {
            _presets = presets;
            InitializeComponent();
            LoadPresets();
        }

        private void InitializeComponent()
        {
            this.listBoxPresets = new ListBox();
            this.buttonOK = new Button();
            this.buttonCancel = new Button();
            this.labelDescription = new Label();
            this.textBoxDescription = new TextBox();
            this.SuspendLayout();
            
            // listBoxPresets
            this.listBoxPresets.FormattingEnabled = true;
            this.listBoxPresets.ItemHeight = 17;
            this.listBoxPresets.Location = new System.Drawing.Point(12, 12);
            this.listBoxPresets.Name = "listBoxPresets";
            this.listBoxPresets.Size = new System.Drawing.Size(360, 200);
            this.listBoxPresets.TabIndex = 0;
            this.listBoxPresets.SelectedIndexChanged += ListBoxPresets_SelectedIndexChanged;
            
            // labelDescription
            this.labelDescription.AutoSize = true;
            this.labelDescription.Location = new System.Drawing.Point(12, 225);
            this.labelDescription.Name = "labelDescription";
            this.labelDescription.Size = new System.Drawing.Size(44, 17);
            this.labelDescription.TabIndex = 1;
            this.labelDescription.Text = "描述:";
            
            // textBoxDescription
            this.textBoxDescription.Location = new System.Drawing.Point(12, 245);
            this.textBoxDescription.Multiline = true;
            this.textBoxDescription.Name = "textBoxDescription";
            this.textBoxDescription.ReadOnly = true;
            this.textBoxDescription.ScrollBars = ScrollBars.Vertical;
            this.textBoxDescription.Size = new System.Drawing.Size(360, 60);
            this.textBoxDescription.TabIndex = 2;
            
            // buttonOK
            this.buttonOK.DialogResult = DialogResult.OK;
            this.buttonOK.Enabled = false;
            this.buttonOK.Location = new System.Drawing.Point(216, 320);
            this.buttonOK.Name = "buttonOK";
            this.buttonOK.Size = new System.Drawing.Size(75, 25);
            this.buttonOK.TabIndex = 3;
            this.buttonOK.Text = "确定";
            this.buttonOK.UseVisualStyleBackColor = true;
            this.buttonOK.Click += ButtonOK_Click;
            
            // buttonCancel
            this.buttonCancel.DialogResult = DialogResult.Cancel;
            this.buttonCancel.Location = new System.Drawing.Point(297, 320);
            this.buttonCancel.Name = "buttonCancel";
            this.buttonCancel.Size = new System.Drawing.Size(75, 25);
            this.buttonCancel.TabIndex = 4;
            this.buttonCancel.Text = "取消";
            this.buttonCancel.UseVisualStyleBackColor = true;
            
            // PresetSelectionForm
            this.AcceptButton = this.buttonOK;
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 17F);
            this.AutoScaleMode = AutoScaleMode.Font;
            this.CancelButton = this.buttonCancel;
            this.ClientSize = new System.Drawing.Size(384, 357);
            this.Controls.Add(this.buttonCancel);
            this.Controls.Add(this.buttonOK);
            this.Controls.Add(this.textBoxDescription);
            this.Controls.Add(this.labelDescription);
            this.Controls.Add(this.listBoxPresets);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "PresetSelectionForm";
            this.StartPosition = FormStartPosition.CenterParent;
            this.Text = "选择预设";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void LoadPresets()
        {
            listBoxPresets.Items.Clear();
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
                textBoxDescription.Text = $"工具: {preset.Tool}\r\n描述: {preset.Description}";
                buttonOK.Enabled = true;
            }
            else
            {
                textBoxDescription.Text = "";
                buttonOK.Enabled = false;
            }
        }

        private void ButtonOK_Click(object sender, EventArgs e)
        {
            if (listBoxPresets.SelectedIndex >= 0)
            {
                SelectedPreset = _presets[listBoxPresets.SelectedIndex];
            }
        }

        private ListBox listBoxPresets;
        private Button buttonOK;
        private Button buttonCancel;
        private Label labelDescription;
        private TextBox textBoxDescription;
    }
}