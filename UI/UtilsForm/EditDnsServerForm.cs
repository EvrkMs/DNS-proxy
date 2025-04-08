using System.Net;
using System.Text.RegularExpressions;
using DNS_proxy.Core.Models;
using DNS_proxy.Data;

namespace DNS_proxy.UI.UtilsForm;

public partial class EditDnsServerForm : Form
{
    private readonly DnsServerEntry? _server;

    public EditDnsServerForm(DnsServerEntry? server = null)
    {
        _server = server;
        InitializeComponent();

        if (_server != null)
        {
            txtAddress.Address = _server.Address;
            chkIsDoh.Checked = _server.IsDoh;
            chkWire.Checked = _server.UseWireFormat;
            numPriority.Value = _server.Priority;
            Text = "Редактирование сервера";
        }
    }

    private void BtnSave_Click(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(txtAddress.Text))
        {
            MessageBox.Show("Адрес не может быть пустым.");
            return;
        }

        // Проверим на IP или CIDR
        string addr = txtAddress.Text;
        bool isValid = IPAddress.TryParse(addr, out _) || Regex.IsMatch(addr, @"^(\d{1,3}\.){3}\d{1,3}/\d{1,2}$");

        if (!isValid)
        {
            MessageBox.Show("Введите корректный IP-адрес или CIDR (например, 192.168.0.1 или 192.168.0.1/24)");
            return;
        }

        using var db = new DnsRulesContext();
        if (_server != null)
        {
            var existing = db.DnsServers.Find(_server.Id);
            if (existing != null)
            {
                existing.Address = txtAddress.Text;
                existing.IsDoh = chkIsDoh.Checked;
                existing.UseWireFormat = chkWire.Checked;
                existing.Priority = (int)numPriority.Value;
            }
        }
        else
        {
            db.DnsServers.Add(new DnsServerEntry
            {
                Address = txtAddress.Text,
                IsDoh = chkIsDoh.Checked,
                UseWireFormat = chkWire.Checked,
                Priority = (int)numPriority.Value
            });
        }

        db.SaveChanges();
        DialogResult = DialogResult.OK;
        Close();
    }
}
