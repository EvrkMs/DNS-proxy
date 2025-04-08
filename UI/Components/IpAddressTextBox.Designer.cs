namespace DNS_proxy.UI.Components;

partial class IpAddressControl
{
    private TextBox txt1;
    private TextBox txt2;
    private TextBox txt3;
    private TextBox txt4;
    private Label slashLabel;
    private TextBox txtCidr;

    private void InitializeComponent()
    {
        txt1 = new TextBox();
        txt2 = new TextBox();
        txt3 = new TextBox();
        txt4 = new TextBox();
        slashLabel = new Label();
        txtCidr = new TextBox();

        txt1.Location = new(0, 0);
        txt1.Size = new(30, 23);

        txt2.Location = new(35, 0);
        txt2.Size = new(30, 23);

        txt3.Location = new(70, 0);
        txt3.Size = new(30, 23);

        txt4.Location = new(105, 0);
        txt4.Size = new(30, 23);

        slashLabel.Text = "/";
        slashLabel.Location = new(140, 3);
        slashLabel.AutoSize = true;

        txtCidr.Location = new(155, 0);
        txtCidr.Size = new(30, 23);

        this.Controls.AddRange([txt1, txt2, txt3, txt4, slashLabel, txtCidr]);
        this.Size = new Size(200, 25);
    }
}
