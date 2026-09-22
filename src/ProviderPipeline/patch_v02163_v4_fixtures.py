from pathlib import Path

p=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002163_ResultSizePolicy_20260921_2208\fixtures_v4\FixtureV4Program.cs")
t=p.read_text(encoding="utf-8-sig")

# Add configurable policy helper.
anchor='''    private static string Policy(string trid,string prid,string provider,string model,string attempt,string fp)
    {
        return "{\\"schema\\":\\"BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1\\",\\"transportRequestId\\":\\""+trid+"\\",\\"providerRequestId\\":\\""+prid+"\\",\\"providerId\\":\\""+provider+"\\",\\"modelId\\":\\""+model+"\\",\\"attemptId\\":\\""+attempt+"\\",\\"requestFingerprint\\":\\""+fp+"\\",\\"timeoutMs\\":\\"1000\\",\\"maxAttempts\\":\\"1\\",\\"maxResultBytes\\":\\"65536\\",\\"credentialRef\\":\\"env:BANNERLORDAI_TEST_PROVIDER_KEY\\"}";
    }
'''
repl=anchor+'''    private static string PolicyWithMax(string trid,string prid,string provider,string model,string attempt,string fp,string maxResultBytes)
    {
        return "{\\"schema\\":\\"BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1\\",\\"transportRequestId\\":\\""+trid+"\\",\\"providerRequestId\\":\\""+prid+"\\",\\"providerId\\":\\""+provider+"\\",\\"modelId\\":\\""+model+"\\",\\"attemptId\\":\\""+attempt+"\\",\\"requestFingerprint\\":\\""+fp+"\\",\\"timeoutMs\\":\\"1000\\",\\"maxAttempts\\":\\"1\\",\\"maxResultBytes\\":\\""+maxResultBytes+"\\",\\"credentialRef\\":\\"env:BANNERLORDAI_TEST_PROVIDER_KEY\\"}";
    }
'''
if "PolicyWithMax(" not in t:
    if anchor not in t:
        raise SystemExit("policy helper anchor missing")
    t=t.replace(anchor,repl,1)

# Add configurable prepare helper after Prepare.
anchor='''    private static PatrolDefenseProviderDispatchQueue Prepare(string fp,string req,out PatrolDefenseProviderRequestRegistrationData pr,out PatrolDefenseProviderTransportRegistrationData tr,out PatrolDefenseProviderExecutionPolicyRegistrationData pol,out PatrolDefenseAdvisoryRuntimeGate gate)
    {
        var q=new PatrolDefenseProviderDispatchQueue(2);
        Check(q.Enqueue(fp,req,1).Enqueued,"prep_enqueue");
        Check(q.Claim(fp,"deterministic_external_worker","attempt-1",2).ClaimApplied,"prep_claim");
        pr=PatrolDefenseProviderRequestRegistration.Evaluate(q,Provider("deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,req));
        Check(pr.Registered,"prep_pr");
        tr=PatrolDefenseProviderTransportRegistration.Evaluate(q,Transport("deterministic_local_loopback",pr.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req)));
        Check(tr.Registered,"prep_tr");
        pol=PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(q,Policy(tr.TransportRequestId,pr.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp));
        Check(pol.Registered,"prep_policy");
        gate=new PatrolDefenseAdvisoryRuntimeGate();gate.Arm(fp);
        return q;
    }
'''
repl=anchor+'''    private static PatrolDefenseProviderDispatchQueue PrepareWithMax(string fp,string req,string maxResultBytes,out PatrolDefenseProviderRequestRegistrationData pr,out PatrolDefenseProviderTransportRegistrationData tr,out PatrolDefenseProviderExecutionPolicyRegistrationData pol,out PatrolDefenseAdvisoryRuntimeGate gate)
    {
        var q=new PatrolDefenseProviderDispatchQueue(2);
        Check(q.Enqueue(fp,req,11).Enqueued,"prepmax_enqueue");
        Check(q.Claim(fp,"deterministic_external_worker","attempt-1",12).ClaimApplied,"prepmax_claim");
        pr=PatrolDefenseProviderRequestRegistration.Evaluate(q,Provider("deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,req));
        Check(pr.Registered,"prepmax_pr");
        tr=PatrolDefenseProviderTransportRegistration.Evaluate(q,Transport("deterministic_local_loopback",pr.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req)));
        Check(tr.Registered,"prepmax_tr");
        pol=PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(q,PolicyWithMax(tr.TransportRequestId,pr.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,maxResultBytes));
        Check(pol.Registered,"prepmax_policy");
        gate=new PatrolDefenseAdvisoryRuntimeGate();gate.Arm(fp);
        return q;
    }
'''
if "PrepareWithMax(" not in t:
    if anchor not in t:
        raise SystemExit("prepare helper anchor missing")
    t=t.replace(anchor,repl,1)

# Extend exact binding checks.
anchor='''        Check(b.ResultSha256==b.ComputedResultSha256&&b.ResultSha256==Sha(exact2),"binding_hash");
        Check(b.ProviderResultAdmission!=null&&b.ProviderResultAdmission.ProviderResultAccepted,"binding_nested");
'''
repl='''        Check(b.ResultSha256==b.ComputedResultSha256&&b.ResultSha256==Sha(exact2),"binding_hash");
        Check(b.ResultSizePolicyReason=="RESULT_SIZE_WITHIN_LIMIT","size_under_reason");
        Check(b.ResultByteCount==Encoding.UTF8.GetByteCount(exact2),"size_under_count");
        Check(b.MaxResultBytes==65536,"size_under_max");
        Check(b.ProviderResultAdmission!=null&&b.ProviderResultAdmission.ProviderResultAccepted,"binding_nested");
'''
if "size_under_reason" not in t:
    if anchor not in t:
        raise SystemExit("exact binding extension anchor missing")
    t=t.replace(anchor,repl,1)

# Insert equal/over/invalid before serializer checks near end.
anchor='''        string bj=PatrolDefenseProviderTransportReceiptResultV4Binding.ToJson(b);
'''
extra=r'''        int exactByteCount=Encoding.UTF8.GetByteCount(exact2);

        PatrolDefenseProviderRequestRegistrationData preq;
        PatrolDefenseProviderTransportRegistrationData treq;
        PatrolDefenseProviderExecutionPolicyRegistrationData poleq;
        PatrolDefenseAdvisoryRuntimeGate gateeq;
        var qeq=PrepareWithMax(fp,req,exactByteCount.ToString(),out preq,out treq,out poleq,out gateeq);
        string reqeq=Result("deterministic_external_worker","attempt-1",fp,preq.ProviderRequestId,treq.TransportRequestId,poleq.ExecutionPolicyId,"SUCCESS",adv64);
        Check(Encoding.UTF8.GetByteCount(reqeq)==exactByteCount,"size_equal_fixture_length");
        string receq=Receipt("deterministic_local_loopback",treq.TransportRequestId,preq.ProviderRequestId,poleq.ExecutionPolicyId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req),"SUCCESS",reqeq);
        var beq=PatrolDefenseProviderTransportReceiptResultV4Binding.Evaluate(gateeq,qeq,receq,reqeq,Encoding.UTF8.GetBytes(reqeq));
        Check(beq.BindingAccepted&&beq.ResultSizePolicyReason=="RESULT_SIZE_WITHIN_LIMIT","size_equal_admitted");
        Check(beq.ResultByteCount==exactByteCount&&beq.MaxResultBytes==exactByteCount,"size_equal_audit");
        Check(beq.ProviderResultAdmission!=null&&beq.ProviderResultAdmission.ProviderResultAccepted,"size_equal_nested");

        PatrolDefenseProviderRequestRegistrationData pro;
        PatrolDefenseProviderTransportRegistrationData tro;
        PatrolDefenseProviderExecutionPolicyRegistrationData polo;
        PatrolDefenseAdvisoryRuntimeGate gateo;
        var qo=PrepareWithMax(fp,req,(exactByteCount-1).ToString(),out pro,out tro,out polo,out gateo);
        string ro=Result("deterministic_external_worker","attempt-1",fp,pro.ProviderRequestId,tro.TransportRequestId,polo.ExecutionPolicyId,"SUCCESS",adv64);
        Check(Encoding.UTF8.GetByteCount(ro)==exactByteCount,"size_over_fixture_length");
        string reco=Receipt("deterministic_local_loopback",tro.TransportRequestId,pro.ProviderRequestId,polo.ExecutionPolicyId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req),"SUCCESS",ro);
        var bo=PatrolDefenseProviderTransportReceiptResultV4Binding.Evaluate(gateo,qo,reco,ro,Encoding.UTF8.GetBytes(ro));
        Check(!bo.BindingAccepted&&bo.RejectionReasons.Contains("RESULT_SIZE_LIMIT_EXCEEDED"),"size_over_rejected");
        Check(bo.ResultSizePolicyReason=="RESULT_SIZE_LIMIT_EXCEEDED","size_over_reason");
        Check(bo.ResultByteCount==exactByteCount&&bo.MaxResultBytes==exactByteCount-1,"size_over_audit");
        Check(bo.ProviderResultAdmission==null&&gateo.PendingRequestFingerprint==fp&&qo.Count==1,"size_over_retains");

        var qi=new PatrolDefenseProviderDispatchQueue(2);
        Check(qi.Enqueue(fp,req,21).Enqueued,"size_invalid_enqueue");
        Check(qi.Claim(fp,"deterministic_external_worker","attempt-1",22).ClaimApplied,"size_invalid_claim");
        var pri=PatrolDefenseProviderRequestRegistration.Evaluate(qi,Provider("deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,req));
        Check(pri.Registered,"size_invalid_pr");
        var tri=PatrolDefenseProviderTransportRegistration.Evaluate(qi,Transport("deterministic_local_loopback",pri.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req)));
        Check(tri.Registered,"size_invalid_tr");
        string invalidPolicyId=new string('9',64);
        var qpi=qi.RegisterExecutionPolicy(fp,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",pri.ProviderRequestId,tri.TransportRequestId,"1000","1","0","env:BANNERLORDAI_TEST_PROVIDER_KEY",invalidPolicyId);
        Check(qpi.Registered,"size_invalid_direct_policy_register");
        var sizeInvalid=qi.EvaluateRegisteredResultSizePolicy(fp,invalidPolicyId,exactByteCount);
        Check(!sizeInvalid.Allowed&&sizeInvalid.Reason=="RESULT_SIZE_POLICY_INVALID","size_invalid_policy_reason");

        string bj=PatrolDefenseProviderTransportReceiptResultV4Binding.ToJson(b);
'''
if "size_equal_admitted" not in t:
    if anchor not in t:
        raise SystemExit("size fixture insertion anchor missing")
    t=t.replace(anchor,extra,1)

# Serializer audit fields.
anchor='''        Check(bj.Contains("\\"executionPolicyId\\":\\""+pol2.ExecutionPolicyId+"\\""),"binding_policy_receipt");
        Check(bj.Contains("\\"executionAuthorized\\":false")&&bj.Contains("\\"behaviorMutation\\":false")&&bj.Contains("\\"scoreMutation\\":false"),"binding_zero_authority");
'''
repl='''        Check(bj.Contains("\\"executionPolicyId\\":\\""+pol2.ExecutionPolicyId+"\\""),"binding_policy_receipt");
        Check(bj.Contains("\\"resultSizePolicyReason\\":\\"RESULT_SIZE_WITHIN_LIMIT\\""),"binding_size_receipt_reason");
        Check(bj.Contains("\\"resultByteCount\\":"+Encoding.UTF8.GetByteCount(exact2).ToString()),"binding_size_receipt_count");
        Check(bj.Contains("\\"maxResultBytes\\":65536"),"binding_size_receipt_max");
        Check(bj.Contains("\\"executionAuthorized\\":false")&&bj.Contains("\\"behaviorMutation\\":false")&&bj.Contains("\\"scoreMutation\\":false"),"binding_zero_authority");
'''
if "binding_size_receipt_reason" not in t:
    if anchor not in t:
        raise SystemExit("serializer audit fixture anchor missing")
    t=t.replace(anchor,repl,1)

p.write_text(t,encoding="utf-8")
print("v02163 v4 result-size fixtures patched")
