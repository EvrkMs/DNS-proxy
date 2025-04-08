using System.Text.RegularExpressions;

namespace DNS_proxy.UI.Components
{
    public partial class IpAddressControl : UserControl
    {
        public IpAddressControl()
        {
            InitializeComponent();
            AttachEvents();
        }

        public string Address
        {
            get
            {
                string[] parts = { txt1.Text, txt2.Text, txt3.Text, txt4.Text };

                if (parts.Any(p => !Regex.IsMatch(p, @"^\d{1,3}$")) || parts.Any(p => int.Parse(p) > 255))
                    return string.Empty;

                string ip = string.Join(".", parts);

                if (!string.IsNullOrWhiteSpace(txtCidr.Text))
                {
                    if (!Regex.IsMatch(txtCidr.Text, @"^\d{1,2}$") || int.Parse(txtCidr.Text) > 32)
                        return string.Empty;

                    return $"{ip}/{txtCidr.Text}";
                }

                return ip;
            }
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    return;

                var match = Regex.Match(value, @"^(?<ip>(\d{1,3}\.){3}\d{1,3})(/(?<cidr>\d{1,2}))?$");
                if (!match.Success) return;

                var octets = match.Groups["ip"].Value.Split('.');
                txt1.Text = octets[0];
                txt2.Text = octets[1];
                txt3.Text = octets[2];
                txt4.Text = octets[3];

                if (match.Groups["cidr"].Success)
                    txtCidr.Text = match.Groups["cidr"].Value;
            }
        }

        private void AttachEvents()
        {
            var tooltip = new ToolTip();
            foreach (var (box, label) in new[] {
        (txt1, "Первый октет (0-255)"),
        (txt2, "Второй октет (0-255)"),
        (txt3, "Третий октет (0-255)"),
        (txt4, "Четвёртый октет (0-255)"),
        (txtCidr, "CIDR (0-32), необязательно")
    })
            {
                tooltip.SetToolTip(box, label);
            }

            foreach (var box in new[] { txt1, txt2, txt3, txt4 })
            {
                box.MaxLength = 3;
                box.KeyPress += TxtBox_KeyPress;
                box.TextChanged += TxtBox_TextChanged;
            }

            txtCidr.MaxLength = 2;
            txtCidr.KeyPress += TxtCidr_KeyPress;
        }

        private void TxtBox_KeyPress(object? sender, KeyPressEventArgs e)
        {
            var box = sender as TextBox;

            // Разрешаем только цифры и точку
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar) && e.KeyChar != '.')
            {
                e.Handled = true;
                return;
            }

            if (e.KeyChar == '.')
            {
                // Проброс фокуса на следующий TextBox
                MoveToNext(box);
                e.Handled = true;
            }
        }

        private void TxtBox_TextChanged(object? sender, EventArgs e)
        {
            var box = sender as TextBox;

            if (box.Text.Length == 3 && int.TryParse(box.Text, out int val) && val <= 255)
            {
                MoveToNext(box);
            }
        }

        private void MoveToNext(TextBox current)
        {
            if (current == txt1) txt2.Focus();
            else if (current == txt2) txt3.Focus();
            else if (current == txt3) txt4.Focus();
            else if (current == txt4) txtCidr.Focus();
        }

        private void TxtCidr_KeyPress(object? sender, KeyPressEventArgs e)
        {
            // Только цифры
            if (!char.IsControl(e.KeyChar) && !char.IsDigit(e.KeyChar))
                e.Handled = true;
        }
    }
}
