# iPhone 比較畫面讀取與局部更新 WebMCP 開發規格書

## 1. 文件資訊

| 項目 | 說明 |
| --- | --- |
| 規格版本 | 1.0 |
| 規格日期 | 2026-11-14 |
| 目標專案 | `WebMcpTest`，ASP.NET Core Razor Pages，`net10.0` |
| 路由 | `/News` |
| 頁面語言 | 繁體中文（`zh-Hant-TW`） |
| 基礎規格 | [iPhone 18 Pro 與 iPhone Duo 自訂規格比較 WebMCP 開發規格書](spec2.md) |

## 2. 需求

讓 agent 讀取使用者目前選擇的機型與規格，並直接新增或移除個別選項。例如，使用者手動選好 Pro Max 與 Duo 後，可以請 agent「再加上串流影片播放」，保留目前選擇的機型與其他規格。畫面狀態只存在目前頁面，重新整理後恢復三款機型與全部規格。

## 3. WebMCP 工具契約

| 工具 | 顯示標題 | 輸入 | `readOnlyHint` |
| --- | --- | --- | --- |
| `get-iphone-comparison-view` | 讀取目前 iPhone 比較畫面 | 空物件 `{}` | `true` |
| `update-iphone-comparison-view` | 局部更新 iPhone 比較畫面 | 至少一個新增或移除欄位 | `false` |

兩個工具的 `consequentialHint` 與 `untrustedContentHint` 均為 `false`，省略 `outputSchema`。工具說明須明示讀取工具回傳目前選擇，更新工具只新增或移除指定項目。沿用 `document.modelContext.registerTool()`，在 `/News` 比較表初始化成功且具備註冊能力時註冊。各工具獨立處理註冊失敗，記錄工具名稱，手動操作仍可使用。

### 3.1 讀取目前畫面

輸入 Schema 為 `{"type":"object","properties":{},"additionalProperties":false}`。成功時回傳當下實際顯示的 `{ models, criteria }`，兩組識別碼均依表格順序排列。手動勾選、重設及 WebMCP 更新後都可立即讀到新狀態。回傳陣列是快照，修改回傳值不影響頁面。

### 3.2 局部更新畫面

輸入 Schema 為物件，僅允許下列四個可選欄位，並使用 `minProperties: 1` 及 `additionalProperties: false`。每個欄位使用 `type: "array"`、`minItems: 1`、`uniqueItems: true`，項目型別為字串，`enum` 沿用 [spec2.md](spec2.md) §4.1 對應的機型或規格白名單。

| 欄位 | 行為 |
| --- | --- |
| `addModels` | 加入指定機型 |
| `removeModels` | 移除指定機型 |
| `addCriteria` | 加入指定規格 |
| `removeCriteria` | 移除指定規格 |

省略的欄位不執行對應操作。加入已顯示的項目或移除未顯示的項目仍成功，可安全重試。同一識別碼出現在同組的新增與移除陣列時，拒絕整次呼叫。一次呼叫可同時移除最後一項並加入另一項，以全部操作完成後的結果判斷每組是否至少保留一項。

例如，目前選擇 Pro Max、Duo 和「重量」，傳入 `{"addCriteria":["streamedVideoPlaybackHours"]}` 後，回傳下列結果。

```json
{
  "models": ["iphone-18-pro-max", "iphone-duo"],
  "criteria": ["weightGrams", "streamedVideoPlaybackHours"]
}
```

## 4. 驗證、更新與相容性

- 兩個工具都須在 callback 驗證輸入。拒絕非物件、陣列及未定義欄位。更新工具另拒絕 `{}`、非陣列欄位、空陣列、重複或未知識別碼、同組新增與移除衝突，以及會清空任一組選擇的結果。回傳 `{"error":{"code":"invalid_input","message":"…"}}`，繁中訊息指出問題及修正方式。
- 更新工具以 callback 執行當下的畫面為基準，先計算並驗證完整結果，再檢查 `options.signal`。合法結果透過現有共用函式同步更新表格、控制項、共同特色跨欄與狀態訊息，完成後才依表格固定順序回傳 `{ models, criteria }`。任何欄位驗證失敗都保留整個原畫面。
- 兩工具在讀取或套用前檢查取消。callback 收到已取消的 signal 時回傳 `{"error":{"code":"aborted","message":"工具執行已取消。"}}`。呼叫原生 `document.modelContext.executeTool(tool, input, { signal })` 時，`input` 直接傳入 JavaScript 物件。若在執行前取消，呼叫端驗收 Promise rejection。兩工具沿用現有比較表的狀態來源及更新函式。
- `set-iphone18-comparison-view` 維持完整設定語意。省略欄位會恢復該組全部選項，`{}` 恢復完整比較表。既有規格查詢工具的輸入、回傳與查詢範圍沿用原契約。

## 5. 驗收與測試

| 情境 | 預期結果 |
| --- | --- |
| 在 HTTPS Chrome 發現工具 | `/News` 顯示原有五個工具與新增兩個工具。 |
| 手動選 Pro Max、Duo 與重量，再讀取、加入串流影片播放、重新讀取 | 首次讀取反映手動選擇。更新後只多出串流影片播放，回傳順序與表格一致。再次讀取與更新回傳相同。 |
| 修改讀取結果中的陣列後再次讀取 | 表格與新的讀取結果均維持原選擇。 |
| 同次移除唯一已選機型並加入另一款，再重複呼叫 | 首次可切換到另一款，重複呼叫仍成功。核取方塊停用狀態和共同特色跨欄數同步更新。 |
| 測試未知欄位、重複值、同組衝突及清空任一組 | 直接呼叫 callback 回傳 `invalid_input`。宿主可先拒絕無效輸入。兩種情況皆保留原表格、控制項與狀態訊息。 |
| 在讀取或套用前取消 | callback 回傳 `aborted`。原生 `executeTool()` 的取消案例驗證 Promise rejection，畫面維持原選擇。 |
| 分別按下重設、重新整理及向既有完整設定工具傳入 `{}` | 各自恢復三款機型與全部規格。既有規格查詢工具仍依原契約回傳。 |
| WebMCP 缺席或註冊失敗 | 手動勾選與重設仍可使用。 |
