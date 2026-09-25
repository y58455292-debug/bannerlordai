from pathlib import Path
import hashlib
import re

ROOT = Path(__file__).resolve().parents[2]
BASE = ROOT / 'src' / 'ClanAI' / 'src' / 'ClanAI'
observer = (BASE / 'Phase4ARecoveryObserverBehavior.cs').read_text(encoding='utf-8')
link = (BASE / 'Phase4ARecoveryObserverBehavior.Recreation.cs').read_text(encoding='utf-8')
policy = (BASE / 'Phase4ARecreationLinkPolicy.cs').read_text(encoding='utf-8')

# Existing recovery code is unchanged except partial, registration, and final newline.
original = observer.replace('public sealed partial class Phase4ARecoveryObserverBehavior',
                            'public sealed class Phase4ARecoveryObserverBehavior')
original = original.replace('            RegisterRecreationLinkEvents();\n', '')
raw = original.rstrip('\n').encode('utf-8')
assert hashlib.sha1(b'blob ' + str(len(raw)).encode() + b'\0' + raw).hexdigest() == \
    '3c99ad44aa99ba36c9bdd83baf029b227cbc0008', 'existing recovery behavior changed'
assert observer.count('RegisterRecreationLinkEvents();') == 1

expected_events = {
    'OnSessionLaunchedEvent', 'MapEventEnded', 'MobilePartyDestroyed',
    'MobilePartyCreated', 'HourlyTickPartyEvent',
    'BeforeSettlementEnteredEvent', 'AfterSettlementEntered',
}
assert set(re.findall(r'CampaignEvents\.(\w+)', link)) == expected_events
for token in ('!battle.HasWinner', 'battle.DefeatedSide', 'battle.PartiesOnSide(battle.DefeatedSide)',
              'party.LeaderHero.StringId', 'DefeatsByHero[identity.HeroId]',
              'DefeatsByHero.TryGetValue(identity.HeroId', 'Phase4ARecreationLinkPolicy.CanLink(',
              '!ReferenceEquals(defeat.OldParty, party.Party)', 'NativePartyStillPresent(defeat.OldParty)',
              'DestroyedParties.ContainsKey(defeat.OldParty)', 'NativeCreationsSeen.Add(party.Party)',
              'DefeatsByHero.Remove(identity.HeroId)', 'PendingRecreations.Remove(party.Party)',
              'SettlementEntriesSeen.Contains(party.Party)', 'Phase4ARecreationLinkPolicy.CompleteFirstVisit(',
              'CaptureRoster(party)', 'PartyContextFields(party)', 'RecreationObservationFault = true'):
    assert token in link, 'missing native evidence gate: ' + token
for event in ('PHASE4A_LINK_DEFEAT', 'PHASE4A_LINK_PARTY_DESTROYED', 'PHASE4A_LINK_CREATION',
              'PHASE4A_LINK_FIRST_SETTLEMENT_PRE', 'PHASE4A_LINK_INCOMPLETE'):
    assert '"' + event + '"' in link
assert not re.search(r'\bClan\b', policy) and 'TaleWorlds' not in policy
assert 'DefeatsByHero.Clear()' in link and 'PendingRecreations.Clear()' in link
print('PASS Phase 4A same-hero recreation wiring and original-observer preservation')

# Reject all native mutator families relevant to this observation seam.
combined = link + '\n' + policy
for forbidden in (r'\bSetMove\w*\s*\(', r'\bSetBehaviorScore\s*\(', r'\bApplyFactor\s*\(',
                  r'\b(?:AddTroop|RemoveTroop|WoundTroop|AddMembers|AddMember|AddElementToMemberRoster)\s*\(',
                  r'\bSetElement\w*\s*\(', r'\b(?:CreateParty|DestroyParty|KillCharacter)\w*\s*\(',
                  r'\b(?:ChangeKingdom|ChangeOwnerOfSettlement|DeclareWar|MakePeace|GiveGold)Action\b',
                  r'AIBehaviorScores\s*\.\s*(?:Add|Insert|Remove|Clear)\s*\(',
                  r'\b(?:party|mobile|oldParty|hero|settlement)\s*\.\s*\w+\s*=(?!=)',
                  r'\.\s*(?:VolunteerTypes|GarrisonParty|MemberRoster|PrisonRoster)\s*\[[^]]+\]\s*=(?!=)',
                  r'\b(?:File|Directory|Process|HttpClient)\s*\.'):
    assert not re.search(forbidden, combined), 'native mutation or external IO: ' + forbidden
for forbidden in ('D:\\BannerlordAIResearch', 'TestRunner', 'DesktopCommander', 'ChatGPT', 'Codex', 'watchdog'):
    assert forbidden not in combined, 'runtime development dependency: ' + forbidden
assert not re.search(r'@?"[A-Za-z]:[\\/]', combined)
assert 'dataStore.' not in combined
print('PASS Phase 4A same-hero recreation no-mutation and standalone invariant')
