using Mcp.Broker;
using Xunit;

namespace Tests.Unit.Mcp.Broker
{
    public class PilotStateTest
    {
        private sealed class Clock : TimeProvider
        {
            internal DateTimeOffset Now = DateTimeOffset.UtcNow;
            public override DateTimeOffset GetUtcNow() => Now;
        }

        [Fact]
        public void RedeemCode_ConcurrentReplay_AdmitsExactlyOne()
        {
            var state = new PilotState(TimeProvider.System);
            var code = state.IssueCode();
            var wins = 0;
            Parallel.For(0, 100, _ => { if (state.RedeemCode(code)) Interlocked.Increment(ref wins); });
            Assert.Equal(1, wins);
            Assert.False(state.RedeemCode(code));
            Assert.False(state.RedeemCode("unissued"));
        }

        [Fact]
        public void Resolve_ReplacementDisconnectAndExpiry_RevokeGrants()
        {
            var clock = new Clock();
            var state = new PilotState(clock);
            var first = state.Enroll("42", "secret", clock.Now.AddMinutes(10));
            Assert.NotNull(first);
            var grant = state.IssueGrant(first.Id, clock.Now.AddMinutes(5));
            Assert.Equal("42", state.Resolve(grant)?.User);
            var replacement = state.Enroll("42", "new-secret", clock.Now.AddMinutes(10));
            Assert.NotNull(replacement);
            Assert.Null(state.Resolve(grant));
            var current = state.IssueGrant(replacement.Id, clock.Now.AddMinutes(1));
            clock.Now = clock.Now.AddMinutes(1);
            Assert.Null(state.Resolve(current));
            var revoked = state.IssueGrant(replacement.Id, clock.Now.AddMinutes(5));
            state.Revoke(replacement.Id);
            Assert.Null(state.Resolve(revoked));
        }

        [Fact]
        public void Resolve_GrantExpiry_IsBoundByCredentialAndFiveMinutes()
        {
            var clock = new Clock();
            var state = new PilotState(clock);
            var credential = state.Enroll("42", "secret", clock.Now.AddSeconds(20));
            Assert.NotNull(credential);
            var grant = state.IssueGrant(credential.Id, clock.Now.AddHours(1));
            clock.Now = clock.Now.AddSeconds(20);
            Assert.Null(state.Resolve(grant));
            credential = state.Enroll("42", "secret", clock.Now.AddHours(1));
            Assert.NotNull(credential);
            grant = state.IssueGrant(credential.Id, clock.Now.AddHours(1));
            clock.Now = clock.Now.AddMinutes(5);
            Assert.Null(state.Resolve(grant));
        }

        [Fact]
        public void Admit_AtomicConcurrentLimits_ApplyAcrossGrantsAndDeployment()
        {
            var state = new PilotState(TimeProvider.System);
            var admissions = new List<IDisposable>();
            for (var index = 0; index < 8; index++)
            {
                for (var slot = 0; slot < 2; slot++)
                {
                    var admission = state.Admit(index.ToString());
                    Assert.NotNull(admission);
                    admissions.Add(admission);
                }
                Assert.Null(state.Admit(index.ToString()));
            }
            Assert.Null(state.Admit("another"));
            admissions[0].Dispose();
            admissions[0].Dispose();
            using var released = state.Admit("another");
            Assert.NotNull(released);
            foreach (var admission in admissions) admission.Dispose();
        }

        [Fact]
        public void Admit_RollingMinute_RejectsThirtyFirstAndResetsAfterWindow()
        {
            var clock = new Clock();
            var state = new PilotState(clock);
            for (var index = 0; index < 30; index++)
            {
                using var admission = state.Admit("42");
                Assert.NotNull(admission);
            }
            Assert.Null(state.Admit("42"));
            clock.Now = clock.Now.AddMinutes(1);
            using var reset = state.Admit("42");
            Assert.NotNull(reset);
        }

        [Fact]
        public void IssueCode_CapacityAndExpiry_AreBounded()
        {
            var clock = new Clock();
            var state = new PilotState(clock);
            var first = state.IssueCode();
            for (var index = 1; index < 512; index++) Assert.NotNull(state.IssueCode());
            Assert.Null(state.IssueCode());
            clock.Now = clock.Now.AddMinutes(2);
            Assert.False(state.RedeemCode(first));
            Assert.NotNull(state.IssueCode());
        }

        [Fact]
        public void Credential_DiagnosticString_NeverExposesJwt()
        {
            var credential = new Credential("id", "42", "sensitive-jwt", DateTimeOffset.UtcNow.AddMinutes(5), "version");
            Assert.DoesNotContain("sensitive-jwt", credential.ToString());
        }

        [Fact]
        public void Enroll_FullCapacity_AllowsSameAccountReplacementWithoutBroadeningCapacity()
        {
            var now = DateTimeOffset.UtcNow;
            var state = new PilotState(TimeProvider.System);
            for (var index = 0; index < 256; index++) Assert.NotNull(state.Enroll(index.ToString(), "secret", now.AddHours(1)));
            Assert.Null(state.Enroll("overflow", "secret", now.AddHours(1)));
            Assert.NotNull(state.Enroll("0", "replacement", now.AddHours(1)));
            Assert.Null(state.Enroll("overflow", "secret", now.AddHours(1)));
        }
    }
}
