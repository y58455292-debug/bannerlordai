# Incident Decision Ledger v1 — 2026-09-19

## Purpose
Make every autonomous incident/menu choice explainable and measurable without hard-scripting the result.

## Decision trace
For every option record:
- actor / date / incident id / source
- objective context facts
- actor-known facts and beliefs separately
- option text/id
- hard eligibility / vetoes
- feature values normalized to [-1, +1]
- actor weight for each feature
- weighted contribution
- memory matches and provenance
- uncertainty/confidence
- total option score
- score gap vs runner-up
- selected option

## Initial feature families
1. family_survival
2. territorial_defense
3. military_readiness
4. logistics_food_supply
5. wealth_reserve
6. local_stability
7. legitimacy_influence
8. personal_relationships
9. mercy_honor
10. fear_deterrence
11. long_term_risk
12. immediate_cost
13. doctrine_alignment
14. memory_resonance
15. uncertainty_penalty

Do not treat these as fixed personality values. Actor-specific weights are calibrated from canon and branch experience.

## Score form
contribution_i = feature_value_i * actor_weight_i
raw_score = sum(contribution_i) + commitment/memory modifiers
selection uses eligible options only.
Store top positive and negative contributions so the choice can be explained in plain language.

## Outcome ledger
Capture before / immediate-after / delayed-after:
- clan influence
- hero relations (specific hero deltas)
- gold
- party size / wounded / prisoners
- settlement loyalty/security/prosperity/food
- faction/war state
- reputation/belief changes
- new memories created
- local tension and rumor propagation

## Causality discipline
Expected outcome != actual outcome.
Decision explanation uses information available at decision time.
Outcome learning happens only after the result is observed.

## Custom social incidents
Prefer a BannerlordAI CampaignBehavior that registers new incidents through TaleWorlds' incident system rather than editing vanilla module files.

Candidate sources:
- civilians
- merchants
- soldiers
- notables
- village elders
- governors
- companions
- family members

Candidate subjects:
- town/castle owner
- governor
- clan/ruler
- recent battle
- taxes/food shortages
- raids
- executions/prisoners
- generosity/public works
- security/loyalty
- military failures/victories

## Rumor / tension model
A rumor is actor belief, NOT objective truth.
Store:
- originating event/fact
- speaker
- subject
- stance
- credibility [0,1]
- reach/locality
- emotional valence
- recipients
- repeated-source count
- relation/influence/tension effects

Rumors may spread or mutate but never overwrite authoritative world truth.
They can affect actor beliefs, relations, settlement sentiment, local tension and future decision weights.

## Performance
Use deterministic templates + engine state for most incidents.
LLM flavor/dialogue is optional and should not be required for execution.
