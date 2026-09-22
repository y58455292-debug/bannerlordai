import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const BASE = "http://127.0.0.1:8420";

async function evaluate(evalPath) {
    const url = new URL("/eval", BASE);
    url.searchParams.set("path", evalPath);

    const response = await fetch(url);

    if (!response.ok) {
        throw new Error(`HTTP ${response.status}`);
    }

    return response.json();
}

async function read(evalPath) {
    try {
        const result = await evaluate(evalPath);
        return result?.value ?? null;
    } catch {
        return null;
    }
}

async function members(evalPath) {
    try {
        const result = await evaluate(
            `${evalPath}.$members`
        );

        return result?.members ?? [];
    } catch {
        return [];
    }
}

function timestamp() {
    const now = new Date();

    return now
        .toISOString()
        .replace(/:/g, "-")
        .replace(/\..+/, "");
}

async function main() {
    console.log("Bannerlord World Mapper v0.1");
    console.log("============================");
    console.log("");

    const root =
        "type:TaleWorlds.CampaignSystem.Campaign.Current._gameModels";

    console.log("Reading campaign model registry...");

    const modelMembers =
        await members(root);

    const modelProperties =
        modelMembers.filter(
            member =>
                member.kind === "property"
        );

    console.log(
        `Found ${modelProperties.length} model properties.`
    );

    console.log("");
    console.log("Mapping runtime implementations...");
    console.log("");

    const models = [];

    let completed = 0;

    for (const model of modelProperties) {
        const modelPath =
            `${root}.${model.name}`;

        const runtimeType =
            await read(`${modelPath}.$type`);

        models.push({
            name: model.name,
            declaredType: model.type,
            runtimeType
        });

        completed++;

        console.log(
            `[${completed}/${modelProperties.length}] ` +
            `${model.name}`
        );

        console.log(
            `    ${runtimeType ?? "UNKNOWN"}`
        );
    }

    const campaignTimeRoot =
        "type:TaleWorlds.CampaignSystem.CampaignTime.Now";

    const campaignTime = {
        year:
            await read(
                `${campaignTimeRoot}.GetYear`
            ),

        dayOfYear:
            await read(
                `${campaignTimeRoot}.GetDayOfYear`
            ),

        season:
            await read(
                `${campaignTimeRoot}.GetSeasonOfYear`
            ),

        hour:
            await read(
                `${campaignTimeRoot}.GetHourOfDay`
            ),

        ticks:
            await read(
                `${campaignTimeRoot}.CurrentTicks`
            )
    };

    const output = {
        mapperVersion: "0.1",

        capturedAtRealTime:
            new Date().toISOString(),

        campaignTime,

        modelCount: models.length,

        models
    };

    const directory =
        path.join(
            __dirname,
            "worldmaps"
        );

    fs.mkdirSync(
        directory,
        { recursive: true }
    );

    const filename =
        `models_${timestamp()}.json`;

    const outputPath =
        path.join(
            directory,
            filename
        );

    fs.writeFileSync(
        outputPath,
        JSON.stringify(output, null, 2),
        "utf8"
    );

    console.log("");
    console.log("============================");
    console.log("WORLD MODEL MAP COMPLETE");
    console.log("============================");

    console.log(
        `Models mapped: ${models.length}`
    );

    console.log("");
    console.log("MAP SAVED:");
    console.log(outputPath);
}

main().catch(error => {
    console.error("");
    console.error("WORLD MAPPER ERROR");
    console.error(error);
});