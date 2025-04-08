using System.Resources;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace DNS_proxy.UI;

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
        DataGridViewCellStyle dataGridViewCellStyle1 = new DataGridViewCellStyle();
        richTextBoxLogs = new RichTextBox();
        rulesGrid = new DataGridView();
        btnSaveRules = new Button();
        btnAddRule = new Button();
        ((System.ComponentModel.ISupportInitialize)rulesGrid).BeginInit();
        SuspendLayout();
        // 
        // richTextBoxLogs
        // 
        richTextBoxLogs.Dock = DockStyle.Left;
        richTextBoxLogs.Location = new Point(0, 0);
        richTextBoxLogs.Name = "richTextBoxLogs";
        richTextBoxLogs.Size = new Size(300, 450);
        richTextBoxLogs.TabIndex = 0;
        richTextBoxLogs.Text = "";
        // 
        // rulesGrid
        // 
        dataGridViewCellStyle1.Font = new Font("Segoe UI", 9F, FontStyle.Italic, GraphicsUnit.Point, 204);
        rulesGrid.AlternatingRowsDefaultCellStyle = dataGridViewCellStyle1;
        rulesGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        rulesGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        rulesGrid.Location = new Point(310, 10);
        rulesGrid.Name = "rulesGrid";
        rulesGrid.Size = new Size(480, 360);
        rulesGrid.TabIndex = 1;
        // 
        // btnSaveRules
        // 
        btnSaveRules.Location = new Point(310, 380);
        btnSaveRules.Name = "btnSaveRules";
        btnSaveRules.Size = new Size(75, 23);
        btnSaveRules.TabIndex = 2;
        btnSaveRules.Text = "Сохранить";
        btnSaveRules.Click += BtnSaveRules_Click;
        // 
        // btnAddRule
        // 
        btnAddRule.Location = new Point(410, 380);
        btnAddRule.Name = "btnAddRule";
        btnAddRule.Size = new Size(75, 23);
        btnAddRule.TabIndex = 3;
        btnAddRule.Text = "Добавить правило";
        btnAddRule.Click += BtnAddRule_Click;
        // 
        // MainForm
        // 
        ClientSize = new Size(800, 450);
        Controls.Add(richTextBoxLogs);
        Controls.Add(rulesGrid);
        Controls.Add(btnSaveRules);
        Controls.Add(btnAddRule);
        Name = "MainForm";
        Text = "DNS Proxy UI";
        Load += MainForm_Load;
        ((System.ComponentModel.ISupportInitialize)rulesGrid).EndInit();
        ResumeLayout(false);
    }
    #endregion
    private void InitDataGrid()
    {
        rulesGrid.AutoGenerateColumns = false;

        rulesGrid.Columns.Clear();

        rulesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "IP",
            DataPropertyName = "SourceIp",
            Name = "ipColumn"
        });
        rulesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Домен",
            DataPropertyName = "DomainPattern",
            Name = "domainColumn"
        });
        rulesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Действие",
            DataPropertyName = "Action",
            Name = "actionColumn"
        });
        rulesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            HeaderText = "Подменный IP",
            DataPropertyName = "RewriteIp",
            Name = "fakeIpColumn"
        });
        rulesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "EditColumn",
            HeaderText = "",
            MinimumWidth = 32,
            ReadOnly = true
        });

        rulesGrid.Columns.Add(new DataGridViewTextBoxColumn
        {
            Name = "DeleteColumn",
            HeaderText = "",
            MinimumWidth = 32,
            ReadOnly = true
        });
    }
    private RichTextBox richTextBoxLogs;
    private DataGridView rulesGrid;
    private Button btnSaveRules;
    private Button btnAddRule;
}