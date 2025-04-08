using DNS_proxy.Core.Models;

namespace DNS_proxy.UI.UtilsForm;

public partial class EditRuleForm : Form
{
    private DnsRule _rule;

    public EditRuleForm(DnsRule rule)
    {
        InitializeComponent();
        _rule = rule;
        txtIp.Text = rule.SourceIp;
        txtDomain.Text = rule.DomainPattern;
        cmbAction.Text = rule.Action;
        txtRewriteIp.Text = rule.RewriteIp;
    }

    private void btnSave_Click(object sender, EventArgs e)
    {
        _rule.SourceIp = txtIp.Text;
        _rule.DomainPattern = txtDomain.Text;
        _rule.Action = cmbAction.Text;
        _rule.RewriteIp = txtRewriteIp.Text;

        DialogResult = DialogResult.OK;
        Close();
    }
}
