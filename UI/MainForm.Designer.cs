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
        serversGrid = new DataGridView();
        btnSaveServers = new Button();
        btnAddServer = new Button();
        ((System.ComponentModel.ISupportInitialize)rulesGrid).BeginInit();
        ((System.ComponentModel.ISupportInitialize)serversGrid).BeginInit();
        SuspendLayout();
        // 
        // richTextBoxLogs
        // 
        richTextBoxLogs.Dock = DockStyle.Left;
        richTextBoxLogs.Location = new Point(0, 0);
        richTextBoxLogs.Name = "richTextBoxLogs";
        richTextBoxLogs.Size = new Size(300, 682);
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
        rulesGrid.Size = new Size(582, 360);
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
        // serversGrid
        // 
        serversGrid.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        serversGrid.Location = new Point(310, 420);
        serversGrid.Name = "serversGrid";
        serversGrid.Size = new Size(582, 200);
        serversGrid.TabIndex = 0;
        // 
        // btnSaveServers
        // 
        btnSaveServers.Location = new Point(310, 630);
        btnSaveServers.Name = "btnSaveServers";
        btnSaveServers.Size = new Size(75, 23);
        btnSaveServers.TabIndex = 1;
        btnSaveServers.Text = "Сохранить DNS";
        btnSaveServers.Click += BtnSaveServers_Click;
        // 
        // btnAddServer
        // 
        btnAddServer.Location = new Point(410, 630);
        btnAddServer.Name = "btnAddServer";
        btnAddServer.Size = new Size(75, 23);
        btnAddServer.TabIndex = 2;
        btnAddServer.Text = "Добавить DNS";
        btnAddServer.Click += BtnAddServer_Click;
        // 
        // MainForm
        // 
        ClientSize = new Size(902, 682);
        Controls.Add(serversGrid);
        Controls.Add(btnSaveServers);
        Controls.Add(btnAddServer);
        Controls.Add(richTextBoxLogs);
        Controls.Add(rulesGrid);
        Controls.Add(btnSaveRules);
        Controls.Add(btnAddRule);
        Name = "MainForm";
        Text = "DNS Proxy UI";
        Load += MainForm_Load;
        ((System.ComponentModel.ISupportInitialize)rulesGrid).EndInit();
        ((System.ComponentModel.ISupportInitialize)serversGrid).EndInit();
        ResumeLayout(false);
    }
    #endregion
    private void InitServersGrid()
    {
        serversGrid.Columns.Clear();

        serversGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Адрес", DataPropertyName = "Address" });
        serversGrid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "DoH", DataPropertyName = "IsDoh" });
        serversGrid.Columns.Add(new DataGridViewCheckBoxColumn { HeaderText = "WireFormat", DataPropertyName = "UseWireFormat" });
        serversGrid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = "Приоритет", DataPropertyName = "Priority" });

        // Добавим редактирование/удаление позже, если нужно
    }

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
    private DataGridView serversGrid;
    private Button btnSaveServers;
    private Button btnAddServer;

}