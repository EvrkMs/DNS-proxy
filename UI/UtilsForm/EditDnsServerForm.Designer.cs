using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using DNS_proxy.UI.Components;

namespace DNS_proxy.UI.UtilsForm;

public partial class EditDnsServerForm
{
    // Designer.cs
    private IpAddressControl txtAddress;
    private CheckBox chkIsDoh;
    private CheckBox chkWire;
    private NumericUpDown numPriority;
    private Button btnSave;

    private void InitializeComponent()
    {
        this.Text = "Добавление DNS-сервера";
        this.Size = new Size(400, 250);

        Label lbl1 = new() { Text = "Адрес:", Location = new(10, 10) };
        Label lbl2 = new() { Text = "DoH:", Location = new(10, 40) };
        Label lbl3 = new() { Text = "WireFormat:", Location = new(10, 70) };
        Label lbl4 = new() { Text = "Приоритет:", Location = new(10, 100) };

        txtAddress = new IpAddressControl() { Location = new(120, 10), Width = 200 };
        chkIsDoh = new() { Location = new(120, 40) };
        chkWire = new() { Location = new(120, 70) };
        numPriority = new() { Location = new(120, 100), Width = 60, Minimum = 0, Maximum = 100 };
        btnSave = new() { Text = "Сохранить", Location = new(120, 140) };

        btnSave.Click += BtnSave_Click;

        Controls.AddRange([lbl1, lbl2, lbl3, lbl4, txtAddress, chkIsDoh, chkWire, numPriority, btnSave]);
    }
}
