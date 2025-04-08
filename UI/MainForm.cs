using System.ComponentModel;
using DNS_proxy.Core.Models;
using DNS_proxy.Data;
using DNS_proxy.Service;
using DNS_proxy.UI.UtilsForm;
using DNS_proxy.Utils;

namespace DNS_proxy.UI
{
    public partial class MainForm : Form
    {
        private readonly CustomDnsServer _server;

        public MainForm(CustomDnsServer server)
        {
            InitializeComponent();
            InitDataGrid();
            _server = server;
            rulesGrid.CellContentClick += RulesGrid_CellClick;
        }


        public void AppendLog(string msg)
        {
            if (richTextBoxLogs.InvokeRequired)
            {
                richTextBoxLogs.Invoke(new Action<string>(AppendLog), msg);
                return;
            }

            richTextBoxLogs.AppendText(msg + "\n");
        }
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true; // Отменяем закрытие
                this.Hide();     // Просто скрываем окно
            }
            else
            {
                base.OnFormClosing(e);
            }
        }
        private void MainForm_Load(object sender, EventArgs e)
        {
            LoadRules();
            Logger.OnLog = AppendLog;
        }
        private void LoadRules()
        {
            using var db = new DnsRulesContext();
            var list = db.DnsRules.ToList();
            rulesGrid.DataSource = new BindingList<DnsRule>(list);

            // Добавляем Unicode иконки вручную в каждую строку
            foreach (DataGridViewRow row in rulesGrid.Rows)
            {
                if (!row.IsNewRow)
                {
                    row.Cells["EditColumn"].Value = "✏️";
                    row.Cells["DeleteColumn"].Value = "🗑️";
                }
            }
        }


        private void BtnSaveRules_Click(object sender, EventArgs e)
        {
            using var db = new DnsRulesContext();

            db.DnsRules.RemoveRange(db.DnsRules); // Очистим всё
            if (rulesGrid.DataSource is BindingList<DnsRule> list)
            {
                db.DnsRules.AddRange(list);
            }

            db.SaveChanges();
            _server.ReloadRulesPublic();
            MessageBox.Show("Правила сохранены!");
        }
        private void BtnAddRule_Click(object sender, EventArgs e)
        {
            var addForm = new AddRuleForm();
            if (addForm.ShowDialog() == DialogResult.OK)
            {
                LoadRules();
                AppendLog("Добавлено новое правило.");
            }
        }
        private void RulesGrid_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0 || e.ColumnIndex < 0)
                return;

            var row = rulesGrid.Rows[e.RowIndex];
            var rule = row.DataBoundItem as DnsRule;

            if (rulesGrid.Columns[e.ColumnIndex].Name == "EditColumn")
            {
                // ✏️ Открываем форму редактирования
                var form = new EditRuleForm(rule);
                if (form.ShowDialog() == DialogResult.OK)
                {
                    using var db = new DnsRulesContext();
                    db.DnsRules.Update(rule);
                    db.SaveChanges();
                    LoadRules(); // обновляем грид
                }
            }
            else if (rulesGrid.Columns[e.ColumnIndex].Name == "DeleteColumn")
            {
                // 🗑️ Удаление с подтверждением
                var confirm = MessageBox.Show("Удалить правило?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                if (confirm == DialogResult.Yes)
                {
                    using var db = new DnsRulesContext();
                    db.DnsRules.Remove(rule);
                    db.SaveChanges();
                    LoadRules(); // обновляем грид
                }
            }
        }
    }
}
