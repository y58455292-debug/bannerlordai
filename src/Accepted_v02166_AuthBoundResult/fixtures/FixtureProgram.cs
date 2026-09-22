using System;
using System.Linq;
using BannerlordAITestRunner;

internal sealed class FixtureMalformedProvider :
    IPatrolDefenseDeliberationProvider
{
    public string ProviderId
    {
        get { return "fixture_malformed"; }
    }

    public string Generate(
        string requestFingerprint,
        string deliberationRequestJson)
    {
        return "{\"schema\":";
    }
}

internal sealed class FixtureThrowingProvider :
    IPatrolDefenseDeliberationProvider
{
    public string ProviderId
    {
        get { return "fixture_throwing"; }
    }

    public string Generate(
        string requestFingerprint,
        string deliberationRequestJson)
    {
        throw new InvalidOperationException("fixture");
    }
}

internal static class FixtureProgram
{
    private static int checks;

    private static void Check(bool value,string name)
    {
        checks++;
        if(!value)
            throw new Exception("fixture_failed:"+name);
    }

    private static bool Changed(
        PatrolDefenseReconsiderationEvidenceData data,
        string component)
    {
        return data.ChangedComponents.Contains(component);
    }

    public static int Main()
    {
        string identity =
            PatrolDefenseReconsiderationEvidenceBuilder
                .BuildIdentityMaterial(
                    "actor","branch","town","bandit");
        string threat =
            PatrolDefenseReconsiderationEvidenceBuilder
                .BuildThreatMaterial(
                    100.0,80,50.0,40,2.0);
        string memory =
            PatrolDefenseReconsiderationEvidenceBuilder
                .BuildPersistentMemoryMaterial(
                    "KNOWN_ENEMY","town","bandit",
                    "{\"id\":\"episode1\",\"source\":\"s\"}");
        string recent =
            PatrolDefenseReconsiderationEvidenceBuilder
                .BuildRecentOutcomeMaterial(
                    "FOUND","event1");

        var tracker =
            new PatrolDefenseReconsiderationTracker();

        var first=tracker.Evaluate(
            identity,threat,memory,recent);
        Check(first.FirstObservation,"first_flag");
        Check(!first.FactualContextChanged,"first_not_changed");
        Check(first.ReconsiderationCandidate,"first_candidate");
        Check(first.ChangedComponents.Length==0,"first_no_changed_components");

        var repeat=tracker.Evaluate(
            identity,threat,memory,recent);
        Check(!repeat.FirstObservation,"repeat_not_first");
        Check(!repeat.FactualContextChanged,"repeat_unchanged");
        Check(!repeat.ReconsiderationCandidate,"repeat_no_candidate");

        // Volatile time/age values are deliberately absent from the
        // stable recent-outcome material API. Same result/event identity
        // therefore remains unchanged regardless of age passage.
        string recentAgeLater =
            PatrolDefenseReconsiderationEvidenceBuilder
                .BuildRecentOutcomeMaterial(
                    "FOUND","event1");
        var ageOnly=tracker.Evaluate(
            identity,threat,memory,recentAgeLater);
        Check(!ageOnly.FactualContextChanged,"age_only_unchanged");
        Check(!ageOnly.ReconsiderationCandidate,"age_only_no_candidate");

        string threatChanged =
            PatrolDefenseReconsiderationEvidenceBuilder
                .BuildThreatMaterial(
                    100.0,80,55.0,38,1.8181818);
        var threatChange=tracker.Evaluate(
            identity,threatChanged,memory,recent);
        Check(threatChange.FactualContextChanged,"threat_changed");
        Check(Changed(threatChange,"currentThreat"),"threat_component");
        Check(threatChange.ReconsiderationCandidate,"threat_candidate");

        string memoryChanged =
            PatrolDefenseReconsiderationEvidenceBuilder
                .BuildPersistentMemoryMaterial(
                    "KNOWN_ENEMY","town","bandit",
                    "{\"id\":\"episode2\",\"source\":\"s\"}");
        var memoryChange=tracker.Evaluate(
            identity,threatChanged,memoryChanged,recent);
        Check(memoryChange.FactualContextChanged,"memory_changed");
        Check(Changed(memoryChange,"persistentMemory"),"memory_component");

        string recentChanged =
            PatrolDefenseReconsiderationEvidenceBuilder
                .BuildRecentOutcomeMaterial(
                    "FOUND","event2");
        var outcomeChange=tracker.Evaluate(
            identity,threatChanged,memoryChanged,recentChanged);
        Check(outcomeChange.FactualContextChanged,"outcome_changed");
        Check(Changed(outcomeChange,"recentOutcome"),"outcome_component");

        string identityChanged =
            PatrolDefenseReconsiderationEvidenceBuilder
                .BuildIdentityMaterial(
                    "actor","branch","town2","bandit");
        var identityChange=tracker.Evaluate(
            identityChanged,threatChanged,memoryChanged,recentChanged);
        Check(identityChange.FactualContextChanged,"identity_changed");
        Check(Changed(identityChange,"identity"),"identity_component");

        var memoryNull=tracker.Evaluate(
            identityChanged,threatChanged,null,recentChanged);
        Check(Changed(memoryNull,"persistentMemory"),"memory_present_to_null");

        var memoryPresent=tracker.Evaluate(
            identityChanged,threatChanged,memoryChanged,recentChanged);
        Check(Changed(memoryPresent,"persistentMemory"),"memory_null_to_present");

        var recentNull=tracker.Evaluate(
            identityChanged,threatChanged,memoryChanged,null);
        Check(Changed(recentNull,"recentOutcome"),"recent_present_to_null");

        string recentNone =
            PatrolDefenseReconsiderationEvidenceBuilder
                .BuildRecentOutcomeMaterial(
                    "NONE",null);
        var recentPresent=tracker.Evaluate(
            identityChanged,threatChanged,memoryChanged,recentNone);
        Check(Changed(recentPresent,"recentOutcome"),"recent_null_to_present");

        string json =
            PatrolDefenseReconsiderationEvidenceBuilder.ToJson(
                recentPresent);
        Check(json.Contains(
            "\"schema\":\"BannerlordAI.PatrolDefenseReconsiderationEvidence.v1\""),
            "json_schema");
        Check(json.Contains("\"interpretationApplied\":false"),
            "json_interpretation_false");
        Check(json.Contains("\"behaviorMutation\":false"),
            "json_behavior_false");
        Check(json.Contains("\"scoreMutation\":false"),
            "json_score_false");

        string baseline =
            "{\"schema\":\"shadow\",\"wouldInterrupt\":false,\"candidateAction\":\"CONTINUE_PATROL\"}";
        string attached =
            PatrolDefenseReconsiderationEvidenceBuilder.AttachToShadow(
                baseline,json);
        Check(attached.Contains("\"wouldInterrupt\":false"),
            "attach_preserves_decision");
        Check(attached.Contains("\"candidateAction\":\"CONTINUE_PATROL\""),
            "attach_preserves_action");
        Check(attached.Contains("\"reconsiderationEvidence\":"+json),
            "attach_exact");
        Check(PatrolDefenseReconsiderationEvidenceBuilder.AttachToShadow(
            baseline,null)==baseline,
            "null_evidence_no_change");

        tracker.Reset();
        var resetFirst=tracker.Evaluate(
            identity,threat,memory,recent);
        Check(resetFirst.FirstObservation,"reset_first");

        var requestTracker =
            new PatrolDefenseReconsiderationTracker();
        var requestReconsideration =
            requestTracker.Evaluate(
                identity,threat,memory,recent);
        string requestReconsiderationJson =
            PatrolDefenseReconsiderationEvidenceBuilder.ToJson(
                requestReconsideration);
        string actorKnownJson =
            "{\"schema\":\"BannerlordAI.PatrolDefenseActorKnownContext.v1\",\"actorId\":\"actor\",\"branchId\":\"branch\"}";

        var request =
            PatrolDefenseDeliberationRequestBuilder.Build(
                actorKnownJson,
                requestReconsiderationJson,
                requestReconsideration,
                false,
                false,
                "no_local_bandit_targeting_local_villager",
                "CONTINUE_PATROL",
                "PATROL_SETTLEMENT:town");
        Check(request != null,"request_eligible_created");
        Check(
            request.RequestFingerprint != null &&
            request.RequestFingerprint.Length == 64,
            "request_fingerprint_64");

        var sameRequest =
            PatrolDefenseDeliberationRequestBuilder.Build(
                actorKnownJson,
                requestReconsiderationJson,
                requestReconsideration,
                false,
                false,
                "no_local_bandit_targeting_local_villager",
                "CONTINUE_PATROL",
                "PATROL_SETTLEMENT:town");
        Check(
            sameRequest != null &&
            sameRequest.RequestFingerprint ==
                request.RequestFingerprint,
            "request_fingerprint_stable");

        var changedTracker =
            new PatrolDefenseReconsiderationTracker();
        string changedIdentity =
            PatrolDefenseReconsiderationEvidenceBuilder
                .BuildIdentityMaterial(
                    "actor","branch","town2","bandit");
        var changedReconsideration =
            changedTracker.Evaluate(
                changedIdentity,threat,memory,recent);
        string changedReconsiderationJson =
            PatrolDefenseReconsiderationEvidenceBuilder.ToJson(
                changedReconsideration);
        var changedRequest =
            PatrolDefenseDeliberationRequestBuilder.Build(
                actorKnownJson,
                changedReconsiderationJson,
                changedReconsideration,
                false,
                false,
                "no_local_bandit_targeting_local_villager",
                "CONTINUE_PATROL",
                "PATROL_SETTLEMENT:town");
        Check(
            changedRequest != null &&
            changedRequest.RequestFingerprint !=
                request.RequestFingerprint,
            "request_context_change_changes_fingerprint");

        var reasonChangedRequest =
            PatrolDefenseDeliberationRequestBuilder.Build(
                actorKnownJson,
                requestReconsiderationJson,
                requestReconsideration,
                false,
                false,
                "different_reason",
                "CONTINUE_PATROL",
                "PATROL_SETTLEMENT:town");
        Check(
            reasonChangedRequest != null &&
            reasonChangedRequest.RequestFingerprint !=
                request.RequestFingerprint,
            "request_baseline_change_changes_fingerprint");

        var ineligibleTracker =
            new PatrolDefenseReconsiderationTracker();
        ineligibleTracker.Evaluate(
            identity,threat,memory,recent);
        var ineligibleReconsideration =
            ineligibleTracker.Evaluate(
                identity,threat,memory,recent);
        string ineligibleJson =
            PatrolDefenseReconsiderationEvidenceBuilder.ToJson(
                ineligibleReconsideration);
        Check(
            PatrolDefenseDeliberationRequestBuilder.Build(
                actorKnownJson,
                ineligibleJson,
                ineligibleReconsideration,
                false,
                false,
                "same",
                "CONTINUE_PATROL",
                "PATROL_SETTLEMENT:town") == null,
            "request_ineligible_null");

        Check(
            PatrolDefenseDeliberationRequestBuilder.Build(
                null,
                requestReconsiderationJson,
                requestReconsideration,
                false,
                false,
                "same",
                "CONTINUE_PATROL",
                "PATROL_SETTLEMENT:town") == null,
            "request_missing_actor_known_null");

        string requestJson =
            PatrolDefenseDeliberationRequestBuilder.ToJson(
                request);
        Check(
            requestJson.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseDeliberationRequest.v1\""),
            "request_json_schema");
        Check(
            requestJson.Contains(
                "\"actorKnownContext\":"+actorKnownJson),
            "request_actor_known_exact");
        Check(
            requestJson.Contains(
                "\"reconsiderationEvidence\":"+
                requestReconsiderationJson),
            "request_reconsideration_exact");
        Check(
            requestJson.Contains(
                "\"wouldInterrupt\":false") &&
            requestJson.Contains(
                "\"candidateAction\":\"CONTINUE_PATROL\"") &&
            requestJson.Contains(
                "\"resumeAction\":\"PATROL_SETTLEMENT:town\""),
            "request_baseline_exact");
        Check(
            requestJson.Contains("\"llmInvoked\":false") &&
            requestJson.Contains("\"plannerInvoked\":false"),
            "request_no_cognition_invoked");
        Check(
            requestJson.Contains("\"behaviorMutation\":false") &&
            requestJson.Contains("\"scoreMutation\":false"),
            "request_zero_authority");

        string requestBaseline =
            "{\"schema\":\"shadow\",\"wouldInterrupt\":false,\"candidateAction\":\"CONTINUE_PATROL\"}";
        string requestAttached =
            PatrolDefenseDeliberationRequestBuilder.AttachToShadow(
                requestBaseline,
                requestJson);
        Check(
            requestAttached.Contains(
                "\"wouldInterrupt\":false") &&
            requestAttached.Contains(
                "\"deliberationRequest\":"+requestJson),
            "request_attachment_receipt_only");

        var routeTracker =
            new PatrolDefenseDeliberationRouteTracker(2);

        var routeFirst =
            routeTracker.Evaluate(
                request.RequestFingerprint);
        Check(
            routeFirst.RouteEligible &&
            routeFirst.RouteReason ==
                "FIRST_UNSEEN_REQUEST" &&
            !routeFirst.Duplicate &&
            routeFirst.SeenCount == 1 &&
            routeFirst.Capacity == 2,
            "route_first_eligible");

        var routeDuplicate =
            routeTracker.Evaluate(
                request.RequestFingerprint);
        Check(
            !routeDuplicate.RouteEligible &&
            routeDuplicate.RouteReason ==
                "DUPLICATE_REQUEST" &&
            routeDuplicate.Duplicate &&
            routeDuplicate.SeenCount == 1,
            "route_duplicate_suppressed");

        var routeChanged =
            routeTracker.Evaluate(
                changedRequest.RequestFingerprint);
        Check(
            routeChanged.RouteEligible &&
            routeChanged.RouteReason ==
                "FIRST_UNSEEN_REQUEST" &&
            !routeChanged.Duplicate &&
            routeChanged.SeenCount == 2,
            "route_changed_eligible");

        var routeInvalid =
            routeTracker.Evaluate(null);
        Check(
            !routeInvalid.RouteEligible &&
            routeInvalid.RouteReason ==
                "INVALID_REQUEST" &&
            !routeInvalid.Duplicate &&
            routeInvalid.SeenCount == 2,
            "route_invalid_ineligible");

        // Capacity=2: a third unique fingerprint evicts the oldest.
        var routeThird =
            routeTracker.Evaluate(
                reasonChangedRequest.RequestFingerprint);
        Check(
            routeThird.RouteEligible &&
            routeTracker.Count == 2,
            "route_capacity_bounded");

        var routeEvictedFirst =
            routeTracker.Evaluate(
                request.RequestFingerprint);
        Check(
            routeEvictedFirst.RouteEligible &&
            routeEvictedFirst.RouteReason ==
                "FIRST_UNSEEN_REQUEST",
            "route_oldest_evicted_deterministically");

        routeTracker.Reset();
        Check(
            routeTracker.Count == 0,
            "route_reset_clears");
        var routeAfterReset =
            routeTracker.Evaluate(
                request.RequestFingerprint);
        Check(
            routeAfterReset.RouteEligible &&
            routeAfterReset.SeenCount == 1,
            "route_reset_reeligible");

        string routeJson =
            PatrolDefenseDeliberationRouteEligibilityBuilder.ToJson(
                routeAfterReset);
        Check(
            routeJson.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseDeliberationRouteEligibility.v1\""),
            "route_json_schema");
        Check(
            routeJson.Contains("\"routeEligible\":true") &&
            routeJson.Contains(
                "\"routeReason\":\"FIRST_UNSEEN_REQUEST\"") &&
            routeJson.Contains("\"duplicate\":false"),
            "route_json_decision");
        Check(
            routeJson.Contains("\"modelInvoked\":false") &&
            routeJson.Contains("\"llmInvoked\":false") &&
            routeJson.Contains("\"plannerInvoked\":false"),
            "route_json_no_model");
        Check(
            routeJson.Contains("\"behaviorMutation\":false") &&
            routeJson.Contains("\"scoreMutation\":false"),
            "route_json_zero_authority");

        string routeBaseline =
            "{\"schema\":\"shadow\",\"candidateAction\":\"CONTINUE_PATROL\"}";
        string routeAttached =
            PatrolDefenseDeliberationRouteEligibilityBuilder.AttachToShadow(
                routeBaseline,
                routeJson);
        Check(
            routeAttached.Contains(
                "\"candidateAction\":\"CONTINUE_PATROL\"") &&
            routeAttached.Contains(
                "\"deliberationRouteEligibility\":"+routeJson),
            "route_attachment_receipt_only");

        string advisoryPrefix =
            "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\",";

        var keepAdmission =
            PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                request.RequestFingerprint,
                advisoryPrefix +
                "\"disposition\":\"KEEP_BASELINE\"}");
        Check(
            keepAdmission.Admitted &&
            keepAdmission.Disposition == "KEEP_BASELINE" &&
            keepAdmission.RejectionReasons.Count == 0,
            "advisory_keep_admitted");

        var reviewAdmission =
            PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                request.RequestFingerprint,
                advisoryPrefix +
                "\"disposition\":\"REVIEW_ALTERNATIVE\"}");
        Check(
            reviewAdmission.Admitted &&
            reviewAdmission.Disposition == "REVIEW_ALTERNATIVE",
            "advisory_review_admitted");

        var abstainAdmission =
            PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                request.RequestFingerprint,
                advisoryPrefix +
                "\"disposition\":\"ABSTAIN\"}");
        Check(
            abstainAdmission.Admitted &&
            abstainAdmission.Disposition == "ABSTAIN",
            "advisory_abstain_admitted");

        var wrongFingerprint =
            PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                request.RequestFingerprint,
                "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
                "\"requestFingerprint\":\"WRONG\"," +
                "\"disposition\":\"KEEP_BASELINE\"}");
        Check(
            !wrongFingerprint.Admitted &&
            wrongFingerprint.RejectionReasons.Contains(
                "REQUEST_FINGERPRINT_MISMATCH"),
            "advisory_wrong_fingerprint_rejected");

        var missingFingerprint =
            PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                request.RequestFingerprint,
                "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
                "\"disposition\":\"KEEP_BASELINE\"}");
        Check(
            !missingFingerprint.Admitted &&
            missingFingerprint.RejectionReasons.Contains(
                "REQUEST_FINGERPRINT_MISSING"),
            "advisory_missing_fingerprint_rejected");

        var missingDisposition =
            PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                request.RequestFingerprint,
                "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
                "\"requestFingerprint\":\"" +
                request.RequestFingerprint +
                "\"}");
        Check(
            !missingDisposition.Admitted &&
            missingDisposition.RejectionReasons.Contains(
                "DISPOSITION_MISSING"),
            "advisory_missing_disposition_rejected");

        var badSchema =
            PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                request.RequestFingerprint,
                "{\"schema\":\"Wrong.Schema\"," +
                "\"requestFingerprint\":\"" +
                request.RequestFingerprint +
                "\"," +
                "\"disposition\":\"KEEP_BASELINE\"}");
        Check(
            !badSchema.Admitted &&
            badSchema.RejectionReasons.Contains(
                "SCHEMA_INVALID"),
            "advisory_bad_schema_rejected");

        var unknownDisposition =
            PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                request.RequestFingerprint,
                advisoryPrefix +
                "\"disposition\":\"ATTACK_NOW\"}");
        Check(
            !unknownDisposition.Admitted &&
            unknownDisposition.RejectionReasons.Contains(
                "DISPOSITION_INVALID"),
            "advisory_unknown_disposition_rejected");

        string[] forbiddenKeys =
            new[]
            {
                "action",
                "target",
                "score",
                "scoreDelta",
                "movement",
                "apply",
                "command"
            };
        bool allForbiddenRejected = true;
        for (int i = 0; i < forbiddenKeys.Length; i++)
        {
            string key = forbiddenKeys[i];
            var forbidden =
                PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                    request.RequestFingerprint,
                    advisoryPrefix +
                    "\"disposition\":\"KEEP_BASELINE\"," +
                    "\"" + key + "\":\"x\"}");
            if (
                forbidden.Admitted ||
                !forbidden.RejectionReasons.Contains(
                    "UNEXPECTED_FIELD:" + key))
            {
                allForbiddenRejected = false;
                break;
            }
        }
        Check(
            allForbiddenRejected,
            "advisory_action_fields_rejected");

        var extraHarmless =
            PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                request.RequestFingerprint,
                advisoryPrefix +
                "\"disposition\":\"KEEP_BASELINE\"," +
                "\"note\":\"hello\"}");
        Check(
            !extraHarmless.Admitted &&
            extraHarmless.RejectionReasons.Contains(
                "UNEXPECTED_FIELD:note"),
            "advisory_unknown_extra_rejected");

        var malformed =
            PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                request.RequestFingerprint,
                "{\"schema\":");
        Check(
            !malformed.Admitted &&
            malformed.RejectionReasons.Contains(
                "MALFORMED_JSON"),
            "advisory_malformed_rejected");

        var duplicateField =
            PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                request.RequestFingerprint,
                "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
                "\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
                "\"requestFingerprint\":\"" +
                request.RequestFingerprint +
                "\"," +
                "\"disposition\":\"KEEP_BASELINE\"}");
        Check(
            !duplicateField.Admitted &&
            duplicateField.RejectionReasons.Contains(
                "MALFORMED_JSON"),
            "advisory_duplicate_field_rejected");

        var missingExpected =
            PatrolDefenseDeliberationAdvisoryAdmission.Evaluate(
                null,
                advisoryPrefix +
                "\"disposition\":\"KEEP_BASELINE\"}");
        Check(
            !missingExpected.Admitted &&
            missingExpected.RejectionReasons.Contains(
                "EXPECTED_REQUEST_FINGERPRINT_MISSING"),
            "advisory_missing_expected_rejected");

        string admissionJson =
            PatrolDefenseDeliberationAdvisoryAdmission.ToJson(
                keepAdmission);
        Check(
            admissionJson.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisoryAdmission.v1\""),
            "admission_schema");
        Check(
            admissionJson.Contains(
                "\"requestFingerprint\":\"" +
                request.RequestFingerprint +
                "\""),
            "admission_fingerprint_exact");
        Check(
            admissionJson.Contains("\"admitted\":true") &&
            admissionJson.Contains(
                "\"disposition\":\"KEEP_BASELINE\""),
            "admission_result_exact");
        Check(
            admissionJson.Contains("\"advisoryOnly\":true") &&
            admissionJson.Contains("\"executionAuthorized\":false"),
            "admission_never_executes");
        Check(
            admissionJson.Contains("\"modelInvoked\":false") &&
            admissionJson.Contains("\"llmInvoked\":false") &&
            admissionJson.Contains("\"plannerInvoked\":false"),
            "admission_no_model_invocation");
        Check(
            admissionJson.Contains("\"behaviorMutation\":false") &&
            admissionJson.Contains("\"intentMutation\":false") &&
            admissionJson.Contains("\"scoreMutation\":false") &&
            admissionJson.Contains("\"nativeMovementCalls\":0"),
            "admission_zero_authority");

        var runtimeGate =
            new PatrolDefenseAdvisoryRuntimeGate();

        var noPending =
            runtimeGate.Evaluate(
                request.RequestFingerprint,
                advisoryPrefix +
                "\"disposition\":\"KEEP_BASELINE\"}");
        Check(
            !noPending.Admitted &&
            noPending.RejectionReasons.Contains(
                "NO_PENDING_ROUTE_REQUEST"),
            "runtime_no_pending_rejected");

        Check(
            runtimeGate.Arm(
                request.RequestFingerprint) &&
            runtimeGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "runtime_pending_armed");

        var pendingMismatch =
            runtimeGate.Evaluate(
                "WRONG",
                advisoryPrefix +
                "\"disposition\":\"KEEP_BASELINE\"}");
        Check(
            !pendingMismatch.Admitted &&
            pendingMismatch.RejectionReasons.Contains(
                "PENDING_REQUEST_FINGERPRINT_MISMATCH") &&
            runtimeGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "runtime_mismatch_retains_pending");

        var pendingMalformed =
            runtimeGate.Evaluate(
                request.RequestFingerprint,
                "{\"schema\":");
        Check(
            !pendingMalformed.Admitted &&
            pendingMalformed.RejectionReasons.Contains(
                "MALFORMED_JSON") &&
            runtimeGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "runtime_malformed_retains_pending");

        var runtimeKeep =
            runtimeGate.Evaluate(
                request.RequestFingerprint,
                advisoryPrefix +
                "\"disposition\":\"KEEP_BASELINE\"}");
        Check(
            runtimeKeep.Admitted &&
            runtimeKeep.Disposition ==
                "KEEP_BASELINE" &&
            runtimeGate.PendingRequestFingerprint == null,
            "runtime_exact_admitted_consumes");

        var replayAfterConsume =
            runtimeGate.Evaluate(
                request.RequestFingerprint,
                advisoryPrefix +
                "\"disposition\":\"KEEP_BASELINE\"}");
        Check(
            !replayAfterConsume.Admitted &&
            replayAfterConsume.RejectionReasons.Contains(
                "NO_PENDING_ROUTE_REQUEST"),
            "runtime_replay_rejected");

        runtimeGate.Arm(
            request.RequestFingerprint);
        runtimeGate.Reset();
        Check(
            runtimeGate.PendingRequestFingerprint == null,
            "runtime_reset_clears");

        string[] runtimeDispositions =
            new[]
            {
                "KEEP_BASELINE",
                "REVIEW_ALTERNATIVE",
                "ABSTAIN"
            };
        bool allRuntimeDispositions = true;
        for (
            int i = 0;
            i < runtimeDispositions.Length;
            i++)
        {
            runtimeGate.Reset();
            runtimeGate.Arm(
                request.RequestFingerprint);
            var runtimeAdmission =
                runtimeGate.Evaluate(
                    request.RequestFingerprint,
                    advisoryPrefix +
                    "\"disposition\":\"" +
                    runtimeDispositions[i] +
                    "\"}");
            if (
                !runtimeAdmission.Admitted ||
                runtimeAdmission.Disposition !=
                    runtimeDispositions[i])
            {
                allRuntimeDispositions = false;
                break;
            }
        }
        Check(
            allRuntimeDispositions,
            "runtime_all_dispositions_admit");

        string runtimeAdmissionJson =
            PatrolDefenseDeliberationAdvisoryAdmission.ToJson(
                runtimeKeep);
        Check(
            runtimeAdmissionJson.Contains(
                "\"executionAuthorized\":false") &&
            runtimeAdmissionJson.Contains(
                "\"behaviorMutation\":false") &&
            runtimeAdmissionJson.Contains(
                "\"scoreMutation\":false"),
            "runtime_admission_zero_authority");

        var providerGate =
            new PatrolDefenseAdvisoryRuntimeGate();
        string providerRequestJson =
            requestJson;

        var providerNoPending =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new PatrolDefenseMockDeliberationProvider(
                    "KEEP_BASELINE"),
                providerGate,
                request.RequestFingerprint,
                providerRequestJson);
        Check(
            providerNoPending != null &&
            !providerNoPending.ProviderInvoked &&
            !providerNoPending.Admission.Admitted &&
            providerNoPending.Admission.RejectionReasons.Contains(
                "NO_PENDING_ROUTE_REQUEST"),
            "provider_no_pending_not_invoked");

        providerGate.Arm(
            request.RequestFingerprint);
        var providerMismatch =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new PatrolDefenseMockDeliberationProvider(
                    "KEEP_BASELINE"),
                providerGate,
                "WRONG",
                providerRequestJson);
        Check(
            providerMismatch != null &&
            !providerMismatch.ProviderInvoked &&
            !providerMismatch.Admission.Admitted &&
            providerMismatch.Admission.RejectionReasons.Contains(
                "PENDING_REQUEST_FINGERPRINT_MISMATCH") &&
            providerGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_mismatch_not_invoked_pending_retained");

        var providerMalformed =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new FixtureMalformedProvider(),
                providerGate,
                request.RequestFingerprint,
                providerRequestJson);
        Check(
            providerMalformed.ProviderInvoked &&
            !providerMalformed.Admission.Admitted &&
            providerMalformed.Admission.RejectionReasons.Contains(
                "MALFORMED_JSON") &&
            providerGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_malformed_rejected_pending_retained");

        var providerThrow =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new FixtureThrowingProvider(),
                providerGate,
                request.RequestFingerprint,
                providerRequestJson);
        Check(
            providerThrow.ProviderInvoked &&
            !providerThrow.Admission.Admitted &&
            providerThrow.Admission.RejectionReasons.Contains(
                "PROVIDER_EXCEPTION:InvalidOperationException") &&
            providerGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_exception_rejected_pending_retained");

        var providerKeep =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new PatrolDefenseMockDeliberationProvider(
                    "KEEP_BASELINE"),
                providerGate,
                request.RequestFingerprint,
                providerRequestJson);
        Check(
            providerKeep.ProviderInvoked &&
            providerKeep.ProviderId ==
                "deterministic_local_mock" &&
            providerKeep.RawAdvisoryJson.Contains(
                "\"requestFingerprint\":\"" +
                request.RequestFingerprint +
                "\"") &&
            providerKeep.Admission.Admitted &&
            providerKeep.Admission.Disposition ==
                "KEEP_BASELINE" &&
            providerGate.PendingRequestFingerprint == null,
            "provider_exact_admitted_consumes");

        var providerReplay =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new PatrolDefenseMockDeliberationProvider(
                    "KEEP_BASELINE"),
                providerGate,
                request.RequestFingerprint,
                providerRequestJson);
        Check(
            !providerReplay.ProviderInvoked &&
            !providerReplay.Admission.Admitted &&
            providerReplay.Admission.RejectionReasons.Contains(
                "NO_PENDING_ROUTE_REQUEST"),
            "provider_replay_rejected");

        string[] providerDispositions =
            new[]
            {
                "KEEP_BASELINE",
                "REVIEW_ALTERNATIVE",
                "ABSTAIN"
            };
        bool allProviderDispositions = true;
        for (
            int i = 0;
            i < providerDispositions.Length;
            i++)
        {
            providerGate.Reset();
            providerGate.Arm(
                request.RequestFingerprint);
            var invocation =
                PatrolDefenseDeliberationProviderRuntime.Invoke(
                    new PatrolDefenseMockDeliberationProvider(
                        providerDispositions[i]),
                    providerGate,
                    request.RequestFingerprint,
                    providerRequestJson);
            if (
                !invocation.ProviderInvoked ||
                !invocation.Admission.Admitted ||
                invocation.Admission.Disposition !=
                    providerDispositions[i])
            {
                allProviderDispositions = false;
                break;
            }
        }
        Check(
            allProviderDispositions,
            "provider_all_allowed_dispositions");

        providerGate.Reset();
        providerGate.Arm(
            request.RequestFingerprint);
        var invalidProviderDisposition =
            PatrolDefenseDeliberationProviderRuntime.Invoke(
                new PatrolDefenseMockDeliberationProvider(
                    "ATTACK_NOW"),
                providerGate,
                request.RequestFingerprint,
                providerRequestJson);
        Check(
            invalidProviderDisposition.ProviderInvoked &&
            !invalidProviderDisposition.Admission.Admitted &&
            invalidProviderDisposition.Admission.RejectionReasons.Contains(
                "DISPOSITION_INVALID") &&
            providerGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_invalid_disposition_rejected_pending_retained");

        string providerJson =
            PatrolDefenseDeliberationProviderRuntime.ToJson(
                providerKeep);
        Check(
            providerJson.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseDeliberationProviderInvocation.v1\"") &&
            providerJson.Contains(
                "\"providerId\":\"deterministic_local_mock\"") &&
            providerJson.Contains(
                "\"providerInvoked\":true"),
            "provider_receipt_identity");
        Check(
            providerJson.Contains(
                "\"externalNetworkUsed\":false") &&
            providerJson.Contains(
                "\"modelInvoked\":false") &&
            providerJson.Contains(
                "\"executionAuthorized\":false"),
            "provider_receipt_no_network_model_execution");
        Check(
            providerJson.Contains(
                "\"behaviorMutation\":false") &&
            providerJson.Contains(
                "\"scoreMutation\":false") &&
            providerJson.Contains(
                "\"nativeMovementCalls\":0"),
            "provider_receipt_zero_authority");

        var dispatchQueue =
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

        string providerResultAdvisory =
            "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\"," +
            "\"disposition\":\"KEEP_BASELINE\"}";
        string providerResultAdvisoryBase64 =
            Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    providerResultAdvisory));

        string ProviderEnvelope(
            string providerId,
            string attemptId,
            string fingerprint,
            string status,
            string advisoryBase64,
            string extra)
        {
            string json =
                "{\"schema\":\"BannerlordAI.PatrolDefenseProviderResult.v1\"," +
                "\"providerId\":\"" + providerId + "\"," +
                "\"attemptId\":\"" + attemptId + "\"," +
                "\"requestFingerprint\":\"" + fingerprint + "\"," +
                "\"status\":\"" + status + "\"," +
                "\"advisoryBase64\":\"" + advisoryBase64 + "\"";
            if (!string.IsNullOrEmpty(extra))
                json += "," + extra;
            return json + "}";
        }

        var resultGate =
            new PatrolDefenseAdvisoryRuntimeGate();
        resultGate.Arm(
            request.RequestFingerprint);

        var resultSuccess =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-1",
                    request.RequestFingerprint,
                    "SUCCESS",
                    providerResultAdvisoryBase64,
                    null));
        Check(
            resultSuccess.ProviderResultAccepted &&
            !resultSuccess.Retryable &&
            resultSuccess.ProviderId ==
                "deterministic_external_worker" &&
            resultSuccess.AttemptId == "attempt-1" &&
            resultSuccess.RequestFingerprint ==
                request.RequestFingerprint &&
            resultSuccess.Status == "SUCCESS" &&
            resultSuccess.AdvisoryAdmission != null &&
            resultSuccess.AdvisoryAdmission.Admitted &&
            resultSuccess.AdvisoryAdmission.Disposition ==
                "KEEP_BASELINE" &&
            resultGate.PendingRequestFingerprint == null,
            "provider_result_success_consumes");

        var resultReplay =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-2",
                    request.RequestFingerprint,
                    "SUCCESS",
                    providerResultAdvisoryBase64,
                    null));
        Check(
            !resultReplay.ProviderResultAccepted &&
            resultReplay.RejectionReasons.Contains(
                "NO_PENDING_ROUTE_REQUEST"),
            "provider_result_replay_rejected");
        Check(
            resultReplay.AdvisoryAdmission == null,
            "provider_result_replay_advisory_null");
        string resultReplayJson =
            PatrolDefenseProviderResultAdmission.ToJson(
                resultReplay);
        Check(
            resultReplayJson.Contains(
                "\"advisoryAdmission\":null") &&
            !resultReplayJson.Contains(
                "\"advisoryAdmission\":,"),
            "provider_result_replay_null_serialized");

        resultGate.Arm(
            request.RequestFingerprint);
        var resultMismatch =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-mismatch",
                    "WRONG",
                    "SUCCESS",
                    providerResultAdvisoryBase64,
                    null));
        Check(
            !resultMismatch.ProviderResultAccepted &&
            resultMismatch.RejectionReasons.Contains(
                "PENDING_REQUEST_FINGERPRINT_MISMATCH") &&
            resultGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_result_mismatch_retains_pending");

        var resultTransient =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-transient",
                    request.RequestFingerprint,
                    "TRANSIENT_FAILURE",
                    "",
                    null));
        Check(
            !resultTransient.ProviderResultAccepted &&
            resultTransient.Retryable &&
            resultTransient.RejectionReasons.Contains(
                "PROVIDER_TRANSIENT_FAILURE") &&
            resultGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_result_transient_retryable");

        var resultPermanent =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-permanent",
                    request.RequestFingerprint,
                    "PERMANENT_FAILURE",
                    "",
                    null));
        Check(
            !resultPermanent.ProviderResultAccepted &&
            !resultPermanent.Retryable &&
            resultPermanent.RejectionReasons.Contains(
                "PROVIDER_PERMANENT_FAILURE") &&
            resultGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_result_permanent_fail_closed");

        var resultBadBase64 =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-bad64",
                    request.RequestFingerprint,
                    "SUCCESS",
                    "%%%",
                    null));
        Check(
            !resultBadBase64.ProviderResultAccepted &&
            resultBadBase64.Retryable &&
            resultBadBase64.RejectionReasons.Contains(
                "ADVISORY_BASE64_INVALID") &&
            resultGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_result_bad_advisory_base64_retry");

        string actionAdvisory =
            "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\"," +
            "\"disposition\":\"KEEP_BASELINE\"," +
            "\"action\":\"ATTACK\"}";
        string actionAdvisoryBase64 =
            Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    actionAdvisory));
        var resultActionBearing =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-action",
                    request.RequestFingerprint,
                    "SUCCESS",
                    actionAdvisoryBase64,
                    null));
        Check(
            !resultActionBearing.ProviderResultAccepted &&
            resultActionBearing.Retryable &&
            resultActionBearing.RejectionReasons.Contains(
                "ADVISORY_NOT_ADMITTED") &&
            resultActionBearing.AdvisoryAdmission != null &&
            resultActionBearing.AdvisoryAdmission.RejectionReasons.Contains(
                "UNEXPECTED_FIELD:action") &&
            resultGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "provider_result_action_advisory_rejected");

        var resultUnknownStatus =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-unknown",
                    request.RequestFingerprint,
                    "MAYBE",
                    providerResultAdvisoryBase64,
                    null));
        Check(
            !resultUnknownStatus.ProviderResultAccepted &&
            resultUnknownStatus.RejectionReasons.Contains(
                "STATUS_INVALID"),
            "provider_result_unknown_status_rejected");

        var resultExtraField =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-extra",
                    request.RequestFingerprint,
                    "SUCCESS",
                    providerResultAdvisoryBase64,
                    "\"command\":\"ATTACK\""));
        Check(
            !resultExtraField.ProviderResultAccepted &&
            resultExtraField.RejectionReasons.Contains(
                "UNEXPECTED_FIELD:command"),
            "provider_result_extra_field_rejected");

        string missingProviderEnvelope =
            "{\"schema\":\"BannerlordAI.PatrolDefenseProviderResult.v1\"," +
            "\"attemptId\":\"attempt-x\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\"," +
            "\"status\":\"SUCCESS\"," +
            "\"advisoryBase64\":\"" +
            providerResultAdvisoryBase64 +
            "\"}";
        var resultMissingProvider =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                missingProviderEnvelope);
        Check(
            !resultMissingProvider.ProviderResultAccepted &&
            resultMissingProvider.RejectionReasons.Contains(
                "PROVIDER_ID_MISSING"),
            "provider_result_missing_provider_rejected");

        string missingAttemptEnvelope =
            "{\"schema\":\"BannerlordAI.PatrolDefenseProviderResult.v1\"," +
            "\"providerId\":\"deterministic_external_worker\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\"," +
            "\"status\":\"SUCCESS\"," +
            "\"advisoryBase64\":\"" +
            providerResultAdvisoryBase64 +
            "\"}";
        var resultMissingAttempt =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                missingAttemptEnvelope);
        Check(
            !resultMissingAttempt.ProviderResultAccepted &&
            resultMissingAttempt.RejectionReasons.Contains(
                "ATTEMPT_ID_MISSING"),
            "provider_result_missing_attempt_rejected");

        var resultMalformedEnvelope =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                "{\"schema\":");
        Check(
            !resultMalformedEnvelope.ProviderResultAccepted &&
            resultMalformedEnvelope.RejectionReasons.Contains(
                "MALFORMED_JSON"),
            "provider_result_malformed_envelope_rejected");

        resultGate.Reset();
        resultGate.Arm(
            request.RequestFingerprint);
        var resultReceiptSample =
            PatrolDefenseProviderResultAdmission.Evaluate(
                resultGate,
                ProviderEnvelope(
                    "deterministic_external_worker",
                    "attempt-receipt",
                    request.RequestFingerprint,
                    "SUCCESS",
                    providerResultAdvisoryBase64,
                    null));
        string resultReceiptJson =
            PatrolDefenseProviderResultAdmission.ToJson(
                resultReceiptSample);
        Check(
            resultReceiptJson.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderResultAdmission.v1\"") &&
            resultReceiptJson.Contains(
                "\"providerId\":\"deterministic_external_worker\"") &&
            resultReceiptJson.Contains(
                "\"attemptId\":\"attempt-receipt\"") &&
            resultReceiptJson.Contains(
                "\"providerResultAccepted\":true") &&
            resultReceiptJson.Contains(
                "\"executionAuthorized\":false") &&
            resultReceiptJson.Contains(
                "\"modelInvoked\":false") &&
            resultReceiptJson.Contains(
                "\"behaviorMutation\":false"),
            "provider_result_receipt_zero_authority");

        var lifecycleQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        var lifecycleEnqueue =
            lifecycleQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                200);
        Check(
            lifecycleEnqueue.Enqueued &&
            lifecycleQueue.Count == 1,
            "lifecycle_enqueue");

        var lifecycleTransient =
            lifecycleQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "TRANSIENT_FAILURE",
                false);
        Check(
            !lifecycleTransient.CompletionApplied &&
            lifecycleTransient.Reason ==
                "TRANSIENT_FAILURE_RETAINED" &&
            lifecycleTransient.QueueCountBefore == 1 &&
            lifecycleTransient.QueueCountAfter == 1 &&
            lifecycleTransient.PendingRetained &&
            lifecycleQueue.Count == 1,
            "lifecycle_transient_retained");

        var lifecycleInvalidStatus =
            lifecycleQueue.ApplyProviderResult(
                request.RequestFingerprint,
                null,
                false);
        Check(
            !lifecycleInvalidStatus.CompletionApplied &&
            lifecycleInvalidStatus.Reason ==
                "RESULT_INVALID_RETAINED" &&
            lifecycleInvalidStatus.PendingRetained &&
            lifecycleQueue.Count == 1,
            "lifecycle_invalid_retained");

        var lifecycleSuccessRejected =
            lifecycleQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "SUCCESS",
                false);
        Check(
            !lifecycleSuccessRejected.CompletionApplied &&
            lifecycleSuccessRejected.Reason ==
                "SUCCESS_NOT_ADMITTED_RETAINED" &&
            lifecycleSuccessRejected.PendingRetained &&
            lifecycleQueue.Count == 1,
            "lifecycle_success_not_admitted_retained");

        var lifecycleSuccess =
            lifecycleQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "SUCCESS",
                true);
        Check(
            lifecycleSuccess.CompletionApplied &&
            lifecycleSuccess.Reason ==
                "SUCCESS_ACKED" &&
            lifecycleSuccess.QueueCountBefore == 1 &&
            lifecycleSuccess.QueueCountAfter == 0 &&
            !lifecycleSuccess.PendingRetained &&
            lifecycleQueue.Count == 0,
            "lifecycle_success_removes");

        var lifecycleDouble =
            lifecycleQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "SUCCESS",
                true);
        Check(
            !lifecycleDouble.CompletionApplied &&
            lifecycleDouble.Reason ==
                "DISPATCH_JOB_NOT_FOUND" &&
            lifecycleDouble.QueueCountBefore == 0 &&
            lifecycleDouble.QueueCountAfter == 0,
            "lifecycle_double_complete_noop");

        var lifecycleAfterSuccess =
            lifecycleQueue.Enqueue(
                changedRequest.RequestFingerprint,
                PatrolDefenseDeliberationRequestBuilder.ToJson(
                    changedRequest),
                201);
        Check(
            lifecycleAfterSuccess.Enqueued &&
            lifecycleAfterSuccess.Job.Sequence >
                lifecycleEnqueue.Job.Sequence &&
            lifecycleQueue.Count == 1,
            "lifecycle_capacity_freed_sequence_monotonic");

        var lifecyclePermanent =
            lifecycleQueue.ApplyProviderResult(
                changedRequest.RequestFingerprint,
                "PERMANENT_FAILURE",
                false);
        Check(
            lifecyclePermanent.CompletionApplied &&
            lifecyclePermanent.Reason ==
                "PERMANENT_FAILURE_ACKED" &&
            lifecyclePermanent.QueueCountBefore == 1 &&
            lifecyclePermanent.QueueCountAfter == 0 &&
            !lifecyclePermanent.PendingRetained &&
            lifecycleQueue.Count == 0,
            "lifecycle_permanent_removes");

        var unrelatedQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        unrelatedQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            300);
        var lifecycleWrong =
            unrelatedQueue.ApplyProviderResult(
                "WRONG",
                "SUCCESS",
                true);
        Check(
            !lifecycleWrong.CompletionApplied &&
            lifecycleWrong.Reason ==
                "DISPATCH_JOB_NOT_FOUND" &&
            unrelatedQueue.Count == 1,
            "lifecycle_wrong_fingerprint_preserves_other");

        var lifecycleInvalidRequest =
            unrelatedQueue.ApplyProviderResult(
                null,
                "SUCCESS",
                true);
        Check(
            !lifecycleInvalidRequest.CompletionApplied &&
            lifecycleInvalidRequest.Reason ==
                "INVALID_REQUEST" &&
            unrelatedQueue.Count == 1,
            "lifecycle_invalid_request_preserves_queue");

        string lifecycleReceipt =
            PatrolDefenseProviderDispatchBuilder.ToCompletionJson(
                lifecycleSuccess);
        Check(
            lifecycleReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderDispatchCompletion.v1\"") &&
            lifecycleReceipt.Contains(
                "\"reason\":\"SUCCESS_ACKED\"") &&
            lifecycleReceipt.Contains(
                "\"queueCountBefore\":1") &&
            lifecycleReceipt.Contains(
                "\"queueCountAfter\":0") &&
            lifecycleReceipt.Contains(
                "\"pendingRetained\":false"),
            "lifecycle_receipt_exact");
        Check(
            lifecycleReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            lifecycleReceipt.Contains(
                "\"modelInvoked\":false") &&
            lifecycleReceipt.Contains(
                "\"executionAuthorized\":false") &&
            lifecycleReceipt.Contains(
                "\"behaviorMutation\":false") &&
            lifecycleReceipt.Contains(
                "\"scoreMutation\":false"),
            "lifecycle_receipt_zero_authority");

        var claimQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        var claimEnqueue =
            claimQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                400);
        Check(
            claimEnqueue.Enqueued &&
            claimEnqueue.Job.State == "PENDING",
            "claim_fixture_enqueued_pending");

        var firstClaim =
            claimQueue.Claim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1",
                500);
        Check(
            firstClaim.ClaimApplied &&
            !firstClaim.Idempotent &&
            firstClaim.Reason == "CLAIMED" &&
            firstClaim.StateBefore == "PENDING" &&
            firstClaim.StateAfter == "CLAIMED" &&
            firstClaim.QueueCount == 1 &&
            firstClaim.ClaimedUtcTicks == 500 &&
            claimEnqueue.Job.State == "CLAIMED" &&
            claimEnqueue.Job.ProviderId ==
                "deterministic_external_worker" &&
            claimEnqueue.Job.AttemptId ==
                "attempt-1",
            "claim_first_exact");

        var sameClaim =
            claimQueue.Claim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1",
                501);
        Check(
            !sameClaim.ClaimApplied &&
            sameClaim.Idempotent &&
            sameClaim.Reason ==
                "SAME_ATTEMPT_ALREADY_CLAIMED" &&
            sameClaim.StateBefore == "CLAIMED" &&
            sameClaim.StateAfter == "CLAIMED" &&
            sameClaim.ClaimedUtcTicks == 500,
            "claim_same_attempt_idempotent");

        var competingClaim =
            claimQueue.Claim(
                request.RequestFingerprint,
                "other_worker",
                "attempt-2",
                502);
        Check(
            !competingClaim.ClaimApplied &&
            !competingClaim.Idempotent &&
            competingClaim.Reason ==
                "ALREADY_CLAIMED" &&
            claimEnqueue.Job.ProviderId ==
                "deterministic_external_worker" &&
            claimEnqueue.Job.AttemptId ==
                "attempt-1",
            "claim_competing_provider_rejected");

        var sameProviderNewAttempt =
            claimQueue.Claim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-2",
                503);
        Check(
            !sameProviderNewAttempt.ClaimApplied &&
            !sameProviderNewAttempt.Idempotent &&
            sameProviderNewAttempt.Reason ==
                "ALREADY_CLAIMED",
            "claim_same_provider_new_attempt_rejected");

        var invalidClaim =
            claimQueue.Claim(
                null,
                "worker",
                "attempt",
                504);
        Check(
            !invalidClaim.ClaimApplied &&
            invalidClaim.Reason ==
                "INVALID_CLAIM" &&
            claimQueue.Count == 1,
            "claim_invalid_rejected");

        var missingJobClaim =
            claimQueue.Claim(
                "missing",
                "worker",
                "attempt",
                505);
        Check(
            !missingJobClaim.ClaimApplied &&
            missingJobClaim.Reason ==
                "DISPATCH_JOB_NOT_FOUND" &&
            claimQueue.Count == 1,
            "claim_missing_job_rejected");

        var transientClaimed =
            claimQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "TRANSIENT_FAILURE",
                false);
        Check(
            !transientClaimed.CompletionApplied &&
            transientClaimed.PendingRetained &&
            claimQueue.Count == 1 &&
            claimEnqueue.Job.State == "CLAIMED",
            "claim_transient_retains_claimed_job");

        var successClaimed =
            claimQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "SUCCESS",
                true);
        Check(
            successClaimed.CompletionApplied &&
            successClaimed.Reason ==
                "SUCCESS_ACKED" &&
            claimQueue.Count == 0,
            "claim_success_removes_claimed_job");

        claimQueue.Reset();
        var afterClaimReset =
            claimQueue.Enqueue(
                changedRequest.RequestFingerprint,
                PatrolDefenseDeliberationRequestBuilder.ToJson(
                    changedRequest),
                506);
        Check(
            afterClaimReset.Enqueued &&
            afterClaimReset.Job.Sequence >
                claimEnqueue.Job.Sequence &&
            afterClaimReset.Job.State ==
                "PENDING",
            "claim_reset_clears_and_sequence_monotonic");

        string claimReceipt =
            PatrolDefenseProviderDispatchBuilder.ToClaimJson(
                firstClaim);
        Check(
            claimReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderJobClaim.v1\"") &&
            claimReceipt.Contains(
                "\"providerId\":\"deterministic_external_worker\"") &&
            claimReceipt.Contains(
                "\"attemptId\":\"attempt-1\"") &&
            claimReceipt.Contains(
                "\"claimApplied\":true") &&
            claimReceipt.Contains(
                "\"stateBefore\":\"PENDING\"") &&
            claimReceipt.Contains(
                "\"stateAfter\":\"CLAIMED\"") &&
            claimReceipt.Contains(
                "\"claimedUtcTicks\":500"),
            "claim_receipt_exact");
        Check(
            claimReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            claimReceipt.Contains(
                "\"modelInvoked\":false") &&
            claimReceipt.Contains(
                "\"executionAuthorized\":false") &&
            claimReceipt.Contains(
                "\"behaviorMutation\":false") &&
            claimReceipt.Contains(
                "\"scoreMutation\":false"),
            "claim_receipt_zero_authority");

        string ClaimBoundEnvelope(
            string providerId,
            string attemptId,
            string fingerprint,
            string status,
            string advisoryBase64)
        {
            return
                "{\"schema\":\"BannerlordAI.PatrolDefenseProviderResult.v1\"," +
                "\"providerId\":\"" + providerId + "\"," +
                "\"attemptId\":\"" + attemptId + "\"," +
                "\"requestFingerprint\":\"" + fingerprint + "\"," +
                "\"status\":\"" + status + "\"," +
                "\"advisoryBase64\":\"" + advisoryBase64 + "\"}";
        }

        string claimBoundAdvisory =
            "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\"," +
            "\"disposition\":\"KEEP_BASELINE\"}";
        string claimBoundAdvisoryBase64 =
            Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    claimBoundAdvisory));

        var claimBoundGate =
            new PatrolDefenseAdvisoryRuntimeGate();
        claimBoundGate.Arm(
            request.RequestFingerprint);
        var claimBoundQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        claimBoundQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            600);

        var unclaimedResult =
            PatrolDefenseProviderResultAdmission.EvaluateClaimBound(
                claimBoundGate,
                claimBoundQueue,
                ClaimBoundEnvelope(
                    "deterministic_external_worker",
                    "attempt-1",
                    request.RequestFingerprint,
                    "SUCCESS",
                    claimBoundAdvisoryBase64));
        Check(
            !unclaimedResult.ProviderResultAccepted &&
            unclaimedResult.ClaimMatchReason ==
                "PROVIDER_JOB_NOT_CLAIMED" &&
            unclaimedResult.RejectionReasons.Contains(
                "PROVIDER_JOB_NOT_CLAIMED") &&
            claimBoundGate.PendingRequestFingerprint ==
                request.RequestFingerprint &&
            claimBoundQueue.Count == 1,
            "claim_bound_unclaimed_rejected");

        claimBoundQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            601);

        var wrongProviderResult =
            PatrolDefenseProviderResultAdmission.EvaluateClaimBound(
                claimBoundGate,
                claimBoundQueue,
                ClaimBoundEnvelope(
                    "other_worker",
                    "attempt-1",
                    request.RequestFingerprint,
                    "SUCCESS",
                    claimBoundAdvisoryBase64));
        Check(
            !wrongProviderResult.ProviderResultAccepted &&
            wrongProviderResult.ClaimMatchReason ==
                "PROVIDER_CLAIM_MISMATCH" &&
            wrongProviderResult.RejectionReasons.Contains(
                "PROVIDER_CLAIM_MISMATCH") &&
            claimBoundGate.PendingRequestFingerprint ==
                request.RequestFingerprint &&
            claimBoundQueue.Count == 1,
            "claim_bound_wrong_provider_rejected");

        var wrongAttemptResult =
            PatrolDefenseProviderResultAdmission.EvaluateClaimBound(
                claimBoundGate,
                claimBoundQueue,
                ClaimBoundEnvelope(
                    "deterministic_external_worker",
                    "attempt-2",
                    request.RequestFingerprint,
                    "SUCCESS",
                    claimBoundAdvisoryBase64));
        Check(
            !wrongAttemptResult.ProviderResultAccepted &&
            wrongAttemptResult.ClaimMatchReason ==
                "PROVIDER_CLAIM_MISMATCH" &&
            claimBoundGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "claim_bound_wrong_attempt_rejected");

        var missingQueueGate =
            new PatrolDefenseAdvisoryRuntimeGate();
        missingQueueGate.Arm(
            request.RequestFingerprint);
        var missingQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        var missingJobResult =
            PatrolDefenseProviderResultAdmission.EvaluateClaimBound(
                missingQueueGate,
                missingQueue,
                ClaimBoundEnvelope(
                    "deterministic_external_worker",
                    "attempt-1",
                    request.RequestFingerprint,
                    "SUCCESS",
                    claimBoundAdvisoryBase64));
        Check(
            !missingJobResult.ProviderResultAccepted &&
            missingJobResult.ClaimMatchReason ==
                "DISPATCH_JOB_NOT_FOUND" &&
            missingQueueGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "claim_bound_missing_job_rejected");

        var malformedClaimBound =
            PatrolDefenseProviderResultAdmission.EvaluateClaimBound(
                claimBoundGate,
                claimBoundQueue,
                "{\"schema\":");
        Check(
            !malformedClaimBound.ProviderResultAccepted &&
            malformedClaimBound.RejectionReasons.Contains(
                "MALFORMED_JSON") &&
            claimBoundGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "claim_bound_malformed_retains_pending");

        var transientClaimBound =
            PatrolDefenseProviderResultAdmission.EvaluateClaimBound(
                claimBoundGate,
                claimBoundQueue,
                ClaimBoundEnvelope(
                    "deterministic_external_worker",
                    "attempt-1",
                    request.RequestFingerprint,
                    "TRANSIENT_FAILURE",
                    ""));
        Check(
            !transientClaimBound.ProviderResultAccepted &&
            transientClaimBound.ClaimMatchReason ==
                "CLAIM_MATCHED" &&
            transientClaimBound.Retryable &&
            transientClaimBound.RejectionReasons.Contains(
                "PROVIDER_TRANSIENT_FAILURE") &&
            claimBoundGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "claim_bound_transient_matches_retains");

        var transientCompletion =
            claimBoundQueue.ApplyProviderResult(
                transientClaimBound.RequestFingerprint,
                transientClaimBound.Status,
                transientClaimBound.ProviderResultAccepted);
        Check(
            transientCompletion.Reason ==
                "TRANSIENT_FAILURE_RETAINED" &&
            transientCompletion.PendingRetained &&
            claimBoundQueue.Count == 1,
            "claim_bound_transient_completion_retains_claim");

        var exactSuccess =
            PatrolDefenseProviderResultAdmission.EvaluateClaimBound(
                claimBoundGate,
                claimBoundQueue,
                ClaimBoundEnvelope(
                    "deterministic_external_worker",
                    "attempt-1",
                    request.RequestFingerprint,
                    "SUCCESS",
                    claimBoundAdvisoryBase64));
        Check(
            exactSuccess.ProviderResultAccepted &&
            exactSuccess.ClaimMatchReason ==
                "CLAIM_MATCHED" &&
            exactSuccess.AdvisoryAdmission != null &&
            exactSuccess.AdvisoryAdmission.Admitted &&
            claimBoundGate.PendingRequestFingerprint == null,
            "claim_bound_exact_success_admitted");

        var exactCompletion =
            claimBoundQueue.ApplyProviderResult(
                exactSuccess.RequestFingerprint,
                exactSuccess.Status,
                exactSuccess.ProviderResultAccepted);
        Check(
            exactCompletion.CompletionApplied &&
            exactCompletion.Reason ==
                "SUCCESS_ACKED" &&
            claimBoundQueue.Count == 0,
            "claim_bound_success_completion_removes");

        var replayClaimBound =
            PatrolDefenseProviderResultAdmission.EvaluateClaimBound(
                claimBoundGate,
                claimBoundQueue,
                ClaimBoundEnvelope(
                    "deterministic_external_worker",
                    "attempt-1",
                    request.RequestFingerprint,
                    "SUCCESS",
                    claimBoundAdvisoryBase64));
        Check(
            !replayClaimBound.ProviderResultAccepted &&
            replayClaimBound.ClaimMatchReason ==
                "DISPATCH_JOB_NOT_FOUND" &&
            replayClaimBound.RejectionReasons.Contains(
                "DISPATCH_JOB_NOT_FOUND"),
            "claim_bound_replay_job_missing");

        var permanentGate =
            new PatrolDefenseAdvisoryRuntimeGate();
        permanentGate.Arm(
            changedRequest.RequestFingerprint);
        var permanentQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        permanentQueue.Enqueue(
            changedRequest.RequestFingerprint,
            PatrolDefenseDeliberationRequestBuilder.ToJson(
                changedRequest),
            700);
        permanentQueue.Claim(
            changedRequest.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-p",
            701);
        var permanentResult =
            PatrolDefenseProviderResultAdmission.EvaluateClaimBound(
                permanentGate,
                permanentQueue,
                ClaimBoundEnvelope(
                    "deterministic_external_worker",
                    "attempt-p",
                    changedRequest.RequestFingerprint,
                    "PERMANENT_FAILURE",
                    ""));
        Check(
            !permanentResult.ProviderResultAccepted &&
            permanentResult.ClaimMatchReason ==
                "CLAIM_MATCHED" &&
            permanentResult.RejectionReasons.Contains(
                "PROVIDER_PERMANENT_FAILURE"),
            "claim_bound_permanent_match");

        var permanentCompletion =
            permanentQueue.ApplyProviderResult(
                permanentResult.RequestFingerprint,
                permanentResult.Status,
                permanentResult.ProviderResultAccepted);
        Check(
            permanentCompletion.CompletionApplied &&
            permanentCompletion.Reason ==
                "PERMANENT_FAILURE_ACKED" &&
            permanentQueue.Count == 0,
            "claim_bound_permanent_completion_removes");

        string claimBoundReceipt =
            PatrolDefenseProviderResultAdmission.ToJson(
                exactSuccess);
        Check(
            claimBoundReceipt.Contains(
                "\"claimMatchReason\":\"CLAIM_MATCHED\"") &&
            claimBoundReceipt.Contains(
                "\"providerResultAccepted\":true") &&
            claimBoundReceipt.Contains(
                "\"executionAuthorized\":false"),
            "claim_bound_receipt_audit");

        var releaseQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        var releaseEnqueue =
            releaseQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                800);
        var releaseClaim =
            releaseQueue.Claim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1",
                801);
        Check(
            releaseEnqueue.Enqueued &&
            releaseClaim.ClaimApplied &&
            releaseEnqueue.Job.State == "CLAIMED",
            "release_fixture_claimed");

        var wrongRelease =
            releaseQueue.ReleaseClaim(
                request.RequestFingerprint,
                "other_worker",
                "attempt-1");
        Check(
            !wrongRelease.ReleaseApplied &&
            wrongRelease.Reason ==
                "PROVIDER_CLAIM_MISMATCH" &&
            releaseEnqueue.Job.State == "CLAIMED",
            "release_wrong_provider_rejected");

        var wrongAttemptRelease =
            releaseQueue.ReleaseClaim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-2");
        Check(
            !wrongAttemptRelease.ReleaseApplied &&
            wrongAttemptRelease.Reason ==
                "PROVIDER_CLAIM_MISMATCH" &&
            releaseEnqueue.Job.State == "CLAIMED",
            "release_wrong_attempt_rejected");

        var invalidRelease =
            releaseQueue.ReleaseClaim(
                null,
                "worker",
                "attempt");
        Check(
            !invalidRelease.ReleaseApplied &&
            invalidRelease.Reason ==
                "INVALID_RELEASE" &&
            releaseQueue.Count == 1,
            "release_invalid_rejected");

        var missingRelease =
            releaseQueue.ReleaseClaim(
                "missing",
                "worker",
                "attempt");
        Check(
            !missingRelease.ReleaseApplied &&
            missingRelease.Reason ==
                "DISPATCH_JOB_NOT_FOUND" &&
            releaseQueue.Count == 1,
            "release_missing_job_rejected");

        var transientBeforeRelease =
            releaseQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "TRANSIENT_FAILURE",
                false);
        Check(
            transientBeforeRelease.Reason ==
                "TRANSIENT_FAILURE_RETAINED" &&
            transientBeforeRelease.PendingRetained &&
            releaseEnqueue.Job.State == "CLAIMED",
            "release_transient_retains_claim");

        var exactRelease =
            releaseQueue.ReleaseClaim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1");
        Check(
            exactRelease.ReleaseApplied &&
            exactRelease.Reason ==
                "CLAIM_RELEASED" &&
            exactRelease.StateBefore ==
                "CLAIMED" &&
            exactRelease.StateAfter ==
                "PENDING" &&
            exactRelease.QueueCount == 1 &&
            releaseEnqueue.Job.State ==
                "PENDING" &&
            releaseEnqueue.Job.ProviderId == null &&
            releaseEnqueue.Job.AttemptId == null &&
            releaseEnqueue.Job.ClaimedUtcTicks == 0,
            "release_exact_to_pending");

        var releaseReplay =
            releaseQueue.ReleaseClaim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1");
        Check(
            !releaseReplay.ReleaseApplied &&
            releaseReplay.Reason ==
                "PROVIDER_JOB_NOT_CLAIMED" &&
            releaseEnqueue.Job.State == "PENDING",
            "release_replay_noop");

        long releaseSequence =
            releaseEnqueue.Job.Sequence;
        string releaseJson =
            releaseEnqueue.Job.DeliberationRequestJson;

        var retryClaim =
            releaseQueue.Claim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-2",
                802);
        Check(
            retryClaim.ClaimApplied &&
            retryClaim.Reason == "CLAIMED" &&
            releaseEnqueue.Job.State ==
                "CLAIMED" &&
            releaseEnqueue.Job.ProviderId ==
                "deterministic_external_worker" &&
            releaseEnqueue.Job.AttemptId ==
                "attempt-2" &&
            releaseEnqueue.Job.Sequence ==
                releaseSequence &&
            releaseEnqueue.Job.DeliberationRequestJson ==
                releaseJson,
            "release_retry_claim_same_job_identity");

        var retrySuccess =
            releaseQueue.ApplyProviderResult(
                request.RequestFingerprint,
                "SUCCESS",
                true);
        Check(
            retrySuccess.CompletionApplied &&
            retrySuccess.Reason ==
                "SUCCESS_ACKED" &&
            releaseQueue.Count == 0,
            "release_retry_success_removes");

        var pendingQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        pendingQueue.Enqueue(
            changedRequest.RequestFingerprint,
            PatrolDefenseDeliberationRequestBuilder.ToJson(
                changedRequest),
            900);
        var pendingRelease =
            pendingQueue.ReleaseClaim(
                changedRequest.RequestFingerprint,
                "worker",
                "attempt");
        Check(
            !pendingRelease.ReleaseApplied &&
            pendingRelease.Reason ==
                "PROVIDER_JOB_NOT_CLAIMED" &&
            pendingRelease.StateBefore == "PENDING" &&
            pendingRelease.StateAfter == "PENDING",
            "release_pending_rejected");

        string releaseReceipt =
            PatrolDefenseProviderDispatchBuilder.ToReleaseJson(
                exactRelease);
        Check(
            releaseReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderJobRelease.v1\"") &&
            releaseReceipt.Contains(
                "\"reason\":\"CLAIM_RELEASED\"") &&
            releaseReceipt.Contains(
                "\"stateBefore\":\"CLAIMED\"") &&
            releaseReceipt.Contains(
                "\"stateAfter\":\"PENDING\"") &&
            releaseReceipt.Contains(
                "\"releaseApplied\":true"),
            "release_receipt_exact");
        Check(
            releaseReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            releaseReceipt.Contains(
                "\"modelInvoked\":false") &&
            releaseReceipt.Contains(
                "\"executionAuthorized\":false") &&
            releaseReceipt.Contains(
                "\"behaviorMutation\":false") &&
            releaseReceipt.Contains(
                "\"scoreMutation\":false"),
            "release_receipt_zero_authority");

        string ProviderRequestInputSha(
            string rawRequest)
        {
            byte[] bytes =
                System.Text.Encoding.UTF8.GetBytes(
                    rawRequest);
            using (
                System.Security.Cryptography.SHA256 sha =
                    System.Security.Cryptography.SHA256.Create())
            {
                byte[] hash = sha.ComputeHash(bytes);
                System.Text.StringBuilder sb =
                    new System.Text.StringBuilder(
                        hash.Length * 2);
                for (int i = 0; i < hash.Length; i++)
                    sb.Append(hash[i].ToString("X2"));
                return sb.ToString();
            }
        }

        string ProviderRequestEnvelope(
            string providerId,
            string modelId,
            string attemptId,
            string fingerprint,
            string rawRequest,
            string promptVersion,
            string responseSchema,
            string inputSha,
            string extra)
        {
            string encoded =
                Convert.ToBase64String(
                    System.Text.Encoding.UTF8.GetBytes(
                        rawRequest));
            string json =
                "{\"schema\":\"BannerlordAI.PatrolDefenseProviderRequest.v1\"," +
                "\"providerId\":\"" + providerId + "\"," +
                "\"modelId\":\"" + modelId + "\"," +
                "\"attemptId\":\"" + attemptId + "\"," +
                "\"requestFingerprint\":\"" + fingerprint + "\"," +
                "\"promptContractVersion\":\"" + promptVersion + "\"," +
                "\"responseSchema\":\"" + responseSchema + "\"," +
                "\"deliberationRequestBase64\":\"" + encoded + "\"," +
                "\"inputSha256\":\"" + inputSha + "\"";
            if (!string.IsNullOrEmpty(extra))
                json += "," + extra;
            return json + "}";
        }

        var requestRegQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        var requestRegEnqueue =
            requestRegQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                1000);
        requestRegQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1001);

        string requestInputSha =
            ProviderRequestInputSha(
                requestJson);
        string requestEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);

        var requestRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                requestRegQueue,
                requestEnvelope);
        Check(
            requestRegistration.Registered &&
            !requestRegistration.Idempotent &&
            requestRegistration.Reason ==
                "REGISTERED" &&
            requestRegistration.QueueCount == 1 &&
            !string.IsNullOrWhiteSpace(
                requestRegistration.ProviderRequestId) &&
            requestRegistration.ProviderRequestId.Length == 64 &&
            requestRegEnqueue.Job.ProviderRequestId ==
                requestRegistration.ProviderRequestId &&
            requestRegEnqueue.Job.ProviderRequestModelId ==
                "deterministic_mock_no_model" &&
            requestRegEnqueue.Job.ProviderRequestInputSha256 ==
                requestInputSha,
            "provider_request_register_exact");

        var requestReplay =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                requestRegQueue,
                requestEnvelope);
        Check(
            !requestReplay.Registered &&
            requestReplay.Idempotent &&
            requestReplay.Reason ==
                "SAME_PROVIDER_REQUEST_ALREADY_REGISTERED" &&
            requestReplay.ProviderRequestId ==
                requestRegistration.ProviderRequestId,
            "provider_request_replay_idempotent");

        string differentEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "different_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var differentRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                requestRegQueue,
                differentEnvelope);
        Check(
            !differentRegistration.Registered &&
            !differentRegistration.Idempotent &&
            differentRegistration.Reason ==
                "PROVIDER_REQUEST_ALREADY_REGISTERED" &&
            requestRegEnqueue.Job.ProviderRequestModelId ==
                "deterministic_mock_no_model",
            "provider_request_different_envelope_rejected");

        var unclaimedRegQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        unclaimedRegQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1100);
        var unclaimedRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                unclaimedRegQueue,
                requestEnvelope);
        Check(
            !unclaimedRegistration.Registered &&
            unclaimedRegistration.Reason ==
                "PROVIDER_JOB_NOT_CLAIMED",
            "provider_request_unclaimed_rejected");

        var mismatchRegQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        mismatchRegQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1200);
        mismatchRegQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1201);

        string wrongProviderEnvelope =
            ProviderRequestEnvelope(
                "other_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var wrongProviderRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                wrongProviderEnvelope);
        Check(
            !wrongProviderRegistration.Registered &&
            wrongProviderRegistration.Reason ==
                "PROVIDER_CLAIM_MISMATCH",
            "provider_request_wrong_provider_rejected");

        string wrongAttemptEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var wrongAttemptRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                wrongAttemptEnvelope);
        Check(
            !wrongAttemptRegistration.Registered &&
            wrongAttemptRegistration.Reason ==
                "PROVIDER_CLAIM_MISMATCH",
            "provider_request_wrong_attempt_rejected");

        string wrongFingerprintEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                "WRONG",
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var wrongFingerprintRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                wrongFingerprintEnvelope);
        Check(
            !wrongFingerprintRegistration.Registered &&
            wrongFingerprintRegistration.Reason ==
                "DISPATCH_JOB_NOT_FOUND",
            "provider_request_wrong_fingerprint_rejected");

        string alteredRequestJson =
            requestJson.Replace(
                "CONTINUE_PATROL",
                "WAIT");
        string alteredRequestSha =
            ProviderRequestInputSha(
                alteredRequestJson);
        string alteredRequestEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                alteredRequestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                alteredRequestSha,
                null);
        var alteredRequestRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                alteredRequestEnvelope);
        Check(
            !alteredRequestRegistration.Registered &&
            alteredRequestRegistration.Reason ==
                "DELIBERATION_REQUEST_MISMATCH",
            "provider_request_altered_payload_rejected");

        string badShaEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                new string('0', 64),
                null);
        var badShaRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                badShaEnvelope);
        Check(
            !badShaRegistration.Registered &&
            badShaRegistration.Reason ==
                "INPUT_SHA256_MISMATCH",
            "provider_request_bad_sha_rejected");

        string wrongPromptEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "Wrong.Prompt",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var wrongPromptRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                wrongPromptEnvelope);
        Check(
            !wrongPromptRegistration.Registered &&
            wrongPromptRegistration.Reason ==
                "PROMPT_CONTRACT_VERSION_INVALID",
            "provider_request_wrong_prompt_rejected");

        string wrongSchemaEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "Wrong.Response",
                requestInputSha,
                null);
        var wrongSchemaRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                wrongSchemaEnvelope);
        Check(
            !wrongSchemaRegistration.Registered &&
            wrongSchemaRegistration.Reason ==
                "RESPONSE_SCHEMA_INVALID",
            "provider_request_wrong_response_schema_rejected");

        string extraEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                "\"action\":\"ATTACK\"");
        var extraRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                extraEnvelope);
        Check(
            !extraRegistration.Registered &&
            extraRegistration.Reason ==
                "UNEXPECTED_FIELD:action",
            "provider_request_extra_field_rejected");

        var malformedRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                mismatchRegQueue,
                "{\"schema\":");
        Check(
            !malformedRegistration.Registered &&
            malformedRegistration.Reason ==
                "MALFORMED_JSON",
            "provider_request_malformed_rejected");

        var releaseRegistration =
            requestRegQueue.ReleaseClaim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1");
        Check(
            releaseRegistration.ReleaseApplied &&
            requestRegEnqueue.Job.ProviderRequestId == null &&
            requestRegEnqueue.Job.ProviderRequestModelId == null &&
            requestRegEnqueue.Job.ProviderRequestPromptContractVersion == null &&
            requestRegEnqueue.Job.ProviderRequestResponseSchema == null &&
            requestRegEnqueue.Job.ProviderRequestInputSha256 == null,
            "provider_request_release_clears_registration");

        requestRegQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-2",
            1300);
        string attempt2Envelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var attempt2Registration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                requestRegQueue,
                attempt2Envelope);
        Check(
            attempt2Registration.Registered &&
            attempt2Registration.Reason ==
                "REGISTERED" &&
            requestRegEnqueue.Job.ProviderRequestId ==
                attempt2Registration.ProviderRequestId &&
            requestRegEnqueue.Job.AttemptId ==
                "attempt-2",
            "provider_request_retry_attempt_registers_new_request");

        string registrationReceipt =
            PatrolDefenseProviderRequestRegistration.ToJson(
                requestRegistration);
        Check(
            registrationReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderRequestRegistration.v1\"") &&
            registrationReceipt.Contains(
                "\"reason\":\"REGISTERED\"") &&
            registrationReceipt.Contains(
                "\"registered\":true") &&
            registrationReceipt.Contains(
                "\"providerRequestId\":\"" +
                requestRegistration.ProviderRequestId +
                "\""),
            "provider_request_registration_receipt_exact");
        Check(
            registrationReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            registrationReceipt.Contains(
                "\"modelInvoked\":false") &&
            registrationReceipt.Contains(
                "\"executionAuthorized\":false") &&
            registrationReceipt.Contains(
                "\"behaviorMutation\":false") &&
            registrationReceipt.Contains(
                "\"scoreMutation\":false"),
            "provider_request_registration_receipt_zero_authority");

        string ProviderResultV2Envelope(
            string providerId,
            string attemptId,
            string fingerprint,
            string providerRequestId,
            string status,
            string advisoryBase64,
            string extra)
        {
            string json =
                "{\"schema\":\"BannerlordAI.PatrolDefenseProviderResult.v2\"," +
                "\"providerId\":\"" + providerId + "\"," +
                "\"attemptId\":\"" + attemptId + "\"," +
                "\"requestFingerprint\":\"" + fingerprint + "\"," +
                "\"providerRequestId\":\"" + providerRequestId + "\"," +
                "\"status\":\"" + status + "\"," +
                "\"advisoryBase64\":\"" + advisoryBase64 + "\"";
            if (!string.IsNullOrEmpty(extra))
                json += "," + extra;
            return json + "}";
        }

        string v2Advisory =
            "{\"schema\":\"BannerlordAI.PatrolDefenseDeliberationAdvisory.v1\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\"," +
            "\"disposition\":\"KEEP_BASELINE\"}";
        string v2AdvisoryBase64 =
            Convert.ToBase64String(
                System.Text.Encoding.UTF8.GetBytes(
                    v2Advisory));

        var v2Queue =
            new PatrolDefenseProviderDispatchQueue(2);
        var v2Enqueue =
            v2Queue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                1400);
        v2Queue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1401);

        string v2InputSha =
            ProviderRequestInputSha(
                requestJson);
        string v2RequestEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                v2InputSha,
                null);
        var v2RequestRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                v2Queue,
                v2RequestEnvelope);
        Check(
            v2RequestRegistration.Registered &&
            !string.IsNullOrWhiteSpace(
                v2RequestRegistration.ProviderRequestId),
            "v2_request_registration_fixture");

        var v2Gate =
            new PatrolDefenseAdvisoryRuntimeGate();
        v2Gate.Arm(
            request.RequestFingerprint);

        string exactV2Envelope =
            ProviderResultV2Envelope(
                "deterministic_external_worker",
                "attempt-1",
                request.RequestFingerprint,
                v2RequestRegistration.ProviderRequestId,
                "SUCCESS",
                v2AdvisoryBase64,
                null);

        var exactV2Result =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2Gate,
                v2Queue,
                exactV2Envelope);
        Check(
            exactV2Result.ProviderResultAccepted &&
            exactV2Result.ClaimMatchReason ==
                "CLAIM_MATCHED" &&
            exactV2Result.ProviderRequestMatchReason ==
                "PROVIDER_REQUEST_MATCHED" &&
            exactV2Result.ProviderRequestId ==
                v2RequestRegistration.ProviderRequestId &&
            exactV2Result.AdvisoryAdmission != null &&
            exactV2Result.AdvisoryAdmission.Admitted &&
            exactV2Result.AdvisoryAdmission.Disposition ==
                "KEEP_BASELINE" &&
            v2Gate.PendingRequestFingerprint == null,
            "v2_exact_registered_success_admitted");

        var exactV2Completion =
            v2Queue.ApplyProviderResult(
                exactV2Result.RequestFingerprint,
                exactV2Result.Status,
                exactV2Result.ProviderResultAccepted);
        Check(
            exactV2Completion.CompletionApplied &&
            exactV2Completion.Reason ==
                "SUCCESS_ACKED" &&
            v2Queue.Count == 0,
            "v2_exact_success_completion");

        var v2NoRegQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        v2NoRegQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1500);
        v2NoRegQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1501);
        var v2NoRegGate =
            new PatrolDefenseAdvisoryRuntimeGate();
        v2NoRegGate.Arm(
            request.RequestFingerprint);
        var noRegV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2NoRegGate,
                v2NoRegQueue,
                exactV2Envelope);
        Check(
            !noRegV2.ProviderResultAccepted &&
            noRegV2.ProviderRequestMatchReason ==
                "PROVIDER_REQUEST_NOT_REGISTERED" &&
            noRegV2.RejectionReasons.Contains(
                "PROVIDER_REQUEST_NOT_REGISTERED") &&
            v2NoRegGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "v2_no_registration_rejected");

        var v2MismatchQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        v2MismatchQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1600);
        v2MismatchQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1601);
        var v2MismatchReg =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                v2MismatchQueue,
                v2RequestEnvelope);
        var v2MismatchGate =
            new PatrolDefenseAdvisoryRuntimeGate();
        v2MismatchGate.Arm(
            request.RequestFingerprint);

        string wrongIdEnvelope =
            ProviderResultV2Envelope(
                "deterministic_external_worker",
                "attempt-1",
                request.RequestFingerprint,
                new string('F', 64),
                "SUCCESS",
                v2AdvisoryBase64,
                null);
        var wrongIdV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2MismatchGate,
                v2MismatchQueue,
                wrongIdEnvelope);
        Check(
            !wrongIdV2.ProviderResultAccepted &&
            wrongIdV2.ProviderRequestMatchReason ==
                "PROVIDER_REQUEST_ID_MISMATCH" &&
            wrongIdV2.RejectionReasons.Contains(
                "PROVIDER_REQUEST_ID_MISMATCH") &&
            v2MismatchGate.PendingRequestFingerprint ==
                request.RequestFingerprint,
            "v2_wrong_request_id_rejected");

        string wrongProviderV2Envelope =
            ProviderResultV2Envelope(
                "other_worker",
                "attempt-1",
                request.RequestFingerprint,
                v2MismatchReg.ProviderRequestId,
                "SUCCESS",
                v2AdvisoryBase64,
                null);
        var wrongProviderV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2MismatchGate,
                v2MismatchQueue,
                wrongProviderV2Envelope);
        Check(
            !wrongProviderV2.ProviderResultAccepted &&
            wrongProviderV2.ClaimMatchReason ==
                "PROVIDER_CLAIM_MISMATCH",
            "v2_wrong_provider_rejected");

        string wrongAttemptV2Envelope =
            ProviderResultV2Envelope(
                "deterministic_external_worker",
                "attempt-2",
                request.RequestFingerprint,
                v2MismatchReg.ProviderRequestId,
                "SUCCESS",
                v2AdvisoryBase64,
                null);
        var wrongAttemptV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2MismatchGate,
                v2MismatchQueue,
                wrongAttemptV2Envelope);
        Check(
            !wrongAttemptV2.ProviderResultAccepted &&
            wrongAttemptV2.ClaimMatchReason ==
                "PROVIDER_CLAIM_MISMATCH",
            "v2_wrong_attempt_rejected");

        string transientV2Envelope =
            ProviderResultV2Envelope(
                "deterministic_external_worker",
                "attempt-1",
                request.RequestFingerprint,
                v2MismatchReg.ProviderRequestId,
                "TRANSIENT_FAILURE",
                "",
                null);
        var transientV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2MismatchGate,
                v2MismatchQueue,
                transientV2Envelope);
        Check(
            !transientV2.ProviderResultAccepted &&
            transientV2.ProviderRequestMatchReason ==
                "PROVIDER_REQUEST_MATCHED" &&
            transientV2.Retryable &&
            transientV2.RejectionReasons.Contains(
                "PROVIDER_TRANSIENT_FAILURE"),
            "v2_transient_exact_request");

        var transientV2Completion =
            v2MismatchQueue.ApplyProviderResult(
                transientV2.RequestFingerprint,
                transientV2.Status,
                transientV2.ProviderResultAccepted);
        Check(
            transientV2Completion.Reason ==
                "TRANSIENT_FAILURE_RETAINED" &&
            transientV2Completion.PendingRetained &&
            v2MismatchQueue.Count == 1,
            "v2_transient_completion_retains");

        var v2PermanentQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        v2PermanentQueue.Enqueue(
            changedRequest.RequestFingerprint,
            PatrolDefenseDeliberationRequestBuilder.ToJson(
                changedRequest),
            1700);
        v2PermanentQueue.Claim(
            changedRequest.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-p",
            1701);
        string changedJson =
            PatrolDefenseDeliberationRequestBuilder.ToJson(
                changedRequest);
        string changedSha =
            ProviderRequestInputSha(
                changedJson);
        string changedRequestEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-p",
                changedRequest.RequestFingerprint,
                changedJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                changedSha,
                null);
        var changedRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                v2PermanentQueue,
                changedRequestEnvelope);
        var permanentGateV2 =
            new PatrolDefenseAdvisoryRuntimeGate();
        permanentGateV2.Arm(
            changedRequest.RequestFingerprint);
        string permanentV2Envelope =
            ProviderResultV2Envelope(
                "deterministic_external_worker",
                "attempt-p",
                changedRequest.RequestFingerprint,
                changedRegistration.ProviderRequestId,
                "PERMANENT_FAILURE",
                "",
                null);
        var permanentV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                permanentGateV2,
                v2PermanentQueue,
                permanentV2Envelope);
        Check(
            !permanentV2.ProviderResultAccepted &&
            permanentV2.ProviderRequestMatchReason ==
                "PROVIDER_REQUEST_MATCHED" &&
            permanentV2.RejectionReasons.Contains(
                "PROVIDER_PERMANENT_FAILURE"),
            "v2_permanent_exact_request");

        var permanentV2Completion =
            v2PermanentQueue.ApplyProviderResult(
                permanentV2.RequestFingerprint,
                permanentV2.Status,
                permanentV2.ProviderResultAccepted);
        Check(
            permanentV2Completion.CompletionApplied &&
            permanentV2Completion.Reason ==
                "PERMANENT_FAILURE_ACKED" &&
            v2PermanentQueue.Count == 0,
            "v2_permanent_completion_removes");

        var malformedV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2MismatchGate,
                v2MismatchQueue,
                "{\"schema\":");
        Check(
            !malformedV2.ProviderResultAccepted &&
            malformedV2.RejectionReasons.Contains(
                "MALFORMED_JSON"),
            "v2_malformed_rejected");

        string extraV2Envelope =
            ProviderResultV2Envelope(
                "deterministic_external_worker",
                "attempt-1",
                request.RequestFingerprint,
                v2MismatchReg.ProviderRequestId,
                "SUCCESS",
                v2AdvisoryBase64,
                "\"command\":\"ATTACK\"");
        var extraV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2MismatchGate,
                v2MismatchQueue,
                extraV2Envelope);
        Check(
            !extraV2.ProviderResultAccepted &&
            extraV2.RejectionReasons.Contains(
                "UNEXPECTED_FIELD:command"),
            "v2_extra_field_rejected");

        var replayV2 =
            PatrolDefenseProviderResultV2Admission.EvaluateRequestBound(
                v2Gate,
                v2Queue,
                exactV2Envelope);
        Check(
            !replayV2.ProviderResultAccepted &&
            replayV2.ClaimMatchReason ==
                "DISPATCH_JOB_NOT_FOUND" &&
            replayV2.ProviderRequestMatchReason == null &&
            replayV2.RejectionReasons.Contains(
                "DISPATCH_JOB_NOT_FOUND"),
            "v2_replay_job_missing");

        string v2Receipt =
            PatrolDefenseProviderResultV2Admission.ToJson(
                exactV2Result);
        Check(
            v2Receipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderResultV2Admission.v1\"") &&
            v2Receipt.Contains(
                "\"providerRequestId\":\"" +
                v2RequestRegistration.ProviderRequestId +
                "\"") &&
            v2Receipt.Contains(
                "\"providerRequestMatchReason\":\"PROVIDER_REQUEST_MATCHED\"") &&
            v2Receipt.Contains(
                "\"providerResultAccepted\":true"),
            "v2_receipt_exact");
        Check(
            v2Receipt.Contains(
                "\"executionAuthorized\":false") &&
            v2Receipt.Contains(
                "\"modelInvoked\":false") &&
            v2Receipt.Contains(
                "\"behaviorMutation\":false") &&
            v2Receipt.Contains(
                "\"scoreMutation\":false"),
            "v2_receipt_zero_authority");

        string TransportRequestEnvelope(
            string transportId,
            string providerRequestId,
            string providerId,
            string modelId,
            string attemptId,
            string fingerprint,
            string requestSha,
            string extra)
        {
            string json =
                "{\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportRequest.v1\"," +
                "\"transportId\":\"" + transportId + "\"," +
                "\"providerRequestId\":\"" + providerRequestId + "\"," +
                "\"providerId\":\"" + providerId + "\"," +
                "\"modelId\":\"" + modelId + "\"," +
                "\"attemptId\":\"" + attemptId + "\"," +
                "\"requestFingerprint\":\"" + fingerprint + "\"," +
                "\"requestInputSha256\":\"" + requestSha + "\"";
            if (!string.IsNullOrEmpty(extra))
                json += "," + extra;
            return json + "}";
        }

        string ExecutionPolicyEnvelope(
            string transportRequestId,
            string providerRequestId,
            string providerId,
            string modelId,
            string attemptId,
            string fingerprint,
            string timeoutMs,
            string maxAttempts,
            string maxResultBytes,
            string credentialRef,
            string extra)
        {
            string json =
                "{\"schema\":\"BannerlordAI.PatrolDefenseProviderExecutionPolicy.v1\"," +
                "\"transportRequestId\":\"" + transportRequestId + "\"," +
                "\"providerRequestId\":\"" + providerRequestId + "\"," +
                "\"providerId\":\"" + providerId + "\"," +
                "\"modelId\":\"" + modelId + "\"," +
                "\"attemptId\":\"" + attemptId + "\"," +
                "\"requestFingerprint\":\"" + fingerprint + "\"," +
                "\"timeoutMs\":\"" + timeoutMs + "\"," +
                "\"maxAttempts\":\"" + maxAttempts + "\"," +
                "\"maxResultBytes\":\"" + maxResultBytes + "\"," +
                "\"credentialRef\":\"" + credentialRef + "\"";
            if (!string.IsNullOrEmpty(extra))
                json += "," + extra;
            return json + "}";
        }

        var transportQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        var transportEnqueue =
            transportQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                1800);
        transportQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1801);
        var transportProviderRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                transportQueue,
                requestEnvelope);
        Check(
            transportProviderRegistration.Registered &&
            transportEnqueue.Job.ProviderRequestId ==
                transportProviderRegistration.ProviderRequestId,
            "transport_provider_request_registered");

        string transportEnvelope =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                null);

        var transportRegistration =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportQueue,
                transportEnvelope);
        Check(
            transportRegistration.Registered &&
            !transportRegistration.Idempotent &&
            transportRegistration.Reason ==
                "REGISTERED" &&
            transportRegistration.QueueCount == 1 &&
            !string.IsNullOrWhiteSpace(
                transportRegistration.TransportRequestId) &&
            transportRegistration.TransportRequestId.Length == 64 &&
            transportRegistration.TransportId ==
                "deterministic_local_loopback" &&
            transportEnqueue.Job.TransportRequestId ==
                transportRegistration.TransportRequestId &&
            transportEnqueue.Job.TransportId ==
                "deterministic_local_loopback",
            "transport_register_exact");

        var transportReplay =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportQueue,
                transportEnvelope);
        Check(
            !transportReplay.Registered &&
            transportReplay.Idempotent &&
            transportReplay.Reason ==
                "SAME_TRANSPORT_REQUEST_ALREADY_REGISTERED" &&
            transportReplay.TransportRequestId ==
                transportRegistration.TransportRequestId,
            "transport_replay_idempotent");

        string secondTransportEnvelope =
            TransportRequestEnvelope(
                "other_loopback",
                transportProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var secondTransportRegistration =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportQueue,
                secondTransportEnvelope);
        Check(
            !secondTransportRegistration.Registered &&
            !secondTransportRegistration.Idempotent &&
            secondTransportRegistration.Reason ==
                "TRANSPORT_REQUEST_ALREADY_REGISTERED" &&
            transportEnqueue.Job.TransportId ==
                "deterministic_local_loopback",
            "transport_second_envelope_rejected");

        var noProviderRequestQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        noProviderRequestQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1810);
        noProviderRequestQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1811);
        var noProviderRequestTransport =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                noProviderRequestQueue,
                transportEnvelope);
        Check(
            !noProviderRequestTransport.Registered &&
            noProviderRequestTransport.Reason ==
                "PROVIDER_REQUEST_NOT_REGISTERED",
            "transport_no_provider_request_rejected");

        var transportMismatchQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        transportMismatchQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1820);
        transportMismatchQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1821);
        var transportMismatchProviderReg =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                transportMismatchQueue,
                requestEnvelope);

        string wrongProviderRequestIdTransport =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                new string('F', 64),
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var wrongProviderRequestIdReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                wrongProviderRequestIdTransport);
        Check(
            !wrongProviderRequestIdReg.Registered &&
            wrongProviderRequestIdReg.Reason ==
                "PROVIDER_REQUEST_ID_MISMATCH",
            "transport_wrong_provider_request_id_rejected");

        string wrongProviderTransport =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportMismatchProviderReg.ProviderRequestId,
                "other_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var wrongProviderTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                wrongProviderTransport);
        Check(
            !wrongProviderTransportReg.Registered &&
            wrongProviderTransportReg.Reason ==
                "PROVIDER_CLAIM_MISMATCH",
            "transport_wrong_provider_rejected");

        string wrongAttemptTransport =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportMismatchProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var wrongAttemptTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                wrongAttemptTransport);
        Check(
            !wrongAttemptTransportReg.Registered &&
            wrongAttemptTransportReg.Reason ==
                "PROVIDER_CLAIM_MISMATCH",
            "transport_wrong_attempt_rejected");

        string wrongModelTransport =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportMismatchProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "wrong_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var wrongModelTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                wrongModelTransport);
        Check(
            !wrongModelTransportReg.Registered &&
            wrongModelTransportReg.Reason ==
                "PROVIDER_REQUEST_MODEL_MISMATCH",
            "transport_wrong_model_rejected");

        string wrongShaTransport =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportMismatchProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                new string('0', 64),
                null);
        var wrongShaTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                wrongShaTransport);
        Check(
            !wrongShaTransportReg.Registered &&
            wrongShaTransportReg.Reason ==
                "PROVIDER_REQUEST_INPUT_SHA_MISMATCH",
            "transport_wrong_input_sha_rejected");

        string wrongFingerprintTransport =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportMismatchProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                "WRONG",
                requestInputSha,
                null);
        var wrongFingerprintTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                wrongFingerprintTransport);
        Check(
            !wrongFingerprintTransportReg.Registered &&
            wrongFingerprintTransportReg.Reason ==
                "DISPATCH_JOB_NOT_FOUND",
            "transport_wrong_fingerprint_rejected");

        var unclaimedTransportQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        unclaimedTransportQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1830);
        var unclaimedTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                unclaimedTransportQueue,
                transportEnvelope);
        Check(
            !unclaimedTransportReg.Registered &&
            unclaimedTransportReg.Reason ==
                "PROVIDER_JOB_NOT_CLAIMED",
            "transport_unclaimed_rejected");

        var malformedTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                "{\"schema\":");
        Check(
            !malformedTransportReg.Registered &&
            malformedTransportReg.Reason ==
                "MALFORMED_JSON",
            "transport_malformed_rejected");

        string extraTransportEnvelope =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                transportMismatchProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                "\"command\":\"ATTACK\"");
        var extraTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                extraTransportEnvelope);
        Check(
            !extraTransportReg.Registered &&
            extraTransportReg.Reason ==
                "UNEXPECTED_FIELD:command",
            "transport_extra_field_rejected");

        string missingTransportFieldEnvelope =
            "{\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportRequest.v1\"," +
            "\"providerRequestId\":\"" +
            transportMismatchProviderReg.ProviderRequestId +
            "\",\"providerId\":\"deterministic_external_worker\"," +
            "\"modelId\":\"deterministic_mock_no_model\"," +
            "\"attemptId\":\"attempt-1\"," +
            "\"requestFingerprint\":\"" +
            request.RequestFingerprint +
            "\",\"requestInputSha256\":\"" +
            requestInputSha +
            "\"}";
        var missingTransportFieldReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportMismatchQueue,
                missingTransportFieldEnvelope);
        Check(
            !missingTransportFieldReg.Registered &&
            missingTransportFieldReg.Reason ==
                "MISSING_FIELD:transportId",
            "transport_missing_field_rejected");

        var transportRelease =
            transportQueue.ReleaseClaim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1");
        Check(
            transportRelease.ReleaseApplied &&
            transportEnqueue.Job.TransportRequestId == null &&
            transportEnqueue.Job.TransportId == null &&
            transportEnqueue.Job.ProviderRequestId == null,
            "transport_release_clears_registration");

        transportQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-2",
            1840);
        string retryProviderRequestEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var retryProviderRequestRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                transportQueue,
                retryProviderRequestEnvelope);
        string retryTransportEnvelope =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                retryProviderRequestRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var retryTransportRegistration =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                transportQueue,
                retryTransportEnvelope);
        Check(
            retryProviderRequestRegistration.Registered &&
            retryTransportRegistration.Registered &&
            retryTransportRegistration.Reason ==
                "REGISTERED" &&
            retryTransportRegistration.TransportRequestId !=
                transportRegistration.TransportRequestId &&
            transportEnqueue.Job.TransportRequestId ==
                retryTransportRegistration.TransportRequestId &&
            transportEnqueue.Job.AttemptId ==
                "attempt-2",
            "transport_retry_registers_new_transport_request");

        string transportReceipt =
            PatrolDefenseProviderTransportRegistration.ToJson(
                transportRegistration);
        Check(
            transportReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderTransportRegistration.v1\"") &&
            transportReceipt.Contains(
                "\"transportId\":\"deterministic_local_loopback\"") &&
            transportReceipt.Contains(
                "\"transportRequestId\":\"" +
                transportRegistration.TransportRequestId +
                "\"") &&
            transportReceipt.Contains(
                "\"registered\":true") &&
            transportReceipt.Contains(
                "\"reason\":\"REGISTERED\""),
            "transport_receipt_exact");
        Check(
            transportReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            transportReceipt.Contains(
                "\"modelInvoked\":false") &&
            transportReceipt.Contains(
                "\"executionAuthorized\":false") &&
            transportReceipt.Contains(
                "\"behaviorMutation\":false") &&
            transportReceipt.Contains(
                "\"scoreMutation\":false"),
            "transport_receipt_zero_authority");

        var policyQueue =
            new PatrolDefenseProviderDispatchQueue(2);
        var policyEnqueue =
            policyQueue.Enqueue(
                request.RequestFingerprint,
                requestJson,
                1900);
        policyQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1901);
        string policyProviderEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var policyProviderRegistration =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                policyQueue,
                policyProviderEnvelope);
        string policyTransportEnvelope =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var policyTransportRegistration =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                policyQueue,
                policyTransportEnvelope);
        Check(
            policyEnqueue.Enqueued &&
            policyProviderRegistration.Registered &&
            policyTransportRegistration.Registered,
            "execution_policy_prepare");

        string policyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var policyRegistration =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                policyEnvelope);
        Check(
            policyRegistration.Registered &&
            !policyRegistration.Idempotent &&
            policyRegistration.Reason == "REGISTERED" &&
            policyRegistration.QueueCount == 1 &&
            !string.IsNullOrWhiteSpace(
                policyRegistration.ExecutionPolicyId) &&
            policyRegistration.ExecutionPolicyId.Length == 64 &&
            policyEnqueue.Job.ExecutionPolicyId ==
                policyRegistration.ExecutionPolicyId &&
            policyEnqueue.Job.ExecutionPolicyTimeoutMs == "1000" &&
            policyEnqueue.Job.ExecutionPolicyMaxAttempts == "1" &&
            policyEnqueue.Job.ExecutionPolicyMaxResultBytes == "65536" &&
            policyEnqueue.Job.ExecutionPolicyCredentialRef ==
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
            "execution_policy_register_exact");

        var policyReplay =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                policyEnvelope);
        Check(
            !policyReplay.Registered &&
            policyReplay.Idempotent &&
            policyReplay.Reason ==
                "SAME_EXECUTION_POLICY_ALREADY_REGISTERED" &&
            policyReplay.ExecutionPolicyId ==
                policyRegistration.ExecutionPolicyId,
            "execution_policy_replay_idempotent");

        string differentPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "2000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var differentPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                differentPolicyEnvelope);
        Check(
            !differentPolicy.Registered &&
            !differentPolicy.Idempotent &&
            differentPolicy.Reason ==
                "EXECUTION_POLICY_ALREADY_REGISTERED" &&
            policyEnqueue.Job.ExecutionPolicyTimeoutMs == "1000",
            "execution_policy_different_rejected");

        var noTransportPolicyQueue =
            new PatrolDefenseProviderDispatchQueue(1);
        noTransportPolicyQueue.Enqueue(
            request.RequestFingerprint,
            requestJson,
            1910);
        noTransportPolicyQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-1",
            1911);
        var noTransportProviderReg =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                noTransportPolicyQueue,
                policyProviderEnvelope);
        string noTransportPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                noTransportProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var noTransportPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                noTransportPolicyQueue,
                noTransportPolicyEnvelope);
        Check(
            !noTransportPolicy.Registered &&
            noTransportPolicy.Reason ==
                "TRANSPORT_REQUEST_NOT_REGISTERED",
            "execution_policy_no_transport_rejected");

        string wrongProviderRequestPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                new string('E', 64),
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var wrongProviderRequestPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                wrongProviderRequestPolicyEnvelope);
        Check(
            !wrongProviderRequestPolicy.Registered &&
            wrongProviderRequestPolicy.Reason ==
                "PROVIDER_REQUEST_ID_MISMATCH",
            "execution_policy_wrong_provider_request_rejected");

        string wrongTransportPolicyEnvelope =
            ExecutionPolicyEnvelope(
                new string('F', 64),
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var wrongTransportPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                wrongTransportPolicyEnvelope);
        Check(
            !wrongTransportPolicy.Registered &&
            wrongTransportPolicy.Reason ==
                "TRANSPORT_REQUEST_ID_MISMATCH",
            "execution_policy_wrong_transport_rejected");

        string wrongModelPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "wrong_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var wrongModelPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                wrongModelPolicyEnvelope);
        Check(
            !wrongModelPolicy.Registered &&
            wrongModelPolicy.Reason ==
                "PROVIDER_REQUEST_MODEL_MISMATCH",
            "execution_policy_wrong_model_rejected");

        string wrongProviderPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "other_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var wrongProviderPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                wrongProviderPolicyEnvelope);
        Check(
            !wrongProviderPolicy.Registered &&
            wrongProviderPolicy.Reason ==
                "PROVIDER_CLAIM_MISMATCH",
            "execution_policy_wrong_provider_rejected");

        string wrongAttemptPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var wrongAttemptPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                wrongAttemptPolicyEnvelope);
        Check(
            !wrongAttemptPolicy.Registered &&
            wrongAttemptPolicy.Reason ==
                "PROVIDER_CLAIM_MISMATCH",
            "execution_policy_wrong_attempt_rejected");

        string wrongFingerprintPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                "WRONG",
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var wrongFingerprintPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                wrongFingerprintPolicyEnvelope);
        Check(
            !wrongFingerprintPolicy.Registered &&
            wrongFingerprintPolicy.Reason ==
                "DISPATCH_JOB_NOT_FOUND",
            "execution_policy_wrong_fingerprint_rejected");

        foreach (
            var invalidNumeric in new[]
            {
                new[] { "0", "1", "65536", "TIMEOUT_MS_INVALID" },
                new[] { "-1", "1", "65536", "TIMEOUT_MS_INVALID" },
                new[] { "x", "1", "65536", "TIMEOUT_MS_INVALID" },
                new[] { "1000", "0", "65536", "MAX_ATTEMPTS_INVALID" },
                new[] { "1000", "-1", "65536", "MAX_ATTEMPTS_INVALID" },
                new[] { "1000", "x", "65536", "MAX_ATTEMPTS_INVALID" },
                new[] { "1000", "1", "0", "MAX_RESULT_BYTES_INVALID" },
                new[] { "1000", "1", "-1", "MAX_RESULT_BYTES_INVALID" },
                new[] { "1000", "1", "x", "MAX_RESULT_BYTES_INVALID" }
            })
        {
            string invalidEnvelope =
                ExecutionPolicyEnvelope(
                    policyTransportRegistration.TransportRequestId,
                    policyProviderRegistration.ProviderRequestId,
                    "deterministic_external_worker",
                    "deterministic_mock_no_model",
                    "attempt-1",
                    request.RequestFingerprint,
                    invalidNumeric[0],
                    invalidNumeric[1],
                    invalidNumeric[2],
                    "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                    null);
            var invalidResult =
                PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                    policyQueue,
                    invalidEnvelope);
            Check(
                !invalidResult.Registered &&
                invalidResult.Reason == invalidNumeric[3],
                "execution_policy_numeric_" +
                    invalidNumeric[3] + "_" +
                    invalidNumeric[0] + "_" +
                    invalidNumeric[1] + "_" +
                    invalidNumeric[2]);
        }

        foreach (
            string badCredential in new[]
            {
                "",
                "secret",
                "env:",
                "env:BAD-NAME",
                "env:BAD NAME",
                "env:KEY=value"
            })
        {
            string badCredentialEnvelope =
                ExecutionPolicyEnvelope(
                    policyTransportRegistration.TransportRequestId,
                    policyProviderRegistration.ProviderRequestId,
                    "deterministic_external_worker",
                    "deterministic_mock_no_model",
                    "attempt-1",
                    request.RequestFingerprint,
                    "1000",
                    "1",
                    "65536",
                    badCredential,
                    null);
            var badCredentialResult =
                PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                    policyQueue,
                    badCredentialEnvelope);
            Check(
                !badCredentialResult.Registered &&
                badCredentialResult.Reason ==
                    "CREDENTIAL_REF_INVALID",
                "execution_policy_bad_credential_" +
                    badCredential.Replace(":", "_").Replace(" ", "_"));
        }

        string extraPolicyEnvelope =
            ExecutionPolicyEnvelope(
                policyTransportRegistration.TransportRequestId,
                policyProviderRegistration.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-1",
                request.RequestFingerprint,
                "1000",
                "1",
                "65536",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                "\"secret\":\"DO_NOT_STORE\"");
        var extraPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                extraPolicyEnvelope);
        Check(
            !extraPolicy.Registered &&
            extraPolicy.Reason ==
                "UNEXPECTED_FIELD:secret",
            "execution_policy_extra_field_rejected");

        var malformedPolicy =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                "{\"schema\":");
        Check(
            !malformedPolicy.Registered &&
            malformedPolicy.Reason ==
                "MALFORMED_JSON",
            "execution_policy_malformed_rejected");

        var policyRelease =
            policyQueue.ReleaseClaim(
                request.RequestFingerprint,
                "deterministic_external_worker",
                "attempt-1");
        Check(
            policyRelease.ReleaseApplied &&
            policyEnqueue.Job.ExecutionPolicyId == null &&
            policyEnqueue.Job.ExecutionPolicyTimeoutMs == null &&
            policyEnqueue.Job.ExecutionPolicyMaxAttempts == null &&
            policyEnqueue.Job.ExecutionPolicyMaxResultBytes == null &&
            policyEnqueue.Job.ExecutionPolicyCredentialRef == null,
            "execution_policy_release_clears");

        policyQueue.Claim(
            request.RequestFingerprint,
            "deterministic_external_worker",
            "attempt-2",
            1920);
        string retryPolicyProviderEnvelope =
            ProviderRequestEnvelope(
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                requestJson,
                "BannerlordAI.PatrolDefenseAdvisoryPrompt.v1",
                "BannerlordAI.PatrolDefenseDeliberationAdvisory.v1",
                requestInputSha,
                null);
        var retryPolicyProviderReg =
            PatrolDefenseProviderRequestRegistration.Evaluate(
                policyQueue,
                retryPolicyProviderEnvelope);
        string retryPolicyTransportEnvelope =
            TransportRequestEnvelope(
                "deterministic_local_loopback",
                retryPolicyProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                requestInputSha,
                null);
        var retryPolicyTransportReg =
            PatrolDefenseProviderTransportRegistration.Evaluate(
                policyQueue,
                retryPolicyTransportEnvelope);
        string retryPolicyEnvelope =
            ExecutionPolicyEnvelope(
                retryPolicyTransportReg.TransportRequestId,
                retryPolicyProviderReg.ProviderRequestId,
                "deterministic_external_worker",
                "deterministic_mock_no_model",
                "attempt-2",
                request.RequestFingerprint,
                "1500",
                "2",
                "131072",
                "env:BANNERLORDAI_TEST_PROVIDER_KEY",
                null);
        var retryPolicyReg =
            PatrolDefenseProviderExecutionPolicyRegistration.Evaluate(
                policyQueue,
                retryPolicyEnvelope);
        Check(
            retryPolicyProviderReg.Registered &&
            retryPolicyTransportReg.Registered &&
            retryPolicyReg.Registered &&
            retryPolicyReg.Reason == "REGISTERED" &&
            retryPolicyReg.ExecutionPolicyId !=
                policyRegistration.ExecutionPolicyId &&
            policyEnqueue.Job.ExecutionPolicyId ==
                retryPolicyReg.ExecutionPolicyId &&
            policyEnqueue.Job.AttemptId == "attempt-2",
            "execution_policy_retry_registers_new_policy");

        string policyReceipt =
            PatrolDefenseProviderExecutionPolicyRegistration.ToJson(
                policyRegistration);
        Check(
            policyReceipt.Contains(
                "\"schema\":\"BannerlordAI.PatrolDefenseProviderExecutionPolicyRegistration.v1\"") &&
            policyReceipt.Contains(
                "\"executionPolicyId\":\"" +
                policyRegistration.ExecutionPolicyId +
                "\"") &&
            policyReceipt.Contains(
                "\"timeoutMs\":\"1000\"") &&
            policyReceipt.Contains(
                "\"maxAttempts\":\"1\"") &&
            policyReceipt.Contains(
                "\"maxResultBytes\":\"65536\"") &&
            policyReceipt.Contains(
                "\"credentialRef\":\"env:BANNERLORDAI_TEST_PROVIDER_KEY\"") &&
            policyReceipt.Contains(
                "\"registered\":true") &&
            policyReceipt.Contains(
                "\"reason\":\"REGISTERED\""),
            "execution_policy_receipt_exact");
        Check(
            policyReceipt.Contains(
                "\"externalNetworkUsed\":false") &&
            policyReceipt.Contains(
                "\"modelInvoked\":false") &&
            policyReceipt.Contains(
                "\"executionAuthorized\":false") &&
            policyReceipt.Contains(
                "\"behaviorMutation\":false") &&
            policyReceipt.Contains(
                "\"scoreMutation\":false"),
            "execution_policy_receipt_zero_authority");

        Console.WriteLine("PASS_FIXTURES checks="+checks);
        return 0;
    }
}
