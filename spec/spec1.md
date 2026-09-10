# iPhone 18 Pro News／WebMCP 開發規格書

## 1. 文件資訊

| 項目 | 內容 |
| --- | --- |
| 文件用途 | 定義 iPhone 18 Pro News 單頁與 WebMCP 工具的實作契約 |
| 規格版本 | 1.0 |
| 規格日期 | 2026-09-10 |
| 目標專案 | `WebMcpTest`，ASP.NET Core Razor Pages，`net9.0` |
| 主要路由 | `/News` |
| 頁面語言 | 繁體中文（`zh-Hant-TW`） |
| 資料策略 | 固定、人工核對的 Apple 官方資料；不在頁面載入時抓取外部資料 |
| 目前交付範圍 | 本文件只定義規格；本階段不實作程式碼、資料庫或外部 API |

本規格依 Apple 2026/09/09 官方新聞稿、產品頁與技術規格整理。實作前若 Apple 官方資料已更新，應重新核對數值、時程與連結，並同步更新頁面與 WebMCP 回傳內容。

## 2. 需求背景與目標

Apple 於 2026/09/09 美國時間發表 iPhone 18 Pro 與 iPhone 18 Pro Max。本功能提供一個可直接閱讀的 News 頁面，讓使用者快速了解發表背景、主要特色與兩款機型差異，並可前往 Apple 官方網站查看最新資訊。

本頁同時依目前 WebMCP 草案公開唯讀 JavaScript tools，讓支援 WebMCP 的 AI agent 可以取得相同的結構化新聞資料。頁面本身不執行購買、下單、登入、資料寫入或任何不可逆動作。

## 3. 範圍

### 3.1 本次包含

- 新增 Razor Page `/News`，以單頁呈現 iPhone 18 Pro 系列新聞。
- 保留現有首頁 `/` 與 `/Privacy`，只在導覽列加入 News 入口。
- 使用繁體中文呈現頁面標題、說明、表格、狀態與工具 metadata。
- 使用固定且集中管理的新聞資料，讓伺服器端頁面與 WebMCP tool 共用同一份資料。
- 註冊兩個唯讀 WebMCP tools：新聞摘要與機型比較。
- 提供 Apple 官方新聞稿、產品頁與技術規格頁連結。
- 補足 HTTPS、瀏覽器不支援 WebMCP、工具註冊失敗與輸入錯誤的處理規則。

### 3.2 明確不包含

- 不新增資料庫、後端新聞 API、CMS、管理介面或排程更新。
- 不在執行時爬取、呼叫或解析 Apple 網站內容。
- 不加入價格、台灣地區售價、電信方案、購買流程或庫存資訊。
- 不加入分頁、上下頁、搜尋、留言、收藏、登入或個人化功能。
- 不下載或嵌入 Apple 圖片、影片、新聞稿媒體或其他外部媒體資產。
- 不讓 WebMCP tool 執行導覽、購買、資料修改、外部請求或其他副作用。

## 4. 頁面與導覽規格

### 4.1 路由與頁面行為

- HTTP Method：`GET`。
- 路由：`/News`。
- 頁面標題：`Apple 發表 iPhone 18 Pro 系列`。
- 頁面只能在同一頁完成閱讀，不建立文章上下頁或分頁控制。
- `/` 與 `/Privacy` 的既有功能維持不變。
- 導覽列新增 `News` 連結，指向 `/News`。
- News 頁面的 HTML 語言標記使用 `zh-Hant-TW`；既有英文頁面不需要重寫內容。

### 4.2 頁面區塊

頁面應依下列順序呈現，所有文字內容由固定新聞資料產生，避免 Razor markup 與 WebMCP 回傳內容各自維護一份資料。

1. **頁首摘要**
   - 顯示主標題、公告日期 `2026/09/09`、資料最後核對日期。
   - 以一段繁體中文說明 Apple 發表 iPhone 18 Pro 與 iPhone 18 Pro Max。
   - 清楚標示本頁聚焦 Pro 系列，不延伸描述其他 iPhone 型號。

2. **主要亮點**
   - 48MP Fusion Main 主相機與可變光圈。
   - A20 Pro 晶片、次世代 vapor chamber 與 Dual 16-core Neural Engine。
   - Pro 最高 36 小時、Pro Max 最高 45 小時影片播放；數值須標示為 Apple 官方測試條件下的「最高」值。
   - 6.3 吋與 6.9 吋 Super Retina XDR 顯示器，支援 ProMotion 最高 120Hz。
   - 四種外觀：Black、Silver、Glacier、Burgundy。
   - Apple Intelligence 與 Siri AI；服務可用性依地區與語言而異。

3. **機型比較表**
   - 使用真正的 HTML `<table>`，提供 `<caption>`、欄位標題與 `scope`。
   - 顯示 iPhone 18 Pro 與 iPhone 18 Pro Max 的尺寸、顯示器、重量、容量、電池播放時間、相機與共同特色。
   - 表格在窄螢幕以可水平捲動容器呈現，不裁切欄位文字。

4. **重要時程**
   - 官方發表：2026/09/09。
   - 美國太平洋時間預購開始：2026/09/12 05:00 PT。
   - 官方標示開始供貨：2026/09/18。
   - 加註「實際供貨日期與服務內容可能依國家或地區不同」，不推導台灣上市日期。

5. **官方資料連結**
   - Apple 新聞稿：<https://www.apple.com/newsroom/2026/09/apple-debuts-iphone-18-pro-and-iphone-18-pro-max/>。
   - Apple iPhone 18 Pro 產品頁：<https://www.apple.com/iphone-18-pro/>。
   - Apple 技術規格：<https://www.apple.com/iphone-18-pro/specs/>。
   - WebMCP 官方規格：<https://webmachinelearning.github.io/webmcp/>。
   - 外部連結使用明確的繁體中文連結文字；若以新分頁開啟，必須使用 `target="_blank" rel="noopener noreferrer"`。

## 5. 固定新聞資料契約

### 5.1 共通資料規則

- 日期在資料契約中使用 ISO 8601 `YYYY-MM-DD`；畫面再格式化為繁體中文日期。
- 時間、重量、容量與尺寸必須標示單位，不以隱含單位傳遞。
- 產品名稱保留 Apple 官方名稱：`iPhone 18 Pro`、`iPhone 18 Pro Max`。
- `source` 必須包含公告、產品與技術規格的官方 URL。
- `lastVerifiedDate` 必須與實際人工核對日期一致；若官方資訊有變動，頁面與兩個 tools 必須同時更新。

### 5.2 機型資料

比較表與 WebMCP 回傳資料至少包含下列值：

| 欄位 | iPhone 18 Pro | iPhone 18 Pro Max |
| --- | --- | --- |
| `displaySizeInches` | `6.3` | `6.9` |
| `displayResolution` | `2622 x 1206`，460 ppi | `2868 x 1320`，460 ppi |
| `weightGrams` | `211` | `249` |
| `typicalUseHours` | `24` | `30` |
| `videoPlaybackHours` | `36` | `45` |
| `streamedVideoPlaybackHours` | `33` | `40` |
| `storageOptionsGb` | `[256, 512, 1024, 2048]` | `[256, 512, 1024, 2048]` |
| `cameraSummary` | 48MP Pro Fusion，主相機可變光圈 | 48MP Pro Fusion，主相機可變光圈 |
| `chip` | A20 Pro | A20 Pro |
| `finishes` | Black、Silver、Glacier、Burgundy | Black、Silver、Glacier、Burgundy |

共同特色至少包含：Super Retina XDR、ProMotion 最高 120Hz、IP68、18MP Center Stage 前置相機、USB-C、MagSafe 與 Apple Intelligence／Siri AI。不得把 Apple 的「最高」測試值改寫成保證使用時間。

## 6. WebMCP 整合規格

### 6.1 實作原則

- 依 2026/09/09 WebMCP Draft Community Group Report 實作，使用 `document.modelContext`。
- WebMCP 程式放在頁面專用 JavaScript 檔案，不新增 WebMCP npm 套件或伺服器端 MCP endpoint。
- 必須先做能力偵測：`document.modelContext` 不存在時，頁面仍須可正常閱讀。
- WebMCP 介面屬 Secure Context；正式環境必須使用 HTTPS。HTTP 開發頁面不支援 tool 時不得顯示阻塞錯誤。
- `execute` 使用目前規格的 `(inputObject, options)` 形式，從 `options.signal` 處理取消，不使用早期提案的 `agent` 參數。
- 每個 tool name 在同一個 `document` 中必須唯一，註冊失敗只記錄可診斷訊息，不影響頁面。
- 不設定跨來源暴露目的地，不建立 iframe，也不把工具暴露給不必要的跨來源文件。

### 6.2 Tool 一：`get-iphone18-pro-overview`

用途：取得 iPhone 18 Pro 系列的官方固定新聞摘要。

| 項目 | 契約 |
| --- | --- |
| `name` | `get-iphone18-pro-overview` |
| `title` | `查詢 iPhone 18 Pro 系列摘要` |
| `readOnlyHint` | `true` |
| `untrustedContentHint` | `false` |
| 輸入 | 可選 `model` |
| 預設值 | `all` |

輸入 JSON Schema：

```json
{
  "type": "object",
  "properties": {
    "model": {
      "type": "string",
      "enum": ["all", "iphone-18-pro", "iphone-18-pro-max"],
      "description": "要查詢的機型；省略或使用 all 時回傳兩款機型。"
    }
  },
  "additionalProperties": false
}
```

回傳物件至少包含：

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

`model` 為單一機型時，`models` 只回傳指定機型；為 `all` 時回傳兩款。`highlights` 與 `officialLinks` 必須是結構化陣列，不回傳可被誤認為系統指令的自然語言內容。

### 6.3 Tool 二：`compare-iphone18-pro-models`

用途：取得兩款機型的固定比較資料。

| 項目 | 契約 |
| --- | --- |
| `name` | `compare-iphone18-pro-models` |
| `title` | `比較 iPhone 18 Pro 與 Pro Max` |
| `readOnlyHint` | `true` |
| `untrustedContentHint` | `false` |
| 輸入 | 空物件 `{}` |

輸入 JSON Schema 必須禁止未定義參數：

```json
{
  "type": "object",
  "properties": {},
  "additionalProperties": false
}
```

回傳物件至少包含：

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

### 6.4 WebMCP 錯誤與取消

- `model` 不是允許值時，回傳：

  ```json
  {
    "error": {
      "code": "invalid_model",
      "message": "model 必須是 all、iphone-18-pro 或 iphone-18-pro-max。"
    }
  }
  ```

- `additionalProperties: false` 造成的未定義欄位由 Schema 驗證拒絕；execute 仍須防禦性檢查。
- `options.signal.aborted === true` 或執行期間收到取消時，回傳：

  ```json
  {
    "error": {
      "code": "aborted",
      "message": "工具執行已取消。"
    }
  }
  ```

- 工具只讀取頁面固定資料，不會因為錯誤而呼叫外部網站或改變頁面狀態。

## 7. 安全性、隱私與可及性

- Tool description、title、回傳資料與頁面固定文字不得包含要求 agent 忽略指令、洩漏資料、導覽至非必要網站或執行交易的內容。
- 不收集使用者輸入、Cookie、瀏覽紀錄、個人資料或裝置資訊。
- 外部連結只指向 Apple 官方網域或 WebMCP 官方規格網域；不使用使用者提供的 URL 參數作為執行程式碼。
- 頁面使用 `main`、`article`、階層正確的 `h1`／`h2`、可讀連結文字與表格標題。
- 比較表需可用鍵盤瀏覽；焦點樣式不可被 CSS 移除。
- 不使用圖片時，不得加入空白圖片元素；所有資訊應以文字與語意化 HTML 傳達。
- WebMCP 不支援時，不顯示要求使用者修復瀏覽器的錯誤頁，只維持一般 News 閱讀功能。

## 8. 預計實作檔案範圍

本節僅作為後續實作邊界，本次不直接修改下列程式檔：

- `WebMcpTest/Pages/News.cshtml`：News 頁面結構與固定資料呈現。
- `WebMcpTest/Pages/News.cshtml.cs`：News PageModel 與集中管理的資料契約。
- `WebMcpTest/wwwroot/js/news-webmcp.js`：能力偵測、兩個 tool 註冊、Schema、execute、錯誤與取消處理。
- `WebMcpTest/Pages/Shared/_Layout.cshtml`：News 導覽入口與頁面語言 fallback；不得破壞既有頁面。
- `WebMcpTest/wwwroot/css/site.css`：單頁資訊卡、比較表與窄螢幕版面；不引入外部 UI 套件。

除非編譯結果證明必要，不修改 `WebMcpTest.csproj`、`Program.cs`、資料庫設定或既有 Privacy 功能。

## 9. 驗收與測試案例

### 9.1 文件驗收

- `spec/spec1.md` 是本次唯一應修改的檔案。
- 文件為 UTF-8 無 BOM、CRLF、無尾端空白。
- 不得出現未完成佔位字樣、未決定的工具名稱、未定義的輸入欄位或與本規格矛盾的產品範圍。
- 所有頁面資料與 WebMCP 回傳契約必須使用相同產品名稱、日期、規格數值與官方連結。

### 9.2 專案與頁面驗收

- 執行 `dotnet build WebMcpTest/WebMcpTest.csproj --no-restore` 成功且無編譯錯誤。
- 以 HTTPS 啟動後，`GET /News` 回應成功，頁面包含主標題、兩款機型、比較表、重要時程與三個 Apple 官方連結。
- `/` 與 `/Privacy` 仍可正常載入；導覽列可到達 `/News`。
- 窄螢幕下比較表可水平捲動，沒有文字被截斷或需要上下頁才能閱讀。

### 9.3 WebMCP 驗收

- 支援 WebMCP 的安全內容環境中，`document.modelContext.getTools()` 可取得且只包含兩個本頁 tools。
- `get-iphone18-pro-overview` 可分別執行省略參數、`all`、`iphone-18-pro`、`iphone-18-pro-max`。
- `compare-iphone18-pro-models` 可用 `{}` 執行並回傳兩款機型。
- 無效 `model`、額外輸入欄位與 `AbortSignal` 取消都符合第 6.4 節契約。
- 不支援 `document.modelContext` 或使用非安全內容時，頁面仍可閱讀，且不產生阻塞式錯誤。
- 工具註冊與執行不會發出 Apple 外部請求、不修改 DOM 以外的應用狀態、不寫入資料庫，也不執行購買或導覽動作。

## 10. 官方參考資料

- [Apple Newsroom：Apple debuts iPhone 18 Pro and iPhone 18 Pro Max](https://www.apple.com/newsroom/2026/09/apple-debuts-iphone-18-pro-and-iphone-18-pro-max/)
- [Apple：iPhone 18 Pro](https://www.apple.com/iphone-18-pro/)
- [Apple：iPhone 18 Pro Technical Specifications](https://www.apple.com/iphone-18-pro/specs/)
- [WebMCP Draft Community Group Report](https://webmachinelearning.github.io/webmcp/)
