using System;
using System.Security.Cryptography;
using System.Text;
using BannerlordAITestRunner;

internal static class FixtureV5Program
{
    private static int checks;
    private static void Check(bool v,string n){checks++;if(!v)throw new Exception("fixture_failed:"+n);}
    private static string Sha(string s){using(var h=SHA256.Create()){var b=h.ComputeHash(Encoding.UTF8.GetBytes(s));var x=new StringBuilder();foreach(var z in b)x.Append(z.ToString("X2"));return x.ToString();}}

    private static string Provider(string p,string m,string a,string fp,string req)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseProviderRequest.v1\",\"providerId\":\""+p+"\",\"modelId\":\""+m+"\",\"attemptId\":\""+a+"\",\"requestFingerprint\":\""+fp+"\",\"promptContractVersion\":\"BannerlordAI.PatrolDefenseAdvisoryPrompt.v1\",\"responseSchema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\",\"deliberationRequestBase64\":\""+Convert.ToBase64String(Encoding.UTF8.GetBytes(req))+"\",\"inputSha256\":\""+Sha(req)+"\"}";
    }
    private static string Transport(string prid,string p,string m,string a,string fp,string input)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportRequest.v1\",\"transportId\":\"deterministic_local_loopback\",\"providerRequestId\":\""+prid+"\",\"providerId\":\""+p+"\",\"modelId\":\""+m+"\",\"attemptId\":\""+a+"\",\"requestFingerprint\":\""+fp+"\",\"requestInputSha256\":\""+input+"\"}";
    }
    private static string Policy(string trid,string prid,string p,string m,string a,string fp)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1\",\"transportRequestId\":\""+trid+"\",\"providerRequestId\":\""+prid+"\",\"providerId\":\""+p+"\",\"modelId\":\""+m+"\",\"attemptId\":\""+a+"\",\"requestFingerprint\":\""+fp+"\",\"timeoutMs\":\"1000\",\"maxAttempts\":\"2\",\"maxResultBytes\":\"65536\",\"credentialRef\":\"env:BANNERLORDAI_TEST_PROVIDER_KEY\"}";
    }
    private static string Advisory(string fp)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\",\"requestFingerprint\":\""+fp+"\",\"disposition\":\"KEEP_BASELINE\"}";
    }
    private static string Result(string p,string a,string fp,string prid,string trid,string epid,string authId,int ord,string adv64)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseProviderResult.v5\",\"providerId\":\""+p+"\",\"attemptId\":\""+a+"\",\"requestFingerprint\":\""+fp+"\",\"providerRequestId\":\""+prid+"\",\"transportRequestId\":\""+trid+"\",\"executionPolicyId\":\""+epid+"\",\"transportExecutionAuthorizationId\":\""+authId+"\",\"transportExecutionAttemptOrdinal\":\""+ord.ToString()+"\",\"status\":\"SUCCESS\",\"advisoryBase64\":\""+adv64+"\"}";
    }
    private static string Receipt(string trid,string prid,string epid,string authId,int ord,string p,string m,string a,string fp,string input,string result)
    {
        return "{\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportReceipt.v4\",\"transportId\":\"deterministic_local_loopback\",\"transportRequestId\":\""+trid+"\",\"providerRequestId\":\""+prid+"\",\"providerId\":\""+p+"\",\"modelId\":\""+m+"\",\"attemptId\":\""+a+"\",\"requestFingerprint\":\""+fp+"\",\"requestInputSha256\":\""+input+"\",\"resultStatus\":\"SUCCESS\",\"executionPolicyId\":\""+epid+"\",\"transportExecutionAuthorizationId\":\""+authId+"\",\"transportExecutionAttemptOrdinal\":\""+ord.ToString()+"\",\"resultSha256\":\""+Sha(result)+"\",\"externalNetworkUsed\":false,\"modelInvoked\":false,\"success\":true,\"errorCode\":null}";
    }

    private static PatrolDefenseProviderDispatchQueue Prepare(string fp,string req,out PatrolDefenseProviderRequestRegistrationData pr,out PatrolDefenseProviderTransportRegistrationData tr,out PatrolDefenseProviderExecutionPolicyRegistrationData pol,out PatrolDefenseAdvisoryRuntimeGate gate)
    {
        var q=new PatrolDefenseProviderDispatchQueue(2);
        Check(q.Enqueue(fp,req,1).Enqueued,"prep_enqueue");
        Check(q.Claim(fp,"deterministic_external_worker","attempt-1",2).ClaimApplied,"prep_claim");
        pr=PatrolDefenseProviderRequestRegistration.Evaluate(q,Provider("deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,req));
        Check(pr.Registered,"prep_pr");
        tr=PatrolDefenseProviderTransportRegistration.Evaluate(q,Transport(pr.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req)));
        Check(tr.Registered,"prep_tr");
        pol=PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(q,Policy(tr.TransportRequestId,pr.ProviderRequestId,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp));
        Check(pol.Registered,"prep_policy");
        gate=new PatrolDefenseAdvisoryRuntimeGate();
        Check(gate.Arm(fp),"prep_gate");
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

        var auth1=q.AuthorizeTransportExecution(fp,pol.ExecutionPolicyId);
        var auth2=q.AuthorizeTransportExecution(fp,pol.ExecutionPolicyId);
        Check(auth1.Authorized&&auth1.AttemptOrdinal==1&&auth1.AttemptCount==1&&auth1.MaxAttempts==2&&auth1.TimeoutMs==1000,"auth1");
        Check(auth2.Authorized&&auth2.AttemptOrdinal==2&&auth2.AttemptCount==2&&auth2.MaxAttempts==2&&auth2.TimeoutMs==1000,"auth2");
        Check(auth1.TransportExecutionAuthorizationId!=auth2.TransportExecutionAuthorizationId,"auth_ids_distinct");

        var staleMatch=q.MatchRegisteredTransportExecutionAuthorization(fp,pol.ExecutionPolicyId,auth1.TransportExecutionAuthorizationId,1);
        Check(!staleMatch.Matched&&staleMatch.Reason=="EXECUTION_AUTHORIZATION_ID_MISMATCH","stale_match_rejected");
        var exactMatch=q.MatchRegisteredTransportExecutionAuthorization(fp,pol.ExecutionPolicyId,auth2.TransportExecutionAuthorizationId,2);
        Check(exactMatch.Matched&&exactMatch.Reason=="EXECUTION_AUTHORIZATION_MATCHED","exact_match");

        string staleResult=Result("deterministic_external_worker","attempt-1",fp,pr.ProviderRequestId,tr.TransportRequestId,pol.ExecutionPolicyId,auth1.TransportExecutionAuthorizationId,1,adv64);
        string staleReceipt=Receipt(tr.TransportRequestId,pr.ProviderRequestId,pol.ExecutionPolicyId,auth1.TransportExecutionAuthorizationId,1,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req),staleResult);
        var stale=PatrolDefenseProviderTransportReceiptResultV5Binding.Evaluate(gate,q,staleReceipt,staleResult,Encoding.UTF8.GetBytes(staleResult));
        Check(!stale.BindingAccepted,"stale_binding_rejected");
        Check(stale.TransportExecutionAuthorizationMatchReason=="EXECUTION_AUTHORIZATION_ID_MISMATCH","stale_binding_reason");
        Check(stale.ProviderResultAdmission==null,"stale_no_nested");
        Check(q.Count==1&&gate.PendingRequestFingerprint==fp,"stale_retains_job");

        string exactResult=Result("deterministic_external_worker","attempt-1",fp,pr.ProviderRequestId,tr.TransportRequestId,pol.ExecutionPolicyId,auth2.TransportExecutionAuthorizationId,2,adv64);
        string exactReceipt=Receipt(tr.TransportRequestId,pr.ProviderRequestId,pol.ExecutionPolicyId,auth2.TransportExecutionAuthorizationId,2,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req),exactResult);
        var exact=PatrolDefenseProviderTransportReceiptResultV5Binding.Evaluate(gate,q,exactReceipt,exactResult,Encoding.UTF8.GetBytes(exactResult));
        Check(exact.BindingAccepted,"exact_binding");
        Check(exact.TransportReceiptMatchReason=="TRANSPORT_RECEIPT_MATCHED","exact_receipt_match");
        Check(exact.ExecutionPolicyMatchReason=="EXECUTION_POLICY_MATCHED","exact_policy_match");
        Check(exact.TransportExecutionAuthorizationMatchReason=="EXECUTION_AUTHORIZATION_MATCHED","exact_auth_match");
        Check(exact.ResultSizePolicyReason=="RESULT_SIZE_WITHIN_LIMIT","exact_size");
        Check(exact.ProviderResultAdmission!=null&&exact.ProviderResultAdmission.ProviderResultAccepted,"exact_nested");
        Check(exact.ProviderResultAdmission.TransportExecutionAuthorizationMatchReason=="EXECUTION_AUTHORIZATION_MATCHED","exact_nested_auth_match");
        Check(exact.TransportExecutionAuthorizationId==auth2.TransportExecutionAuthorizationId&&exact.TransportExecutionAttemptOrdinal==2,"exact_auth_provenance");
        Check(gate.PendingRequestFingerprint==null,"exact_consumes_gate");

        var completion=q.ApplyProviderResult(exact.RequestFingerprint,exact.ResultStatus,exact.ProviderResultAdmission.ProviderResultAccepted);
        Check(completion.CompletionApplied&&completion.Reason=="SUCCESS_ACKED"&&q.Count==0,"exact_completion");

        var replay=PatrolDefenseProviderTransportReceiptResultV5Binding.Evaluate(gate,q,exactReceipt,exactResult,Encoding.UTF8.GetBytes(exactResult));
        Check(!replay.BindingAccepted&&replay.RejectionReasons.Contains("DISPATCH_JOB_NOT_FOUND"),"replay_job_missing");

        PatrolDefenseProviderRequestRegistrationData pr2;
        PatrolDefenseProviderTransportRegistrationData tr2;
        PatrolDefenseProviderExecutionPolicyRegistrationData pol2;
        PatrolDefenseAdvisoryRuntimeGate gate2;
        var q2=Prepare(fp,req,out pr2,out tr2,out pol2,out gate2);
        var a21=q2.AuthorizeTransportExecution(fp,pol2.ExecutionPolicyId);
        var a22=q2.AuthorizeTransportExecution(fp,pol2.ExecutionPolicyId);
        string r22=Result("deterministic_external_worker","attempt-1",fp,pr2.ProviderRequestId,tr2.TransportRequestId,pol2.ExecutionPolicyId,a22.TransportExecutionAuthorizationId,2,adv64);
        string receiptWrongId=Receipt(tr2.TransportRequestId,pr2.ProviderRequestId,pol2.ExecutionPolicyId,new string('F',64),2,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req),r22);
        var wrongId=PatrolDefenseProviderTransportReceiptResultV5Binding.Evaluate(gate2,q2,receiptWrongId,r22,Encoding.UTF8.GetBytes(r22));
        Check(!wrongId.BindingAccepted&&wrongId.RejectionReasons.Contains("RECEIPT_RESULT_EXECUTION_AUTHORIZATION_MISMATCH"),"receipt_result_id_disagree");

        string rWrongOrdinal=Result("deterministic_external_worker","attempt-1",fp,pr2.ProviderRequestId,tr2.TransportRequestId,pol2.ExecutionPolicyId,a22.TransportExecutionAuthorizationId,1,adv64);
        string recWrongOrdinal=Receipt(tr2.TransportRequestId,pr2.ProviderRequestId,pol2.ExecutionPolicyId,a22.TransportExecutionAuthorizationId,2,"deterministic_external_worker","deterministic_mock_no_model","attempt-1",fp,Sha(req),rWrongOrdinal);
        var wrongOrdinal=PatrolDefenseProviderTransportReceiptResultV5Binding.Evaluate(gate2,q2,recWrongOrdinal,rWrongOrdinal,Encoding.UTF8.GetBytes(rWrongOrdinal));
        Check(!wrongOrdinal.BindingAccepted&&wrongOrdinal.RejectionReasons.Contains("RECEIPT_RESULT_EXECUTION_AUTHORIZATION_ORDINAL_MISMATCH"),"receipt_result_ordinal_disagree");

        string directStale=Result("deterministic_external_worker","attempt-1",fp,pr2.ProviderRequestId,tr2.TransportRequestId,pol2.ExecutionPolicyId,a21.TransportExecutionAuthorizationId,1,adv64);
        var staleAdmission=PatrolDefenseProviderResultV5Admission.EvaluateRequestBound(gate2,q2,directStale);
        Check(!staleAdmission.ProviderResultAccepted&&staleAdmission.TransportExecutionAuthorizationMatchReason=="EXECUTION_AUTHORIZATION_ID_MISMATCH","direct_stale_rejected");
        Check(gate2.PendingRequestFingerprint==fp&&q2.Count==1,"direct_stale_retains");

        string json=PatrolDefenseProviderTransportReceiptResultV5Binding.ToJson(exact);
        Check(json.Contains("\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportReceiptResultV5Binding.v1\""),"binding_json_schema");
        Check(json.Contains("\"transportExecutionAuthorizationId\":\""+auth2.TransportExecutionAuthorizationId+"\""),"binding_json_auth");
        Check(json.Contains("\"transportExecutionAttemptOrdinal\":2"),"binding_json_ordinal");
        Check(json.Contains("\"executionAuthorized\":false")&&json.Contains("\"behaviorMutation\":false")&&json.Contains("\"scoreMutation\":false"),"binding_zero_authority");

        Console.WriteLine("PASS_V5_FIXTURES checks="+checks);
        return 0;
    }
}
