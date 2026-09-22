from pathlib import Path

p=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002164_MaxAttemptsPolicy_20260921_2218\fixtures_v4\FixtureV4Program.cs")
t=p.read_text(encoding="utf-8-sig")

anchor='''    private static string PolicyWithMax(string trid,string prid,string provider,string model,string attempt,string fp,string maxResultBytes)
    {
        return "{\\"schema\\":\\"BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1\\",\\"transportRequestId\\":\\""+trid+"\\",\\"providerRequestId\\":\\""+prid+"\\",\\"providerId\\":\\""+provider+"\\",\\"modelId\\":\\""+model+"\\",\\"attemptId\\":\\""+attempt+"\\",\\"requestFingerprint\\":\\""+fp+"\\",\\"timeoutMs\\":\\"1000\\",\\"maxAttempts\\":\\"1\\",\\"maxResultBytes\\":\\""+maxResultBytes+"\\",\\"credentialRef\\":\\"env:BANNERLORDAI_TEST_PROVIDER_KEY\\"}";
    }
'''
repl=anchor+'''    private static string PolicyWithAttempts(string trid,string prid,string provider,string model,string attempt,string fp,string maxAttempts)
    {
        return "{\\"schema\\":\\"BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1\\",\\"transportRequestId\\":\\""+trid+"\\",\\"providerRequestId\\":\\""+prid+"\\",\\"providerId\\":\\""+provider+"\\",\\"modelId\\":\\""+model+"\\",\\"attemptId\\":\\""+attempt+"\\",\\"requestFingerprint\\":\\""+fp+"\\",\\"timeoutMs\\":\\"1000\\",\\"maxAttempts\\":\\""+maxAttempts+"\\",\\"maxResultBytes\\":\\"65536\\",\\"credentialRef\\":\\"env:BANNERLORDAI_TEST_PROVIDER_KEY\\"}";
    }
'''
if "PolicyWithAttempts(" not in t:
    if anchor not in t:
        raise SystemExit("PolicyWithMax anchor missing")
    t=t.replace(anchor,repl,1)

anchor='''    private static PatrolDefenseProviderDispatchQueue PrepareWithMax(string fp,string req,string maxResultBytes,out PatrolDefenseProviderRequestRegistrationData pr,out PatrolDefenseProviderTransportRegistrationData tr,out PatrolDefenseProviderExecutionPolicyRegistrationData pol,out PatrolDefenseAdvisoryRuntimeGate gate)
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
repl=anchor+'''    private static PatrolDefenseProviderDispatchQueue PrepareWithAttempts(string fp,string req,string maxAttempts,out PatrolDefenseProviderRequestRegistrationData pr,out PatrolDefenseProviderTransportRegistrationData tr,out PatrolDefenseProviderExecutionPolicyRegistrationData pol,out PatrolDefenseAdvisoryRuntimeGate gate)
    {
        var q=new PatrolDefenseProviderDispatchQueue(2);
        Check(q.Enqueue(fp,req,31).Enqueued,"preattempt_enqueue");
        Check(q.Claim(fp,"deterministic_external_worker","attempt-1",32).ClaimApplied,"preattempt_claim");
        pr=PatrolDefenseProviderRequestRegistration.Evaluate(q,Provider("deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,req));
        Check(pr.Registered,"preattempt_pr");
        tr=PatrolDefenseProviderTransportRegistration.Evaluate(q,Transport("deterministic_local_loopback",pr.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req)));
        Check(tr.Registered,"preattempt_tr");
        pol=PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(q,PolicyWithAttempts(tr.TransportRequestId,pr.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,maxAttempts));
        Check(pol.Registered,"preattempt_policy");
        gate=new PatrolDefenseAdvisoryRuntimeGate();gate.Arm(fp);
        return q;
    }
'''
if "PrepareWithAttempts(" not in t:
    if anchor not in t:
        raise SystemExit("PrepareWithMax anchor missing")
    t=t.replace(anchor,repl,1)

anchor='''        Console.WriteLine("PASS_V4_FIXTURES checks="+checks);
'''
extra=r'''        PatrolDefenseProviderRequestRegistrationData pra1;
        PatrolDefenseProviderTransportRegistrationData tra1;
        PatrolDefenseProviderExecutionPolicyRegistrationData pola1;
        PatrolDefenseAdvisoryRuntimeGate gatea1;
        var qa1=PrepareWithAttempts(fp,req,"1",out pra1,out tra1,out pola1,out gatea1);
        var auth1=qa1.AuthorizeTransportExecution(fp,pola1.ExecutionPolicyId);
        Check(auth1.Authorized&&auth1.Reason=="EXECUTION_ATTEMPT_AUTHORIZED","attempt_max1_first_authorized");
        Check(auth1.AttemptOrdinal==1&&auth1.AttemptCount==1&&auth1.MaxAttempts==1,"attempt_max1_first_counts");
        Check(!string.IsNullOrWhiteSpace(auth1.TransportExecutionAuthorizationId)&&auth1.TransportExecutionAuthorizationId.Length==64,"attempt_auth_id_64");
        var auth1second=qa1.AuthorizeTransportExecution(fp,pola1.ExecutionPolicyId);
        Check(!auth1second.Authorized&&auth1second.Reason=="EXECUTION_ATTEMPT_LIMIT_EXCEEDED","attempt_max1_second_rejected");
        Check(auth1second.AttemptCount==1&&auth1second.MaxAttempts==1&&auth1second.AttemptOrdinal==2,"attempt_max1_second_no_increment");

        PatrolDefenseProviderRequestRegistrationData praMirror;
        PatrolDefenseProviderTransportRegistrationData traMirror;
        PatrolDefenseProviderExecutionPolicyRegistrationData polaMirror;
        PatrolDefenseAdvisoryRuntimeGate gateMirror;
        var qMirror=PrepareWithAttempts(fp,req,"1",out praMirror,out traMirror,out polaMirror,out gateMirror);
        var authMirror=qMirror.AuthorizeTransportExecution(fp,polaMirror.ExecutionPolicyId);
        Check(authMirror.Authorized&&authMirror.TransportExecutionAuthorizationId==auth1.TransportExecutionAuthorizationId,"attempt_auth_id_deterministic");

        var wrongPolicyAuth=qa1.AuthorizeTransportExecution(fp,new string('F',64));
        Check(!wrongPolicyAuth.Authorized&&wrongPolicyAuth.Reason=="EXECUTION_POLICY_ID_MISMATCH","attempt_wrong_policy_rejected");
        var missingJobAuth=new PatrolDefenseProviderDispatchQueue(1).AuthorizeTransportExecution(fp,pola1.ExecutionPolicyId);
        Check(!missingJobAuth.Authorized&&missingJobAuth.Reason=="DISPATCH_JOB_NOT_FOUND","attempt_missing_job_rejected");

        PatrolDefenseProviderRequestRegistrationData pra2;
        PatrolDefenseProviderTransportRegistrationData tra2;
        PatrolDefenseProviderExecutionPolicyRegistrationData pola2;
        PatrolDefenseAdvisoryRuntimeGate gatea2;
        var qa2=PrepareWithAttempts(fp,req,"2",out pra2,out tra2,out pola2,out gatea2);
        var auth21=qa2.AuthorizeTransportExecution(fp,pola2.ExecutionPolicyId);
        var auth22=qa2.AuthorizeTransportExecution(fp,pola2.ExecutionPolicyId);
        var auth23=qa2.AuthorizeTransportExecution(fp,pola2.ExecutionPolicyId);
        Check(auth21.Authorized&&auth21.AttemptOrdinal==1&&auth21.AttemptCount==1&&auth21.MaxAttempts==2,"attempt_max2_first");
        Check(auth22.Authorized&&auth22.AttemptOrdinal==2&&auth22.AttemptCount==2&&auth22.MaxAttempts==2,"attempt_max2_second");
        Check(!auth23.Authorized&&auth23.Reason=="EXECUTION_ATTEMPT_LIMIT_EXCEEDED"&&auth23.AttemptCount==2,"attempt_max2_third_rejected");

        var qInvalidAttempts=new PatrolDefenseProviderDispatchQueue(2);
        Check(qInvalidAttempts.Enqueue(fp,req,41).Enqueued,"attempt_invalid_enqueue");
        Check(qInvalidAttempts.Claim(fp,"deterministic_external_worker","attempt-1",42).ClaimApplied,"attempt_invalid_claim");
        var priA=PatrolDefenseProviderRequestRegistration.Evaluate(qInvalidAttempts,Provider("deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,req));
        Check(priA.Registered,"attempt_invalid_pr");
        var triA=PatrolDefenseProviderTransportRegistration.Evaluate(qInvalidAttempts,Transport("deterministic_local_loopback",priA.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req)));
        Check(triA.Registered,"attempt_invalid_tr");
        string invalidAttemptPolicyId=new string('8',64);
        var invalidAttemptPolicy=qInvalidAttempts.RegisterExecutionPolicy(fp,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",priA.ProviderRequestId,triA.TransportRequestId,"1000","0","65536","env:BANNERLORDAI_TEST_PROVIDER_KEY",invalidAttemptPolicyId);
        Check(invalidAttemptPolicy.Registered,"attempt_invalid_direct_policy");
        var invalidAttemptAuth=qInvalidAttempts.AuthorizeTransportExecution(fp,invalidAttemptPolicyId);
        Check(!invalidAttemptAuth.Authorized&&invalidAttemptAuth.Reason=="EXECUTION_ATTEMPT_POLICY_INVALID","attempt_invalid_policy_rejected");

        PatrolDefenseProviderRequestRegistrationData prRel;
        PatrolDefenseProviderTransportRegistrationData trRel;
        PatrolDefenseProviderExecutionPolicyRegistrationData polRel;
        PatrolDefenseAdvisoryRuntimeGate gateRel;
        var qRel=PrepareWithAttempts(fp,req,"2",out prRel,out trRel,out polRel,out gateRel);
        var relAuth=qRel.AuthorizeTransportExecution(fp,polRel.ExecutionPolicyId);
        Check(relAuth.Authorized&&relAuth.AttemptOrdinal==1,"attempt_release_initial");
        var released=qRel.ReleaseClaim(fp,"deterministic_external_worker","attempt-1");
        Check(released.ReleaseApplied&&released.Reason=="CLAIM_RELEASED","attempt_release_applied");
        var afterReleaseAuth=qRel.AuthorizeTransportExecution(fp,polRel.ExecutionPolicyId);
        Check(!afterReleaseAuth.Authorized&&afterReleaseAuth.Reason=="EXECUTION_POLICY_NOT_REGISTERED","attempt_release_clears_policy_state");
        Check(qRel.Claim(fp,"deterministic_external_worker","attempt-2",43).ClaimApplied,"attempt_release_reclaim");
        var prRel2=PatrolDefenseProviderRequestRegistration.Evaluate(qRel,Provider("deterministic_external_worker","deterministic_mock_no_model","attempt-2",fp,req));
        Check(prRel2.Registered,"attempt_release_pr2");
        var trRel2=PatrolDefenseProviderTransportRegistration.Evaluate(qRel,Transport("deterministic_local_loopback",prRel2.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-2",fp,Sha(req)));
        Check(trRel2.Registered,"attempt_release_tr2");
        var polRel2=PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(qRel,PolicyWithAttempts(trRel2.TransportRequestId,prRel2.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-2",fp,"2"));
        Check(polRel2.Registered,"attempt_release_policy2");
        var relAuth2=qRel.AuthorizeTransportExecution(fp,polRel2.ExecutionPolicyId);
        Check(relAuth2.Authorized&&relAuth2.AttemptOrdinal==1&&relAuth2.AttemptCount==1,"attempt_release_new_policy_resets_count");

        string authReceipt=PatrolDefenseProviderDispatchBuilder.ToTransportExecutionAuthorizationJson(auth1);
        Check(authReceipt.Contains("\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportExecutionAuthorization.v1\""),"attempt_receipt_schema");
        Check(authReceipt.Contains("\"authorized\":true")&&authReceipt.Contains("\"reason\":\"EXECUTION_ATTEMPT_AUTHORIZED\""),"attempt_receipt_decision");
        Check(authReceipt.Contains("\"attemptOrdinal\":1")&&authReceipt.Contains("\"attemptCount\":1")&&authReceipt.Contains("\"maxAttempts\":1"),"attempt_receipt_counts");
        Check(authReceipt.Contains("\"executionAuthorized\":false")&&authReceipt.Contains("\"externalNetworkUsed\":false")&&authReceipt.Contains("\"modelInvoked\":false"),"attempt_receipt_zero_authority");

        Console.WriteLine("PASS_V4_FIXTURES checks="+checks);
'''
if "attempt_max1_first_authorized" not in t:
    if anchor not in t:
        raise SystemExit("fixture print anchor missing")
    t=t.replace(anchor,extra,1)

p.write_text(t,encoding="utf-8")
print("v02164 maxAttempts runtime fixtures patched")
