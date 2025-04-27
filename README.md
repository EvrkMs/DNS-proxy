# DNS Proxy

**DNS Proxy** — лёгкий self‑hosted DNS‑прокси‑сервер (ASP.NET Core 8.0) с DoH/UDP‑резолвингом, web‑интерфейсом на Razor Pages и SQLite‑хранилищем статистики.

> Разрабатывался для домашнего роутера — блокировка/разрешение доменов, кеш, принудительные апстримы и удобный UI.

---

## Возможности

|  | Описание |
|---|---|
| **DoH + UDP резолвинг** | Wire (RFC 8484) и JSON (Cloudflare/Google) форматы, fallback, bootstrap IP. |
| **Гибкие правила** | Block / Allow / Rewrite, фильтр по IP клиента, wildcard‑домены, include/exclude серверов, принудительный сервер. |
| **Кеш** | In‑memory, TTL из ответа. |
| **Статистика** | Домен, клиент, апстрим, RCODE, действие. |
| **Web UI** | Bootstrap 5 (Darkly), modal‑формы, фильтр/сортировка. |
| **Docker & Windows‑service** | Готовый `Dockerfile`; скрипт `setup-service.ps1` устанавливает службу *DnsProxy* в Windows. |
| **Логирование ошибок** | Serilog → `logs/errors-*.txt` (rolling‑file + console). |

---

## Web интерфейс

| Раздел | URL | Описание |
|---|---|---|
| **Серверы** | `/Servers` | Управление апстримами (DoH/UDP, приоритет, статичный IP). |
| **Правила** | `/Rules` | Block / Allow / Rewrite, include/exclude, force‑server. |
| **Запросы** | `/` | Последние 200 резолвов, поиск + сортировка. |
| **Health** | `/Health` | Пинг‑чек каждого апстрима (`chatgpt.com`). |
| **Логи** | `/Logs` | Просмотр `errors-*.txt`, копирование, очистка. |

---

## Внутри проекта

```
┌ Program.cs (DI) ┐            background
│ Razor Pages UI │◄────────────DnsProxyServer (порт 53)
└─────────────────┘             │  ↳ ResolverService (DoH/UDP)
                                │  ↳ Cache / Rules / Statistics
```

* **Models** — `DnsRule`, `DnsServerEntry`, `VisitStatistic` (EF Core).
* **Services** — `ResolverService`, `CacheService`, `RuleService`, `DnsConfigService`.
* **Utils** — `RuleHelper`, `IpMatchHelper`.
* **Pages/** — Razor UI на Bootstrap 5 + vanilla JS.

### Конвейер `ExecuteAsync`

1. Правила (`RuleHelper.Apply`)
2. In‑memory кеш
3. Fallback‑перебор upstream‑пула
4. Статистика + логирование

### ResolverService (коротко)

* Перебор апстримов по `Priority`.
* DoH‑Wire → `POST`, при 400 — `GET ?dns=`.
* `StaticAddress` минует bootstrap.
* 10 повторных попыток при `forceServer`.

---

## TODO

* Prometheus `/metrics` + Grafana дашборд.
* Live‑chart статистики в UI.
* DNS‑over‑TLS support.
* ACL + аутентификация.

---

© 2024 DNS Proxy • MIT