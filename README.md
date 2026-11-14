# WebMcpTest

WebMcpTest 是一個以 ASP.NET Core Razor Pages 與 .NET 10 建立的 WebMCP 示範專案。專案目前以 **/News** 頁面呈現固定的 iPhone 18 Pro 系列新聞資料，並提供兩個唯讀 WebMCP tool 和一個比較表操作 tool。

本專案的重點不是建立後端 MCP server，而是示範「網頁本身如何提供可被 agent 呼叫的工具」：

- 使用瀏覽器端的 **document.modelContext**。
- 由頁面專用 JavaScript 註冊 WebMCP tools。
- 查詢工具讀取頁面的固定 JSON，畫面工具更新目前比較表的顯示選擇。
- 支援 JavaScript 的瀏覽器可用核取方塊手動篩選。停用 JavaScript 時，頁面提供完整比較表。

## 專案目標

**/News** 是一個單頁新聞摘要，內容聚焦：

- iPhone 18 Pro 與 iPhone 18 Pro Max 的主要亮點。
- 兩款機型的尺寸、顯示器、重量、容量、續航、相機、晶片與外觀比較。
- Apple 官方發表、預購與供貨時程。
- Apple 新聞稿、產品頁、技術規格與 WebMCP 官方規格連結。

新聞資料是固定且集中管理的示範資料，不會在頁面載入時抓取外部網站。頁面與 WebMCP tool 共用同一份資料來源，避免畫面顯示內容與 agent 取得的資料不一致。

## 快速開始

### 環境需求

- .NET 10 SDK
- 支援 HTTPS 的瀏覽器
- 若要實際測試 WebMCP，需使用支援 **document.modelContext** 的瀏覽器或 agent host。

### 還原、建置與啟動

在儲存庫根目錄執行：

```powershell
dotnet restore .\WebMcpTest\WebMcpTest.csproj
dotnet build .\WebMcpTest\WebMcpTest.csproj --no-restore
dotnet run --project .\WebMcpTest\WebMcpTest.csproj --launch-profile https
```

HTTPS 啟動後可使用：

- 首頁：<https://localhost:7020/>
- News 頁面：<https://localhost:7020/News>
- Privacy 頁面：<https://localhost:7020/Privacy>

HTTP 開發網址為 <http://localhost:5133/>。HTTP 頁面仍可閱讀，但 WebMCP 需要 Secure Context，因此工具可能不會註冊。若本機瀏覽器尚未信任開發憑證，可依 .NET SDK 的環境設定使用 **dotnet dev-certs https --trust**。

## 頁面與資料架構

目前的資料流如下：

```text
NewsContent.Article
        │
        │  NewsPageModel.SerializedArticle
        ▼
News.cshtml
  <script id="news-data" type="application/json">...</script>
        │
        │  載入頁面專用 JavaScript
        ▼
wwwroot/js/news-webmcp.js
        │
        ├─ 初始化比較表的手動篩選
        └─ 有 WebMCP 註冊能力時
           ├─ 比較表初始化成功後註冊畫面工具
           └─ 讀取 #news-data 並註冊兩個唯讀工具
```

**WebMcpTest/Pages/News.cshtml.cs** 中的 **NewsContent.Article** 是固定新聞資料的單一來源。**NewsPageModel.SerializedArticle** 使用 camelCase JSON 輸出，讓前端 JavaScript 可以直接使用 **announcementDate**、**models**、**officialLinks** 等欄位。

**WebMcpTest/Pages/News.cshtml** 同時負責：

- 產生一般使用者閱讀的語意化 HTML。
- 將同一份新聞資料嵌入 **#news-data** JSON script。
- 載入 **wwwroot/js/news-webmcp.js**。

## WebMCP 整合

### WebMCP 的角色

WebMCP 讓支援的 AI agent 以結構化方式呼叫網頁提供的功能。這個專案採用目前規格中的 **document.modelContext**，不新增 npm 套件、伺服器端 MCP endpoint、資料庫或外部新聞 API。

查詢工具讀取固定新聞資料。畫面工具的作用範圍限於當前比較表、核取方塊與狀態訊息。工具執行限制如下：

- 導覽到其他頁面。
- 執行購買、登入、下單或資料寫入。
- 呼叫 Apple 網站或其他外部 API。
- 讀取 Cookie、瀏覽紀錄、個人資料或裝置資訊。
- 接受使用者提供的 URL 作為程式碼或請求目的地。

### Tool 清單

| Tool name | 用途 | 輸入 | 回傳類型 | Annotation |
| --- | --- | --- | --- | --- |
| **get-iphone18-pro-overview** | 取得 iPhone 18 Pro 系列新聞摘要與機型資料 | 可選的 **model** | **iphone18-pro-overview** | **readOnlyHint: true**、**untrustedContentHint: false**、**consequentialHint: false** |
| **compare-iphone18-pro-models** | 取得兩款機型的比較資料 | 空物件 **{}** | **iphone18-pro-comparison** | **readOnlyHint: true**、**untrustedContentHint: false**、**consequentialHint: false** |
| **set-iphone18-comparison-view** | 設定比較表顯示的機型與規格 | 可選的 **models**、**criteria** 陣列 | **{ models, criteria }** | **readOnlyHint: false**、**untrustedContentHint: false**、**consequentialHint: false** |

每個工具獨立註冊。註冊失敗會記錄工具名稱及錯誤，手動篩選仍可使用。

### get-iphone18-pro-overview

用途是取得固定的官方新聞摘要。輸入 Schema 只允許一個可選欄位：

```json
{
  "type": "object",
  "properties": {
    "model": {
      "type": "string",
      "enum": ["all", "iphone-18-pro", "iphone-18-pro-max"]
    }
  },
  "additionalProperties": false
}
```

行為規則：

- 不傳 **model** 時視為 **all**。
- **all** 回傳兩款機型。
- **iphone-18-pro** 只回傳 iPhone 18 Pro。
- **iphone-18-pro-max** 只回傳 iPhone 18 Pro Max。
- Schema 與 **execute** 都會拒絕未定義欄位。

回傳資料的主要形狀如下：

```json
{
  "type": "iphone18-pro-overview",
  "announcementDate": "2026-09-09",
  "lastVerifiedDate": "2026-09-10",
  "model": "all",
  "summary": "Apple 發表 iPhone 18 Pro 與 iPhone 18 Pro Max。",
  "highlights": [],
  "release": {
    "preorderStart": "2026-09-12T05:00:00-07:00",
    "availabilityStart": "2026-09-18",
    "timezoneNote": "時程以 Apple 美國官方資料為準，地區供貨可能不同。"
  },
  "models": [],
  "officialLinks": []
}
```

**highlights**、**models** 與 **officialLinks** 都是結構化陣列，不會把可被誤解為系統指令的自然語言內容當成 tool 指令傳遞。

### compare-iphone18-pro-models

用途是回傳兩款機型的固定比較資料。輸入 Schema 明確禁止未定義欄位：

```json
{
  "type": "object",
  "properties": {},
  "additionalProperties": false
}
```

回傳資料包含比較欄位順序、兩款機型資料與官方連結：

```json
{
  "type": "iphone18-pro-comparison",
  "criteria": [
    "displaySizeInches",
    "displayResolution",
    "weightGrams",
    "storageOptionsGb",
    "typicalUseHours",
    "videoPlaybackHours",
    "streamedVideoPlaybackHours",
    "cameraSummary",
    "chip",
    "finishes"
  ],
  "models": [],
  "officialLinks": []
}
```

正常呼叫方式是傳入 **{}**；傳入額外欄位會被拒絕。

### set-iphone18-comparison-view

在「機型」和「規格」下勾選要顯示的項目，每組至少保留一項。按「顯示完整規格」或重新整理頁面，即可恢復兩款機型與全部規格。顯示選擇只存在當前頁面。兩個唯讀工具的回傳範圍獨立於畫面選擇。

每次呼叫會設定整張比較表。省略的欄位會顯示該組全部選項，`{}` 恢復完整比較表。已指定的欄位必須是非空陣列，識別碼必須有效且不重複。完整識別碼與顯示順序請見 [自訂比較規格](spec/spec2.md)。

## 執行流程與防禦性處理

**wwwroot/js/news-webmcp.js** 先初始化手動篩選，再檢查 WebMCP 註冊能力。手動操作和畫面工具共用更新函式，同步更新可見欄列、核取方塊、共同特色跨欄數及狀態訊息。兩個唯讀工具直接讀取 **#news-data** 的 JSON。

工具 callback 內部的錯誤回傳如下。若呼叫端透過 **executeTool(..., { signal })** 取消，**executeTool()** 的 Promise 會 reject；呼叫端應捕捉取消錯誤（通常是 **signal.reason**），不要把它當作工具結果解析。

| 情境 | **error.code** | 說明 |
| --- | --- | --- |
| **model** 不是允許值 | **invalid_model** | 只接受 **all**、**iphone-18-pro**、**iphone-18-pro-max** |
| 輸入不是物件或包含額外欄位 | **invalid_input** | 防禦性檢查補足 Schema 驗證 |
| 工具 callback 內部的 **AbortSignal** 已取消或執行中收到取消 | **aborted** | callback 內部回傳「工具執行已取消」錯誤物件 |
| tool 執行發生未預期例外 | **execution_failed** | 記錄診斷訊息並回傳一般化錯誤 |

回傳前會複製陣列與物件，避免 agent 修改結果時意外改變頁面內嵌的原始資料。

## 安全性與限制

### Secure Context

WebMCP 介面需要 Secure Context。請使用 HTTPS 啟動設定測試：

```text
https://localhost:7020/News
```

在不支援 WebMCP、使用 HTTP，或瀏覽器尚未提供 **document.modelContext** 時，**/News** 仍應正常呈現標題、亮點、比較表、時程與官方連結。

### 固定資料與外部連結

News 頁面的資料是固定內容，程式不會在執行期間重新抓取 Apple 網站。頁面提供的官方連結包括：

- [Apple 新聞稿](https://www.apple.com/newsroom/2026/09/apple-debuts-iphone-18-pro-and-iphone-18-pro-max/)
- [Apple iPhone 18 Pro 產品頁](https://www.apple.com/iphone-18-pro/)
- [Apple 技術規格](https://www.apple.com/iphone-18-pro/specs/)
- [WebMCP 官方規格](https://webmachinelearning.github.io/webmcp/)

上述連結是供使用者閱讀與查證的固定連結，不是 WebMCP tool 的執行目的地。

### 明確不包含的功能

目前不包含：

- 資料庫、CMS、管理介面或排程更新。
- 執行時爬取或解析 Apple 網站。
- 價格、台灣售價、電信方案、庫存或購買流程。
- 登入、留言、收藏、個人化或使用者資料收集。
- tool 觸發的導覽、交易、持久化資料修改或不可逆操作。

## 如何驗證 WebMCP

### 一般頁面驗證

1. 使用 HTTPS 啟動專案。
2. 開啟 **https://localhost:7020/News**。
3. 確認頁面包含主標題、兩款機型、比較表、重要時程與官方連結。
4. 另行開啟 **/** 與 **/Privacy**，確認既有頁面仍可使用。
5. 開啟瀏覽器 DevTools Console，確認沒有 JavaScript 錯誤。

### Tool 註冊與執行驗證

在支援 WebMCP 的安全內容環境中，先檢查 `document.modelContext` 是否存在，再列出目前 document 已註冊的工具：

```javascript
typeof document.modelContext;
const tools = await document.modelContext.getTools();
tools.map((tool) => tool.name);
```

`typeof document.modelContext` 預期為 `"object"`，工具名稱應包含下列三個；目前 Chrome 的回傳順序為字母排序：

```text
[
  "compare-iphone18-pro-models",
  "get-iphone18-pro-overview",
  "set-iphone18-comparison-view"
]
```

`getTools()` 回傳的是已註冊工具描述，不是原始註冊物件，因此應使用 `document.modelContext.executeTool()` 執行，不要直接呼叫 `overview.execute()`。Chrome 文件目前以 JavaScript 物件作為輸入範例，並註明 JSON 字串輸入將於 Chrome 155 淘汰；頁面內的 tool `execute(inputObject, options)` 會收到物件。

可用下列方式驗證實際執行與回傳內容：

```javascript
const overview = tools.find((tool) => tool.name === "get-iphone18-pro-overview");
const comparison = tools.find((tool) => tool.name === "compare-iphone18-pro-models");
const parseToolResult = (raw) => typeof raw === "string" ? JSON.parse(raw) : raw;

const overviewResult = parseToolResult(await document.modelContext.executeTool(
  overview,
  { model: "iphone-18-pro" }
));
overviewResult;

const comparisonResult = parseToolResult(await document.modelContext.executeTool(
  comparison,
  {}
));
comparisonResult;
```

預期 `overviewResult.model` 為 `"iphone-18-pro"` 且 `overviewResult.models` 只有一筆；`comparisonResult.models` 應包含兩款機型。

也可用相同的 `executeTool()` 方式驗證防禦性行為：

```javascript
const invalidModelResult = parseToolResult(await document.modelContext.executeTool(
  overview,
  { model: "unknown" }
));
invalidModelResult.error.code; // "invalid_model"

const invalidInputResult = parseToolResult(await document.modelContext.executeTool(
  comparison,
  { unexpected: true }
));
invalidInputResult.error.code; // "invalid_input"

const preAbortedController = new AbortController();
preAbortedController.abort();
let preAbortedReason;
try {
  await document.modelContext.executeTool(overview, {}, { signal: preAbortedController.signal });
} catch (error) {
  preAbortedReason = error;
}
preAbortedReason === preAbortedController.signal.reason; // true

const runningController = new AbortController();
const runningExecution = document.modelContext.executeTool(
  overview,
  {},
  { signal: runningController.signal }
);
runningController.abort();
let runningAbortReason;
try {
  await runningExecution;
} catch (error) {
  runningAbortReason = error;
}
runningAbortReason === runningController.signal.reason; // true
```

取消驗收分別涵蓋呼叫前已取消，以及 **executeTool()** 呼叫發出後、Promise 尚待處理時取消；兩者都應 reject，並由呼叫端捕捉 **signal.reason**，不應預期取得 **error.code === "aborted"** 的工具結果物件。無效輸入仍應回傳 **invalid_model** 或 **invalid_input**。若 `document.modelContext` 為 `undefined`，頁面仍可閱讀與手動篩選。

### 比較畫面驗證

在支援 WebMCP 的 Chrome 中使用 HTTPS 開啟 **/News**，確認 Tool Inspector 列出三個工具。接續上方的 `tools` 和 `parseToolResult` 執行：

```javascript
const view = tools.find((tool) => tool.name === "set-iphone18-comparison-view");
const applyView = async (input) =>
  parseToolResult(await document.modelContext.executeTool(view, input));

await applyView({ models: ["iphone-18-pro-max"], criteria: ["weightGrams"] });
await applyView({ criteria: ["videoPlaybackHours", "weightGrams"] });
await applyView({});
```

第二次呼叫應恢復兩款機型，並依序顯示和回傳重量、影片播放。用鍵盤改選單一機型並保留「共同特色」，確認該列跨欄數為 1，重設後為 2。手機寬度下可水平捲動表格。停用 WebMCP 時手動篩選仍可用，停用 JavaScript 時仍可閱讀完整表格。

驗證錯誤與取消時，先記下畫面的篩選狀態。傳入未知欄位、未知識別碼、空陣列或重複值，應由宿主拒絕或收到 callback 的 `invalid_input`。用已取消的 signal 呼叫 `executeTool(view, input, { signal })` 應收到 Promise rejection。每次都要核對畫面仍維持呼叫前的狀態。

## 專案結構

```text
.
├─ WebMcpTest/
│  ├─ Pages/
│  │  ├─ News.cshtml          # News 頁面與 #news-data JSON bootstrap
│  │  ├─ News.cshtml.cs       # 固定新聞資料與 JSON 序列化
│  │  └─ Shared/
│  │     └─ _Layout.cshtml    # 共用版型與 News 導覽
│  ├─ wwwroot/
│  │  └─ js/
│  │     └─ news-webmcp.js    # WebMCP 能力偵測、tool 註冊與 execute
│  ├─ Program.cs              # ASP.NET Core 啟動設定
│  └─ WebMcpTest.csproj       # .NET 10 Razor Pages 專案
├─ spec/
│  └─ spec1.md                # News 與 WebMCP 功能規格
├─ docs/
│  ├─ readme-template.md      # README 初始建立指引
│  └─ superpowers/plans/      # News/WebMCP 實作計畫
└─ README.md
```

## 相關文件

- [WebMCP 功能規格](spec/spec1.md)
- [自訂比較畫面規格](spec/spec2.md)
- [News/WebMCP 實作計畫](docs/superpowers/plans/2026-09-10-iphone18-pro-news-webmcp.md)
- [News PageModel](WebMcpTest/Pages/News.cshtml.cs)
- [News Razor Page](WebMcpTest/Pages/News.cshtml)
- [WebMCP JavaScript](WebMcpTest/wwwroot/js/news-webmcp.js)
- [WebMCP Draft Community Group Report](https://webmachinelearning.github.io/webmcp/)

本 README 只描述目前已存在的 WebMCP 行為；若未來調整 tool 名稱、輸入 Schema、回傳契約或資料來源，應同步更新程式、對應規格與本文件。