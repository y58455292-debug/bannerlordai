import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const BASE = "http://127.0.0.1:8420";
const TARGET_ID = "town_ES7";

async function evaluate(evalPath) {
    const url = new URL("/eval", BASE);
    url.searchParams.set("path", evalPath);

    const response = await fetch(url);

    if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
    }

    return response.json();
}

function valueOf(result) {
    return result?.value ?? null;
}

async function read(evalPath) {
    try {
        const result = await evaluate(evalPath);
        return valueOf(result);
    } catch {
        return null;
    }
}

function cleanEnum(value) {
    if (
        value &&
        typeof value === "object" &&
        typeof value.value === "string"
    ) {
        return value.value;
    }

    return value;
}

async function findSettlement(id) {
    const countResult = await evaluate("Settlement.All.Count");
    const count = Number(valueOf(countResult));

    for (let i = 0; i < count; i++) {
        const result = await evaluate(
            `Settlement.All.[${i}].StringId`
        );

        if (valueOf(result) === id) {
            return i;
        }
    }

    return -1;
}

async function readExplanation(evalPath) {
    const countValue = await read(
        `${evalPath}._explainer.Lines.$count`
    );

    const count = Number(countValue ?? 0);
    const lines = [];

    for (let i = 0; i < count; i++) {
        const line =
            `${evalPath}._explainer.Lines.[${i}]`;

        lines.push({
            name: await read(`${line}.Name`),
            number: await read(`${line}.Number`),
            operationType: cleanEnum(
                await read(`${line}.OperationType`)
            )
        });
    }

    return {
        result: await read(`${evalPath}.ResultNumber`),
        base: await read(`${evalPath}.BaseNumber`),
        sumOfFactors: await read(
            `${evalPath}.SumOfFactors`
        ),
        lineCount: count,
        lines
    };
}

function printExplanation(title, explanation) {
    console.log("");
    console.log(title);
    console.log("-".repeat(title.length));

    if (!explanation || explanation.lineCount === 0) {
        console.log("No explanation lines found.");
        return;
    }

    for (const line of explanation.lines) {
        const number =
            typeof line.number === "number"
                ? line.number.toFixed(2)
                : String(line.number);

        const sign =
            typeof line.number === "number" &&
            line.number > 0
                ? "+"
                : "";

        console.log(
            `${String(line.name).padEnd(30)} ${sign}${number}`
        );
    }

    console.log(
        `${"RESULT".padEnd(30)} ${explanation.result}`
    );
}

function timestamp() {
    const now = new Date();
    const pad = value =>
        String(value).padStart(2, "0");

    return (
        now.getFullYear() +
        "-" +
        pad(now.getMonth() + 1) +
        "-" +
        pad(now.getDate()) +
        "_" +
        pad(now.getHours()) +
        "-" +
        pad(now.getMinutes()) +
        "-" +
        pad(now.getSeconds())
    );
}

function saveSnapshot(data) {
    const directory = path.join(
        __dirname,
        "snapshots"
    );

    fs.mkdirSync(directory, {
        recursive: true
    });

    const filename =
        `syronea_${timestamp()}.json`;

    const fullPath = path.join(
        directory,
        filename
    );

    fs.writeFileSync(
        fullPath,
        JSON.stringify(data, null, 2),
        "utf8"
    );

    return fullPath;
}

async function main() {
    console.log(
        "Bannerlord Economy Collector v0.5"
    );
    console.log(
        "--------------------------------"
    );

    console.log("Finding Syronea...");

    const index =
        await findSettlement(TARGET_ID);

    if (index < 0) {
        throw new Error(
            "Syronea not found."
        );
    }

    console.log(
        `Found Syronea at settlement index ${index}.`
    );

    const settlement =
        `Settlement.All.[${index}]`;

    const town =
        `${settlement}.Town`;

    const campaignTimePath =
        "type:TaleWorlds.CampaignSystem.CampaignTime.Now";

    console.log(
        "Reading campaign time..."
    );

    const campaignTime = {
        year: await read(
            `${campaignTimePath}.GetYear`
        ),

        dayOfYear: await read(
            `${campaignTimePath}.GetDayOfYear`
        ),

        season: cleanEnum(
            await read(
                `${campaignTimePath}.GetSeasonOfYear`
            )
        ),

        hour: await read(
            `${campaignTimePath}.GetHourOfDay`
        ),

        ticks: await read(
            `${campaignTimePath}.CurrentTicks`
        )
    };

    console.log(
        "Reading live economy..."
    );

    const data = {
        collectorVersion: "0.5",

        capturedAtRealTime:
            new Date().toISOString(),

        campaignTime,

        stringId: TARGET_ID,
        settlementIndex: index,

        prosperity:
            await read(`${town}.Prosperity`),

        prosperityChange:
            await read(
                `${town}.ProsperityChange`
            ),

        security:
            await read(`${town}.Security`),

        securityChange:
            await read(
                `${town}.SecurityChange`
            ),

        foodStocks:
            await read(`${town}.FoodStocks`),

        foodChange:
            await read(`${town}.FoodChange`),

        gold:
            await read(`${town}.Gold`),

        loyalty:
            await read(`${town}.Loyalty`),

        loyaltyChange:
            await read(
                `${town}.LoyaltyChange`
            ),

        construction:
            await read(
                `${town}.Construction`
            ),

        militia:
            await read(
                `${settlement}.Militia`
            ),

        militiaChange:
            await read(
                `${town}.MilitiaChange`
            ),

        tradeTaxAccumulated:
            await read(
                `${town}.TradeTaxAccumulated`
            ),

        workshopCount:
            await read(
                `${town}.Workshops.$count`
            ),

        villageCount:
            await read(
                `${town}.Villages.$count`
            ),

        tradeBoundVillageCount:
            await read(
                `${town}.TradeBoundVillages.$count`
            ),

        garrisonCount:
            await read(
                `${town}.GarrisonParty.MemberRoster.TotalManCount`
            ),

        isTown:
            await read(`${town}.IsTown`),

        isCastle:
            await read(`${town}.IsCastle`),

        isUnderSiege:
            await read(
                `${town}.IsUnderSiege`
            )
    };

    console.log(
        "Reading calculation explanations..."
    );

    data.explanations = {
        food:
            await readExplanation(
                `${town}.FoodChangeExplanation`
            ),

        prosperity:
            await readExplanation(
                `${town}.ProsperityChangeExplanation`
            ),

        security:
            await readExplanation(
                `${town}.SecurityChangeExplanation`
            ),

        loyalty:
            await readExplanation(
                `${town}.LoyaltyChangeExplanation`
            ),

        militia:
            await readExplanation(
                `${town}.MilitiaChangeExplanation`
            )
    };

    const savedPath =
        saveSnapshot(data);

    console.log("");
    console.log(
        "SYRONEA ECONOMIC BASELINE"
    );
    console.log(
        "========================="
    );

    console.log(
        `Campaign:        Year ${campaignTime.year}, ` +
        `Day ${campaignTime.dayOfYear}, ` +
        `${campaignTime.season}, ` +
        `Hour ${campaignTime.hour}`
    );

    console.log(
        `Campaign ticks:  ${campaignTime.ticks}`
    );

    console.log("");

    console.log(
        `Prosperity:      ${data.prosperity} (${data.prosperityChange})`
    );

    console.log(
        `Security:        ${data.security} (${data.securityChange})`
    );

    console.log(
        `Food:            ${data.foodStocks} (${data.foodChange})`
    );

    console.log(
        `Gold:            ${data.gold}`
    );

    console.log(
        `Loyalty:         ${data.loyalty} (${data.loyaltyChange})`
    );

    console.log(
        `Militia:         ${data.militia} (${data.militiaChange})`
    );

    console.log(
        `Garrison:        ${data.garrisonCount}`
    );

    console.log(
        `Trade tax:       ${data.tradeTaxAccumulated}`
    );

    console.log(
        `Workshops:       ${data.workshopCount}`
    );

    console.log(
        `Villages:        ${data.villageCount}`
    );

    console.log(
        `Trade villages:  ${data.tradeBoundVillageCount}`
    );

    printExplanation(
        "FOOD CHANGE",
        data.explanations.food
    );

    printExplanation(
        "PROSPERITY CHANGE",
        data.explanations.prosperity
    );

    printExplanation(
        "SECURITY CHANGE",
        data.explanations.security
    );

    printExplanation(
        "LOYALTY CHANGE",
        data.explanations.loyalty
    );

    printExplanation(
        "MILITIA CHANGE",
        data.explanations.militia
    );

    console.log("");
    console.log("SNAPSHOT SAVED");
    console.log(savedPath);
}

main().catch(error => {
    console.error("");
    console.error("COLLECTOR ERROR");
    console.error(error);
});