using NUnit.Framework;

public class ShieldRegenerationTests
{
    private static ShieldRegeneration CreateRegeneration(
        out Shield shield,
        out StatsEventsRecorder eventsRecorder)
    {
        shield = new Shield();
        shield.Initialize();

        eventsRecorder = new StatsEventsRecorder();

        return new ShieldRegeneration(shield, eventsRecorder);
    }

    #region Recharge
    [Test]
    public void Recharge_WhenDurationIsZero_ShouldRechargeImmediatelyAndCallOnShieldRecharged()
    {
        ShieldRegeneration regeneration = CreateRegeneration(
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        float startShield = shield.Current;
        float rechargeAmount = shield.Max * TestValues.QuarterRatio;
        float expectedShield = startShield + rechargeAmount;

        regeneration.Recharge(rechargeAmount, TestValues.Empty);

        Assert.AreEqual(expectedShield, shield.Current, TestValues.Tolerance, "Verifies that zero-duration recharge is applied immediately.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should be called once when zero-duration recharge restores shield.");
        Assert.AreEqual(rechargeAmount, eventsRecorder.LastShieldRechargedAmount, TestValues.Tolerance, "OnShieldRecharged should receive the immediate recharged amount.");
    }

    [Test]
    public void Recharge_WhenDurationIsPositive_ShouldNotApplyRechargeImmediately()
    {
        ShieldRegeneration regeneration = CreateRegeneration(
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        float startShield = shield.Current;
        float rechargeAmount = shield.Max * TestValues.QuarterRatio;

        regeneration.Recharge(rechargeAmount, TestValues.OneSecond);

        Assert.AreEqual(startShield, shield.Current, TestValues.Tolerance, "Verifies that positive-duration recharge waits for Tick instead of applying instantly.");
        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should not be called before over-time recharge ticks.");
    }

    [Test]
    public void Tick_WhenHalfDurationPasses_ShouldRechargeHalfOfTargetAmount()
    {
        ShieldRegeneration regeneration = CreateRegeneration(
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        float startShield = shield.Current;
        float rechargeAmount = shield.Max * TestValues.QuarterRatio;
        float expectedRechargeStep = rechargeAmount * TestValues.HalfRatio;
        float expectedShield = startShield + expectedRechargeStep;

        regeneration.Recharge(rechargeAmount, TestValues.OneSecond);
        regeneration.Tick(TestValues.HalfSecond);

        Assert.AreEqual(expectedShield, shield.Current, TestValues.Tolerance, "Verifies that half of the recharge duration restores half of the target amount.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should be called for the first over-time recharge step.");
        Assert.AreEqual(expectedRechargeStep, eventsRecorder.LastShieldRechargedAmount, TestValues.Tolerance, "OnShieldRecharged should receive the actual recharge step amount.");
    }

    [Test]
    public void Tick_WhenDurationCompletes_ShouldRechargeToTargetAmount()
    {
        ShieldRegeneration regeneration = CreateRegeneration(
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        float startShield = shield.Current;
        float rechargeAmount = shield.Max * TestValues.QuarterRatio;
        float expectedShield = startShield + rechargeAmount;
        float expectedRechargeStep = rechargeAmount * TestValues.HalfRatio;

        regeneration.Recharge(rechargeAmount, TestValues.OneSecond);
        regeneration.Tick(TestValues.HalfSecond);
        regeneration.Tick(TestValues.HalfSecond);

        Assert.AreEqual(expectedShield, shield.Current, TestValues.Tolerance, "Verifies that completing the recharge duration restores the full target amount.");
        Assert.AreEqual(TestValues.TwoCalls, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should be called once per positive recharge step.");
        Assert.AreEqual(expectedRechargeStep, eventsRecorder.LastShieldRechargedAmount, TestValues.Tolerance, "The final OnShieldRecharged call should receive only the final recharge step.");
    }

    [Test]
    public void Recharge_WhenAmountExceedsMissingShield_ShouldClampTargetToMaxShield()
    {
        ShieldRegeneration regeneration = CreateRegeneration(
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        float absorbedDamage = shield.Max * TestValues.QuarterRatio;
        shield.AbsorbDamage(absorbedDamage);

        regeneration.Recharge(shield.Max, TestValues.Empty);

        Assert.AreEqual(shield.Max, shield.Current, TestValues.Tolerance, "Verifies that recharge cannot increase Current shield above Max shield.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should be called when clamped recharge restores shield.");
        Assert.AreEqual(absorbedDamage, eventsRecorder.LastShieldRechargedAmount, TestValues.Tolerance, "OnShieldRecharged should receive only the actually restored shield amount after clamping.");
    }

    [Test]
    public void Recharge_WhenShieldIsFull_ShouldNotCallOnShieldRecharged()
    {
        ShieldRegeneration regeneration = CreateRegeneration(
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        regeneration.Recharge(shield.Max * TestValues.QuarterRatio, TestValues.OneSecond);
        regeneration.Tick(TestValues.OneSecond);

        Assert.AreEqual(shield.Max, shield.Current, TestValues.Tolerance, "Verifies that full shield remains full when recharge is requested.");
        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should not be called when shield cannot increase.");
    }
    #endregion

    #region Validation
    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void Recharge_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float amount)
    {
        ShieldRegeneration regeneration = CreateRegeneration(
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => regeneration.Recharge(amount, TestValues.OneSecond),
            "ShieldRegeneration.Recharge should reject zero or negative recharge amounts."
        );
    }

    [Test]
    public void Recharge_WhenDurationIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ShieldRegeneration regeneration = CreateRegeneration(
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => regeneration.Recharge(shield.Max * TestValues.QuarterRatio, TestValues.InvalidNegativeAmount),
            "ShieldRegeneration.Recharge should reject negative durations."
        );
    }

    [Test]
    public void Tick_WhenDeltaTimeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        ShieldRegeneration regeneration = CreateRegeneration(
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => regeneration.Tick(TestValues.InvalidNegativeAmount),
            "ShieldRegeneration.Tick should reject negative delta time values."
        );
    }
    #endregion
}
