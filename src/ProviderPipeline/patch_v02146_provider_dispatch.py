from pathlib import Path

root=Path(r"D:\BannerlordAIResearch\workspace\_PatchStaging\AutonomousOperator_v002146_ProviderDispatchOutbox_20260921_1425")
queue_file=root/"candidate"/"PatrolDefenseProviderDispatch.cs"
q=queue_file.read_text(encoding="utf-8-sig")
q=q.replace(
'''        internal void Reset()
        {
            _pending.Clear();
            _sequence = 0;
        }
''',
'''        internal void Reset()
        {
            _pending.Clear();
        }
''')
queue_file.write_text(q,encoding="utf-8")

p=root/"candidate"/"SubModule.cs"
t=p.read_text(encoding="utf-8-sig")

path_anchor='''        private const string PatrolDefenseDeliberationProviderPath =
            Root + @"\\patrol_defense_deliberation_provider_invocations.jsonl";
'''
path_repl=path_anchor+'''        private const string PatrolDefenseProviderOutboxPath =
            Root + @"\\patrol_defense_provider_outbox.jsonl";
'''
if "PatrolDefenseProviderOutboxPath" not in t:
    if path_anchor not in t:
        raise SystemExit("outbox path anchor missing")
    t=t.replace(path_anchor,path_repl,1)

field_anchor='''        private readonly PatrolDefenseDeliberationRouteTracker
            _patrolDefenseDeliberationRouteTracker =
                new PatrolDefenseDeliberationRouteTracker(128);
'''
field_repl=field_anchor+'''        private readonly PatrolDefenseProviderDispatchQueue
            _patrolDefenseProviderDispatchQueue =
                new PatrolDefenseProviderDispatchQueue(32);
'''
if "_patrolDefenseProviderDispatchQueue" not in t:
    if field_anchor not in t:
        raise SystemExit("dispatch queue field anchor missing")
    t=t.replace(field_anchor,field_repl,1)

reset_anchor='''            _patrolDefenseDeliberationRouteTracker.Reset();
'''
reset_repl='''            _patrolDefenseDeliberationRouteTracker.Reset();
            _patrolDefenseProviderDispatchQueue.Reset();
'''
if "_patrolDefenseProviderDispatchQueue.Reset();" not in t:
    t=t.replace(reset_anchor,reset_repl)

build_anchor='''                string deliberationRouteEligibilityJson =
                    BuildPatrolDefenseDeliberationRouteEligibility(
                        deliberationRequestJson);

                lock (IoLock)
'''
build_repl='''                string deliberationRouteEligibilityJson =
                    BuildPatrolDefenseDeliberationRouteEligibility(
                        deliberationRequestJson);
                string providerDispatchOutboxJobJson;
                string providerDispatchJson =
                    BuildPatrolDefenseProviderDispatch(
                        deliberationRequestJson,
                        deliberationRouteEligibilityJson,
                        out providerDispatchOutboxJobJson);

                lock (IoLock)
'''
if build_anchor not in t:
    raise SystemExit("dispatch build anchor missing")
t=t.replace(build_anchor,build_repl,1)

attach_anchor='''                    receiptJson =
                        PatrolDefenseDeliberationRouteEligibilityBuilder.AttachToShadow(
                            receiptJson,
                            deliberationRouteEligibilityJson);

                    File.AppendAllText(
                        PatrolDefenseShadowPath,
                        receiptJson +
                        Environment.NewLine);
'''
attach_repl='''                    receiptJson =
                        PatrolDefenseDeliberationRouteEligibilityBuilder.AttachToShadow(
                            receiptJson,
                            deliberationRouteEligibilityJson);
                    receiptJson =
                        PatrolDefenseProviderDispatchBuilder.AttachToShadow(
                            receiptJson,
                            providerDispatchJson);

                    File.AppendAllText(
                        PatrolDefenseShadowPath,
                        receiptJson +
                        Environment.NewLine);

                    if (!string.IsNullOrWhiteSpace(
                            providerDispatchOutboxJobJson))
                    {
                        File.AppendAllText(
                            PatrolDefenseProviderOutboxPath,
                            providerDispatchOutboxJobJson +
                            Environment.NewLine);
                    }
'''
if attach_anchor not in t:
    raise SystemExit("dispatch attach anchor missing")
t=t.replace(attach_anchor,attach_repl,1)

method_anchor='''        private string BuildPatrolDefenseDeliberationRouteEligibility(
'''
method=r'''        private string BuildPatrolDefenseProviderDispatch(
            string deliberationRequestJson,
            string deliberationRouteEligibilityJson,
            out string outboxJobJson)
        {
            outboxJobJson = null;

            if (string.IsNullOrWhiteSpace(
                    deliberationRequestJson) ||
                string.IsNullOrWhiteSpace(
                    deliberationRouteEligibilityJson))
            {
                return null;
            }

            bool routeEligible;
            if (!TryExtractJsonBooleanProperty(
                    deliberationRouteEligibilityJson,
                    "routeEligible",
                    out routeEligible) ||
                !routeEligible)
            {
                return null;
            }

            string requestFingerprint = null;
            TryExtractJsonStringProperty(
                deliberationRouteEligibilityJson,
                "requestFingerprint",
                out requestFingerprint);

            PatrolDefenseProviderDispatchResult dispatch =
                _patrolDefenseProviderDispatchQueue.Enqueue(
                    requestFingerprint,
                    deliberationRequestJson,
                    DateTime.UtcNow.Ticks);

            if (dispatch != null &&
                dispatch.Enqueued &&
                dispatch.Job != null)
            {
                outboxJobJson =
                    PatrolDefenseProviderDispatchBuilder.ToOutboxJobJson(
                        dispatch.Job);
            }

            return
                PatrolDefenseProviderDispatchBuilder.ToReceiptJson(
                    dispatch,
                    requestFingerprint);
        }


'''
if method not in t:
    if method_anchor not in t:
        raise SystemExit("dispatch method anchor missing")
    t=t.replace(method_anchor,method+method_anchor,1)

p.write_text(t,encoding="utf-8")

proj=root/"fixtures"/"Fixtures.csproj"
x=proj.read_text(encoding="utf-8-sig")
include='''    <Compile Include="..\\candidate\\PatrolDefenseProviderDispatch.cs" Link="PatrolDefenseProviderDispatch.cs" />
'''
if "PatrolDefenseProviderDispatch.cs" not in x:
    x=x.replace(
        '''    <Compile Include="..\\candidate\\PatrolDefenseDeliberationProvider.cs" Link="PatrolDefenseDeliberationProvider.cs" />
''',
        '''    <Compile Include="..\\candidate\\PatrolDefenseDeliberationProvider.cs" Link="PatrolDefenseDeliberationProvider.cs" />
'''+include)
proj.write_text(x,encoding="utf-8")

fixture=root/"fixtures"/"FixtureProgram.cs"
f=fixture.read_text(encoding="utf-8-sig")
needle='''        Console.WriteLine("PASS_FIXTURES checks="+checks);
'''
extra=r'''        var dispatchQueue =
            new PatrolDefenseProviderDispatchQueue(2);

        var dispatchFirst =
            dispatchQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                100);
        Check(
            dispatchFirst.Enqueued &&
            dispatchFirst.Reason == "ENQUEUED" &&
            dispatchFirst.QueueCount == 1 &&
            dispatchFirst.Capacity == 2 &&
            dispatchFirst.Job != null &&
            dispatchFirst.Job.Sequence == 1 &&
            dispatchFirst.Job.RequestFingerprint ==
                request.RequestFingerprint &&
            dispatchFirst.Job.DeliberationRequestJson ==
                requestJson &&
            dispatchFirst.Job.State == "PENDING" &&
            dispatchFirst.Job.EnqueuedUtcTicks == 100,
            "dispatch_first_exact");

        var dispatchDuplicate =
            dispatchQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                101);
        Check(
            !dispatchDuplicate.Enqueued &&
            dispatchDuplicate.Reason ==
                "DUPLICATE_REQUEST" &&
            dispatchDuplicate.QueueCount == 1 &&
            dispatchDuplicate.Job != null &&
            dispatchDuplicate.Job.Sequence == 1,
            "dispatch_duplicate_suppressed");

        var dispatchSecond =
            dispatchQueue.Enqueue(
                changedRequest.RequestFingerprint,
                PatrolDefenseDeliberationRequestBuilder.ToJson(
                    changedRequest),
                102);
        Check(
            dispatchSecond.Enqueued &&
            dispatchSecond.Job.Sequence == 2 &&
            dispatchSecond.QueueCount == 2,
            "dispatch_changed_enqueued");

        var dispatchFull =
            dispatchQueue.Enqueue(
                reasonChangedRequest.RequestFingerprint,
                PatrolDefenseDeliberationRequestBuilder.ToJson(
                    reasonChangedRequest),
                103);
        Check(
            !dispatchFull.Enqueued &&
            dispatchFull.Reason == "QUEUE_FULL" &&
            dispatchFull.QueueCount == 2 &&
            dispatchFull.Job == null,
            "dispatch_full_no_pending_eviction");

        var dispatchInvalid =
            dispatchQueue.Enqueue(
                null,
                requestJson,
                104);
        Check(
            !dispatchInvalid.Enqueued &&
            dispatchInvalid.Reason ==
                "INVALID_REQUEST" &&
            dispatchInvalid.QueueCount == 2,
            "dispatch_invalid_rejected");

        string dispatchReceipt =
            PatrolDefenseProviderDispatchBuilder.ToReceiptJson(
                dispatchFirst,
                request.RequestFingerprint);
        Check(
            dispatchReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderDispatch.v1\"") &&
            dispatchReceipt.Contains(
                "\"enqueued\":true") &&
            dispatchReceipt.Contains(
                "\"reason\":\"ENQUEUED\"") &&
            dispatchReceipt.Contains(
                "\"queueCount\":1") &&
            dispatchReceipt.Contains(
                "\"capacity\":2") &&
            dispatchReceipt.Contains(
                "\"sequence\":1"),
            "dispatch_receipt_exact");
        Check(
            dispatchReceipt.Contains(
                "\"providerInvoked\":false") &&
            dispatchReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            dispatchReceipt.Contains(
                "\"modelInvoked\":false") &&
            dispatchReceipt.Contains(
                "\"executionAuthorized\":false"),
            "dispatch_receipt_no_provider_network_model_execution");

        string dispatchOutbox =
            PatrolDefenseProviderDispatchBuilder.ToOutboxJobJson(
                dispatchFirst.Job);
        Check(
            dispatchOutbox.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderDispatchJob.v1\"") &&
            dispatchOutbox.Contains(
                "\"requestFingerprint\":\"" +
                request.RequestFingerprint +
                "\"") &&
            dispatchOutbox.Contains(
                "\"state\":\"PENDING\"") &&
            dispatchOutbox.Contains(
                "\"enqueuedUtcTicks\":100") &&
            dispatchOutbox.Contains(
                "\"deliberationRequest\":" +
                requestJson),
            "dispatch_outbox_exact_payload");

        string dispatchBaseline =
            "{\"schema\":\"shadow\",\"candidateAction\":\"CONTINUE_PATROL\"}";
        string dispatchAttached =
            PatrolDefenseProviderDispatchBuilder.AttachToShadow(
                dispatchBaseline,
                dispatchReceipt);
        Check(
            dispatchAttached.Contains(
                "\"candidateAction\":\"CONTINUE_PATROL\"") &&
            dispatchAttached.Contains(
                "\"providerDispatch\":" +
                dispatchReceipt),
            "dispatch_attachment_receipt_only");

        dispatchQueue.Reset();
        Check(
            dispatchQueue.Count == 0,
            "dispatch_reset_clears_pending");

        var dispatchAfterReset =
            dispatchQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                105);
        Check(
            dispatchAfterReset.Enqueued &&
            dispatchAfterReset.Job.Sequence == 3,
            "dispatch_sequence_monotonic_after_reset");

'''
if extra not in f:
    if needle not in f:
        raise SystemExit("fixture insertion anchor missing")
    f=f.replace(needle,extra+needle,1)
fixture.write_text(f,encoding="utf-8")

print("v02146 dispatch queue + outbox integration + fixtures patched")
