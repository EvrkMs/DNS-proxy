using DNS_proxy.UI.Components;

namespace DNS_proxy.UI.UtilsForm
{
    partial class AddRuleForm
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
            Text = "Добавление правила";
            Size = new Size(400, 250);

            Label lbl1 = new() { Text = "IP источника:", Location = new(10, 10) };
            Label lbl2 = new() { Text = "Домен:", Location = new(10, 40) };
            Label lbl3 = new() { Text = "Действие:", Location = new(10, 70) };
            Label lbl4 = new() { Text = "Подменный IP:", Location = new(10, 100) };

            txtIp = new IpAddressControl() { Location = new(120, 10), Width = 200 };
            txtPattern = new() { Location = new(120, 40), Width = 200 };
            cmbAction = new() { Location = new(120, 70), Width = 200 };
            txtRewriteIp = new() { Location = new(120, 100), Width = 200 };
            btnAdd = new() { Text = "Добавить", Location = new(120, 140) };

            cmbAction.Items.AddRange(["Block", "Allow", "Rewrite"]);
            cmbAction.SelectedIndex = 0;

            btnAdd.Click += BtnAdd_Click;

            Controls.AddRange([lbl1, lbl2, lbl3, lbl4, txtIp, txtPattern, cmbAction, txtRewriteIp, btnAdd]);

            this.components = new System.ComponentModel.Container();
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(800, 450);
            this.Text = "AddRuleForm";
        }

        #endregion

        private TextBox txtPattern, txtRewriteIp;
        private IpAddressControl txtIp;
        private ComboBox cmbAction;
        private Button btnAdd;
    }
}