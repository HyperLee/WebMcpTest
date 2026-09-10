(() => {
    const modelContext = document.modelContext;
    if (!modelContext) {
        return;
    }

    const dataElement = document.getElementById("news-data");
    if (!dataElement) {
        console.warn("WebMCP News data element is unavailable.");
        return;
    }

    let newsData;
    try {
        newsData = JSON.parse(dataElement.textContent || "{}");
    } catch (error) {
        console.warn("WebMCP News data could not be parsed.", error);
        return;
    }

    const allowedModels = new Set(["all", "iphone-18-pro", "iphone-18-pro-max"]);
    const abortedResult = () => ({
        error: { code: "aborted", message: "工具執行已取消。" }
    });
    const invalidModelResult = () => ({
        error: { code: "invalid_model", message: "model 必須是 all、iphone-18-pro 或 iphone-18-pro-max。" }
    });
    const invalidInputResult = (message) => ({
        error: { code: "invalid_input", message }
    });
    const clone = (value) => JSON.parse(JSON.stringify(value));
    const isObject = (value) => value !== null && typeof value === "object" && !Array.isArray(value);
    const hasUnexpectedKeys = (value, allowedKeys) =>
        Object.keys(value).some((key) => !allowedKeys.includes(key));

    const executeWithAbort = (signal, work) => new Promise((resolve) => {
        let completed = false;

        const finish = (result) => {
            if (completed) {
                return;
            }

            completed = true;
            signal?.removeEventListener("abort", onAbort);
            resolve(result);
        };

        const onAbort = () => finish(abortedResult());

        if (signal?.aborted) {
            finish(abortedResult());
            return;
        }

        signal?.addEventListener("abort", onAbort, { once: true });
        Promise.resolve()
            .then(() => {
                if (signal?.aborted) {
                    finish(abortedResult());
                    return;
                }

                finish(work());
            })
            .catch((error) => {
                console.warn("WebMCP News tool execution failed.", error);
                finish({ error: { code: "execution_failed", message: "工具執行失敗。" } });
            });
    });

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

    const overviewTool = {
        name: "get-iphone18-pro-overview",
        title: "查詢 iPhone 18 Pro 系列摘要",
        description: "讀取本頁固定的 iPhone 18 Pro 系列官方新聞摘要與機型資料。",
        inputSchema: overviewSchema,
        annotations: readOnlyAnnotations,
        execute: (inputObject, options) => executeWithAbort(options?.signal, () => {
            const input = inputObject ?? {};
            if (!isObject(input)) {
                return invalidInputResult("輸入必須是物件。");
            }

            if (hasUnexpectedKeys(input, ["model"])) {
                return invalidInputResult("輸入包含未定義欄位。");
            }

            const model = input.model === undefined ? "all" : input.model;
            if (!allowedModels.has(model)) {
                return invalidModelResult();
            }

            return {
                type: "iphone18-pro-overview",
                announcementDate: newsData.announcementDate,
                lastVerifiedDate: newsData.lastVerifiedDate,
                model,
                summary: newsData.summary,
                highlights: clone(newsData.highlights),
                release: clone(newsData.release),
                models: model === "all"
                    ? clone(newsData.models)
                    : clone(newsData.models.filter((item) => item.id === model)),
                officialLinks: clone(newsData.officialLinks)
            };
        })
    };

    const comparisonTool = {
        name: "compare-iphone18-pro-models",
        title: "比較 iPhone 18 Pro 與 Pro Max",
        description: "讀取本頁固定的 iPhone 18 Pro 與 iPhone 18 Pro Max 官方比較資料。",
        inputSchema: comparisonSchema,
        annotations: readOnlyAnnotations,
        execute: (inputObject, options) => executeWithAbort(options?.signal, () => {
            const input = inputObject ?? {};
            if (!isObject(input)) {
                return invalidInputResult("輸入必須是物件。");
            }

            if (Object.keys(input).length > 0) {
                return invalidInputResult("比較工具不接受輸入欄位。");
            }

            return {
                type: "iphone18-pro-comparison",
                criteria: [
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
                models: clone(newsData.models),
                officialLinks: clone(newsData.officialLinks)
            };
        })
    };

    const register = (tool) => {
        try {
            const registration = modelContext.registerTool(tool);
            return registration?.catch((error) => {
                console.warn(`WebMCP tool registration failed: ${tool.name}`, error);
            });
        } catch (error) {
            console.warn(`WebMCP tool registration failed: ${tool.name}`, error);
            return undefined;
        }
    };

    void register(overviewTool);
    void register(comparisonTool);
})();
