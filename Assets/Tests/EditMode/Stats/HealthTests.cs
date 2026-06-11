using NUnit.Framework;

public class HealthTests
{
    #region Runtime Behavior
    [Test]
    public void Initialize_ShouldSetCurrentToMax()
    {
        Health health = new Health();

        health.Initialize();

        Assert.AreEqual(health.Max, health.Current, TestValues.Tolerance, "Verifies that Initialize sets Current health equal to Max health.");
    }

    [Test]
    public void TakeDamage_WhenAmountIsPositive_ShouldReduceCurrent()
    {
        Health health = new Health();
        health.Initialize();

        float startHealth = health.Current;
        float damage = startHealth * TestValues.QuarterRatio;
        float expectedHealth = startHealth - damage;

        health.TakeDamage(damage);

        Assert.AreEqual(expectedHealth, health.Current, TestValues.Tolerance, "Verifies that positive damage reduces Current health by the exact damage amount.");
    }

    [Test]
    public void TakeDamage_WhenAmountExceedsCurrent_ShouldNotReduceCurrentBelowZero()
    {
        Health health = new Health();
        health.Initialize();

        float damage = health.Current + TestValues.ExtraDamage;

        health.TakeDamage(damage);

        Assert.AreEqual(TestValues.Empty, health.Current, TestValues.Tolerance, "Verifies that damage greater than Current health clamps Current to 0 instead of going negative.");
    }
    
    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void TakeDamage_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float damage)
    {
        Health health = new Health();
        health.Initialize();

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => health.TakeDamage(damage),
            "TakeDamage should reject zero or negative damage values."
        );
    }

    [Test]
    public void TakeDamage_WhenHealthIsDead_ShouldNotChangeCurrent()
    {
        Health health = new Health();
        health.Initialize();

        health.TakeDamage(health.Current + TestValues.ExtraDamage);

        health.TakeDamage(health.Max * TestValues.SmallRatio);

        Assert.AreEqual(TestValues.Empty, health.Current, TestValues.Tolerance, "Verifies that taking damage after death does not change Current health.");
    }

    [Test]
    public void Heal_WhenAmountIsPositive_ShouldIncreaseCurrent()
    {
        Health health = new Health();
        health.Initialize();

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        float currentBeforeHeal = health.Current;
        float healAmount = health.Max * TestValues.QuarterRatio;
        float expectedHealth = currentBeforeHeal + healAmount;

        health.Heal(healAmount);

        Assert.AreEqual(expectedHealth, health.Current, TestValues.Tolerance, "Verifies that positive healing increases Current health by the heal amount.");
    }

    [Test]
    public void Heal_WhenAmountExceedsMissingHealth_ShouldNotIncreaseCurrentAboveMax()
    {
        Health health = new Health();
        health.Initialize();

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        health.Heal(health.Max);

        Assert.AreEqual(health.Max, health.Current, TestValues.Tolerance, "Verifies that healing cannot increase Current health above Max health.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void Heal_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float healAmount)
    {
        Health health = new Health();
        health.Initialize();

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => health.Heal(healAmount),
            "Heal should reject zero or negative heal values."
        );
    }

    [Test]
    public void Heal_WhenHealthIsDead_ShouldNotChangeCurrent()
    {
        Health health = new Health();
        health.Initialize();

        health.TakeDamage(health.Current + TestValues.ExtraDamage);

        health.Heal(health.Max * TestValues.SmallRatio);

        Assert.AreEqual(TestValues.Empty, health.Current, TestValues.Tolerance, "Verifies that healing does not affect dead health.");
    }

    [Test]
    public void IsAlive_AfterInitialize_ShouldBeTrue()
    {
        Health health = new Health();

        health.Initialize();

        Assert.IsTrue(health.IsAlive, "Verifies that health is alive after initialization.");
    }

    [Test]
    public void IsAlive_WhenCurrentReachesZero_ShouldBeFalse()
    {
        Health health = new Health();
        health.Initialize();

        health.TakeDamage(health.Current + TestValues.ExtraDamage);

        Assert.IsFalse(health.IsAlive, "Verifies that health is no longer alive when Current reaches 0.");
    }

    [TestCase(TestValues.SmallRatio)]
    [TestCase(TestValues.QuarterRatio)]
    [TestCase(TestValues.HalfRatio)]
    public void IncreaseMaxHealthBy_WhenIncreaseCurrentByAmountIsTrue_ShouldIncreaseCurrent(float increaseRatio)
    {
        Health health = new Health();
        health.Initialize();

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        float increaseAmount = health.Max * increaseRatio;
        float startCurrent = health.Current;
        float expectedCurrent = startCurrent + increaseAmount;

        health.IncreaseMaxHealthBy(increaseAmount, true);

        Assert.AreEqual(expectedCurrent, health.Current, TestValues.Tolerance, "Verifies that increasing Max health also increases Current health when healing is enabled.");
    }

    [TestCase(TestValues.SmallRatio, true)]
    [TestCase(TestValues.QuarterRatio, true)]
    [TestCase(TestValues.HalfRatio, true)]
    [TestCase(TestValues.SmallRatio, false)]
    [TestCase(TestValues.QuarterRatio, false)]
    [TestCase(TestValues.HalfRatio, false)]
    public void IncreaseMaxHealthBy_WhenAmountIsPositive_ShouldAlwaysIncreaseMax(
        float increaseRatio,
        bool increaseCurrentByAmount)
    {
        Health health = new Health();
        health.Initialize();

        float increaseAmount = health.Max * increaseRatio;
        float startMax = health.Max;
        float expectedMax = startMax + increaseAmount;

        health.IncreaseMaxHealthBy(increaseAmount, increaseCurrentByAmount);

        Assert.AreEqual(expectedMax, health.Max, TestValues.Tolerance, "Verifies that positive max health increases always raise Max, regardless of the healing option.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void IncreaseMaxHealthBy_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float increaseAmount)
    {
        Health health = new Health();
        health.Initialize();

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => health.IncreaseMaxHealthBy(increaseAmount),
            "IncreaseMaxHealthBy should reject zero or negative increase values."
        );
    }

    [TestCase(TestValues.SmallRatio)]
    [TestCase(TestValues.QuarterRatio)]
    [TestCase(TestValues.HalfRatio)]
    public void IncreaseMaxHealthBy_WhenIncreaseCurrentByAmountIsFalse_ShouldKeepCurrentUnchanged(float increaseRatio)
    {
        Health health = new Health();
        health.Initialize();

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        float increaseAmount = health.Max * increaseRatio;
        float startCurrent = health.Current;

        health.IncreaseMaxHealthBy(increaseAmount, false);

        Assert.AreEqual(startCurrent, health.Current, TestValues.Tolerance, "Verifies that increasing Max health does not change Current health when increasing Current by amount is disabled.");
    }

    [TestCase(TestValues.SmallRatio)]
    [TestCase(TestValues.QuarterRatio)]
    [TestCase(TestValues.HalfRatio)]
    public void IncreaseMaxHealthBy_WhenHealthIsDead_ShouldKeepCurrentAtZero(float increaseRatio)
    {
        Health health = new Health();
        health.Initialize();

        health.TakeDamage(health.Current + TestValues.ExtraDamage);

        float increaseAmount = health.Max * increaseRatio;
        float startMax = health.Max;
        float expectedMax = startMax + increaseAmount;

        health.IncreaseMaxHealthBy(increaseAmount, true);

        Assert.AreEqual(expectedMax, health.Max, TestValues.Tolerance, "Increasing Max health should still update Max even when health is dead.");
        Assert.AreEqual(TestValues.Empty, health.Current, TestValues.Tolerance, "Verifies that increasing Max health does not restore Current health when health is dead.");
    }
    #endregion


    #region Test Helpers
    [Test]
    public void SetCurrentForTests_WhenValueExceedsMax_ShouldReturnFalse()
    {
        Health health = new Health();
        health.Initialize();

        float startHealth = health.Current;
        float value = health.Max + TestValues.ValueAboveMaxOffset;

        bool result = health.SetCurrentForTests(value);

        Assert.IsFalse(result, "Current values above Max should be rejected.");
        Assert.AreEqual(startHealth, health.Current, TestValues.Tolerance, "Verifies that Current cannot be set above Max through the test helper.");
    }

    [Test]
    public void SetMaxForTests_WhenValueIsValid_ShouldSetMax()
    {
        Health health = new Health();
        health.Initialize();

        float newMax = health.Max * TestValues.DoubleRatio;

        bool result = health.SetMaxForTests(newMax);

        Assert.IsTrue(result, "A valid Max value should be accepted.");
        Assert.AreEqual(newMax, health.Max, TestValues.Tolerance, "A valid Max value should update Max health.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void SetMaxForTests_WhenValueIsNotPositive_ShouldReturnFalse(float value)
    {
        Health health = new Health();
        health.Initialize();

        float startMax = health.Max;
        float startCurrent = health.Current;

        bool result = health.SetMaxForTests(value);

        Assert.IsFalse(result, "Zero or negative Max values should be rejected.");
        Assert.AreEqual(startMax, health.Max, TestValues.Tolerance, "Rejected Max values should not change Max health.");
        Assert.AreEqual(startCurrent, health.Current, TestValues.Tolerance, "Rejected Max values should not change Current health.");
    }

    [Test]
    public void SetMaxForTests_WhenNewMaxIsLowerThanCurrent_ShouldClampCurrentToNewMax()
    {
        Health health = new Health();
        health.Initialize();

        float newMax = health.Max * TestValues.HalfRatio;

        bool result = health.SetMaxForTests(newMax);

        Assert.IsTrue(result, "A lower positive Max value should be accepted.");
        Assert.AreEqual(newMax, health.Max, TestValues.Tolerance, "SetMaxForTests should update Max to the lower valid value.");
        Assert.AreEqual(newMax, health.Current, TestValues.Tolerance, "Lowering Max below Current should clamp Current to the new Max.");
    }

    [Test]
    public void SetMaxForTests_WhenNewMaxIsHigherThanCurrent_ShouldKeepCurrentUnchanged()
    {
        Health health = new Health();
        health.Initialize();

        float current = health.Max * TestValues.HalfRatio;
        bool setCurrentResult = health.SetCurrentForTests(current);

        Assert.IsTrue(setCurrentResult, "SetCurrentForTests should accept the setup Current value.");

        float newMax = health.Max * TestValues.DoubleRatio;

        bool result = health.SetMaxForTests(newMax);

        Assert.IsTrue(result, "A higher positive Max value should be accepted.");
        Assert.AreEqual(newMax, health.Max, TestValues.Tolerance, "SetMaxForTests should update Max to the higher valid value.");
        Assert.AreEqual(current, health.Current, TestValues.Tolerance, "Verifies that raising Max does not change Current health.");
    }

    [Test]
    public void SetCurrentForTests_WhenValueIsValid_ShouldSetCurrent()
    {
        Health health = new Health();
        health.Initialize();

        float value = health.Max * TestValues.HalfRatio;

        bool result = health.SetCurrentForTests(value);

        Assert.IsTrue(result, "A valid Current value should be accepted.");
        Assert.AreEqual(value, health.Current, TestValues.Tolerance, "Verifies that a valid test value updates Current health.");
    }

    [Test]
    public void SetCurrentForTests_WhenValueIsNegative_ShouldReturnFalse()
    {
        Health health = new Health();
        health.Initialize();

        float startHealth = health.Current;

        bool result = health.SetCurrentForTests(-health.Max * TestValues.SmallRatio);

        Assert.IsFalse(result, "Negative Current values should be rejected.");
        Assert.AreEqual(startHealth, health.Current, TestValues.Tolerance, "Negative Current values should not change Current health.");
    }

    [TestCase(TestValues.HalfRatio)]
    [TestCase(TestValues.FullRatio)]
    [TestCase(TestValues.DoubleRatio)]
    public void MaxHealth_WhenMaxIsChangedForTests_ShouldMatchMax(float maxRatio)
    {
        Health health = new Health();
        health.Initialize();

        float max = health.Max * maxRatio;

        bool result = health.SetMaxForTests(max);

        Assert.IsTrue(result, "SetMaxForTests should accept the test Max value before verifying MaxHealth.");
        Assert.AreEqual(health.Max, health.MaxHealth, TestValues.Tolerance, "Verifies that MaxHealth always returns the same value as Max.");
    }

    [TestCase(TestValues.Empty)]
    [TestCase(TestValues.QuarterRatio)]
    [TestCase(TestValues.FullRatio)]
    public void CurrentHealth_WhenCurrentIsChangedForTests_ShouldMatchCurrent(float currentRatio)
    {
        Health health = new Health();
        health.Initialize();

        float current = health.Max * currentRatio;

        bool result = health.SetCurrentForTests(current);

        Assert.IsTrue(result, "SetCurrentForTests should accept the test Current value before verifying CurrentHealth.");
        Assert.AreEqual(health.Current, health.CurrentHealth, TestValues.Tolerance, "Verifies that CurrentHealth always returns the same value as Current.");
    }
    #endregion

}
