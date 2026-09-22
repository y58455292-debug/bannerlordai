using System;
using System.Security.Cryptography;
using System.Text;
using BannerlordAITestRunner;

internal static class FixtureV4Program
{
    private static int checks;
    private static void Check(bool v,string n){checks++;if(!v)throw new Exception("fixture_failed:"+n);}
    private static string Sha(string s){using(var h=SHA256.Create()){var b=h.ComputeHash(Encoding.UTF8.GetBytes(s));var x=new StringBuilder();foreach(var z in b)x.Append(z.ToString("X2"));return x.ToString();}}

    private static string Provider(string provider,string model,string attempt,string fp,string req)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseProviderRequest.v1\",\"providerId\":\""+provider+"\",\"modelId\":\""+model+"\",\"attemptId\":\""+attempt+"\",\"requestFingerprint\":\""+fp+"\",\"promptContractVersion\":\"BannerlordAI.PatrolDefenseAdvisoryPrompt.v1\",\"responseSchema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\",\"deliberationRequestBase64\":\""+Convert.ToBase64String(Encoding.UTF8.GetBytes(req))+"\",\"inputSha256\":\""+Sha(req)+"\"}";
    }
    private static string Transport(string tid,string prid,string provider,string model,string attempt,string fp,string input)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportRequest.v1\",\"transportId\":\""+tid+"\",\"providerRequestId\":\""+prid+"\",\"providerId\":\""+provider+"\",\"modelId\":\""+model+"\",\"attemptId\":\""+attempt+"\",\"requestFingerprint\":\""+fp+"\",\"requestInputSha256\":\""+input+"\"}";
    }
    private static string Policy(string trid,string prid,string provider,string model,string attempt,string fp)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1\",\"transportRequestId\":\""+trid+"\",\"providerRequestId\":\""+prid+"\",\"providerId\":\""+provider+"\",\"modelId\":\""+model+"\",\"attemptId\":\""+attempt+"\",\"requestFingerprint\":\""+fp+"\",\"timeoutMs\":\"1000\",\"maxAttempts\":\"1\",\"maxResultBytes\":\"65536\",\"credentialRef\":\"env:BANNERLORDAI_TEST_PROVIDER_KEY\"}";
    }
    private static string PolicyWithMax(string trid,string prid,string provider,string model,string attempt,string fp,string maxResultBytes)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1\",\"transportRequestId\":\""+trid+"\",\"providerRequestId\":\""+prid+"\",\"providerId\":\""+provider+"\",\"modelId\":\""+model+"\",\"attemptId\":\""+attempt+"\",\"requestFingerprint\":\""+fp+"\",\"timeoutMs\":\"1000\",\"maxAttempts\":\"1\",\"maxResultBytes\":\""+maxResultBytes+"\",\"credentialRef\":\"env:BANNERLORDAI_TEST_PROVIDER_KEY\"}";
    }
    private static string PolicyWithAttempts(string trid,string prid,string provider,string model,string attempt,string fp,string maxAttempts)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1\",\"transportRequestId\":\""+trid+"\",\"providerRequestId\":\""+prid+"\",\"providerId\":\""+provider+"\",\"modelId\":\""+model+"\",\"attemptId\":\""+attempt+"\",\"requestFingerprint\":\""+fp+"\",\"timeoutMs\":\"1000\",\"maxAttempts\":\""+maxAttempts+"\",\"maxResultBytes\":\"65536\",\"credentialRef\":\"env:BANNERLORDAI_TEST_PROVIDER_KEY\"}";
    }
    private static string Advisory(string fp)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\",\"requestFingerprint\":\""+fp+"\",\"disposition\":\"KEEP_BASELINE\"}";
    }
    private static string Result(string provider,string attempt,string fp,string prid,string trid,string epid,string status,string adv)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseProviderResult.v4\",\"providerId\":\""+provider+"\",\"attemptId\":\""+attempt+"\",\"requestFingerprint\":\""+fp+"\",\"providerRequestId\":\""+prid+"\",\"transportRequestId\":\""+trid+"\",\"executionPolicyId\":\""+epid+"\",\"status\":\""+status+"\",\"advisoryBase64\":\""+adv+"\"}";
    }
    private static string Receipt(string tid,string trid,string prid,string epid,string provider,string model,string attempt,string fp,string input,string status,string result)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportReceipt.v3\",\"transportId\":\""+tid+"\",\"transportRequestId\":\""+trid+"\",\"providerRequestId\":\""+prid+"\",\"executionPolicyId\":\""+epid+"\",\"providerId\":\""+provider+"\",\"modelId\":\""+model+"\",\"attemptId\":\""+attempt+"\",\"requestFingerprint\":\""+fp+"\",\"requestInputSha256\":\""+input+"\",\"resultStatus\":\""+status+"\",\"resultSha256\":\""+Sha(result)+"\",\"externalNetworkUsed\":false,\"modelInvoked\":false,\"success\":true,\"errorCode\":null}";
    }

    private static PatrolDefenseProviderDispatchQueue Prepare(string fp,string req,out PatrolDefenseProviderRequestRegistrationData pr,out PatrolDefenseProviderTransportRegistrationData tr,out PatrolDefenseProviderExecutionPolicyRegistrationData pol,out PatrolDefenseAdvisoryRuntimeGate gate)
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
    private static PatrolDefenseProviderDispatchQueue PrepareWithMax(string fp,string req,string maxResultBytes,out PatrolDefenseProviderRequestRegistrationData pr,out PatrolDefenseProviderTransportRegistrationData tr,out PatrolDefenseProviderExecutionPolicyRegistrationData pol,out PatrolDefenseAdvisoryRuntimeGate gate)
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
    private static PatrolDefenseProviderDispatchQueue PrepareWithAttempts(string fp,string req,string maxAttempts,out PatrolDefenseProviderRequestRegistrationData pr,out PatrolDefenseProviderTransportRegistrationData tr,out PatrolDefenseProviderExecutionPolicyRegistrationData pol,out PatrolDefenseAdvisoryRuntimeGate gate)
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

    public static int Main()
    {
        string fp=new string('A',64);
        string req="{\"requestFingerprint\":\""+fp+"\",\"baselineDecision\":{\"candidateAction\":\"CONTINUE_PATROL\"}}";
        string adv64=Convert.ToBase64String(Encoding.UTF8.GetBytes(Advisory(fp)));

        PatrolDefenseProviderRequestRegistrationData pr;
        PatrolDefenseProviderTransportRegistrationData tr;
        PatrolDefenseProviderExecutionPolicyRegistrationData pol;
        PatrolDefenseAdvisoryRuntimeGate gate;
        var q=Prepare(fp,req,out pr,out tr,out pol,out gate);
        var m=q.MatchRegisteredExecutionPolicy(fp,"deterministic_external_worker","attempt-1",pr.ProviderRequestId,tr.TransportRequestId,pol.ExecutionPolicyId);
        Check(m.Matched&&m.Reason=="EXECUTION_POLICY_MATCHED","match_exact");
        var badm=q.MatchRegisteredExecutionPolicy(fp,"deterministic_external_worker","attempt-1",pr.ProviderRequestId,tr.TransportRequestId,new string('F',64));
        Check(!badm.Matched&&badm.Reason=="EXECUTION_POLICY_ID_MISMATCH","match_wrong");

        string r4=Result("deterministic_external_worker","attempt-1",fp,pr.ProviderRequestId,tr.TransportRequestId,pol.ExecutionPolicyId,"SUCCESS",adv64);
        var a=PatrolDefenseProviderResultV4Admission.EvaluateRequestBound(gate,q,r4);
        Check(a.ProviderResultAccepted,"result_exact");
        Check(a.ClaimMatchReason=="CLAIM_MATCHED","result_claim");
        Check(a.ProviderRequestMatchReason=="PROVIDER_REQUEST_MATCHED","result_pr");
        Check(a.TransportRequestMatchReason=="TRANSPORT_REQUEST_MATCHED","result_tr");
        Check(a.ExecutionPolicyMatchReason=="EXECUTION_POLICY_MATCHED","result_policy");
        Check(a.ExecutionPolicyId==pol.ExecutionPolicyId,"result_policy_id");
        Check(a.AdvisoryAdmission!=null&&a.AdvisoryAdmission.Admitted&&a.AdvisoryAdmission.Disposition=="KEEP_BASELINE","result_advisory");
        var comp=q.ApplyProviderResult(a.RequestFingerprint,a.Status,a.ProviderResultAccepted);
        Check(comp.CompletionApplied&&comp.Reason=="SUCCESS_ACKED"&&q.Count==0,"result_complete");

        PatrolDefenseProviderRequestRegistrationData pr2;
        PatrolDefenseProviderTransportRegistrationData tr2;
        PatrolDefenseProviderExecutionPolicyRegistrationData pol2;
        PatrolDefenseAdvisoryRuntimeGate gate2;
        var q2=Prepare(fp,req,out pr2,out tr2,out pol2,out gate2);
        string wrong=Result("deterministic_external_worker","attempt-1",fp,pr2.ProviderRequestId,tr2.TransportRequestId,new string('E',64),"SUCCESS",adv64);
        var wa=PatrolDefenseProviderResultV4Admission.EvaluateRequestBound(gate2,q2,wrong);
        Check(!wa.ProviderResultAccepted&&wa.ExecutionPolicyMatchReason=="EXECUTION_POLICY_ID_MISMATCH","result_wrong_policy");
        Check(gate2.PendingRequestFingerprint==fp&&q2.Count==1,"result_wrong_policy_retains");

        string exact2=Result("deterministic_external_worker","attempt-1",fp,pr2.ProviderRequestId,tr2.TransportRequestId,pol2.ExecutionPolicyId,"SUCCESS",adv64);
        string rec2=Receipt("deterministic_local_loopback",tr2.TransportRequestId,pr2.ProviderRequestId,pol2.ExecutionPolicyId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req),"SUCCESS",exact2);
        var b=PatrolDefenseProviderTransportReceiptResultV4Binding.Evaluate(gate2,q2,rec2,exact2,Encoding.UTF8.GetBytes(exact2));
        Check(b.BindingAccepted,"binding_exact");
        Check(b.TransportReceiptMatchReason=="TRANSPORT_RECEIPT_MATCHED","binding_receipt");
        Check(b.ExecutionPolicyMatchReason=="EXECUTION_POLICY_MATCHED","binding_policy");
        Check(b.ExecutionPolicyId==pol2.ExecutionPolicyId,"binding_policy_id");
        Check(b.ResultSha256==b.ComputedResultSha256&&b.ResultSha256==Sha(exact2),"binding_hash");
        Check(b.ResultSizePolicyReason=="RESULT_SIZE_WITHIN_LIMIT","size_under_reason");
        Check(b.ResultByteCount==Encoding.UTF8.GetByteCount(exact2),"size_under_count");
        Check(b.MaxResultBytes==65536,"size_under_max");
        Check(b.ProviderResultAdmission!=null&&b.ProviderResultAdmission.ProviderResultAccepted,"binding_nested");

        PatrolDefenseProviderRequestRegistrationData pr3;
        PatrolDefenseProviderTransportRegistrationData tr3;
        PatrolDefenseProviderExecutionPolicyRegistrationData pol3;
        PatrolDefenseAdvisoryRuntimeGate gate3;
        var q3=Prepare(fp,req,out pr3,out tr3,out pol3,out gate3);
        string rr=Result("deterministic_external_worker","attempt-1",fp,pr3.ProviderRequestId,tr3.TransportRequestId,pol3.ExecutionPolicyId,"SUCCESS",adv64);
        string badPolicyReceipt=Receipt("deterministic_local_loopback",tr3.TransportRequestId,pr3.ProviderRequestId,new string('D',64),"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req),"SUCCESS",rr);
        var bp=PatrolDefenseProviderTransportReceiptResultV4Binding.Evaluate(gate3,q3,badPolicyReceipt,rr,Encoding.UTF8.GetBytes(rr));
        Check(!bp.BindingAccepted&&bp.RejectionReasons.Contains("RECEIPT_RESULT_EXECUTION_POLICY_MISMATCH"),"binding_receipt_policy_mismatch");

        string badHashReceipt=Receipt("deterministic_local_loopback",tr3.TransportRequestId,pr3.ProviderRequestId,pol3.ExecutionPolicyId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req),"SUCCESS",rr).Replace(Sha(rr),new string('0',64));
        var bh=PatrolDefenseProviderTransportReceiptResultV4Binding.Evaluate(gate3,q3,badHashReceipt,rr,Encoding.UTF8.GetBytes(rr));
        Check(bh.RejectionReasons.Contains("RESULT_SHA256_MISMATCH"),"binding_hash_mismatch");

        string altered=rr+" ";
        string goodRec=Receipt("deterministic_local_loopback",tr3.TransportRequestId,pr3.ProviderRequestId,pol3.ExecutionPolicyId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req),"SUCCESS",rr);
        var alt=PatrolDefenseProviderTransportReceiptResultV4Binding.Evaluate(gate3,q3,goodRec,altered,Encoding.UTF8.GetBytes(altered));
        Check(alt.RejectionReasons.Contains("RESULT_SHA256_MISMATCH"),"binding_altered_bytes");

        string statusRec=goodRec.Replace("\"resultStatus\":\"SUCCESS\"","\"resultStatus\":\"PERMANENT_FAILURE\"");
        var st=PatrolDefenseProviderTransportReceiptResultV4Binding.Evaluate(gate3,q3,statusRec,rr,Encoding.UTF8.GetBytes(rr));
        Check(st.RejectionReasons.Contains("RECEIPT_RESULT_STATUS_MISMATCH"),"binding_status_mismatch");

        int exactByteCount=Encoding.UTF8.GetByteCount(exact2);

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
        Check(bj.Contains("\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportReceiptResultV4Binding.v1\""),"binding_schema");
        Check(bj.Contains("\"executionPolicyMatchReason\":\"EXECUTION_POLICY_MATCHED\""),"binding_match_receipt");
        Check(bj.Contains("\"executionPolicyId\":\""+pol2.ExecutionPolicyId+"\""),"binding_policy_receipt");
        Check(bj.Contains("\"resultSizePolicyReason\":\"RESULT_SIZE_WITHIN_LIMIT\""),"binding_size_receipt_reason");
        Check(bj.Contains("\"resultByteCount\":"+Encoding.UTF8.GetByteCount(exact2).ToString()),"binding_size_receipt_count");
        Check(bj.Contains("\"maxResultBytes\":65536"),"binding_size_receipt_max");
        Check(bj.Contains("\"executionAuthorized\":false")&&bj.Contains("\"behaviorMutation\":false")&&bj.Contains("\"scoreMutation\":false"),"binding_zero_authority");

        PatrolDefenseProviderRequestRegistrationData pra1;
        PatrolDefenseProviderTransportRegistrationData tra1;
        PatrolDefenseProviderExecutionPolicyRegistrationData pola1;
        PatrolDefenseAdvisoryRuntimeGate gatea1;
        var qa1=PrepareWithAttempts(fp,req,"1",out pra1,out tra1,out pola1,out gatea1);
        var auth1=qa1.AuthorizeTransportExecution(fp,pola1.ExecutionPolicyId);
        Check(auth1.Authorized&&auth1.Reason=="EXECUTION_ATTEMPT_AUTHORIZED","attempt_max1_first_authorized");
        Check(auth1.AttemptOrdinal==1&&auth1.AttemptCount==1&&auth1.MaxAttempts==1,"attempt_max1_first_counts");
        Check(auth1.TimeoutMs==1000,"attempt_timeout_bound");
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

        var qInvalidTimeoutZero=new PatrolDefenseProviderDispatchQueue(2);
        Check(qInvalidTimeoutZero.Enqueue(fp,req,51).Enqueued,"timeout_zero_enqueue");
        Check(qInvalidTimeoutZero.Claim(fp,"deterministic_external_worker","attempt-1",52).ClaimApplied,"timeout_zero_claim");
        var prtz=PatrolDefenseProviderRequestRegistration.Evaluate(qInvalidTimeoutZero,Provider("deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,req));
        Check(prtz.Registered,"timeout_zero_pr");
        var trtz=PatrolDefenseProviderTransportRegistration.Evaluate(qInvalidTimeoutZero,Transport("deterministic_local_loopback",prtz.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req)));
        Check(trtz.Registered,"timeout_zero_tr");
        string timeoutZeroPolicyId=new string('7',64);
        Check(qInvalidTimeoutZero.RegisterExecutionPolicy(fp,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",prtz.ProviderRequestId,trtz.TransportRequestId,"0","1","65536","env:BANNERLORDAI_TEST_PROVIDER_KEY",timeoutZeroPolicyId).Registered,"timeout_zero_direct_policy");
        var timeoutZeroAuth=qInvalidTimeoutZero.AuthorizeTransportExecution(fp,timeoutZeroPolicyId);
        Check(!timeoutZeroAuth.Authorized&&timeoutZeroAuth.Reason=="EXECUTION_TIMEOUT_POLICY_INVALID"&&timeoutZeroAuth.AttemptCount==0,"timeout_zero_rejected_no_increment");

        var qInvalidTimeoutText=new PatrolDefenseProviderDispatchQueue(2);
        Check(qInvalidTimeoutText.Enqueue(fp,req,61).Enqueued,"timeout_text_enqueue");
        Check(qInvalidTimeoutText.Claim(fp,"deterministic_external_worker","attempt-1",62).ClaimApplied,"timeout_text_claim");
        var prtt=PatrolDefenseProviderRequestRegistration.Evaluate(qInvalidTimeoutText,Provider("deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,req));
        Check(prtt.Registered,"timeout_text_pr");
        var trtt=PatrolDefenseProviderTransportRegistration.Evaluate(qInvalidTimeoutText,Transport("deterministic_local_loopback",prtt.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req)));
        Check(trtt.Registered,"timeout_text_tr");
        string timeoutTextPolicyId=new string('6',64);
        Check(qInvalidTimeoutText.RegisterExecutionPolicy(fp,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",prtt.ProviderRequestId,trtt.TransportRequestId,"abc","1","65536","env:BANNERLORDAI_TEST_PROVIDER_KEY",timeoutTextPolicyId).Registered,"timeout_text_direct_policy");
        var timeoutTextAuth=qInvalidTimeoutText.AuthorizeTransportExecution(fp,timeoutTextPolicyId);
        Check(!timeoutTextAuth.Authorized&&timeoutTextAuth.Reason=="EXECUTION_TIMEOUT_POLICY_INVALID"&&timeoutTextAuth.AttemptCount==0,"timeout_text_rejected_no_increment");

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
        Check(authReceipt.Contains("\"timeoutMs\":1000"),"attempt_receipt_timeout");
        Check(authReceipt.Contains("\"executionAuthorized\":false")&&authReceipt.Contains("\"externalNetworkUsed\":false")&&authReceipt.Contains("\"modelInvoked\":false"),"attempt_receipt_zero_authority");

        Console.WriteLine("PASS_V4_FIXTURES checks="+checks);
        return 0;
    }
}
