using System.Net;
using DNS_proxy.Core.Models;
using DNS_proxy.Data;

namespace DNS_proxy.UI.UtilsForm;

public partial class AddRuleForm : Form
{
    private readonly DnsRule? _editRule;

    public AddRuleForm(DnsRule? rule = null)
    {
        _editRule = rule;
        InitializeComponent();

        if (_editRule != null)
        {
            Text = "Редактирование правила";
            txtIp.Address = _editRule.SourceIp;
            txtPattern.Text = _editRule.DomainPattern;
            cmbAction.SelectedItem = _editRule.Action;
            txtRewriteIp.Text = _editRule.RewriteIp;
        }
    }

    private void BtnAdd_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtPattern.Text))
        {
            MessageBox.Show("Укажи домен.");
            return;
        }

        if (cmbAction.SelectedItem?.ToString() == "Rewrite"
            && !IPAddress.TryParse(txtRewriteIp.Text, out _))
        {
            MessageBox.Show("IP подмены некорректен.");
            return;
        }

        using var db = new DnsRulesContext();

        if (_editRule != null)
        {
            var existing = db.DnsRules.Find(_editRule.Id);
            if (existing != null)
            {
                existing.SourceIp = txtIp.Address;
                existing.DomainPattern = txtPattern.Text;
                existing.Action = cmbAction.SelectedItem.ToString();
                existing.RewriteIp = txtRewriteIp.Text;
            }
        }
        else
        {
            db.DnsRules.Add(new DnsRule
            {
                SourceIp = txtIp.Address,
                DomainPattern = txtPattern.Text,
                Action = cmbAction.SelectedItem.ToString(),
                RewriteIp = txtRewriteIp.Text
            });
        }

        db.SaveChanges();
        DialogResult = DialogResult.OK;
        Close();

    }
}
