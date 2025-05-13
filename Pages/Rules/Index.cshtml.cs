using DnsProxy.Data;
using DnsProxy.Models;
using DnsProxy.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;

namespace DnsProxy.Pages.Rules
{
    public class IndexModel(AppDbContext db, IDnsConfigService cfg) : PageModel
    {
        public List<DnsRule> Items { get; private set; } = [];

        /* -------- modal support -------- */
        public EditViewModel? ModalItem { get; private set; }
        public List<SelectListItem> ServerSelect { get; private set; } = [];

        public async Task OnGetAsync(int? id)
        {
            Items = await db.Rules.ToListAsync();

            if (id is not null)             // запрос «/Rules?id=5» → показать модалку
            {
                var rule = Items.FirstOrDefault(r => r.Id == id) ?? new DnsRule();
                ModalItem = new EditViewModel(rule);
                await FillServerSelectAsync();
            }
        }

        /* GET partial for JS-load ( /Rules?handler=Partial&id=... ) */
        public async Task<IActionResult> OnGetPartialAsync(int? id)
        {
            var rule = await db.Rules.FindAsync(id) ?? new DnsRule();
            var vm = new EditViewModel(rule);
            await FillServerSelectAsync(vm);

            return Partial("_RuleModal", vm);   // <-- отдаём только partial
        }

        /* POST save */
        public async Task<IActionResult> OnPostSaveAsync(EditViewModel vm)
        {
            if (!ModelState.IsValid)
                return Partial("_RuleModal", vm);

            DnsRule ent = vm.ToEntity();

            if (ent.Id == 0) db.Rules.Add(ent);
            else db.Rules.Update(ent);

            await db.SaveChangesAsync();
            return new JsonResult(new { ok = true });
        }

        /* POST delete */
        public async Task<IActionResult> OnPostDeleteAsync(int id)
        {
            var r = await db.Rules.FindAsync(id);
            if (r is null) return NotFound();
            db.Rules.Remove(r);
            await db.SaveChangesAsync();
            return RedirectToPage();
        }

        /* helpers */
        private async Task FillServerSelectAsync(EditViewModel? vm = null)
        {
            var servers = await cfg.GetAllAsync();

            ServerSelect = servers
                .Select(s => new SelectListItem(
                    $"{s.Address} [{s.Protocol}]",
                    s.Id.ToString()))
                .ToList();

            if (vm != null) vm.ServerSelect = ServerSelect;
        }
    }

    /* маленькая VM для модалки */
    public class EditViewModel
    {
        public int Id { get; set; }
        public string SourceIp { get; set; } = "*";
        public string DomainPattern { get; set; } = string.Empty;
        public RuleAction Action { get; set; } = RuleAction.Allow;
        public string RewriteIp { get; set; } = string.Empty;

        public int? ForceServerId { get; set; }

        public List<SelectListItem> ServerSelect { get; set; } = [];

        public List<int> IncludeServerIds { get; set; } = [];
        public List<int> ExcludeServerIds { get; set; } = [];

        public EditViewModel() { }

        public EditViewModel(DnsRule r)
        {
            Id = r.Id;
            SourceIp = r.SourceIp;
            DomainPattern = r.DomainPattern;
            Action = r.Action;
            RewriteIp = r.RewriteIp;
            ForceServerId = r.ForceServerId;

            // Преобразуем строку вида "1,3" → List<int>
            IncludeServerIds = r.IncludeServers?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(int.Parse).ToList() ?? [];
            ExcludeServerIds = r.ExcludeServers?.Split(',', StringSplitOptions.RemoveEmptyEntries)
                                 .Select(int.Parse).ToList() ?? [];
        }

        public DnsRule ToEntity() => new()
        {
            Id = Id,
            SourceIp = SourceIp,
            DomainPattern = DomainPattern,
            Action = Action,
            RewriteIp = RewriteIp,
            ForceServerId = ForceServerId,
            IncludeServers = string.Join(',', IncludeServerIds),
            ExcludeServers = string.Join(',', ExcludeServerIds),
        };
    }
}