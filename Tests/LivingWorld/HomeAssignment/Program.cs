using System;
using System.Collections.Generic;
using ClanAI;
class Program
{
    static int count;
    static void Check(bool ok, string name) { if (!ok) throw new Exception(name); count++; Console.WriteLine("PASS " + name); }
    static HomeAssignmentDecision P(bool eligible=true, bool valid=true, bool peace=true,
        bool urgent=false, bool exact=true, HomeAssignmentCandidateKind kind=HomeAssignmentCandidateKind.Visit,
        float native=1f, float current=1f)
    { return HomeAssignmentPolicy.Evaluate(eligible, valid, peace, urgent, exact, kind, native, current); }
    static void Main()
    {
        Check(!P(valid:false).Apply && P(valid:false).Factor == 1f, "no/invalid assignment exact passthrough");
        Check(!P(eligible:false).Apply, "main/ineligible actor passthrough");
        Check(!P(exact:false).Apply, "unrelated target passthrough");
        Check(P().Apply && P().Factor == 1.25f, "exact home visit factor fixed");
        Check(P(kind:HomeAssignmentCandidateKind.Patrol).Factor == 1.25f, "matching native patrol support");
        Check(!P(kind:HomeAssignmentCandidateKind.Other).Apply, "defend/other not peacetime policy");
        foreach(float score in new[]{0f,-1f,float.NaN,float.PositiveInfinity,float.NegativeInfinity})
        { Check(!P(native:score).Apply, "native nonviability " + score); Check(!P(current:score).Apply, "working nonviability " + score); }
        Check(!P(peace:false).Apply && !P(urgent:true).Apply, "war and urgency preserve established responsibility");
        Check(P(native:0.025f,current:0.025f).Factor * 0.025f == 0.03125f, "bounded return support without new viability");
        Check(P().Factor >= 1f && P().Factor <= 1.25f, "explicit factor bounds");
        Check(HomeAssignmentPolicy.CanExpose(true,true,true,false,false,true,true,10f), "far legal native home exposure");
        Check(!HomeAssignmentPolicy.CanExpose(true,true,true,false,true,true,true,10f), "existing home not duplicated");
        Check(!HomeAssignmentPolicy.CanExpose(true,true,true,false,false,false,true,10f), "unsuitable hostile home refused");
        Check(!HomeAssignmentPolicy.CanExpose(true,true,true,false,false,true,false,10f), "invalid naval navigation refused");
        Check(!HomeAssignmentPolicy.CanExpose(false,true,true,false,false,true,true,10f), "ineligible seam passthrough");
        Check(!HomeAssignmentPolicy.CanExpose(true,false,true,false,false,true,true,10f), "foreign missing home seam refused");
        Check(!HomeAssignmentPolicy.CanExpose(true,true,false,false,false,true,true,10f), "war seam passthrough");
        foreach(float distance in new[]{-1f,float.NaN,float.PositiveInfinity})
            Check(!HomeAssignmentPolicy.CanExpose(true,true,true,false,false,true,true,distance), "invalid distance " + distance);
        var records = new HomeAssignmentRecords();
        Check(records.Set("brother","castle_a") && records.Set("son","town_b"), "multiple distinct leaders");
        string home; Check(records.TryGet("brother",out home) && home=="castle_a", "exact identity lookup");
        var restored = new HomeAssignmentRecords(); restored.Import(records.Export());
        Check(restored.TryGet("son",out home) && home=="town_b", "D1 round trip");
        Check(restored.Set("brother","castle_c") && restored.TryGet("brother",out home) && home=="castle_c", "replace assignment");
        Check(!restored.Set("brother","castle_c"), "same assignment no revision/message");
        Check(restored.Clear("brother") && !restored.TryGet("brother",out home), "clear assignment");
        Check(restored.TryGet("son",out home) && home=="town_b", "clear leaves other home intact");
        var rows=records.Export(); rows.Add(rows[0]); restored.Import(rows);
        Check(restored.Export().Count==2, "identical duplicate coalesced");
        var conflicting = new HomeAssignmentRecords(); conflicting.Set("brother","other");
        rows.Add(conflicting.Export()[0]); restored.Import(rows);
        Check(!restored.TryGet("brother",out home), "conflicting duplicate fails closed");
        rows.Reverse(); restored.Import(rows);
        Check(!restored.TryGet("brother",out home), "conflict independent of order");
        restored.Import(new List<string>{null,"D2|YQ==|Yg==","D1|%%%|%%%","D1||Yg==","D1|/w==|Yg=="});
        Check(restored.Export().Count==0 && restored.RejectedRows==5, "malformed strict safe import");
        restored.Import(null); Check(restored.Export().Count==0, "missing new save key safe");
        Check(!restored.Set("", "home"), "invalid ID refused");
        var oversized = new List<string>(); for(int i=0;i<257;i++) oversized.Add("D1|YQ==|Yg==");
        restored.Import(oversized); Check(restored.Export().Count==0, "bounded import");
        Console.WriteLine("TOTAL " + count + " PASS");
    }
}
