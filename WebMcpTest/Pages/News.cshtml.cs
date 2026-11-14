using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace WebMcpTest.Pages;

/// <summary>
/// 提供 News 頁面所需的固定新聞資料，並將同一份資料交給 Razor 與前端 WebMCP 腳本使用。
/// </summary>
public class NewsPageModel : PageModel
{
    /// <summary>
    /// 取得頁面呈現用的新聞資料；資料由 <see cref="NewsContent" /> 集中建立，避免頁面與 WebMCP 各自維護不同內容。
    /// </summary>
    public NewsArticle Article { get; } = NewsContent.Article;

    /// <summary>
    /// 取得 iPhone Duo 的固定新聞與規格資料。
    /// </summary>
    public NewsArticle DuoArticle { get; } = NewsContent.DuoArticle;

    /// <summary>
    /// 依比較表順序提供兩款 Pro 與 iPhone Duo。
    /// </summary>
    public IReadOnlyList<ProductModel> ComparisonModels { get; } =
        [.. NewsContent.Article.Models, .. NewsContent.DuoArticle.Models];

    /// <summary>
    /// 將新聞資料序列化為嵌入 Razor JSON script 的 camelCase JSON 字串。
    /// </summary>
    public string SerializedArticle => NewsContent.Serialize(Article);

    /// <summary>
    /// 將 Duo 資料序列化供 WebMCP 使用。
    /// </summary>
    public string SerializedDuoArticle => NewsContent.Serialize(DuoArticle);

    /// <summary>
    /// 處理 News 頁面的 GET 請求；本頁使用預先建立的固定資料，因此不需要額外查詢或初始化動作。
    /// </summary>
    public void OnGet()
    {
    }
}

/// <summary>
/// 集中管理 News 頁面的固定內容、官方來源網址與 JSON 序列化規則。
/// </summary>
public static class NewsContent
{
    // 官方來源網址集中定義，讓新聞資料與頁面連結共用同一組值，避免發生連結不一致。
    private const string NewsroomUrl = "https://www.apple.com/newsroom/2026/09/apple-debuts-iphone-18-pro-and-iphone-18-pro-max/";
    private const string ProductUrl = "https://www.apple.com/iphone-18-pro/";
    private const string SpecificationsUrl = "https://www.apple.com/iphone-18-pro/specs/";
    private const string WebMcpUrl = "https://webmachinelearning.github.io/webmcp/";
    private const string ProFrontCamera = "18MP Center Stage 前置相機";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        // JSON 會被以 Html.Raw 放入 application/json script；Default encoder 會處理 HTML 敏感字元，避免資料被誤解為 markup。
        Encoder = JavaScriptEncoder.Default,
        // news-webmcp.js 以 camelCase 欄位讀取 bootstrap JSON，因此伺服器端輸出需固定使用相同命名規則。
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        // bootstrap 資料只供頁面初始化使用，不需額外縮排以增加回應內容體積。
        WriteIndented = false
    };

    // 這份固定資料是 Razor 顯示內容與 WebMCP 回傳資料的唯一來源；更新規格時應同步核對日期、數值與官方來源。
    /// <summary>
    /// 取得 News 頁面使用的完整固定新聞資料。
    /// </summary>
    public static NewsArticle Article { get; } = new()
    {
        Title = "Apple 發表 iPhone 18 Pro 系列",
        AnnouncementDate = "2026-09-09",
        LastVerifiedDate = "2026-09-10",
        Summary = "Apple 發表 iPhone 18 Pro 與 iPhone 18 Pro Max。",
        ScopeNote = "本頁聚焦 Pro 系列，不延伸描述其他 iPhone 型號。",
        Highlights = new[]
        {
            new NewsHighlight
            {
                Key = "camera",
                Title = "48MP Fusion Main 主相機與可變光圈",
                Description = "48MP Fusion Main 主相機首次加入可變光圈，提供更大的景深與低光控制彈性。"
            },
            new NewsHighlight
            {
                Key = "performance",
                Title = "A20 Pro 與持續效能",
                Description = "A20 Pro、次世代 vapor chamber 與 Dual 16-core Neural Engine 帶來新一代 Pro 效能。"
            },
            new NewsHighlight
            {
                Key = "battery",
                Title = "Apple 官方測試下的電池表現",
                Description = "影片播放最高 36 小時（Pro）與 45 小時（Pro Max）；數值依 Apple 官方測試條件，並非保證使用時間。"
            },
            new NewsHighlight
            {
                Key = "display",
                Title = "兩種尺寸的 Super Retina XDR 顯示器",
                Description = "6.3 吋與 6.9 吋 Super Retina XDR 顯示器，支援 ProMotion 最高 120Hz。"
            },
            new NewsHighlight
            {
                Key = "finishes",
                Title = "四種外觀",
                Description = "Black、Silver、Glacier 與 Burgundy 四種外觀選擇。"
            },
            new NewsHighlight
            {
                Key = "intelligence",
                Title = "Apple Intelligence 與 Siri AI",
                Description = "Apple Intelligence 與 Siri AI 的服務可用性依地區與語言而異。"
            }
        },
        Release = new ReleaseSchedule
        {
            PreorderStart = "2026-09-12T05:00:00-07:00",
            PreorderDisplay = "2026/09/12 05:00 PT",
            AvailabilityStart = "2026-09-18",
            TimezoneNote = "實際供貨日期與服務內容可能依國家或地區不同；時程以 Apple 美國官方資料為準。"
        },
        Models = new[]
        {
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
                FrontCameraSummary = ProFrontCamera,
                Chip = "A20 Pro",
                Finishes = new[] { "Black", "Silver", "Glacier", "Burgundy" }
            },
            new ProductModel
            {
                Id = "iphone-18-pro-max",
                Name = "iPhone 18 Pro Max",
                Dimensions = "78.0 x 163.4 x 8.75 mm",
                DisplaySizeInches = 6.9,
                DisplayResolution = "2868 x 1320，460 ppi",
                WeightGrams = 249,
                TypicalUseHours = 30,
                VideoPlaybackHours = 45,
                StreamedVideoPlaybackHours = 40,
                StorageOptionsGb = new[] { 256, 512, 1024, 2048 },
                CameraSummary = "48MP Pro Fusion，主相機可變光圈",
                FrontCameraSummary = ProFrontCamera,
                Chip = "A20 Pro",
                Finishes = new[] { "Black", "Silver", "Glacier", "Burgundy" }
            }
        },
        SharedFeatures = new[]
        {
            "Super Retina XDR 顯示器",
            "ProMotion 最高 120Hz",
            "IP68 防潑水、防水與防塵",
            "USB-C",
            "MagSafe",
            "Apple Intelligence 與 Siri AI（可用性依地區與語言而異）"
        },
        Source = new NewsSource
        {
            Announcement = NewsroomUrl,
            Product = ProductUrl,
            TechnicalSpecifications = SpecificationsUrl
        },
        OfficialLinks = new[]
        {
            new OfficialLink
            {
                Key = "newsroom",
                Label = "Apple 新聞稿",
                Url = NewsroomUrl
            },
            new OfficialLink
            {
                Key = "product",
                Label = "Apple iPhone 18 Pro 產品頁",
                Url = ProductUrl
            },
            new OfficialLink
            {
                Key = "specifications",
                Label = "Apple 技術規格",
                Url = SpecificationsUrl
            },
            new OfficialLink
            {
                Key = "webmcp",
                Label = "WebMCP 官方規格",
                Url = WebMcpUrl
            }
        }
    };

    private const string DuoAnnouncementUrl = "https://www.apple.com/newsroom/2026/09/apple-unveils-iphone-duo/";
    private const string DuoTaiwanNewsUrl = "https://www.apple.com/tw/newsroom/2026/10/open-up-to-new-possibilities-with-iphone-duo/";
    private const string DuoProductUrl = "https://www.apple.com/tw/iphone-duo/";
    private const string DuoSpecificationsUrl = "https://www.apple.com/tw/iphone-duo/specs/";

    /// <summary>
    /// 取得 Duo 新聞、台灣上市時程與內外螢幕的個別規格。
    /// </summary>
    public static NewsArticle DuoArticle { get; } = new()
    {
        Title = "iPhone Duo",
        AnnouncementDate = "2026-09-09",
        LastVerifiedDate = "2026-11-14",
        Summary = "iPhone Duo 採用鈦金屬可摺疊設計，搭載 A20 Pro，已在台灣開賣。",
        Highlights =
        [
            new() { Key = "foldable", Title = "可摺疊雙螢幕", Description = "展開可使用內螢幕進行多工，闔起時可透過外螢幕操作。兩個螢幕均支援 USB-C Apple Pencil。" },
            new() { Key = "touch-id", Title = "側邊 Touch ID", Description = "側邊按鈕整合指紋辨識感測器。" }
        ],
        Release = new ReleaseSchedule
        {
            PreorderStart = "2026-10-16T20:00:00+08:00",
            PreorderDisplay = "2026/10/16 20:00（台灣時間）",
            AvailabilityStart = "2026-10-23",
            TimezoneNote = "預訂與開賣時程依 Apple 台灣公告。"
        },
        Models =
        [
            new()
            {
                Id = "iphone-duo",
                Name = "iPhone Duo",
                Dimensions = "展開 164.6 × 117.8 × 5.2 mm；闔起 84.1 × 117.8 × 11.3 mm",
                WeightGrams = 254,
                TypicalUseHours = 24,
                StorageOptionsGb = [256, 512, 1024, 2048],
                CameraSummary = "48 MP Fusion 主相機與 48 MP Fusion 超廣角；外螢幕配備 12 MP Center Stage 前置相機，內螢幕配備螢幕下 FaceTime 相機",
                Chip = "A20 Pro",
                Finishes = ["夜空色", "星光白色"],
                Displays =
                [
                    new() { Id = "inner", Label = "內螢幕", DisplaySizeInches = 7.6, DisplayResolution = "1878 × 2670，430 ppi", VideoPlaybackHours = 31, StreamedVideoPlaybackHours = 26 },
                    new() { Id = "outer", Label = "外螢幕", DisplaySizeInches = 5.4, DisplayResolution = "1398 × 2034，460 ppi", VideoPlaybackHours = 44, StreamedVideoPlaybackHours = 37 }
                ]
            }
        ],
        Source = new NewsSource
        {
            Announcement = DuoAnnouncementUrl,
            Product = DuoProductUrl,
            TechnicalSpecifications = DuoSpecificationsUrl
        },
        OfficialLinks =
        [
            new() { Key = "duo-announcement", Label = "Apple iPhone Duo 發表新聞稿", Url = DuoAnnouncementUrl },
            new() { Key = "duo-taiwan-news", Label = "Apple 台灣 iPhone Duo 預訂公告", Url = DuoTaiwanNewsUrl },
            new() { Key = "duo-product", Label = "Apple 台灣 iPhone Duo 產品頁", Url = DuoProductUrl },
            new() { Key = "duo-specifications", Label = "Apple 台灣 iPhone Duo 技術規格", Url = DuoSpecificationsUrl }
        ]
    };

    /// <summary>
    /// 使用 News 頁面既定的 JSON 設定序列化新聞資料，供 Razor 輸出給前端 WebMCP 腳本。
    /// </summary>
    /// <param name="article">要序列化的新聞資料。</param>
    /// <returns>使用 camelCase 命名且適合嵌入 JSON script 的 JSON 字串。</returns>
    public static string Serialize(NewsArticle article) =>
        JsonSerializer.Serialize(article, JsonOptions);
}

/// <summary>
/// 描述 News 頁面的完整新聞內容，供 Razor 與 WebMCP 共用。
/// </summary>
public sealed class NewsArticle
{
    public string Title { get; init; } = string.Empty;
    public string AnnouncementDate { get; init; } = string.Empty;
    public string LastVerifiedDate { get; init; } = string.Empty;
    public string Summary { get; init; } = string.Empty;
    public string ScopeNote { get; init; } = string.Empty;
    public IReadOnlyList<NewsHighlight> Highlights { get; init; } = Array.Empty<NewsHighlight>();
    public ReleaseSchedule Release { get; init; } = new();
    public IReadOnlyList<ProductModel> Models { get; init; } = Array.Empty<ProductModel>();
    public IReadOnlyList<string> SharedFeatures { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 提供新聞對應的 Apple 原始資料來源；WebMCP 規格連結則由 <see cref="OfficialLinks" /> 另外提供。
    /// </summary>
    public NewsSource Source { get; init; } = new();

    /// <summary>
    /// 提供頁面顯示及 WebMCP 回傳使用的官方連結清單。
    /// </summary>
    public IReadOnlyList<OfficialLink> OfficialLinks { get; init; } = Array.Empty<OfficialLink>();
}

/// <summary>
/// 描述新聞摘要中的單一產品亮點。
/// </summary>
public sealed class NewsHighlight
{
    public string Key { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
}

/// <summary>
/// 描述預購與供貨時程；同時保留機器可解析的時間與頁面顯示用文字。
/// </summary>
public sealed class ReleaseSchedule
{
    /// <summary>
    /// 取得包含時區偏移量的預購開始時間，供結構化資料使用。
    /// </summary>
    public string PreorderStart { get; init; } = string.Empty;

    /// <summary>
    /// 取得已依頁面需求格式化的預購開始時間文字。
    /// </summary>
    public string PreorderDisplay { get; init; } = string.Empty;
    public string AvailabilityStart { get; init; } = string.Empty;
    public string TimezoneNote { get; init; } = string.Empty;
}

/// <summary>
/// 描述單一 iPhone 產品的比較規格。
/// </summary>
public sealed class ProductModel
{
    public string Id { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Dimensions { get; init; } = string.Empty;
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public double? DisplaySizeInches { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public string? DisplayResolution { get; init; }
    public int WeightGrams { get; init; }
    public int TypicalUseHours { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? VideoPlaybackHours { get; init; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public int? StreamedVideoPlaybackHours { get; init; }
    public IReadOnlyList<int> StorageOptionsGb { get; init; } = Array.Empty<int>();
    public string CameraSummary { get; init; } = string.Empty;
    // 既有 Pro 工具的 CameraSummary 沿用原契約，頁面則補充前置相機差異。
    [JsonIgnore]
    public string? FrontCameraSummary { get; init; }
    public string Chip { get; init; } = string.Empty;
    public IReadOnlyList<string> Finishes { get; init; } = Array.Empty<string>();

    /// <summary>
    /// 雙螢幕機型的獨立資料。單螢幕機型沿用既有的平面欄位。
    /// </summary>
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public IReadOnlyList<ProductDisplay>? Displays { get; init; }
}

/// <summary>
/// 表示一塊螢幕的尺寸、解析度和 Apple 測試的最長播放時間。
/// </summary>
public sealed class ProductDisplay
{
    public required string Id { get; init; }
    public required string Label { get; init; }
    public required double DisplaySizeInches { get; init; }
    public required string DisplayResolution { get; init; }
    public required int VideoPlaybackHours { get; init; }
    public required int StreamedVideoPlaybackHours { get; init; }
}

/// <summary>
/// 保存 Apple 新聞稿、產品頁與技術規格頁的原始來源網址。
/// </summary>
public sealed class NewsSource
{
    public string Announcement { get; init; } = string.Empty;
    public string Product { get; init; } = string.Empty;
    public string TechnicalSpecifications { get; init; } = string.Empty;
}

/// <summary>
/// 描述頁面及 WebMCP 可使用的單一官方連結。
/// </summary>
public sealed class OfficialLink
{
    public string Key { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public string Url { get; init; } = string.Empty;
}
