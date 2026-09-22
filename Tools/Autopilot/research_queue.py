from pathlib import Path
import json, sys

ROOT=Path(r"D:\BannerlordAIResearch\Longitudinal\EvidenceIndex\ResearchInbox\Queues")
VALID={"coder","analyzer","front","library"}

def main():
    if len(sys.argv)!=2 or sys.argv[1].lower() not in VALID:
        print(json.dumps({"status":"FAIL","reason":"usage: research_queue.py coder|analyzer|front|library"}))
        return 2
    role=sys.argv[1].lower()
    p=ROOT/f"{role}.json"
    if not p.exists():
        print(json.dumps({"schema":"BannerlordAI.ResearchQueueView.v1","role":role,"count":0,"items":[]},indent=2))
        return 0
    q=json.loads(p.read_text(encoding="utf-8-sig"))
    items=q.get("items") or []
    compact=[{
        "id":x.get("id"),
        "title":x.get("title"),
        "priority":x.get("priority"),
        "reason":x.get("reason"),
        "item_pointer":x.get("item_pointer"),
        "status":x.get("status")
    } for x in items if x.get("status")=="PENDING_CONSIDERATION"]
    print(json.dumps({"schema":"BannerlordAI.ResearchQueueView.v1","role":role,"count":len(compact),"items":compact},indent=2))
    return 0

if __name__=="__main__":
    raise SystemExit(main())
