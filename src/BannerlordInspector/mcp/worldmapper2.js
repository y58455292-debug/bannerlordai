import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const BASE = "http://127.0.0.1:8420";

async function getJson(route, params = {}) {
    const url = new URL(route, BASE);

    for (const [key, value] of Object.entries(params)) {
        if (value !== null && value !== undefined) {
            url.searchParams.set(key, String(value));
        }
    }

    const response = await fetch(url);

    if (!response.ok) {
        throw new Error(
            `${response.status} ${response.statusText} for ${url}`
        );
    }

    return response.json();
}

async function evaluate(evalPath) {
    return getJson("/eval", {
        path: evalPath
    });
}

async function read(evalPath) {
    try {
        const result = await evaluate(evalPath);
        return result?.value ?? null;
    } catch {
        return null;
    }
}

async function readMembers(evalPath) {
    try {
        const result = await evaluate(
            `${evalPath}.$members`
        );

        return result?.members ?? [];
    } catch {
        return [];
    }
}

async function inspectType(typeName) {
    try {
        return await getJson("/members", {
            type: typeName
        });
    } catch (error) {
        return {
            type: typeName,
            error: String(error)
        };
    }
}

function cleanEnum(value) {
    if (
        value &&
        typeof value === "object" &&
        "value" in value
    ) {
        return value.value;
    }

    return value;
}

function timestamp() {
    return new Date()
        .toISOString()
        .replace(/:/g, "-")
        .replace(/\..+/, "");
}

function classifyModel(name, runtimeType) {
    const text =
        `${name} ${runtimeType ?? ""}`.toLowerCase();

    const rules = [
        [
            "economy_trade",
            [
                "econom",
                "trade",
                "price",
                "workshop",
                "production",
                "caravan",
                "tax",
                "valuation"
            ]
        ],

        [
            "settlements",
            [
                "settlement",
                "village",
                "prosperity",
                "loyalty",
                "security",
                "garrison",
                "militia",
                "food"
            ]
        ],

        [
            "warfare",
            [
                "battle",
                "combat",
                "siege",
                "raid",
                "army",
                "troop",
                "casualty",
                "wallhit",
                "prisoner",
                "ransom"
            ]
        ],

        [
            "dynasty_heroes",
            [
                "hero",
                "heir",
                "pregnancy",
                "romance",
                "marriage",
                "death",
                "character",
                "age"
            ]
        ],

        [
            "kingdom_diplomacy",
            [
                "kingdom",
                "clan",
                "diplom",
                "relation",
                "vassal",
                "emissary",
                "persuasion",
                "influence",
                "policy"
            ]
        ],

        [
            "parties_movement",
            [
                "party",
                "movement",
                "map",
                "encounter",
                "targetscore",
                "ferry"
            ]
        ],

        [
            "naval",
            [
                "naval",
                "ship",
                "fleet",
                "sea"
            ]
        ],

        [
            "progression",
            [
                "xp",
                "progression",
                "skill",
                "perk",
                "volunteer",
                "recruit"
            ]
        ]
    ];

    const categories = [];

    for (const [category, words] of rules) {
        if (
            words.some(word =>
                text.includes(word)
            )
        ) {
            categories.push(category);
        }
    }

    if (categories.length === 0) {
        categories.push("other");
    }

    return categories;
}

async function main() {
    console.log(
        "Bannerlord World Mapper v0.2"
    );

    console.log(
        "============================"
    );

    console.log("");

    const modelRoot =
        "type:TaleWorlds.CampaignSystem.Campaign.Current._gameModels";

    console.log(
        "Reading active model registry..."
    );

    const registry =
        await readMembers(modelRoot);

    const modelProperties =
        registry.filter(
            member =>
                member.kind === "property"
        );

    console.log(
        `Found ${modelProperties.length} active models.`
    );

    console.log("");

    const models = [];

    for (
        let i = 0;
        i < modelProperties.length;
        i++
    ) {
        const model =
            modelProperties[i];

        const modelPath =
            `${modelRoot}.${model.name}`;

        const runtimeType =
            await read(
                `${modelPath}.$type`
            );

        console.log(
            `[${i + 1}/${modelProperties.length}] ${model.name}`
        );

        console.log(
            `    runtime: ${runtimeType ?? "UNKNOWN"}`
        );

        let structure = null;

        if (runtimeType) {
            structure =
                await inspectType(runtimeType);
        }

        const methods =
            structure?.methods ?? [];

        const properties =
            structure?.properties ?? [];

        const fields =
            structure?.fields ?? [];

        const queryableMethods =
            methods.filter(
                method =>
                    method.queryable === true
            );

        const categories =
            classifyModel(
                model.name,
                runtimeType
            );

        console.log(
            `    methods: ${methods.length}`
        );

        console.log(
            `    queryable: ${queryableMethods.length}`
        );

        console.log(
            `    properties: ${properties.length}`
        );

        console.log(
            `    fields: ${fields.length}`
        );

        console.log(
            `    category: ${categories.join(", ")}`
        );

        models.push({
            name: model.name,

            declaredType:
                model.type,

            runtimeType,

            categories,

            assembly:
                structure?.assembly ?? null,

            baseType:
                structure?.baseType ?? null,

            interfaces:
                structure?.interfaces ?? [],

            counts:
                structure?.counts ?? null,

            properties,

            fields,

            methods,

            queryableMethods
        });
    }

    console.log("");
    console.log(
        "Reading campaign clock..."
    );

    const timeRoot =
        "type:TaleWorlds.CampaignSystem.CampaignTime.Now";

    const campaignTime = {
        year:
            await read(
                `${timeRoot}.GetYear`
            ),

        dayOfYear:
            await read(
                `${timeRoot}.GetDayOfYear`
            ),

        season:
            cleanEnum(
                await read(
                    `${timeRoot}.GetSeasonOfYear`
                )
            ),

        hour:
            await read(
                `${timeRoot}.GetHourOfDay`
            ),

        ticks:
            await read(
                `${timeRoot}.CurrentTicks`
            )
    };

    const categorySummary = {};

    let totalMethods = 0;
    let totalQueryable = 0;
    let totalProperties = 0;
    let totalFields = 0;

    for (const model of models) {
        totalMethods +=
            model.methods.length;

        totalQueryable +=
            model.queryableMethods.length;

        totalProperties +=
            model.properties.length;

        totalFields +=
            model.fields.length;

        for (
            const category
            of model.categories
        ) {
            categorySummary[category] =
                (categorySummary[category] ?? 0) + 1;
        }
    }

    const output = {
        mapperVersion: "0.2",

        capturedAtRealTime:
            new Date().toISOString(),

        campaignTime,

        summary: {
            modelCount:
                models.length,

            totalMethods,

            totalQueryableMethods:
                totalQueryable,

            totalProperties,

            totalFields,

            categories:
                categorySummary
        },

        models
    };

    const directory =
        path.join(
            __dirname,
            "worldmaps"
        );

    fs.mkdirSync(
        directory,
        {
            recursive: true
        }
    );

    const filename =
        `rules_${timestamp()}.json`;

    const outputPath =
        path.join(
            directory,
            filename
        );

    fs.writeFileSync(
        outputPath,
        JSON.stringify(
            output,
            null,
            2
        ),
        "utf8"
    );

    console.log("");
    console.log(
        "================================"
    );

    console.log(
        "WORLD RULE MAP COMPLETE"
    );

    console.log(
        "================================"
    );

    console.log(
        `Models:            ${models.length}`
    );

    console.log(
        `Methods:           ${totalMethods}`
    );

    console.log(
        `Queryable methods: ${totalQueryable}`
    );

    console.log(
        `Properties:        ${totalProperties}`
    );

    console.log(
        `Fields:            ${totalFields}`
    );

    console.log("");
    console.log(
        "CATEGORY SUMMARY"
    );

    for (
        const [category, count]
        of Object.entries(categorySummary)
            .sort(
                (a, b) =>
                    b[1] - a[1]
            )
    ) {
        console.log(
            `${category.padEnd(22)} ${count}`
        );
    }

    console.log("");
    console.log("RULE MAP SAVED:");
    console.log(outputPath);
}

main().catch(error => {
    console.error("");
    console.error(
        "WORLD MAPPER ERROR"
    );

    console.error(error);
});