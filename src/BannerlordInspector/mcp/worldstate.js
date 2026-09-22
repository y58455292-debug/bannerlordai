import fs from "node:fs";
import path from "node:path";
import { fileURLToPath } from "node:url";

const __filename = fileURLToPath(import.meta.url);
const __dirname = path.dirname(__filename);

const BASE = "http://127.0.0.1:8420";
const ROOT = "Settlement.All";

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

function cleanValue(value) {
    if (
        value &&
        typeof value === "object" &&
        "value" in value
    ) {
        return value.value;
    }

    return value;
}

function textValue(value) {
    value = cleanValue(value);

    if (value === null || value === undefined) {
        return null;
    }

    if (typeof value === "string") {
        return value;
    }

    if (typeof value === "number" ||
        typeof value === "boolean") {
        return value;
    }

    return value;
}

function timestamp() {
    return new Date()
        .toISOString()
        .replace(/:/g, "-")
        .replace(/\..+/, "");
}

async function campaignClock() {
    const root =
        "type:TaleWorlds.CampaignSystem.CampaignTime.Now";

    return {
        year: await read(`${root}.GetYear`),
        dayOfYear: await read(`${root}.GetDayOfYear`),
        season: cleanValue(
            await read(`${root}.GetSeasonOfYear`)
        ),
        hour: await read(`${root}.GetHourOfDay`),
        ticks: await read(`${root}.CurrentTicks`)
    };
}

async function basicSettlement(index) {
    const s = `${ROOT}.[${index}]`;

    const [
        stringId,
        name,
        isTown,
        isCastle,
        isVillage,
        isHideout,
        owner,
        faction,
        culture,
        isUnderSiege,
        isUnderRaid
    ] = await Promise.all([
        read(`${s}.StringId`),
        read(`${s}.Name`),
        read(`${s}.IsTown`),
        read(`${s}.IsCastle`),
        read(`${s}.IsVillage`),
        read(`${s}.IsHideout`),
        read(`${s}.Owner.Name`),
        read(`${s}.MapFaction.Name`),
        read(`${s}.Culture.Name`),
        read(`${s}.IsUnderSiege`),
        read(`${s}.IsUnderRaid`)
    ]);

    let type = "other";

    if (isTown) type = "town";
    else if (isCastle) type = "castle";
    else if (isVillage) type = "village";
    else if (isHideout) type = "hideout";

    return {
        index,
        stringId,
        name: textValue(name),
        type,
        owner: textValue(owner),
        faction: textValue(faction),
        culture: textValue(culture),
        isUnderSiege,
        isUnderRaid
    };
}

async function townOrCastleState(index) {
    const settlement = `${ROOT}.[${index}]`;
    const town = `${settlement}.Town`;

    return {
        prosperity:
            await read(`${town}.Prosperity`),

        prosperityChange:
            await read(`${town}.ProsperityChange`),

        foodStocks:
            await read(`${town}.FoodStocks`),

        foodChange:
            await read(`${town}.FoodChange`),

        security:
            await read(`${town}.Security`),

        securityChange:
            await read(`${town}.SecurityChange`),

        loyalty:
            await read(`${town}.Loyalty`),

        loyaltyChange:
            await read(`${town}.LoyaltyChange`),

        militia:
            await read(`${settlement}.Militia`),

        militiaChange:
            await read(`${town}.MilitiaChange`),

        garrison:
            await read(
                `${town}.GarrisonParty.MemberRoster.TotalManCount`
            ),

        gold:
            await read(`${town}.Gold`),

        tradeTaxAccumulated:
            await read(`${town}.TradeTaxAccumulated`),

        workshopCount:
            await read(`${town}.Workshops.$count`),

        villageCount:
            await read(`${town}.Villages.$count`),

        tradeBoundVillageCount:
            await read(
                `${town}.TradeBoundVillages.$count`
            ),

        construction:
            await read(`${town}.Construction`)
    };
}

async function villageState(index) {
    const settlement = `${ROOT}.[${index}]`;
    const village = `${settlement}.Village`;

    const productionCount =
        Number(
            await read(
                `${village}.VillageType.Productions.$count`
            )
        ) || 0;

    const productions = [];

    for (let i = 0; i < productionCount; i++) {
        const p =
            `${village}.VillageType.Productions.[${i}]`;

        productions.push({
            index: i,

            stringId:
                await read(`${p}.StringId`),

            name:
                textValue(
                    await read(`${p}.Name`)
                )
        });
    }

    return {
        hearth:
            await read(`${village}.Hearth`),

        hearthChange:
            await read(`${village}.HearthChange`),

        militia:
            await read(`${village}.Militia`),

        militiaChange:
            await read(`${village}.MilitiaChange`),

        gold:
            await read(`${village}.Gold`),

        tradeTaxAccumulated:
            await read(
                `${village}.TradeTaxAccumulated`
            ),

        villageState:
            cleanValue(
                await read(`${village}.VillageState`)
            ),

        villageTypeId:
            await read(
                `${village}.VillageType.StringId`
            ),

        villageTypeName:
            textValue(
                await read(
                    `${village}.VillageType.Name`
                )
            ),

        primaryProductionId:
            await read(
                `${village}.VillageType.PrimaryProduction.StringId`
            ),

        primaryProductionName:
            textValue(
                await read(
                    `${village}.VillageType.PrimaryProduction.Name`
                )
            ),

        tradeBoundId:
            await read(
                `${village}.TradeBound.StringId`
            ),

        tradeBoundName:
            textValue(
                await read(
                    `${village}.TradeBound.Name`
                )
            ),

        boundSettlementId:
            await read(
                `${village}.Bound.StringId`
            ),

        boundSettlementName:
            textValue(
                await read(
                    `${village}.Bound.Name`
                )
            ),

        productions
    };
}

async function hideoutState(index) {
    const settlement = `${ROOT}.[${index}]`;

    return {
        faction:
            textValue(
                await read(`${settlement}.MapFaction.Name`)
            ),

        owner:
            textValue(
                await read(`${settlement}.Owner.Name`)
            ),

        isActive:
            await read(`${settlement}.IsActive`),

        isVisible:
            await read(`${settlement}.IsVisible`)
    };
}

function printProgress(current, total, item) {
    const name =
        item.name ??
        item.stringId ??
        "unknown";

    console.log(
        `[${current}/${total}] ` +
        `${item.type.padEnd(8)} ` +
        `${name}`
    );
}

async function main() {
    console.log("Bannerlord World State Mapper v0.1");
    console.log("=================================");
    console.log("");

    console.log("Reading campaign clock...");

    const clock = await campaignClock();

    console.log(
        `Campaign: Year ${clock.year}, ` +
        `Day ${clock.dayOfYear}, ` +
        `${clock.season}, Hour ${clock.hour}`
    );

    console.log("");

    console.log("Counting settlement objects...");

    const total =
        Number(await read(`${ROOT}.Count`));

    if (!Number.isFinite(total) || total <= 0) {
        throw new Error(
            `Invalid Settlement.All.Count: ${total}`
        );
    }

    console.log(
        `Settlement objects reported: ${total}`
    );

    console.log("");
    console.log("Scanning world...");
    console.log("");

    const settlements = [];

    const counts = {
        town: 0,
        castle: 0,
        village: 0,
        hideout: 0,
        other: 0
    };

    const errors = [];

    for (let i = 0; i < total; i++) {
        try {
            const item =
                await basicSettlement(i);

            counts[item.type] =
                (counts[item.type] ?? 0) + 1;

            printProgress(
                i + 1,
                total,
                item
            );

            if (
                item.type === "town" ||
                item.type === "castle"
            ) {
                item.state =
                    await townOrCastleState(i);
            }

            if (item.type === "village") {
                item.state =
                    await villageState(i);
            }

            if (item.type === "hideout") {
                item.state =
                    await hideoutState(i);
            }

            settlements.push(item);
        } catch (error) {
            console.log(
                `[${i + 1}/${total}] ERROR at index ${i}`
            );

            errors.push({
                index: i,
                error: String(error)
            });
        }
    }

    const towns =
        settlements.filter(
            x => x.type === "town"
        );

    const castles =
        settlements.filter(
            x => x.type === "castle"
        );

    const villages =
        settlements.filter(
            x => x.type === "village"
        );

    const hideouts =
        settlements.filter(
            x => x.type === "hideout"
        );

    const output = {
        mapperVersion: "0.1",

        capturedAtRealTime:
            new Date().toISOString(),

        campaignTime: clock,

        summary: {
            expectedSettlementObjects: total,
            successfullyMapped: settlements.length,
            errorCount: errors.length,
            counts
        },

        towns,
        castles,
        villages,
        hideouts,

        other:
            settlements.filter(
                x => x.type === "other"
            ),

        errors
    };

    const directory =
        path.join(
            __dirname,
            "worldstates"
        );

    fs.mkdirSync(
        directory,
        { recursive: true }
    );

    const filename =
        `worldstate_${timestamp()}.json`;

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
    console.log("=================================");
    console.log("WORLD STATE MAP COMPLETE");
    console.log("=================================");

    console.log(
        `Expected objects: ${total}`
    );

    console.log(
        `Mapped objects:   ${settlements.length}`
    );

    console.log(
        `Errors:           ${errors.length}`
    );

    console.log("");

    console.log(`Towns:     ${counts.town}`);
    console.log(`Castles:   ${counts.castle}`);
    console.log(`Villages:  ${counts.village}`);
    console.log(`Hideouts:  ${counts.hideout}`);
    console.log(`Other:     ${counts.other}`);

    console.log("");
    console.log("WORLD STATE SAVED:");
    console.log(outputPath);
}

main().catch(error => {
    console.error("");
    console.error("WORLD STATE MAPPER ERROR");
    console.error(error);
});