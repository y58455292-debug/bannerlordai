import time,json,pathlib
ROOT=pathlib.Path(r"D:\BannerlordAIResearch\Automation\TestRunner")
OUT=pathlib.Path(r"D:\BannerlordAIResearch\Longitudinal\LiveValidation\SoldierInDebt_TimingTrace.jsonl")
start=time.perf_counter_ns()
deadline=time.time()+20
last=None
with OUT.open("a",encoding="utf-8") as f:
    while time.time()<deadline:
        now_ns=time.perf_counter_ns()
        status={}
        p=ROOT/"status.txt"
        if p.exists():
            for line in p.read_text(encoding="utf-8-sig",errors="replace").splitlines():
                if "=" in line:
                    k,v=line.split("=",1); status[k]=v
        key=(status.get("timeControl"),status.get("blockers"),status.get("lastCommand"),status.get("lastResult"),status.get("decisionType"),status.get("decisionTitle"))
        if key!=last:
            row={
                "mono_ms":round((now_ns-start)/1_000_000,3),
                "wall_unix":time.time(),
                "status":status
            }
            f.write(json.dumps(row,ensure_ascii=False)+"\n"); f.flush()
            last=key
        time.sleep(0.05)
print(str(OUT))
