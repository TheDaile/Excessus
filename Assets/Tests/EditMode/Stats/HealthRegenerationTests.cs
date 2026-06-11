using NUnit.Framework;

public class HealthRegenerationTests
{
    private static HealthRegeneration CreateRegeneration(
        out Health health,
        out StatsEventsRecorder eventsRecorder)
    {
        health = new Health();
        health.Initialize();

        eventsRecorder = new StatsEventsRecorder();

        return new HealthRegeneration(health, eventsRecorder);
    }

    #region Healing
    [Test]
    public void Heal_WhenDurationIsZero_ShouldHealImmediatelyAndCallOnHealed()
    {
        HealthRegeneration regeneration = CreateRegeneration(
            out Health health,
            out StatsEventsRecorder eventsRecorder
        );

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        float startHealth = health.Current;
        float healAmount = health.Max * TestValues.QuarterRatio;
        float expectedHealth = startHealth + healAmount;

        regeneration.Heal(healAmount, TestValues.Empty);

        Assert.AreEqual(expectedHealth, health.Current, TestValues.Tolerance, "Verifies that zero-duration healing is applied immediately.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.HealedCallCount, "OnHealed should be called once when zero-duration healing restores health.");
        Assert.AreEqual(healAmount, eventsRecorder.LastHealedAmount, TestValues.Tolerance, "OnHealed should receive the immediate healed amount.");
    }

    [Test]
    public void Heal_WhenDurationIsPositive_ShouldNotApplyHealingImmediately()
    {
        HealthRegeneration regeneration = CreateRegeneration(
            out Health health,
            out StatsEventsRecorder eventsRecorder
        );

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        float startHealth = health.Current;
        float healAmount = health.Max * TestValues.QuarterRatio;

        regeneration.Heal(healAmount, TestValues.OneSecond);

        Assert.AreEqual(startHealth, health.Current, TestValues.Tolerance, "Verifies that positive-duration healing waits for Tick instead of applying instantly.");
        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.HealedCallCount, "OnHealed should not be called before over-time healing ticks.");
    }

    [Test]
    public void Tick_WhenHalfDurationPasses_ShouldHealHalfOfTargetAmount()
    {
        HealthRegeneration regeneration = CreateRegeneration(
            out Health health,
            out StatsEventsRecorder eventsRecorder
        );

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        float startHealth = health.Current;
        float healAmount = health.Max * TestValues.QuarterRatio;
        float expectedHealStep = healAmount * TestValues.HalfRatio;
        float expectedHealth = startHealth + expectedHealStep;

        regeneration.Heal(healAmount, TestValues.OneSecond);
        regeneration.Tick(TestValues.HalfSecond);

        Assert.AreEqual(expectedHealth, health.Current, TestValues.Tolerance, "Verifies that half of the healing duration restores half of the target amount.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.HealedCallCount, "OnHealed should be called for the first over-time healing step.");
        Assert.AreEqual(expectedHealStep, eventsRecorder.LastHealedAmount, TestValues.Tolerance, "OnHealed should receive the actual healing step amount.");
    }

    [Test]
    public void Tick_WhenDurationCompletes_ShouldHealToTargetAmount()
    {
        HealthRegeneration regeneration = CreateRegeneration(
            out Health health,
            out StatsEventsRecorder eventsRecorder
        );

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        float startHealth = health.Current;
        float healAmount = health.Max * TestValues.QuarterRatio;
        float expectedHealth = startHealth + healAmount;
        float expectedHealStep = healAmount * TestValues.HalfRatio;

        regeneration.Heal(healAmount, TestValues.OneSecond);
        regeneration.Tick(TestValues.HalfSecond);
        regeneration.Tick(TestValues.HalfSecond);

        Assert.AreEqual(expectedHealth, health.Current, TestValues.Tolerance, "Verifies that completing the healing duration restores the full target amount.");
        Assert.AreEqual(TestValues.TwoCalls, eventsRecorder.HealedCallCount, "OnHealed should be called once per positive healing step.");
        Assert.AreEqual(expectedHealStep, eventsRecorder.LastHealedAmount, TestValues.Tolerance, "The final OnHealed call should receive only the final healing step.");
    }

    [Test]
    public void Heal_WhenAmountExceedsMissingHealth_ShouldClampTargetToMaxHealth()
    {
        HealthRegeneration regeneration = CreateRegeneration(
            out Health health,
            out StatsEventsRecorder eventsRecorder
        );

        float damage = health.Max * TestValues.QuarterRatio;
        health.TakeDamage(damage);

        regeneration.Heal(health.Max, TestValues.Empty);

        Assert.AreEqual(health.Max, health.Current, TestValues.Tolerance, "Verifies that healing cannot increase Current health above Max health.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.HealedCallCount, "OnHealed should be called when clamped healing restores health.");
        Assert.AreEqual(damage, eventsRecorder.LastHealedAmount, TestValues.Tolerance, "OnHealed should receive only the actually restored health amount after clamping.");
    }

    [Test]
    public void Heal_WhenHealthIsDead_ShouldNotHealOrCallOnHealed()
    {
        HealthRegeneration regeneration = CreateRegeneration(
            out Health health,
            out StatsEventsRecorder eventsRecorder
        );

        health.TakeDamage(health.Current + TestValues.ExtraDamage);

        regeneration.Heal(health.Max * TestValues.QuarterRatio, TestValues.Empty);

        Assert.AreEqual(TestValues.Empty, health.Current, TestValues.Tolerance, "Verifies that dead health is not restored by regeneration.");
        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.HealedCallCount, "OnHealed should not be called when regeneration is requested for dead health.");
    }

    [Test]
    public void Tick_WhenHealthDiesDuringRegeneration_ShouldStopWithoutAdditionalHealing()
    {
        HealthRegeneration regeneration = CreateRegeneration(
            out Health health,
            out StatsEventsRecorder eventsRecorder
        );

        health.TakeDamage(health.Max * TestValues.HalfRatio);
        regeneration.Heal(health.Max * TestValues.QuarterRatio, TestValues.OneSecond);

        regeneration.Tick(TestValues.HalfSecond);
        int healedCallsBeforeDeath = eventsRecorder.HealedCallCount;

        health.TakeDamage(health.Current + TestValues.ExtraDamage);
        regeneration.Tick(TestValues.HalfSecond);

        Assert.AreEqual(TestValues.Empty, health.Current, TestValues.Tolerance, "Verifies that over-time healing stops after health reaches 0 during regeneration.");
        Assert.AreEqual(TestValues.OneCall, healedCallsBeforeDeath, "The setup should apply one healing step before health dies.");
        Assert.AreEqual(healedCallsBeforeDeath, eventsRecorder.HealedCallCount, "OnHealed should not be called again after health dies during regeneration.");
    }
    #endregion

    #region Validation
    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void Heal_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float amount)
    {
        HealthRegeneration regeneration = CreateRegeneration(
            out Health health,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => regeneration.Heal(amount, TestValues.OneSecond),
            "HealthRegeneration.Heal should reject zero or negative healing amounts."
        );
    }

    [Test]
    public void Heal_WhenDurationIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        HealthRegeneration regeneration = CreateRegeneration(
            out Health health,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => regeneration.Heal(health.Max * TestValues.QuarterRatio, TestValues.InvalidNegativeAmount),
            "HealthRegeneration.Heal should reject negative durations."
        );
    }

    [Test]
    public void Tick_WhenDeltaTimeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        HealthRegeneration regeneration = CreateRegeneration(
            out Health health,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => regeneration.Tick(TestValues.InvalidNegativeAmount),
            "HealthRegeneration.Tick should reject negative delta time values."
        );
    }
    #endregion
}
