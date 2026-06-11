using NUnit.Framework;

public class StaminaTests
{
    #region Runtime Behavior
    [Test]
    public void Initialize_ShouldSetCurrentToMax()
    {
        Stamina stamina = new Stamina();

        stamina.Initialize();

        Assert.AreEqual(stamina.Max, stamina.Current, TestValues.Tolerance, "Verifies that Initialize sets Current stamina equal to Max stamina.");
    }

    [Test]
    public void Use_WhenAmountIsPositive_ShouldReduceCurrent()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float startStamina = stamina.Current;
        float amount = startStamina * TestValues.QuarterRatio;
        float expectedStamina = startStamina - amount;

        bool result = stamina.Use(amount, TestValues.Empty);

        Assert.IsTrue(result, "Use should return true when enough stamina is available.");
        Assert.AreEqual(expectedStamina, stamina.Current, TestValues.Tolerance, "Verifies that positive stamina use reduces Current stamina by the used amount.");
    }

    [Test]
    public void Use_WhenAmountExceedsCurrent_ShouldReturnFalseAndKeepCurrentUnchanged()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        bool setCurrentResult = stamina.SetCurrentForTests(stamina.Max * TestValues.HalfRatio);
        Assert.IsTrue(setCurrentResult, "SetCurrentForTests should accept the setup Current value.");

        float startStamina = stamina.Current;
        float amount = stamina.Max;

        bool result = stamina.Use(amount, TestValues.Empty);

        Assert.IsFalse(result, "Use should return false when Current stamina is lower than the requested amount.");
        Assert.AreEqual(startStamina, stamina.Current, TestValues.Tolerance, "Verifies that failed stamina use does not change Current stamina.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void Use_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float amount)
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => stamina.Use(amount, TestValues.Empty),
            "Use should reject zero or negative stamina values."
        );
    }

    [Test]
    public void Regenerate_WhenAmountIsPositive_ShouldIncreaseCurrent()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        stamina.Use(stamina.Max * TestValues.HalfRatio, TestValues.Empty);

        float startStamina = stamina.Current;
        float regenerationAmount = stamina.Max * TestValues.QuarterRatio;
        float expectedStamina = startStamina + regenerationAmount;

        stamina.Regenerate(regenerationAmount);

        Assert.AreEqual(expectedStamina, stamina.Current, TestValues.Tolerance, "Verifies that positive regeneration increases Current stamina by the regeneration amount.");
    }

    [Test]
    public void Regenerate_WhenAmountExceedsMissingStamina_ShouldNotIncreaseCurrentAboveMax()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        stamina.Use(stamina.Max * TestValues.HalfRatio, TestValues.Empty);

        stamina.Regenerate(stamina.Max);

        Assert.AreEqual(stamina.Max, stamina.Current, TestValues.Tolerance, "Verifies that regeneration cannot increase Current stamina above Max stamina.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void Regenerate_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float amount)
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => stamina.Regenerate(amount),
            "Regenerate should reject zero or negative regeneration values."
        );
    }

    [Test]
    public void Tick_WhenDeltaTimeIsZero_ShouldNotChangeCurrent()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        stamina.Use(stamina.Max * TestValues.HalfRatio, TestValues.Empty);

        float startStamina = stamina.Current;

        stamina.Tick(TestValues.Empty, TestValues.OneSecond);

        Assert.AreEqual(startStamina, stamina.Current, TestValues.Tolerance, "Verifies that zero delta time does not regenerate stamina.");
    }

    [Test]
    public void Tick_WhenDeltaTimeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => stamina.Tick(TestValues.InvalidNegativeAmount, TestValues.Empty),
            "Tick should reject negative delta time values."
        );
    }

    [Test]
    public void Tick_WhenRegenerationDelayHasNotPassed_ShouldKeepCurrentUnchanged()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        bool setRegenerationDelayResult = stamina.SetRegenerationDelayForTests(TestValues.OneSecond);
        Assert.IsTrue(setRegenerationDelayResult, "SetRegenerationDelayForTests should accept the setup regeneration delay.");

        stamina.Use(stamina.Max * TestValues.HalfRatio, TestValues.Empty);

        float startStamina = stamina.Current;
        float currentTimeBeforeDelay = TestValues.TimeBeforeRegenerationDelay;

        stamina.Tick(TestValues.OneSecond, currentTimeBeforeDelay);

        Assert.AreEqual(startStamina, stamina.Current, TestValues.Tolerance, "Verifies that stamina does not regenerate before the regeneration delay has passed.");
    }

    [Test]
    public void Tick_WhenRegenerationDelayHasPassed_ShouldRegenerateCurrent()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float regenerationPerSecond = stamina.Max * TestValues.QuarterRatio;
        bool setRegenerationPerSecondResult = stamina.SetRegenerationPerSecondForTests(regenerationPerSecond);
        bool setRegenerationDelayResult = stamina.SetRegenerationDelayForTests(TestValues.OneSecond);

        Assert.IsTrue(setRegenerationPerSecondResult, "SetRegenerationPerSecondForTests should accept the setup regeneration rate.");
        Assert.IsTrue(setRegenerationDelayResult, "SetRegenerationDelayForTests should accept the setup regeneration delay.");

        stamina.Use(stamina.Max * TestValues.HalfRatio, TestValues.Empty);

        float startStamina = stamina.Current;
        float deltaTime = TestValues.OneSecond;
        float currentTimeAfterDelay = TestValues.OneSecond;
        float expectedStamina = startStamina + regenerationPerSecond * deltaTime;

        stamina.Tick(deltaTime, currentTimeAfterDelay);

        Assert.AreEqual(expectedStamina, stamina.Current, TestValues.Tolerance, "Verifies that Tick regenerates Current stamina after the regeneration delay has passed.");
    }

    [Test]
    public void HasStamina_AfterInitialize_ShouldBeTrue()
    {
        Stamina stamina = new Stamina();

        stamina.Initialize();

        Assert.IsTrue(stamina.HasStamina, "Verifies that stamina is available after initialization.");
    }

    [Test]
    public void HasStamina_WhenCurrentReachesZero_ShouldBeFalse()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        stamina.Use(stamina.Current, TestValues.Empty);

        Assert.IsFalse(stamina.HasStamina, "Verifies that stamina is unavailable when Current stamina reaches 0.");
    }
    #endregion

    #region Test Helpers
    [Test]
    public void SetCurrentForTests_WhenValueIsValid_ShouldSetCurrent()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float value = stamina.Max * TestValues.HalfRatio;

        bool result = stamina.SetCurrentForTests(value);

        Assert.IsTrue(result, "A valid Current value should be accepted.");
        Assert.AreEqual(value, stamina.Current, TestValues.Tolerance, "Verifies that a valid test value updates Current stamina.");
    }

    [Test]
    public void SetCurrentForTests_WhenValueIsNegative_ShouldReturnFalse()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float startStamina = stamina.Current;

        bool result = stamina.SetCurrentForTests(-stamina.Max * TestValues.SmallRatio);

        Assert.IsFalse(result, "Negative Current values should be rejected.");
        Assert.AreEqual(startStamina, stamina.Current, TestValues.Tolerance, "Negative Current values should not change Current stamina.");
    }

    [Test]
    public void SetCurrentForTests_WhenValueExceedsMax_ShouldReturnFalse()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float startStamina = stamina.Current;
        float value = stamina.Max + TestValues.ValueAboveMaxOffset;

        bool result = stamina.SetCurrentForTests(value);

        Assert.IsFalse(result, "Current values above Max should be rejected.");
        Assert.AreEqual(startStamina, stamina.Current, TestValues.Tolerance, "Verifies that Current cannot be set above Max through the test helper.");
    }

    [Test]
    public void SetMaxForTests_WhenValueIsValid_ShouldSetMax()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float newMax = stamina.Max * TestValues.DoubleRatio;

        bool result = stamina.SetMaxForTests(newMax);

        Assert.IsTrue(result, "A valid Max value should be accepted.");
        Assert.AreEqual(newMax, stamina.Max, TestValues.Tolerance, "A valid Max value should update Max stamina.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void SetMaxForTests_WhenValueIsNotPositive_ShouldReturnFalse(float value)
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float startMax = stamina.Max;
        float startCurrent = stamina.Current;

        bool result = stamina.SetMaxForTests(value);

        Assert.IsFalse(result, "Zero or negative Max values should be rejected.");
        Assert.AreEqual(startMax, stamina.Max, TestValues.Tolerance, "Rejected Max values should not change Max stamina.");
        Assert.AreEqual(startCurrent, stamina.Current, TestValues.Tolerance, "Rejected Max values should not change Current stamina.");
    }

    [Test]
    public void SetMaxForTests_WhenNewMaxIsLowerThanCurrent_ShouldClampCurrentToNewMax()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float newMax = stamina.Max * TestValues.HalfRatio;

        bool result = stamina.SetMaxForTests(newMax);

        Assert.IsTrue(result, "A lower positive Max value should be accepted.");
        Assert.AreEqual(newMax, stamina.Max, TestValues.Tolerance, "SetMaxForTests should update Max to the lower valid value.");
        Assert.AreEqual(newMax, stamina.Current, TestValues.Tolerance, "Lowering Max below Current should clamp Current to the new Max.");
    }

    [Test]
    public void SetMaxForTests_WhenNewMaxIsHigherThanCurrent_ShouldKeepCurrentUnchanged()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float current = stamina.Max * TestValues.HalfRatio;
        bool setCurrentResult = stamina.SetCurrentForTests(current);

        Assert.IsTrue(setCurrentResult, "SetCurrentForTests should accept the setup Current value.");

        float newMax = stamina.Max * TestValues.DoubleRatio;

        bool result = stamina.SetMaxForTests(newMax);

        Assert.IsTrue(result, "A higher positive Max value should be accepted.");
        Assert.AreEqual(newMax, stamina.Max, TestValues.Tolerance, "SetMaxForTests should update Max to the higher valid value.");
        Assert.AreEqual(current, stamina.Current, TestValues.Tolerance, "Verifies that raising Max does not change Current stamina.");
    }

    [TestCase(TestValues.HalfRatio)]
    [TestCase(TestValues.FullRatio)]
    [TestCase(TestValues.DoubleRatio)]
    public void MaxStamina_WhenMaxIsChangedForTests_ShouldMatchMax(float maxRatio)
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float max = stamina.Max * maxRatio;

        bool result = stamina.SetMaxForTests(max);

        Assert.IsTrue(result, "SetMaxForTests should accept the test Max value before verifying MaxStamina.");
        Assert.AreEqual(stamina.Max, stamina.MaxStamina, TestValues.Tolerance, "Verifies that MaxStamina always returns the same value as Max.");
    }

    [TestCase(TestValues.Empty)]
    [TestCase(TestValues.QuarterRatio)]
    [TestCase(TestValues.FullRatio)]
    public void CurrentStamina_WhenCurrentIsChangedForTests_ShouldMatchCurrent(float currentRatio)
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float current = stamina.Max * currentRatio;

        bool result = stamina.SetCurrentForTests(current);

        Assert.IsTrue(result, "SetCurrentForTests should accept the test Current value before verifying CurrentStamina.");
        Assert.AreEqual(stamina.Current, stamina.CurrentStamina, TestValues.Tolerance, "Verifies that CurrentStamina always returns the same value as Current.");
    }

    [Test]
    public void SetRegenerationPerSecondForTests_WhenValueIsValid_ShouldAffectTickRegeneration()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float value = stamina.Max * TestValues.QuarterRatio;

        bool result = stamina.SetRegenerationPerSecondForTests(value);
        bool setRegenerationDelayResult = stamina.SetRegenerationDelayForTests(TestValues.Empty);

        Assert.IsTrue(result, "Positive regeneration per second values should be accepted.");
        Assert.IsTrue(setRegenerationDelayResult, "SetRegenerationDelayForTests should accept the setup regeneration delay.");

        stamina.Use(stamina.Max * TestValues.HalfRatio, TestValues.Empty);

        float startStamina = stamina.Current;
        float expectedStamina = startStamina + value * TestValues.OneSecond;

        stamina.Tick(TestValues.OneSecond, TestValues.Empty);

        Assert.AreEqual(expectedStamina, stamina.Current, TestValues.Tolerance, "Verifies that the test regeneration rate affects Tick regeneration.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void SetRegenerationPerSecondForTests_WhenValueIsNotPositive_ShouldReturnFalse(float value)
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        bool result = stamina.SetRegenerationPerSecondForTests(value);

        Assert.IsFalse(result, "Zero or negative regeneration per second values should be rejected.");
    }

    [Test]
    public void SetRegenerationDelayForTests_WhenValueIsZero_ShouldAllowImmediateRegeneration()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        float regenerationPerSecond = stamina.Max * TestValues.QuarterRatio;
        bool setRegenerationPerSecondResult = stamina.SetRegenerationPerSecondForTests(regenerationPerSecond);
        bool result = stamina.SetRegenerationDelayForTests(TestValues.Empty);

        Assert.IsTrue(result, "Zero or positive regeneration delay values should be accepted.");
        Assert.IsTrue(setRegenerationPerSecondResult, "SetRegenerationPerSecondForTests should accept the setup regeneration rate.");

        stamina.Use(stamina.Max * TestValues.HalfRatio, TestValues.Empty);

        float startStamina = stamina.Current;
        float expectedStamina = startStamina + regenerationPerSecond * TestValues.OneSecond;

        stamina.Tick(TestValues.OneSecond, TestValues.Empty);

        Assert.AreEqual(expectedStamina, stamina.Current, TestValues.Tolerance, "Verifies that zero regeneration delay allows immediate stamina regeneration.");
    }

    [Test]
    public void SetRegenerationDelayForTests_WhenValueIsPositive_ShouldDelayRegeneration()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        bool result = stamina.SetRegenerationDelayForTests(TestValues.TwoSeconds);

        Assert.IsTrue(result, "Zero or positive regeneration delay values should be accepted.");

        stamina.Use(stamina.Max * TestValues.HalfRatio, TestValues.Empty);

        float startStamina = stamina.Current;

        stamina.Tick(TestValues.OneSecond, TestValues.OneSecond);

        Assert.AreEqual(startStamina, stamina.Current, TestValues.Tolerance, "Verifies that a positive regeneration delay prevents early stamina regeneration.");
    }

    [Test]
    public void SetRegenerationDelayForTests_WhenValueIsNegative_ShouldReturnFalse()
    {
        Stamina stamina = new Stamina();
        stamina.Initialize();

        bool result = stamina.SetRegenerationDelayForTests(TestValues.InvalidNegativeAmount);

        Assert.IsFalse(result, "Negative regeneration delay values should be rejected.");
    }
    #endregion
}
