# iPhone 18 Pro News／WebMCP Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在 ASP.NET Core Razor Pages 專案新增 `/News` 單頁，使用集中固定的 Apple 官方資料呈現 iPhone 18 Pro 系列，並註冊兩個安全、唯讀的 WebMCP tools。

**Architecture:** `News.cshtml.cs` 是固定新聞資料與 JSON 序列化的單一來源；`News.cshtml` 以同一個模型產生語意化 HTML，並輸出 `application/json` bootstrap 資料。頁面專用 `news-webmcp.js` 只在 `document.modelContext` 可用時註冊 tools，所有 tool 回傳都從 bootstrap 資料讀取，不建立 API、不抓取外部網站。

**Tech Stack:** ASP.NET Core Razor Pages、.NET 9、C# nullable reference types、原生 JavaScript、JSON Schema、Bootstrap 現有資產與原生 CSS。

**Spec:** `spec/spec1.md`

## Global Constraints

- 目標專案維持 `WebMcpTest`、`net9.0`；不新增資料庫、後端新聞 API、CMS、排程或 WebMCP npm 套件。
- 新增路由必須是 `GET /News`；既有 `/` 與 `/Privacy` 行為維持不變。
- 所有新聞資料固定且集中於 `WebMcpTest/Pages/News.cshtml.cs`；頁面與 WebMCP tools 不得各自複製規格數值。
- WebMCP 必須先偵測 `document.modelContext`，使用 `execute(inputObject, options)` 與 `options.signal`；註冊失敗只能記錄可診斷訊息，不得阻塞頁面。
- tools 只讀取固定頁面資料，不發出 Apple 外部請求、不修改資料庫、不導覽、不購買、不登入、不寫入頁面應用狀態。
- tool 名稱必須唯一；兩個 tool 的 `readOnlyHint` 為 `true`、`untrustedContentHint` 為 `false`，並以 `consequentialHint: false` 明確表示沒有副作用。
- 頁面語言為 `zh-Hant-TW`；既有英文頁面透過 layout fallback 維持 `en`。
- 產品程式變更限制為規格列出的五個檔案：`News.cshtml`、`News.cshtml.cs`、`news-webmcp.js`、`_Layout.cshtml`、`site.css`。測試用暫存檔只放在 `.tmp`，驗證後移除，不納入交付。
- 不修改 `spec/spec1.md`、`WebMcpTest.csproj`、`Program.cs`、資料庫設定或既有 Privacy 程式。
- 不加入 Apple 圖片、影片、外部媒體資產、價格、台灣售價、電信方案、庫存或購買流程。

---

### Task 1: 建立 WebMCP 契約測試與紅燈基線

**Files:**
- Create temporarily: `.tmp/news-webmcp.test.mjs`
- Create temporarily: `.tmp/news-page-smoke.ps1`

**Interfaces:**
- Consumes: future `WebMcpTest/wwwroot/js/news-webmcp.js` IIFE and the JSON bootstrap shape emitted by `News.cshtml`.
- Produces: a Node.js contract test that captures the two registered tools and exercises their public behavior; a PowerShell HTTP smoke script for the later HTTPS integration check.

- [ ] **Step 1: Create the temporary Node test harness before production JavaScript**

~~~powershell
New-Item -ItemType Directory -Force -Path '.tmp' | Out-Null
~~~

Create `.tmp/news-webmcp.test.mjs` with this executable test:

~~~javascript
import assert from "node:assert/strict";
import { readFile } from "node:fs/promises";
import { test } from "node:test";
import vm from "node:vm";

const scriptPath = new URL("../WebMcpTest/wwwroot/js/news-webmcp.js", import.meta.url);
const fixture = {
  announcementDate: "2026-09-09",
  lastVerifiedDate: "2026-09-10",
  summary: "Apple 發表 iPhone 18 Pro 與 iPhone 18 Pro Max。",
  highlights: [{ key: "camera", title: "相機", description: "48MP Fusion Main 主相機與可變光圈。" }],
  release: {
    preorderStart: "2026-09-12T05:00:00-07:00",
    availabilityStart: "2026-09-18",
    timezoneNote: "時程以 Apple 美國官方資料為準，地區供貨可能不同。"
  },
  officialLinks: [
    { key: "newsroom", label: "Apple 新聞稿", url: "https://www.apple.com/newsroom/2026/09/apple-debuts-iphone-18-pro-and-iphone-18-pro-max/" },
    { key: "product", label: "Apple iPhone 18 Pro 產品頁", url: "https://www.apple.com/iphone-18-pro/" },
    { key: "specs", label: "Apple 技術規格", url: "https://www.apple.com/iphone-18-pro/specs/" }
  ],
  models: [
    { id: "iphone-18-pro", name: "iPhone 18 Pro", displaySizeInches: 6.3, displayResolution: "2622 x 1206，460 ppi", weightGrams: 211, typicalUseHours: 24, videoPlaybackHours: 36, streamedVideoPlaybackHours: 33, storageOptionsGb: [256, 512, 1024, 2048], cameraSummary: "48MP Pro Fusion，主相機可變光圈", chip: "A20 Pro", finishes: ["Black", "Silver", "Glacier", "Burgundy"] },
    { id: "iphone-18-pro-max", name: "iPhone 18 Pro Max", displaySizeInches: 6.9, displayResolution: "2868 x 1320，460 ppi", weightGrams: 249, typicalUseHours: 30, videoPlaybackHours: 45, streamedVideoPlaybackHours: 40, storageOptionsGb: [256, 512, 1024, 2048], cameraSummary: "48MP Pro Fusion，主相機可變光圈", chip: "A20 Pro", finishes: ["Black", "Silver", "Glacier", "Burgundy"] }
  ]
};

async function loadTools() {
  const registrations = [];
  const warnings = [];
  const modelContext = {
    registerTool(tool) {
      registrations.push(tool);
      return Promise.resolve();
    }
  };
  const document = {
    modelContext,
    getElementById(id) {
      return id === "news-data" ? { textContent: JSON.stringify(fixture) } : null;
    }
  };
  const source = await readFile(scriptPath, "utf8");
  vm.runInNewContext(source, {
    AbortController,
    Promise,
    JSON,
    console: { warn: (...args) => warnings.push(args.join(" ")) },
    document
  });
  await new Promise((resolve) => setImmediate(resolve));
  return { registrations, warnings };
}

test("registers exactly two read-only tools with closed schemas", async () => {
  const { registrations, warnings } = await loadTools();
  assert.deepEqual(registrations.map((tool) => tool.name), [
    "get-iphone18-pro-overview",
    "compare-iphone18-pro-models"
  ]);
  assert.equal(registrations[0].annotations.readOnlyHint, true);
  assert.equal(registrations[0].annotations.untrustedContentHint, false);
  assert.equal(registrations[1].annotations.readOnlyHint, true);
  assert.equal(registrations[1].annotations.untrustedContentHint, false);
  assert.equal(registrations[0].inputSchema.additionalProperties, false);
  assert.equal(registrations[1].inputSchema.additionalProperties, false);
  assert.deepEqual(warnings, []);
});

test("overview supports omitted, all, and each model filter", async () => {
  const { registrations } = await loadTools();
  const overview = registrations[0];
  const signal = new AbortController().signal;
  const all = await overview.execute({}, { signal });
  const explicitAll = await overview.execute({ model: "all" }, { signal });
  const pro = await overview.execute({ model: "iphone-18-pro" }, { signal });
  const proMax = await overview.execute({ model: "iphone-18-pro-max" }, { signal });

  assert.equal(all.type, "iphone18-pro-overview");
  assert.equal(all.model, "all");
  assert.equal(all.models.length, 2);
  assert.deepEqual(explicitAll.models, all.models);
  assert.deepEqual(pro.models.map((model) => model.id), ["iphone-18-pro"]);
  assert.deepEqual(proMax.models.map((model) => model.id), ["iphone-18-pro-max"]);
});

test("comparison returns both models and exact criteria", async () => {
  const { registrations } = await loadTools();
  const result = await registrations[1].execute({}, { signal: new AbortController().signal });
  assert.equal(result.type, "iphone18-pro-comparison");
  assert.deepEqual(result.criteria, [
    "displaySizeInches", "displayResolution", "weightGrams", "storageOptionsGb",
    "typicalUseHours", "videoPlaybackHours", "streamedVideoPlaybackHours",
    "cameraSummary", "chip", "finishes"
  ]);
  assert.deepEqual(result.models.map((model) => model.id), ["iphone-18-pro", "iphone-18-pro-max"]);
});

test("rejects invalid model and extra fields", async () => {
  const { registrations } = await loadTools();
  const signal = new AbortController().signal;
  assert.deepEqual(await registrations[0].execute({ model: "iphone-19" }, { signal }), {
    error: { code: "invalid_model", message: "model 必須是 all、iphone-18-pro 或 iphone-18-pro-max。" }
  });
  assert.deepEqual(await registrations[0].execute({ model: "all", extra: true }, { signal }), {
    error: { code: "invalid_input", message: "輸入包含未定義欄位。" }
  });
  assert.deepEqual(await registrations[1].execute({ extra: true }, { signal }), {
    error: { code: "invalid_input", message: "比較工具不接受輸入欄位。" }
  });
});

test("returns the aborted contract before and during execution", async () => {
  const { registrations } = await loadTools();
  const controller = new AbortController();
  controller.abort();
  assert.deepEqual(await registrations[0].execute({}, { signal: controller.signal }), {
    error: { code: "aborted", message: "工具執行已取消。" }
  });

  const inFlight = new AbortController();
  const pending = registrations[0].execute({}, { signal: inFlight.signal });
  inFlight.abort();
  assert.deepEqual(await pending, {
    error: { code: "aborted", message: "工具執行已取消。" }
  });
});

test("does nothing when document.modelContext is unavailable", async () => {
  const source = await readFile(scriptPath, "utf8");
  const warnings = [];
  vm.runInNewContext(source, {
    JSON,
    console: { warn: (...args) => warnings.push(args.join(" ")) },
    document: { getElementById: () => ({ textContent: JSON.stringify(fixture) }) }
  });
  assert.deepEqual(warnings, []);
});
~~~

- [ ] **Step 2: Run the contract test and verify the expected red failure**

Run:

~~~powershell
node --test .tmp/news-webmcp.test.mjs
~~~

Expected: the runner reports a missing-feature failure because `WebMcpTest/wwwroot/js/news-webmcp.js` does not yet exist. The test itself must parse and execute; do not accept a syntax error in the harness as the red state.

- [ ] **Step 3: Create the temporary HTTPS smoke script**

Create `.tmp/news-page-smoke.ps1`:

~~~powershell
$baseUrl = 'https://localhost:7020'
foreach ($route in @('/', '/Privacy', '/News')) {
    $response = Invoke-WebRequest -Uri ($baseUrl + $route) -SkipCertificateCheck -UseBasicParsing
    if ($response.StatusCode -ne 200) { throw "Expected HTTP 200 for $route, got $($response.StatusCode)." }
}
$news = (Invoke-WebRequest -Uri ($baseUrl + '/News') -SkipCertificateCheck -UseBasicParsing).Content
foreach ($fragment in @(
    'Apple 發表 iPhone 18 Pro 系列',
    'iPhone 18 Pro',
    'iPhone 18 Pro Max',
    '<table',
    '2026/09/12 05:00 PT',
    'https://www.apple.com/newsroom/2026/09/apple-debuts-iphone-18-pro-and-iphone-18-pro-max/',
    'https://www.apple.com/iphone-18-pro/',
    'https://www.apple.com/iphone-18-pro/specs/'
)) {
    if (-not $news.Contains($fragment)) { throw "News page is missing required fragment: $fragment" }
}
if (-not $news.Contains('<html lang="zh-Hant-TW">')) { throw 'News page must use lang="zh-Hant-TW".' }
~~~

### Task 2: Add the centralized fixed news data contract

**Files:**
- Create: `WebMcpTest/Pages/News.cshtml.cs`

**Interfaces:**
- Consumes: no services, database, network, request parameters, cookies, or external API.
- Produces: `NewsPageModel.Article`, `NewsPageModel.SerializedArticle`, and the serializable data types consumed by both Razor and JavaScript.

- [ ] **Step 1: Define the PageModel and serializable types**

Create `NewsPageModel : PageModel`, `NewsContent`, `NewsArticle`, `NewsHighlight`, `ReleaseSchedule`, `ProductModel`, `NewsSource`, and `OfficialLink`. The PageModel must expose:

~~~csharp
public NewsArticle Article { get; } = NewsContent.Article;
public string SerializedArticle => NewsContent.Serialize(Article);

public void OnGet()
{
}
~~~

Use init-only properties with non-null defaults. `NewsContent.Article` must contain:

~~~csharp
AnnouncementDate = "2026-09-09",
LastVerifiedDate = "2026-09-10",
Title = "Apple 發表 iPhone 18 Pro 系列",
Summary = "Apple 於 2026 年 9 月 9 日發表 iPhone 18 Pro 與 iPhone 18 Pro Max。",
ScopeNote = "本頁聚焦 Pro 系列，不延伸描述其他 iPhone 型號。",
Release = new ReleaseSchedule
{
    PreorderStart = "2026-09-12T05:00:00-07:00",
    PreorderDisplay = "2026/09/12 05:00 PT",
    AvailabilityStart = "2026-09-18",
    TimezoneNote = "實際供貨日期與服務內容可能依國家或地區不同；時程以 Apple 美國官方資料為準。"
}
~~~

Create both product entries with these exact values:

~~~csharp
new ProductModel
{
    Id = "iphone-18-pro",
    Name = "iPhone 18 Pro",
    Dimensions = "71.9 x 150.0 x 8.75 mm",
    DisplaySizeInches = 6.3,
    DisplayResolution = "2622 x 1206，460 ppi",
    WeightGrams = 211,
    TypicalUseHours = 24,
    VideoPlaybackHours = 36,
    StreamedVideoPlaybackHours = 33,
    StorageOptionsGb = new[] { 256, 512, 1024, 2048 },
    CameraSummary = "48MP Pro Fusion，主相機可變光圈",
    Chip = "A20 Pro",
    Finishes = new[] { "Black", "Silver", "Glacier", "Burgundy" }
}
~~~

Use the same shape for `iphone-18-pro-max`, changing only `Id`, `Name`, `Dimensions`, `DisplaySizeInches`, `DisplayResolution`, `WeightGrams`, `TypicalUseHours`, `VideoPlaybackHours`, and `StreamedVideoPlaybackHours` to `iphone-18-pro-max`, `iPhone 18 Pro Max`, `78.0 x 163.4 x 8.75 mm`, `6.9`, `2868 x 1320，460 ppi`, `249`, `30`, `45`, and `40`.

Include highlights for the 48MP variable-aperture camera, A20 Pro/vapor chamber/Dual 16-core Neural Engine, up-to-36/up-to-45-hour video playback, 6.3/6.9-inch Super Retina XDR with ProMotion up to 120Hz, four finishes, and Apple Intelligence/Siri AI with regional/language availability caveat. Include shared features containing Super Retina XDR, ProMotion up to 120Hz, IP68, 18MP Center Stage front camera, USB-C, MagSafe, and Apple Intelligence/Siri AI.

Set `OfficialLinks` to structured entries for the Apple newsroom, Apple product page, Apple technical specifications, and WebMCP specification. Set `Source` to structured entries for the three Apple URLs. Keep all URL values exactly as in `spec/spec1.md`.

- [ ] **Step 2: Serialize with camelCase and an HTML-safe encoder**

Use:

~~~csharp
private static readonly JsonSerializerOptions JsonOptions = new()
{
    Encoder = JavaScriptEncoder.Default,
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false
};

public static string Serialize(NewsArticle article) =>
    JsonSerializer.Serialize(article, JsonOptions);
~~~

Import `System.Text.Encodings.Web`, `System.Text.Json`, and `System.Text.Json.Serialization`. Keep `JavaScriptEncoder.Default` so `Html.Raw(Model.SerializedArticle)` is safe in the non-executable JSON script element.

- [ ] **Step 3: Build the PageModel before adding the view**

Run:

~~~powershell
dotnet build .\WebMcpTest\WebMcpTest.csproj --no-restore
~~~

Expected: exit code `0`, no compiler errors.

### Task 3: Add the semantic News page and navigation/language behavior

**Files:**
- Create: `WebMcpTest/Pages/News.cshtml`
- Modify: `WebMcpTest/Pages/Shared/_Layout.cshtml`

**Interfaces:**
- Consumes: `NewsPageModel.Article`, `NewsPageModel.SerializedArticle`, and the existing layout.
- Produces: `GET /News), a `zh-Hant-TW` document, semantic one-page content, and a News navigation link.

- [ ] **Step 1: Create the page shell and JSON bootstrap**

Start `News.cshtml` with:

~~~cshtml
@page
@model NewsPageModel
@{
    ViewData["Title"] = "Apple 發表 iPhone 18 Pro 系列";
    ViewData["PageLanguage"] = "zh-Hant-TW";
}
<article class="news-page">
    <script id="news-data" type="application/json">@Html.Raw(Model.SerializedArticle)</script>
    <header class="news-hero">
        <p class="news-eyebrow">Apple 官方新聞摘要</p>
        <h1>@Model.Article.Title</h1>
        <p class="news-summary">@Model.Article.Summary</p>
        <p class="news-scope">@Model.Article.ScopeNote</p>
        <dl class="news-meta">
            <div><dt>公告日期</dt><dd>@Model.Article.AnnouncementDate.Replace('-', '/')</dd></div>
            <div><dt>最後核對</dt><dd>@Model.Article.LastVerifiedDate.Replace('-', '/')</dd></div>
        </dl>
    </header>
    <section aria-labelledby="highlights-heading">
        <h2 id="highlights-heading">主要亮點</h2>
        <div class="news-highlight-grid">
            @foreach (var highlight in Model.Article.Highlights)
            {
                <article class="news-card">
                    <h3>@highlight.Title</h3>
                    <p>@highlight.Description</p>
                </article>
            }
        </div>
    </section>
~~~

Continue the same article; do not create another page or hard-code product values outside model-driven labels/loops.

- [ ] **Step 2: Render an accessible comparison table**

Use a `tabindex="0"` scroll wrapper and a real table with caption, `scope="col"`, and `scope="row"`. Include rows for dimensions, display, weight, storage, typical use, video playback, streamed playback, camera, chip, finishes, and shared features. The table cells must format units explicitly (`mm`, `吋`, `g`, `GB`, `小時`) and label battery numbers as `最高`, not guaranteed usage.

Use this structure for the header and data rows:

~~~cshtml
<div class="news-table-scroll" tabindex="0" aria-label="iPhone 18 Pro 系列比較表，可水平捲動">
    <table class="news-comparison-table">
        <caption>iPhone 18 Pro 與 iPhone 18 Pro Max 主要規格比較</caption>
        <thead>
            <tr>
                <th scope="col">規格</th>
                @foreach (var model in Model.Article.Models)
                {
                    <th scope="col">@model.Name</th>
                }
            </tr>
        </thead>
        <tbody>
            <tr>
                <th scope="row">尺寸</th>
                @foreach (var model in Model.Article.Models) { <td>@model.Dimensions</td> }
            </tr>
            <tr>
                <th scope="row">顯示器</th>
                @foreach (var model in Model.Article.Models) { <td>@model.DisplaySizeInches 吋 Super Retina XDR；@model.DisplayResolution；ProMotion 最高 120Hz</td> }
            </tr>
            <tr>
                <th scope="row">重量</th>
                @foreach (var model in Model.Article.Models) { <td>@model.WeightGrams g</td> }
            </tr>
            <tr>
                <th scope="row">容量</th>
                @foreach (var model in Model.Article.Models) { <td>@string.Join("、", model.StorageOptionsGb.Select(value => $"{value}GB"))</td> }
            </tr>
            <tr>
                <th scope="row">一般使用</th>
                @foreach (var model in Model.Article.Models) { <td>最高 @model.TypicalUseHours 小時</td> }
            </tr>
            <tr>
                <th scope="row">影片播放</th>
                @foreach (var model in Model.Article.Models) { <td>最高 @model.VideoPlaybackHours 小時</td> }
            </tr>
            <tr>
                <th scope="row">串流影片播放</th>
                @foreach (var model in Model.Article.Models) { <td>最高 @model.StreamedVideoPlaybackHours 小時</td> }
            </tr>
            <tr>
                <th scope="row">相機</th>
                @foreach (var model in Model.Article.Models) { <td>@model.CameraSummary</td> }
            </tr>
            <tr>
                <th scope="row">晶片</th>
                @foreach (var model in Model.Article.Models) { <td>@model.Chip</td> }
            </tr>
            <tr>
                <th scope="row">外觀</th>
                @foreach (var model in Model.Article.Models) { <td>@string.Join("、", model.Finishes)</td> }
            </tr>
            <tr>
                <th scope="row">共同特色</th>
                <td colspan="2"><ul>@foreach (var feature in Model.Article.SharedFeatures) { <li>@feature</li> }</ul></td>
            </tr>
        </tbody>
    </table>
</div>
~~~

- [ ] **Step 3: Render schedule, links, and the page script**

Use the model for dates and links:

~~~cshtml
<section aria-labelledby="schedule-heading">
    <h2 id="schedule-heading">重要時程</h2>
    <dl class="news-schedule">
        <div><dt>官方發表</dt><dd>@Model.Article.AnnouncementDate.Replace('-', '/')</dd></div>
        <div><dt>美國太平洋時間預購開始</dt><dd>@Model.Article.Release.PreorderDisplay</dd></div>
        <div><dt>官方標示開始供貨</dt><dd>@Model.Article.Release.AvailabilityStart.Replace('-', '/')</dd></div>
    </dl>
    <p class="news-note">@Model.Article.Release.TimezoneNote</p>
</section>
<section aria-labelledby="links-heading">
    <h2 id="links-heading">官方資料連結</h2>
    <ul class="news-link-list">
        @foreach (var link in Model.Article.OfficialLinks)
        {
            <li><a href="@link.Url" target="_blank" rel="noopener noreferrer">@link.Label</a></li>
        }
    </ul>
</section>
</article>
@section Scripts {
    <script src="~/js/news-webmcp.js" asp-append-version="true"></script>
}
~~~

- [ ] **Step 4: Add the navigation link and per-page language fallback**

At the top of `_Layout.cshtml`, before `<!DOCTYPE html>`, add:

~~~cshtml
@{
    var pageLanguage = ViewData["PageLanguage"] as string ?? "en";
}
~~~

Change the html element to `<html lang="@pageLanguage">`. Add this item beside the existing Home and Privacy links:

~~~cshtml
<li class="nav-item">
    <a class="nav-link text-dark" asp-area="" asp-page="/News">News</a>
</li>
~~~

Keep the current Home/Privacy links, footer, scripts, and layout structure.

- [ ] **Step 5: Compile the Razor route**

Run:

~~~powershell
dotnet build .\WebMcpTest\WebMcpTest.csproj --no-restore
~~~

Expected: exit code `0`, no compiler errors.

### Task 4: Implement the two WebMCP tools with defensive validation

**Files:**
- Create: `WebMcpTest/wwwroot/js/news-webmcp.js`

**Interfaces:**
- Consumes: `#news-data` JSON from `News.cshtml); `document.modelContext) when available.
- Produces: exactly the two tool names, schemas, annotations, results, errors, and cancellation behavior required by `spec/spec1.md`.

- [ ] **Step 1: Add capability detection and fixed-data parsing**

Begin with an IIFE. Return immediately when `document.modelContext` is absent. Read `#news-data), parse JSON inside try/catch, and log only a diagnostic warning on missing/invalid data. Do not call `fetch`, `XMLHttpRequest`, `window.open`, `location`, cookies, storage, or any Apple URL from this file.

- [ ] **Step 2: Add exact schemas, titles, and annotations**

Use:

~~~javascript
const overviewSchema = {
  type: "object",
  properties: {
    model: {
      type: "string",
      enum: ["all", "iphone-18-pro", "iphone-18-pro-max"],
      description: "要查詢的機型；省略或使用 all 時回傳兩款機型。"
    }
  },
  additionalProperties: false
};

const comparisonSchema = {
  type: "object",
  properties: {},
  additionalProperties: false
};

const readOnlyAnnotations = {
  readOnlyHint: true,
  untrustedContentHint: false,
  consequentialHint: false
};
~~~

Register `get-iphone18-pro-overview` titled `查詢 iPhone 18 Pro 系列摘要`, and `compare-iphone18-pro-models` titled `比較 iPhone 18 Pro 與 Pro Max`. Descriptions must describe fixed read-only news/comparison data only.

- [ ] **Step 3: Implement defensive input and cancellation helpers**

Return these exact errors:

~~~javascript
const abortedResult = () => ({
  error: { code: "aborted", message: "工具執行已取消。" }
});
const invalidModelResult = () => ({
  error: { code: "invalid_model", message: "model 必須是 all、iphone-18-pro 或 iphone-18-pro-max。" }
});
~~~

Check `signal.aborted` before work, attach an `abort` listener, resolve the aborted result if cancellation wins, remove the listener on completion, and use a microtask so an immediate abort after calling `execute` is observable. Reject overview keys other than `model` with `invalid_input / 輸入包含未定義欄位。`; reject comparison keys with `invalid_input / 比較工具不接受輸入欄位。`.

- [ ] **Step 4: Implement the exact overview and comparison objects**

Overview defaults omitted `model` to `all), validates the three allowed values, filters `models) for a single model, and returns:

~~~javascript
{
  type: "iphone18-pro-overview",
  announcementDate: newsData.announcementDate,
  lastVerifiedDate: newsData.lastVerifiedDate,
  model,
  summary: newsData.summary,
  highlights: newsData.highlights,
  release: newsData.release,
  models: model === "all" ? newsData.models : newsData.models.filter((item) => item.id === model),
  officialLinks: newsData.officialLinks
}
~~~

Comparison returns `type: "iphone18-pro-comparison"`, the ten criteria in the specification order, `newsData.models`, and `newsData.officialLinks`. Return cloned objects/arrays if needed to keep the in-memory bootstrap immutable.

- [ ] **Step 5: Register independently and complete the red-green cycle**

Call `document.modelContext.registerTool` separately for each tool. Catch each registration rejection and log `WebMCP tool registration failed: <tool name>`; do not rethrow or stop the other registration. Run:

~~~powershell
node --test .tmp/news-webmcp.test.mjs
~~~

Expected: all tests pass with zero failures and no captured warnings in success-path tests. Fix implementation code, not assertions.

### Task 5: Add responsive, scoped News styling

**Files:**
- Modify: `WebMcpTest/wwwroot/css/site.css`

**Interfaces:**
- Consumes: `.news-*` classes emitted by `News.cshtml`.
- Produces: readable cards, accessible table presentation, preserved focus indicators, and a narrow-screen horizontal scroll region.

- [ ] **Step 1: Add scoped base styles**

Append styles for `.news-page`, `.news-hero`, `.news-eyebrow`, `.news-summary`, `.news-scope`, `.news-meta`, `.news-highlight-grid`, `.news-card`, `.news-table-scroll`, `.news-comparison-table`, `.news-schedule`, `.news-link-list`, and `.news-note`. Put `overflow-x: auto` on `.news-table-scroll`, a table `min-width`, and `:focus-visible` outline styles. Do not remove the existing global focus rule.

- [ ] **Step 2: Add the narrow-screen rule**

Use `@media (max-width: 767.98px)` to reduce page padding and keep the table horizontally scrollable. Do not apply `overflow: hidden` to the table or its scroll ancestor.

- [ ] **Step 3: Build**

~~~powershell
dotnet build .\WebMcpTest\WebMcpTest.csproj --no-restore
~~~

Expected: exit code `0`, no compiler errors.

### Task 6: Run HTTPS integration and final verification

**Files:**
- Modify: none beyond the five implementation files above.
- Delete individually after verification: `.tmp/news-webmcp.test.mjs`, `.tmp/news-page-smoke.ps1`.

**Interfaces:**
- Consumes: built application, temporary contract tests, and the existing `https` launch profile at `https://localhost:7020`.
- Produces: fresh evidence for routes, HTML content, WebMCP behavior, no external calls, and a clean final diff.

- [ ] **Step 1: Start the HTTPS application**

Run in a dedicated terminal session:

~~~powershell
dotnet run --project .\WebMcpTest\WebMcpTest.csproj --launch-profile https --no-build
~~~

- [ ] **Step 2: Run the page smoke test**

In a second terminal run:

~~~powershell
pwsh -NoProfile -File .tmp/news-page-smoke.ps1
~~~

Expected: exit code `0`; `/`, `/Privacy`, and `/News` return 200; News has `lang="zh-Hant-TW"`, the heading, both models, comparison table, schedule, and all three Apple URLs.

- [ ] **Step 3: Run final WebMCP contract tests**

~~~powershell
node --test .tmp/news-webmcp.test.mjs
~~~

Expected: all tests pass with zero failures, including schema closure, all overview filters, comparison, invalid input, extra fields, pre-abort, in-flight abort, and unavailable WebMCP.

- [ ] **Step 4: Check static no-side-effect constraints**

~~~powershell
rg -n "fetch|XMLHttpRequest|window\.open|location\.|navigator\.sendBeacon|document\.cookie|localStorage|sessionStorage|registerTool|execute\s*=" .\WebMcpTest\wwwroot\js\news-webmcp.js
~~~

Expected: only intentional `registerTool` and `execute` matches appear; no network, navigation, cookie, storage, or transaction API appears.

- [ ] **Step 5: Stop the app and run final build/diff checks**

After stopping `dotnet run`, run:

~~~powershell
dotnet build .\WebMcpTest\WebMcpTest.csproj --no-restore
git diff --check
git status --short
~~~

Expected: build exit code `0` with zero warnings/errors; `git diff --check` has no output; status contains only the five intended implementation files. Remove only the two known temporary files:

~~~powershell
Remove-Item -LiteralPath '.tmp/news-webmcp.test.mjs'
Remove-Item -LiteralPath '.tmp/news-page-smoke.ps1'
~~~

- [ ] **Step 6: Re-read the specification against the final diff**

Check sections 3 through 9 of `spec/spec1.md): route/navigation, fixed data, table semantics, schedule caveat, official links, exact tool names/schemas/annotations, invalid input, cancellation, capability fallback, no side effects, and unchanged existing pages. Report any requirement not directly evidenced instead of claiming it passed.

