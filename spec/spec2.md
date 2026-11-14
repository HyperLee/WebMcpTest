# iPhone 18 Pro 自訂規格比較 WebMCP 開發規格書

## 1. 文件資訊

| 項目 | 說明 |
| --- | --- |
| 規格版本 | 1.1 |
| 規格日期 | 2026-11-14 |
| 目標專案 | `WebMcpTest`，ASP.NET Core Razor Pages，`net10.0` |
| 路由 | `/News` |
| 頁面語言 | 繁體中文（`zh-Hant-TW`） |
| 基礎規格 | [iPhone 18 Pro News／WebMCP 開發規格書](spec1.md) |

## 2. 需求背景與範圍

使用者可在 `/News` 選擇想比較的機型與規格，也可請支援 WebMCP 的 agent 套用同一組選擇。查詢用途仍由 `get-iphone18-pro-overview` 與 `compare-iphone18-pro-models` 提供，兩個工具的輸入與回傳契約沿用基礎規格，回傳範圍獨立於比較表的顯示設定。

本規格補充基礎規格第 3.2 節的工具副作用限制，允許新增工具更新目前頁面的比較表、對應控制項與狀態訊息。資料沿用 Razor PageModel 提供的固定新聞資料，顯示設定只放在目前頁面的記憶體。條件推薦、外觀與容量預覽、購買流程、外部請求、後端 API 與設定保存皆在本期範圍之外。

## 3. 比較表與頁面操作

### 3.1 顯示與操作

- 初次開啟或重新整理頁面時，顯示兩款機型與全部規格。比較表保留語意化 HTML 表格、表格標題、列欄標頭及窄版水平捲動。
- 在表格前提供「機型」和「規格」兩組有標籤的核取方塊，以及「顯示完整規格」按鈕。每組至少保留一項，停用取消該組最後一項的操作。按下按鈕後恢復全部選項。
- 手動控制與 WebMCP 共用同一套顯示邏輯。每次套用同步更新核取方塊、停用狀態、可見欄列與 `aria-live="polite"` 狀態區。狀態區顯示實際可見的機型與規格數量。
- 先顯示 iPhone 18 Pro，再顯示 iPhone 18 Pro Max。單機型模式下，欄標頭與對應資料欄一併隱藏。若顯示「共同特色」，其內容儲存格的 `colspan` 等於可見機型數。
- 頁面操作初始化與 WebMCP 註冊各自判斷所需能力。支援 JavaScript 時可使用手動控制。缺少 WebMCP 或工具註冊失敗時，手動控制仍可用。JavaScript 停用時顯示完整比較表，互動控制維持隱藏。

### 3.2 規格識別碼與顯示順序

比較表沿用目前順序，將原「顯示器」列拆為「螢幕尺寸」和「顯示器解析度」，使兩項可以各自選取。原顯示器類型及 ProMotion 規格隨「螢幕尺寸」呈現。

| 識別碼 | 表格列名 |
| --- | --- |
| `dimensions` | 尺寸 |
| `displaySizeInches` | 螢幕尺寸 |
| `displayResolution` | 顯示器解析度 |
| `weightGrams` | 重量 |
| `storageOptionsGb` | 容量 |
| `typicalUseHours` | 一般使用 |
| `videoPlaybackHours` | 影片播放 |
| `streamedVideoPlaybackHours` | 串流影片播放 |
| `cameraSummary` | 相機 |
| `chip` | 晶片 |
| `finishes` | 外觀 |
| `sharedFeatures` | 共同特色 |

## 4. WebMCP 工具契約

| 項目 | 契約 |
| --- | --- |
| `name` | `set-iphone18-comparison-view` |
| `title` | 自訂 iPhone 18 Pro 規格比較畫面 |
| `description` | 更新目前頁面比較表所顯示的機型與規格。省略任一欄位會顯示該組全部選項，傳入空物件可恢復完整比較表。 |
| `readOnlyHint` | `false` |
| `consequentialHint` | `false` |
| `untrustedContentHint` | `false` |

使用 `document.modelContext.registerTool()`，僅在 `/News` 的比較表初始化成功且具備註冊能力時註冊新工具。註冊失敗時記錄可診斷訊息，其他工具的註冊各自處理。

第一版使用命令式註冊。手動控制依第 3 節接到共用顯示邏輯。工具註冊省略選用的 `outputSchema`，回傳格式依第 4.2 與 4.3 節驗收。待目標瀏覽器與 agent 均確認支援後，再評估宣告成功與錯誤回傳的 Schema。

### 4.1 輸入

```json
{
  "type": "object",
  "properties": {
    "models": {
      "type": "array",
      "description": "畫面要顯示的機型。省略時顯示兩款。",
      "minItems": 1,
      "uniqueItems": true,
      "items": {
        "type": "string",
        "enum": ["iphone-18-pro", "iphone-18-pro-max"]
      }
    },
    "criteria": {
      "type": "array",
      "description": "畫面要顯示的規格。省略時顯示全部規格。",
      "minItems": 1,
      "uniqueItems": true,
      "items": {
        "type": "string",
        "enum": [
          "dimensions",
          "displaySizeInches",
          "displayResolution",
          "weightGrams",
          "storageOptionsGb",
          "typicalUseHours",
          "videoPlaybackHours",
          "streamedVideoPlaybackHours",
          "cameraSummary",
          "chip",
          "finishes",
          "sharedFeatures"
        ]
      }
    }
  },
  "additionalProperties": false
}
```

每次呼叫都會設定完整的比較畫面。省略的欄位取該組全部識別碼，空物件 `{}` 會恢復完整比較表。

使用 `document.modelContext.executeTool(tool, input)` 驗收時，`input` 直接傳入 JavaScript 物件。

### 4.2 成功回傳

共用顯示邏輯完成表格、控制項與狀態區的更新後，工具才回傳實際顯示的識別碼，依頁面固定順序排列。`toolactivated` 只代表 callback 已被呼叫，完成時機以共用顯示邏輯的更新結果為準。例如輸入 `{"criteria":["videoPlaybackHours","weightGrams"]}` 時，回傳：

```json
{
  "models": ["iphone-18-pro", "iphone-18-pro-max"],
  "criteria": ["weightGrams", "videoPlaybackHours"]
}
```

### 4.3 驗證與取消

- `inputSchema` 描述工具的輸入契約。`execute(inputObject, options)` 須自行依相同白名單驗證輸入，讓未強制執行 Schema 驗證的宿主也能拒絕無效設定。輸入為非物件或為陣列、含未定義欄位、指定值非陣列、陣列為空、存在重複值或含未知識別碼時，回傳 `{"error":{"code":"invalid_input","message":"…"}}`。中文訊息指出有問題的欄位及可修正的條件。
- 驗證與正規化須在修改畫面之前完成。套用前檢查 `options.signal`，若已取消，callback 回傳 `{"error":{"code":"aborted","message":"工具執行已取消。"}}`。合法設定在同一次同步更新內套用，結果以套用完成的狀態為準。
- 呼叫端使用帶 `signal` 的 `executeTool()` 取消時，依基礎規格處理 Promise rejection。驗收取消案例須在設定套用之前觸發取消，並核對畫面。

## 5. 驗收與測試

| 情境 | 預期結果 |
| --- | --- |
| 執行 `dotnet build WebMcpTest/WebMcpTest.csproj`，以瀏覽器開啟 `/News` | 建置成功，初始畫面含兩款機型與全部規格，瀏覽器主控台無 JavaScript 錯誤。 |
| 鍵盤操作核取方塊，篩到一款機型並保留「共同特色」 | 表格只顯示該機型，`colspan` 為 `1`，最後一個勾選項不可取消，狀態區反映結果。 |
| 檢查手機窄版，再按「顯示完整規格」 | 可水平捲動閱讀；恢復後全部核取方塊已勾選，`colspan` 為 `2`。 |
| 使用未啟用 WebMCP 的瀏覽器，以及停用 JavaScript 的瀏覽器 | 前者可手動篩選，後者可閱讀完整比較表。 |
| 在 HTTPS 的 WebMCP 支援環境開啟 Tool Inspector | `/News` 可發現兩個唯讀工具和一個畫面操作工具。 |
| 指定 Pro Max 與重量，再只送 `criteria`；最後傳入 `{}` | 先顯示單一機型，第二次恢復兩款機型並套用新的規格，第三次恢復完整表格。各次回傳順序與畫面一致，手動控制可接續操作。 |
| 分別傳入未知欄位、未知識別碼、空陣列或重複值 | 直接驗證 callback 時回傳 `invalid_input`。經宿主呼叫時，可由宿主先拒絕或由 callback 回傳錯誤。兩種情況皆保留原畫面與控制狀態。 |
| 透過 `executeTool()` 的 `signal` 在套用前取消 | 呼叫端收到 Promise rejection，畫面與控制狀態保留。 |
| 篩選後呼叫原有查詢工具，再重新整理頁面 | 查詢依原契約回傳；重新整理後回復完整比較表。 |
| 對 agent 說「兩款影片播放時間差多少？」 | agent 使用唯讀工具取數據，畫面保持當前的顯示設定。 |
| 對 agent 說「兩款都保留，只顯示重量和影片播放時間」 | agent 呼叫畫面操作工具，表格保留兩款機型與指定兩列，控制項和回傳值皆與畫面一致。 |

## 6. 參考資料

- [Chrome WebMCP Imperative API](https://developer.chrome.com/docs/ai/webmcp/imperative-api)
- [WebMCP Draft Community Group Report](https://webmachinelearning.github.io/webmcp/)
- [WebMCP 草案的 outputSchema 變更](https://github.com/webmachinelearning/webmcp/pull/254)
- [Chrome WebMCP Pizza Maker 範例](https://github.com/GoogleChromeLabs/webmcp-tools/tree/main/demos/pizza-maker)
- [《WebMCP 實戰》簡報，第 28 頁測試與除錯](https://s.itho.me/ccms_slides/2026/10/27/c7c490f0-dc2d-41e0-a40e-e9d3bb94f0b5.pdf#page=28)
