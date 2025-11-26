const sampleInput = [
    {
        "categoryName": "TVs",
        "subCategories": [
            {"categoryId": 80, "categoryName": "TVs"},
            {"categoryId": 948, "categoryName": "All-Weather TVs"},
            {"categoryId": 37, "categoryName": "TV Accessories"}
        ]
    }
];

const inputEl = document.getElementById("inputJson");
const outputEl = document.getElementById("outputJson");
const statusEl = document.getElementById("status");
const buttonEl = document.getElementById("generateButton");

// Prefill the textarea with a sample payload on first load.
inputEl.value = JSON.stringify(sampleInput, null, 2);

async function generateAttributes() {
    const raw = inputEl.value.trim();
    if (!raw) {
        statusEl.textContent = "Please provide a non-empty JSON payload.";
        statusEl.className = "status error";
        return;
    }

    let parsed;
    try {
        parsed = JSON.parse(raw);
    } catch (err) {
        statusEl.textContent = "Client-side JSON parsing failed: " + err;
        statusEl.className = "status error";
        return;
    }

    buttonEl.disabled = true;
    statusEl.textContent = "Calling backend and OpenAI…";
    statusEl.className = "status";

    const start = performance.now();
    try {
        const response = await fetch("/api/category-attributes", {
            method: "POST",
            headers: {
                "Content-Type": "application/json"
            },
            body: JSON.stringify(parsed)
        });

        const text = await response.text();
        let data;
        try {
            data = JSON.parse(text);
        } catch {
            outputEl.textContent = text;
            statusEl.textContent = "Backend returned a non-JSON response.";
            statusEl.className = "status error";
            return;
        }

        if (!response.ok) {
            statusEl.textContent = data.message || "Backend returned an error.";
            statusEl.className = "status error";
        } else {

            const elapsedMs = performance.now() - start;
            statusEl.textContent = `Successfully generated attributes in ${elapsedMs.toLocaleString()} ms`;
            statusEl.className = "status success";
        }

        outputEl.textContent = JSON.stringify(data, null, 2);
    } catch (err) {
        statusEl.textContent = "Network error while calling backend: " + err;
        statusEl.className = "status error";
    } finally {
        buttonEl.disabled = false;
    }
}

buttonEl.addEventListener("click", () => {
    generateAttributes();
});