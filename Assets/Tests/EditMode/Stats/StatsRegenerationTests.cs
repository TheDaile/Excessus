using NUnit.Framework;

public class StatsRegenerationTests
{
    private static StatsRegeneration CreateRegeneration(
        out Health health,
        out Shield shield,
        out StatsEventsRecorder eventsRecorder)
    {
        health = new Health();
        shield = new Shield();

        health.Initialize();
        shield.Initialize();

        eventsRecorder = new StatsEventsRecorder();

        return new StatsRegeneration(health, shield, eventsRecorder);
    }

    #region Aggregation
    [Test]
    public void Tick_WhenHealthAndShieldRegenerationAreActive_ShouldRegenerateBothStats()
    {
        StatsRegeneration regeneration = CreateRegeneration(
            out Health health,
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        health.TakeDamage(health.Max * TestValues.HalfRatio);
        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        float startHealth = health.Current;
        float startShield = shield.Current;
        float healAmount = health.Max * TestValues.QuarterRatio;
        float rechargeAmount = shield.Max * TestValues.QuarterRatio;
        float expectedHealth = startHealth + healAmount * TestValues.HalfRatio;
        float expectedShield = startShield + rechargeAmount * TestValues.HalfRatio;

        regeneration.Heal(healAmount, TestValues.OneSecond);
        regeneration.RechargeShield(rechargeAmount, TestValues.OneSecond);

        regeneration.Tick(TestValues.HalfSecond);

        Assert.AreEqual(expectedHealth, health.Current, TestValues.Tolerance, "Verifies that StatsRegeneration forwards Tick to health regeneration.");
        Assert.AreEqual(expectedShield, shield.Current, TestValues.Tolerance, "Verifies that StatsRegeneration forwards Tick to shield regeneration.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.HealedCallCount, "OnHealed should be called when aggregated health regeneration ticks.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should be called when aggregated shield regeneration ticks.");
    }

    [Test]
    public void Heal_WhenDurationIsZero_ShouldUseHealthRegenerationImmediately()
    {
        StatsRegeneration regeneration = CreateRegeneration(
            out Health health,
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        float startHealth = health.Current;
        float healAmount = health.Max * TestValues.QuarterRatio;
        float expectedHealth = startHealth + healAmount;

        regeneration.Heal(healAmount, TestValues.Empty);

        Assert.AreEqual(expectedHealth, health.Current, TestValues.Tolerance, "Verifies that StatsRegeneration delegates zero-duration healing to HealthRegeneration.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.HealedCallCount, "OnHealed should be called by delegated zero-duration healing.");
    }

    [Test]
    public void RechargeShield_WhenDurationIsZero_ShouldUseShieldRegenerationImmediately()
    {
        StatsRegeneration regeneration = CreateRegeneration(
            out Health health,
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        float startShield = shield.Current;
        float rechargeAmount = shield.Max * TestValues.QuarterRatio;
        float expectedShield = startShield + rechargeAmount;

        regeneration.RechargeShield(rechargeAmount, TestValues.Empty);

        Assert.AreEqual(expectedShield, shield.Current, TestValues.Tolerance, "Verifies that StatsRegeneration delegates zero-duration shield recharge to ShieldRegeneration.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should be called by delegated zero-duration recharge.");
    }
    #endregion

    #region Validation
    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void Heal_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float amount)
    {
        StatsRegeneration regeneration = CreateRegeneration(
            out Health health,
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => regeneration.Heal(amount, TestValues.OneSecond),
            "StatsRegeneration.Heal should reject zero or negative healing amounts through HealthRegeneration."
        );
    }

    [Test]
    public void Heal_WhenDurationIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        StatsRegeneration regeneration = CreateRegeneration(
            out Health health,
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => regeneration.Heal(health.Max * TestValues.QuarterRatio, TestValues.InvalidNegativeAmount),
            "StatsRegeneration.Heal should reject negative durations through HealthRegeneration."
        );
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void RechargeShield_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float amount)
    {
        StatsRegeneration regeneration = CreateRegeneration(
            out Health health,
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => regeneration.RechargeShield(amount, TestValues.OneSecond),
            "StatsRegeneration.RechargeShield should reject zero or negative recharge amounts through ShieldRegeneration."
        );
    }

    [Test]
    public void RechargeShield_WhenDurationIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        StatsRegeneration regeneration = CreateRegeneration(
            out Health health,
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => regeneration.RechargeShield(shield.Max * TestValues.QuarterRatio, TestValues.InvalidNegativeAmount),
            "StatsRegeneration.RechargeShield should reject negative durations through ShieldRegeneration."
        );
    }

    [Test]
    public void Tick_WhenDeltaTimeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        StatsRegeneration regeneration = CreateRegeneration(
            out Health health,
            out Shield shield,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => regeneration.Tick(TestValues.InvalidNegativeAmount),
            "StatsRegeneration.Tick should reject negative delta time values through child regeneration systems."
        );
    }
    #endregion
}
