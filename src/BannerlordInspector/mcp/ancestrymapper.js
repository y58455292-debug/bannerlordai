import fs from "fs";
import path from "path";

const BASE = "http://127.0.0.1:8420";

const CHARACTERS =
  "type:TaleWorlds.CampaignSystem.Campaign.Current.Characters";

const CAMPAIGN_TIME =
  "type:TaleWorlds.CampaignSystem.CampaignTime.Now";

// ------------------------------------------------------------
// Bannerlord Inspector helpers
// ------------------------------------------------------------

async function evaluate(evalPath) {
  const url = new URL("/eval", BASE);
  url.searchParams.set("path", evalPath);

  const response = await fetch(url);

  if (!response.ok) {
    throw new Error(`HTTP ${response.status} while reading ${evalPath}`);
  }

  return response.json();
}

function valueOf(result) {
  return result?.value ?? null;
}

async function read(evalPath) {
  try {
    return valueOf(await evaluate(evalPath));
  } catch {
    return null;
  }
}

async function count(evalPath) {
  const value = await read(`${evalPath}.$count`);
  const n = Number(value);

  return Number.isFinite(n) ? n : 0;
}

// ------------------------------------------------------------
// Value normalization
// ------------------------------------------------------------

function primitive(value) {
  if (value === null || value === undefined) return null;

  if (
    typeof value === "string" ||
    typeof value === "number" ||
    typeof value === "boolean"
  ) {
    return value;
  }

  if (typeof value === "object") {
    if ("value" in value) return value.value;
  }

  return value;
}

async function readText(evalPath) {
  const value = await read(evalPath);

  if (value === null || value === undefined) return null;

  if (typeof value === "string") return value;

  if (typeof value === "number" || typeof value === "boolean") {
    return String(value);
  }

  if (typeof value === "object") {
    if (typeof value.value === "string") return value.value;
    if (typeof value.text === "string") return value.text;
  }

  return null;
}

async function readEnum(evalPath) {
  const value = await read(evalPath);

  if (value === null || value === undefined) return null;

  if (typeof value === "string" || typeof value === "number") {
    return value;
  }

  if (typeof value === "object") {
    if ("value" in value) return value.value;
  }

  return null;
}

// ------------------------------------------------------------
// Campaign time
// ------------------------------------------------------------

async function readCampaignClock() {
  return {
    year: await read(`${CAMPAIGN_TIME}.GetYear`),
    season: await read(`${CAMPAIGN_TIME}.GetSeasonOfYear`),
    dayOfYear: await read(`${CAMPAIGN_TIME}.GetDayOfYear`),
    hour: await read(`${CAMPAIGN_TIME}.GetHourOfDay`),
    ticks: await read(`${CAMPAIGN_TIME}.CurrentTicks`)
  };
}

async function readDate(datePath) {
  if (!datePath) return null;

  const ticks = await read(`${datePath}.CurrentTicks`);

  const year = await read(`${datePath}.GetYear`);
  const season = await read(`${datePath}.GetSeasonOfYear`);
  const dayOfYear = await read(`${datePath}.GetDayOfYear`);
  const hour = await read(`${datePath}.GetHourOfDay`);

  if (
    ticks === null &&
    year === null &&
    season === null &&
    dayOfYear === null &&
    hour === null
  ) {
    return null;
  }

  return {
    ticks: primitive(ticks),
    year: primitive(year),
    season: primitive(season),
    dayOfYear: primitive(dayOfYear),
    hour: primitive(hour)
  };
}

// ------------------------------------------------------------
// References
// ------------------------------------------------------------

async function readHeroRef(heroPath) {
  if (!heroPath) return null;

  const stringId = await read(`${heroPath}.StringId`);
  const name = await readText(`${heroPath}.Name`);

  if (stringId === null && name === null) return null;

  return {
    stringId: primitive(stringId),
    name
  };
}

async function readClanRef(clanPath) {
  if (!clanPath) return null;

  const stringId = await read(`${clanPath}.StringId`);
  const name = await readText(`${clanPath}.Name`);

  if (stringId === null && name === null) return null;

  return {
    stringId: primitive(stringId),
    name
  };
}

async function readSettlementRef(settlementPath) {
  if (!settlementPath) return null;

  const stringId = await read(`${settlementPath}.StringId`);
  const name = await readText(`${settlementPath}.Name`);

  if (stringId === null && name === null) return null;

  return {
    stringId: primitive(stringId),
    name
  };
}

// ------------------------------------------------------------
// Lists
// ------------------------------------------------------------

async function readHeroList(listPath) {
  const total = await count(listPath);
  const results = [];

  for (let i = 0; i < total; i++) {
    const ref = await readHeroRef(`${listPath}.[${i}]`);

    if (ref) {
      results.push(ref);
    }
  }

  return results;
}

// ------------------------------------------------------------
// One Hero
// ------------------------------------------------------------

async function readHero(characterPath, index) {
  const hero = `${characterPath}.HeroObject`;

  // If HeroObject is null, this CharacterObject is not a Hero.
  const heroId = await read(`${hero}.StringId`);

  if (heroId === null) {
    return null;
  }

  const record = {
    sourceCharacterIndex: index,

    identity: {
      stringId: primitive(heroId),
      name: await readText(`${hero}.Name`),
      firstName: await readText(`${hero}.FirstName`),

      isFemale: await read(`${characterPath}.IsFemale`),
      occupation: await readEnum(`${characterPath}.Occupation`),

      isAlive: await read(`${hero}.IsAlive`),
      isChild: await read(`${hero}.IsChild`),
      heroState: await readEnum(`${hero}.HeroState`)
    },

    life: {
      birth: await readDate(`${hero}.BirthDay`),
      death: await readDate(`${hero}.DeathDay`),

      age: await read(`${hero}.Age`),

      deathMark: await readEnum(`${hero}.DeathMark`),
      deathMarkKillerHero: await readHeroRef(
        `${hero}.DeathMarkKillerHero`
      )
    },

    parents: {
      father: await readHeroRef(`${hero}.Father`),
      mother: await readHeroRef(`${hero}.Mother`)
    },

    marriage: {
      currentSpouse: await readHeroRef(`${hero}.Spouse`),
      exSpouses: await readHeroList(`${hero}.ExSpouses`),

      // We have NOT proven exact marriage dates are stored.
      // Never fabricate them.
      marriageDatesRecovered: false,
      knownMarriageEvents: []
    },

    children: await readHeroList(`${hero}.Children`),

    clan: {
      current: await readClanRef(`${hero}.Clan`),
      origin: await readClanRef(`${hero}.OriginClan`),
      companionOf: await readClanRef(`${hero}.CompanionOf`)
    },

    geography: {
      bornSettlement: await readSettlementRef(
        `${hero}.BornSettlement`
      ),

      homeSettlement: await readSettlementRef(
        `${hero}.HomeSettlement`
      ),

      currentSettlement: await readSettlementRef(
        `${hero}.CurrentSettlement`
      ),

      stayingInSettlement: await readSettlementRef(
        `${hero}.StayingInSettlement`
      )
    },

    validation: {
      evidenceLevel: "raw_game_data",
      conflicts: []
    }
  };

  return record;
}

// ------------------------------------------------------------
// Relationship derivation
// ------------------------------------------------------------

function buildRelationshipGraph(heroes) {
  const byId = new Map();

  for (const hero of heroes) {
    const id = hero.identity.stringId;

    if (id) {
      byId.set(id, hero);
    }
  }

  const parentToChildren = new Map();

  function addParentChild(parentRef, childId) {
    const parentId = parentRef?.stringId;

    if (!parentId || !childId) return;

    if (!parentToChildren.has(parentId)) {
      parentToChildren.set(parentId, new Set());
    }

    parentToChildren.get(parentId).add(childId);
  }

  for (const hero of heroes) {
    const id = hero.identity.stringId;

    addParentChild(hero.parents.father, id);
    addParentChild(hero.parents.mother, id);
  }

  const relationships = {};

  for (const hero of heroes) {
    const id = hero.identity.stringId;

    if (!id) continue;

    const fatherId = hero.parents.father?.stringId ?? null;
    const motherId = hero.parents.mother?.stringId ?? null;

    const siblingCandidates = new Set();

    if (fatherId && parentToChildren.has(fatherId)) {
      for (const child of parentToChildren.get(fatherId)) {
        if (child !== id) siblingCandidates.add(child);
      }
    }

    if (motherId && parentToChildren.has(motherId)) {
      for (const child of parentToChildren.get(motherId)) {
        if (child !== id) siblingCandidates.add(child);
      }
    }

    const fullSiblings = [];
    const halfSiblings = [];

    for (const siblingId of siblingCandidates) {
      const sibling = byId.get(siblingId);

      if (!sibling) continue;

      const siblingFather =
        sibling.parents.father?.stringId ?? null;

      const siblingMother =
        sibling.parents.mother?.stringId ?? null;

      const sameFather =
        fatherId !== null && fatherId === siblingFather;

      const sameMother =
        motherId !== null && motherId === siblingMother;

      if (sameFather && sameMother) {
        fullSiblings.push(siblingId);
      } else {
        halfSiblings.push(siblingId);
      }
    }

    const grandparents = new Set();

    for (const parentRef of [
      hero.parents.father,
      hero.parents.mother
    ]) {
      const parent = byId.get(parentRef?.stringId);

      if (!parent) continue;

      if (parent.parents.father?.stringId) {
        grandparents.add(parent.parents.father.stringId);
      }

      if (parent.parents.mother?.stringId) {
        grandparents.add(parent.parents.mother.stringId);
      }
    }

    const grandchildren = new Set();

    for (const childRef of hero.children) {
      const child = byId.get(childRef.stringId);

      if (!child) continue;

      for (const grandchild of child.children) {
        if (grandchild.stringId) {
          grandchildren.add(grandchild.stringId);
        }
      }
    }

    const auntsUncles = new Set();

    for (const parentRef of [
      hero.parents.father,
      hero.parents.mother
    ]) {
      const parent = byId.get(parentRef?.stringId);

      if (!parent) continue;

      const pf = parent.parents.father?.stringId ?? null;
      const pm = parent.parents.mother?.stringId ?? null;

      for (const candidate of heroes) {
        const cid = candidate.identity.stringId;

        if (!cid || cid === parent.identity.stringId) continue;

        const cf = candidate.parents.father?.stringId ?? null;
        const cm = candidate.parents.mother?.stringId ?? null;

        if (
          (pf && cf === pf) ||
          (pm && cm === pm)
        ) {
          auntsUncles.add(cid);
        }
      }
    }

    const cousins = new Set();

    for (const auntUncleId of auntsUncles) {
      const auntUncle = byId.get(auntUncleId);

      if (!auntUncle) continue;

      for (const child of auntUncle.children) {
        if (child.stringId && child.stringId !== id) {
          cousins.add(child.stringId);
        }
      }
    }

    relationships[id] = {
      fullSiblings: [...fullSiblings],
      halfSiblings: [...halfSiblings],
      grandparents: [...grandparents],
      grandchildren: [...grandchildren],
      auntsUncles: [...auntsUncles],
      cousins: [...cousins],

      evidence: {
        parents: "direct_game_reference",
        siblings: "derived_from_shared_parents",
        grandparents: "derived_from_parent_links",
        grandchildren: "derived_from_child_links",
        auntsUncles: "derived_from_shared_parentage",
        cousins: "derived_from_parent_sibling_child_chain"
      }
    };
  }

  return relationships;
}

// ------------------------------------------------------------
// Main
// ------------------------------------------------------------

async function main() {
  console.log("");
  console.log("BANNERLORD ANCESTRY MAPPER v0.1");
  console.log("================================");
  console.log("");

  console.log("Reading campaign clock...");
  const campaign = await readCampaignClock();

  console.log(
    `Campaign: Year ${campaign.year}, ` +
    `Day ${campaign.dayOfYear}, ` +
    `${campaign.season}, Hour ${campaign.hour}`
  );

  console.log("");
  console.log("Counting CharacterObjects...");

  const characterCount = await count(CHARACTERS);

  console.log(`Characters registered: ${characterCount}`);
  console.log("");
  console.log("Scanning for HeroObjects...");
  console.log("");

  const heroes = [];
  let nonHeroes = 0;
  let errors = 0;

  for (let i = 0; i < characterCount; i++) {
    const character = `${CHARACTERS}.[${i}]`;

    try {
      const hero = await readHero(character, i);

      if (hero) {
        heroes.push(hero);

        const status =
          hero.identity.isAlive === false ? "DEAD" : "alive";

        console.log(
          `[${i + 1}/${characterCount}] HERO ${status} ` +
          `${hero.identity.name ?? hero.identity.stringId}`
        );
      } else {
        nonHeroes++;

        if ((i + 1) % 100 === 0) {
          console.log(
            `[${i + 1}/${characterCount}] scanning...`
          );
        }
      }
    } catch (error) {
      errors++;

      console.log(
        `[${i + 1}/${characterCount}] ERROR: ${error.message}`
      );
    }
  }

  console.log("");
  console.log("Building ancestry relationships...");

  const relationships = buildRelationshipGraph(heroes);

  const livingHeroes = heroes.filter(
    h => h.identity.isAlive === true
  ).length;

  const deadHeroes = heroes.filter(
    h => h.identity.isAlive === false
  ).length;

  const unknownLifeState = heroes.length -
    livingHeroes -
    deadHeroes;

  const output = {
    metadata: {
      mapper: "Bannerlord Ancestry Mapper",
      version: "0.1",
      createdAtRealWorld: new Date().toISOString(),

      campaign,

      source: {
        characterRegistry:
          "Campaign.Current.Characters",
        registeredCharacters: characterCount
      },

      counts: {
        heroes: heroes.length,
        livingHeroes,
        deadHeroes,
        unknownLifeState,
        nonHeroCharacters: nonHeroes,
        scanErrors: errors
      },

      methodology: {
        directEvidence: [
          "Hero parent references",
          "Hero child references",
          "Hero spouse references",
          "Hero ex-spouse references",
          "Hero birth/death values",
          "Hero clan/origin clan references"
        ],

        derivedEvidence: [
          "siblings",
          "half-siblings",
          "grandparents",
          "grandchildren",
          "aunts/uncles",
          "cousins"
        ],

        marriageDatePolicy:
          "No marriage date is inferred unless a retained dated marriage event is discovered."
      }
    },

    heroes,
    relationships
  };

  const directory = path.join(
    process.cwd(),
    "ancestry"
  );

  fs.mkdirSync(directory, {
    recursive: true
  });

  const stamp = new Date()
    .toISOString()
    .replace(/:/g, "-")
    .replace(/\./g, "-");

  const filename = path.join(
    directory,
    `ancestry_${stamp}.json`
  );

  fs.writeFileSync(
    filename,
    JSON.stringify(output, null, 2),
    "utf8"
  );

  console.log("");
  console.log("================================");
  console.log("ANCESTRY MAP COMPLETE");
  console.log("================================");
  console.log(`Registered characters: ${characterCount}`);
  console.log(`Heroes discovered:     ${heroes.length}`);
  console.log(`Living heroes:         ${livingHeroes}`);
  console.log(`Dead heroes:           ${deadHeroes}`);
  console.log(`Unknown life state:    ${unknownLifeState}`);
  console.log(`Non-hero characters:   ${nonHeroes}`);
  console.log(`Errors:                ${errors}`);

  console.log("");
  console.log("ANCESTRY MAP SAVED:");
  console.log(filename);

  console.log("");
  console.log(
    "Next phase: validate chronology and search retained marriage history."
  );
}

main().catch(error => {
  console.error("");
  console.error("ANCESTRY MAPPER FAILED");
  console.error(error);
});