import csv,collections,pathlib,json,datetime as dt
p=pathlib.Path(r"D:\BannerlordAIResearch\Telemetry\BannerlordInspector\ai\behavior-delta\20260918_010335_pid5640\behavior_changes.csv")
rows=list(csv.DictReader(p.open(encoding="utf-8",newline="")))
reasons=collections.Counter(r["reason"] for r in rows)
behtrans=collections.Counter((r["beforeBehavior"],r["afterBehavior"]) for r in rows if r["beforeBehavior"]!=r["afterBehavior"])
afterbeh=collections.Counter(r["afterBehavior"] for r in rows if r["afterBehavior"])
king=collections.Counter(r["kingdomName"] for r in rows if r["kingdomName"])
parties=collections.Counter(r["partyName"] for r in rows)
campaign=collections.Counter(r["campaignTime"] for r in rows)
first=rows[0];last=rows[-1]
out={
"rows":len(rows),
"real_start":first["realTime"],"real_end":last["realTime"],
"campaign_start":first["campaignTime"],"campaign_end":last["campaignTime"],
"unique_parties":len(parties),
"top_reasons":reasons.most_common(12),
"top_after_behaviors":afterbeh.most_common(12),
"top_behavior_transitions":[[list(k),v] for k,v in behtrans.most_common(15)],
"kingdom_counts":king.most_common(),
"top_parties":parties.most_common(15),
"campaign_times":campaign.most_common(15)
}
print(json.dumps(out,indent=2))
path=pathlib.Path(r"D:\BannerlordAIResearch\Longitudinal\DerivedReports\behavior_delta_full_session_summary.json")
path.write_text(json.dumps(out,indent=2),encoding="utf-8")
