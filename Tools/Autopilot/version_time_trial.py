from pathlib import Path
import datetime, json, os, statistics

ROOT=Path(r'D:\\BannerlordAIResearch')
EVENTS=ROOT/r'Longitudinal\EvidenceIndex\HandoffV3\events.jsonl'
CURRENT=ROOT/r'Longitudinal\EvidenceIndex\HandoffV3\current.json'
OUT_JSON=ROOT/r'Automation\Office\version_time_trial.json'
OUT_MD=ROOT/r'Automation\Office\version_time_trial.md'

def load(p):
    return json.loads(p.read_text(encoding='utf-8-sig'))

def atomic(p,obj):
    p.parent.mkdir(parents=True,exist_ok=True)
    t=p.with_suffix(p.suffix+'.tmp')
    t.write_text(json.dumps(obj,indent=2,ensure_ascii=False)+'\n',encoding='utf-8')
    os.replace(t,p)

def accepted_rows():
    out=[]
    for line in EVENTS.read_text(encoding='utf-8').splitlines():
        if not line.strip(): continue
        e=json.loads(line)
        m=(e.get('patch') or {}).get('last_accepted_milestone') or {}
        if m.get('version'):
            out.append({'checkpoint_seq':e.get('checkpoint_seq'),'timestamp':e.get('timestamp'),'version':m.get('version'),'status':m.get('status')})
    return out
def distinct(rows):
    by={}; order=[]
    for r in rows:
        if r['version'] not in by: order.append(r['version'])
        by[r['version']]=r
    return [by[v] for v in order]

def enrich(rows):
    out=[]; prev=None; prev_elapsed=None
    for r in rows:
        ts=datetime.datetime.fromisoformat(r['timestamp'])
        elapsed=None if prev is None else (ts-prev).total_seconds()/60.0
        score=None if not elapsed else round(60000.0/elapsed)
        change=None
        if elapsed is not None and prev_elapsed:
            change=round((prev_elapsed-elapsed)/prev_elapsed*100.0,2)
        x=dict(r)
        x.update({'elapsed_minutes':None if elapsed is None else round(elapsed,2),'speed_score':score,'change_vs_previous_percent':change})
        out.append(x); prev=ts
        if elapsed is not None: prev_elapsed=elapsed
    ranked=[x for x in out if x['elapsed_minutes'] is not None]
    ranked.sort(key=lambda x:(x['elapsed_minutes'],x['timestamp']))
    for i,x in enumerate(ranked,1): x['rank']=i
    ranks={x['version']:x['rank'] for x in ranked}
    for x in out: x['rank']=ranks.get(x['version'])
    return out,ranked
def main():
    versions=distinct(accepted_rows())
    league=[r for r in versions if r['version'].startswith('v0.2.10.')]
    league_rows,league_ranked=enrich(league)
    current=load(CURRENT)
    now=datetime.datetime.now().astimezone()
    last=datetime.datetime.fromisoformat(versions[-1]['timestamp']) if versions else now
    active=(current.get('active_candidate') or {}).get('version')
    current_elapsed=round((now-last.astimezone(now.tzinfo)).total_seconds()/60.0,2)
    times=[x['elapsed_minutes'] for x in league_rows if x['elapsed_minutes'] is not None]
    obj={'schema':'BannerlordAI.VersionTimeTrial.v1','generated_at':now.isoformat(),
         'timing_rule':'acceptance-to-acceptance wall-clock minutes','scoring_rule':'round(60000 / elapsed_minutes); higher is faster',
         'current_run':{'version':active,'elapsed_minutes':current_elapsed,'status':(current.get('active_candidate') or {}).get('status')},
         'league':{'name':'v0.2.10.x','rows':league_rows,'leaderboard':league_ranked,
                   'average_minutes':round(statistics.mean(times),2) if times else None,
                   'median_minutes':round(statistics.median(times),2) if times else None}}
    atomic(OUT_JSON,obj)
    lines=['# BannerlordAI Version Time Trial','','Score = 60000 / elapsed minutes. 1000 points equals a 60-minute version.','',
           '| Rank | Version | Minutes | Score | Change vs previous |','|---:|---|---:|---:|---:|']
    for r in league_ranked:
        ch=r.get('change_vs_previous_percent')
        cs='-' if ch is None else (('+' if ch>=0 else '')+str(ch)+'%')
        lines.append('| %s | %s | %.2f | %s | %s |' % (r['rank'],r['version'],r['elapsed_minutes'],r['speed_score'],cs))
    lines += ['', 'Current run: %s - %.2f minutes elapsed.' % (active,current_elapsed)]
    OUT_MD.write_text('\n'.join(lines)+'\n',encoding='utf-8')
    print(json.dumps(obj,indent=2,ensure_ascii=False))
    return 0

if __name__=='__main__':
    raise SystemExit(main())