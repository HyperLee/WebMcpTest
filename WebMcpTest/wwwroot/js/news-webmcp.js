(() => {
    const initializeComparisonView = () => {
        const section = document.getElementById("comparison-heading")?.parentElement;
        const controls = section?.querySelector(".news-comparison-controls");
        const table = section?.querySelector(".news-comparison-table");
        const modelOptions = controls?.querySelector('[data-comparison-options="models"]');
        const criterionOptions = controls?.querySelector('[data-comparison-options="criteria"]');
        const reset = controls?.querySelector("[data-comparison-reset]");
        const status = controls?.querySelector("[data-comparison-status]");
        const sharedFeatures = table?.querySelector('[data-comparison-criterion="sharedFeatures"] td');
        if (!table || !modelOptions || !criterionOptions || !reset || !status || !sharedFeatures) {
            return null;
        }

        const modelHeaders = Array.from(table.querySelectorAll("th[scope='col'][data-comparison-model]"));
        const modelCells = table.querySelectorAll("[data-comparison-model]");
        const criterionRows = Array.from(table.querySelectorAll("tr[data-comparison-criterion]"));
        if (modelHeaders.length === 0 || criterionRows.length === 0 ||
            criterionRows.some((row) => !row.querySelector("th[scope='row']"))) {
            return null;
        }

        const addChoice = (container, id, text) => {
            const label = document.createElement("label");
            const checkbox = document.createElement("input");
            checkbox.type = "checkbox";
            checkbox.value = id;
            checkbox.autocomplete = "off";
            label.append(checkbox, text.trim());
            container.append(label);
            return checkbox;
        };

        // 依表格順序建立選項，讓工具白名單、回傳順序與畫面使用同一組識別碼。
        const modelChoices = modelHeaders.map((header) =>
            addChoice(modelOptions, header.dataset.comparisonModel, header.textContent));
        const criterionChoices = criterionRows.map((row) =>
            addChoice(criterionOptions, row.dataset.comparisonCriterion, row.querySelector("th[scope='row']").textContent));
        const allModels = modelChoices.map((choice) => choice.value);
        const allCriteria = criterionChoices.map((choice) => choice.value);

        const updateChoices = (choices, selected) => {
            for (const choice of choices) {
                choice.checked = selected.includes(choice.value);
                choice.disabled = choice.checked && selected.length === 1;
            }
        };

        const apply = ({ models, criteria }) => {
            updateChoices(modelChoices, models);
            updateChoices(criterionChoices, criteria);

            for (const cell of modelCells) {
                cell.hidden = !models.includes(cell.dataset.comparisonModel);
            }
            for (const row of criterionRows) {
                row.hidden = !criteria.includes(row.dataset.comparisonCriterion);
            }

            sharedFeatures.colSpan = models.length;
            status.textContent = `顯示 ${models.length} 款機型、${criteria.length} 項規格`;
            return { models, criteria };
        };

        const selectedIds = (choices) => choices.filter((choice) => choice.checked).map((choice) => choice.value);
        const restore = () => apply({ models: allModels, criteria: allCriteria });
        controls.addEventListener("change", () => apply({
            models: selectedIds(modelChoices),
            criteria: selectedIds(criterionChoices)
        }));
        reset.addEventListener("click", restore);
        restore();
        controls.hidden = false;
        return { apply, allModels, allCriteria };
    };

    const comparisonView = initializeComparisonView();
    const modelContext = document.modelContext;
    if (typeof modelContext?.registerTool !== "function") {
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

    const register = async (tool) => {
        try {
            await modelContext.registerTool(tool);
        } catch (error) {
            console.warn(`WebMCP tool registration failed: ${tool.name}`, error);
        }
    };

    if (comparisonView) {
        const { apply, allModels, allCriteria } = comparisonView;
        const arraySchema = (ids, description) => ({
            type: "array",
            description,
            minItems: 1,
            uniqueItems: true,
            items: { type: "string", enum: ids }
        });
        const validSelection = (selection, allowed) =>
            Array.isArray(selection) &&
            selection.length > 0 &&
            new Set(selection).size === selection.length &&
            Array.from(selection).every((id) => allowed.includes(id));

        void register({
            name: "set-iphone18-comparison-view",
            title: "自訂 iPhone 18 Pro 規格比較畫面",
            description: "更新目前頁面比較表所顯示的機型與規格。省略任一欄位會顯示該組全部選項，傳入空物件可恢復完整比較表。",
            inputSchema: {
                type: "object",
                properties: {
                    models: arraySchema(allModels, "畫面要顯示的機型。省略時顯示兩款。"),
                    criteria: arraySchema(allCriteria, "畫面要顯示的規格。省略時顯示全部規格。")
                },
                additionalProperties: false
            },
            annotations: { readOnlyHint: false, consequentialHint: false, untrustedContentHint: false },
            execute: async (input, options) => {
                if (!isObject(input)) {
                    return invalidInputResult("輸入必須是物件，可指定 models 或 criteria。");
                }

                const unexpectedKeys = Object.keys(input).filter((key) => key !== "models" && key !== "criteria");
                if (unexpectedKeys.length > 0) {
                    return invalidInputResult(`未定義欄位：${unexpectedKeys.join("、")}。請只使用 models 或 criteria。`);
                }

                const models = Object.hasOwn(input, "models") ? input.models : allModels;
                const criteria = Object.hasOwn(input, "criteria") ? input.criteria : allCriteria;
                if (!validSelection(models, allModels)) {
                    return invalidInputResult("models 必須是非空陣列，並包含有效且不重複的機型識別碼。");
                }
                if (!validSelection(criteria, allCriteria)) {
                    return invalidInputResult("criteria 必須是非空陣列，並包含有效且不重複的規格識別碼。");
                }

                const selection = {
                    models: allModels.filter((id) => models.includes(id)),
                    criteria: allCriteria.filter((id) => criteria.includes(id))
                };
                // 取消檢查與畫面更新之間沒有等待點，成功回傳代表畫面已套用。
                if (options?.signal?.aborted) {
                    return abortedResult();
                }
                return apply(selection);
            }
        });
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

    void register(overviewTool);
    void register(comparisonTool);
})();
