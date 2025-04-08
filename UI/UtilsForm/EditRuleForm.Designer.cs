using DNS_proxy.UI.Components;

namespace DNS_proxy.UI.UtilsForm;

public partial class EditRuleForm
{
    private System.ComponentModel.IContainer components = null;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null))
            components.Dispose();

        base.Dispose(disposing);
    }

    #region Windows Form Designer generated code

    private void InitializeComponent()
    {
        this.txtIp = new IpAddressControl();
        this.txtDomain = new TextBox();
        this.cmbAction = new ComboBox();
        this.txtRewriteIp = new TextBox();
        this.lblIp = new Label();
        this.lblDomain = new Label();
        this.lblAction = new Label();
        this.lblRewrite = new Label();
        this.btnSave = new Button();
        this.btnCancel = new Button();
        this.SuspendLayout();
        // 
        // txtIp
        // 
        this.txtIp.Location = new Point(120, 15);
        this.txtIp.Name = "txtIp";
        this.txtIp.Size = new Size(200, 23);
        this.txtIp.TabIndex = 0;
        // 
        // txtDomain
        // 
        this.txtDomain.Location = new Point(120, 50);
        this.txtDomain.Name = "txtDomain";
        this.txtDomain.Size = new Size(200, 23);
        this.txtDomain.TabIndex = 1;
        // 
        // cmbAction
        // 
        this.cmbAction.DropDownStyle = ComboBoxStyle.DropDownList;
        this.cmbAction.Items.AddRange(new object[] { "Allow", "Block", "Rewrite" });
        this.cmbAction.Location = new Point(120, 85);
        this.cmbAction.Name = "cmbAction";
        this.cmbAction.Size = new Size(200, 23);
        this.cmbAction.TabIndex = 2;
        // 
        // txtRewriteIp
        // 
        this.txtRewriteIp.Location = new Point(120, 120);
        this.txtRewriteIp.Name = "txtRewriteIp";
        this.txtRewriteIp.Size = new Size(200, 23);
        this.txtRewriteIp.TabIndex = 3;
        // 
        // lblIp
        // 
        this.lblIp.AutoSize = true;
        this.lblIp.Location = new Point(20, 18);
        this.lblIp.Name = "lblIp";
        this.lblIp.Size = new Size(72, 15);
        this.lblIp.Text = "IP клиента:";
        // 
        // lblDomain
        // 
        this.lblDomain.AutoSize = true;
        this.lblDomain.Location = new Point(20, 53);
        this.lblDomain.Name = "lblDomain";
        this.lblDomain.Size = new Size(50, 15);
        this.lblDomain.Text = "Домен:";
        // 
        // lblAction
        // 
        this.lblAction.AutoSize = true;
        this.lblAction.Location = new Point(20, 88);
        this.lblAction.Name = "lblAction";
        this.lblAction.Size = new Size(65, 15);
        this.lblAction.Text = "Действие:";
        // 
        // lblRewrite
        // 
        this.lblRewrite.AutoSize = true;
        this.lblRewrite.Location = new Point(20, 123);
        this.lblRewrite.Name = "lblRewrite";
        this.lblRewrite.Size = new Size(93, 15);
        this.lblRewrite.Text = "Подменный IP:";
        // 
        // btnSave
        // 
        this.btnSave.Location = new Point(120, 160);
        this.btnSave.Name = "btnSave";
        this.btnSave.Size = new Size(95, 27);
        this.btnSave.TabIndex = 4;
        this.btnSave.Text = "Сохранить";
        this.btnSave.Click += new EventHandler(this.btnSave_Click);
        // 
        // btnCancel
        // 
        this.btnCancel.Location = new Point(225, 160);
        this.btnCancel.Name = "btnCancel";
        this.btnCancel.Size = new Size(95, 27);
        this.btnCancel.TabIndex = 5;
        this.btnCancel.Text = "Отмена";
        this.btnCancel.DialogResult = DialogResult.Cancel;
        // 
        // EditRuleForm
        // 
        this.ClientSize = new Size(350, 210);
        this.Controls.Add(this.txtIp);
        this.Controls.Add(this.txtDomain);
        this.Controls.Add(this.cmbAction);
        this.Controls.Add(this.txtRewriteIp);
        this.Controls.Add(this.lblIp);
        this.Controls.Add(this.lblDomain);
        this.Controls.Add(this.lblAction);
        this.Controls.Add(this.lblRewrite);
        this.Controls.Add(this.btnSave);
        this.Controls.Add(this.btnCancel);
        this.Name = "EditRuleForm";
        this.Text = "Редактирование правила";
        this.ResumeLayout(false);
        this.PerformLayout();
    }

    #endregion

    private IpAddressControl txtIp;
    private TextBox txtDomain;
    private ComboBox cmbAction;
    private TextBox txtRewriteIp;
    private Label lblIp;
    private Label lblDomain;
    private Label lblAction;
    private Label lblRewrite;
    private Button btnSave;
    private Button btnCancel;
}
