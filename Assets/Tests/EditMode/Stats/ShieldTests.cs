using NUnit.Framework;

public class ShieldTests
{
    #region Runtime Behavior
    [Test]
    public void Initialize_ShouldSetCurrentToMax()
    {
        Shield shield = new Shield();

        shield.Initialize();

        Assert.AreEqual(shield.Max, shield.Current, TestValues.Tolerance, "Verifies that Initialize sets Current shield equal to Max shield.");
    }

    [Test]
    public void AbsorbDamage_WhenDamageIsPositive_ShouldReduceCurrent()
    {
        Shield shield = new Shield();
        shield.Initialize();

        float startShield = shield.Current;
        float damage = startShield * TestValues.QuarterRatio;
        float expectedShield = startShield - damage;

        float remainingDamage = shield.AbsorbDamage(damage);

        Assert.AreEqual(expectedShield, shield.Current, TestValues.Tolerance, "Verifies that positive damage reduces Current shield by the absorbed amount.");
        Assert.AreEqual(TestValues.Empty, remainingDamage, TestValues.Tolerance, "Verifies that shield fully absorbs damage when Current shield is greater than damage.");
    }

    [Test]
    public void AbsorbDamage_WhenDamageExceedsCurrent_ShouldReturnRemainingDamage()
    {
        Shield shield = new Shield();
        shield.Initialize();

        float expectedRemainingDamage = shield.Max * TestValues.SmallRatio;
        float damage = shield.Current + expectedRemainingDamage;

        float remainingDamage = shield.AbsorbDamage(damage);

        Assert.AreEqual(expectedRemainingDamage, remainingDamage, TestValues.Tolerance, "Verifies that damage exceeding Current shield returns the unabsorbed damage.");
        Assert.AreEqual(TestValues.Empty, shield.Current, TestValues.Tolerance, "Verifies that damage exceeding Current shield clamps Current shield to 0.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void AbsorbDamage_WhenDamageIsNotPositive_ShouldThrowArgumentOutOfRangeException(float damage)
    {
        Shield shield = new Shield();
        shield.Initialize();

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => shield.AbsorbDamage(damage),
            "AbsorbDamage should reject zero or negative damage values."
        );
    }

    [Test]
    public void AbsorbDamage_WhenShieldIsEmpty_ShouldReturnFullDamage()
    {
        Shield shield = new Shield();
        shield.Initialize();

        shield.AbsorbDamage(shield.Current);

        float damage = shield.Max * TestValues.QuarterRatio;

        float remainingDamage = shield.AbsorbDamage(damage);

        Assert.AreEqual(damage, remainingDamage, TestValues.Tolerance, "Verifies that empty shield cannot absorb damage.");
        Assert.AreEqual(TestValues.Empty, shield.Current, TestValues.Tolerance, "Verifies that empty shield remains at 0 after absorbing damage.");
    }

    [Test]
    public void Recharge_WhenAmountIsPositive_ShouldIncreaseCurrent()
    {
        Shield shield = new Shield();
        shield.Initialize();

        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        float startShield = shield.Current;
        float rechargeAmount = shield.Max * TestValues.QuarterRatio;
        float expectedShield = startShield + rechargeAmount;

        shield.Recharge(rechargeAmount);

        Assert.AreEqual(expectedShield, shield.Current, TestValues.Tolerance, "Verifies that positive recharge increases Current shield by the recharge amount.");
    }

    [Test]
    public void Recharge_WhenAmountExceedsMissingShield_ShouldNotIncreaseCurrentAboveMax()
    {
        Shield shield = new Shield();
        shield.Initialize();

        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        shield.Recharge(shield.Max);

        Assert.AreEqual(shield.Max, shield.Current, TestValues.Tolerance, "Verifies that recharge cannot increase Current shield above Max shield.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void Recharge_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float rechargeAmount)
    {
        Shield shield = new Shield();
        shield.Initialize();

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => shield.Recharge(rechargeAmount),
            "Recharge should reject zero or negative recharge values."
        );
    }

    [Test]
    public void HasShield_AfterInitialize_ShouldBeTrue()
    {
        Shield shield = new Shield();

        shield.Initialize();

        Assert.IsTrue(shield.HasShield, "Verifies that shield is active after initialization.");
    }

    [Test]
    public void HasShield_WhenCurrentReachesZero_ShouldBeFalse()
    {
        Shield shield = new Shield();
        shield.Initialize();

        shield.AbsorbDamage(shield.Current);

        Assert.IsFalse(shield.HasShield, "Verifies that shield is inactive when Current shield reaches 0.");
    }

    [TestCase(TestValues.SmallRatio)]
    [TestCase(TestValues.QuarterRatio)]
    [TestCase(TestValues.HalfRatio)]
    public void IncreaseMaxShieldBy_WhenIncreaseCurrentByAmountIsTrue_ShouldIncreaseCurrent(float increaseRatio)
    {
        Shield shield = new Shield();
        shield.Initialize();

        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        float increaseAmount = shield.Max * increaseRatio;
        float startCurrent = shield.Current;
        float expectedCurrent = startCurrent + increaseAmount;

        shield.IncreaseMaxShieldBy(increaseAmount, true);

        Assert.AreEqual(expectedCurrent, shield.Current, TestValues.Tolerance, "Verifies that increasing Max shield also increases Current shield when recharging is enabled.");
    }

    [TestCase(TestValues.SmallRatio, true)]
    [TestCase(TestValues.QuarterRatio, true)]
    [TestCase(TestValues.HalfRatio, true)]
    [TestCase(TestValues.SmallRatio, false)]
    [TestCase(TestValues.QuarterRatio, false)]
    [TestCase(TestValues.HalfRatio, false)]
    public void IncreaseMaxShieldBy_WhenAmountIsPositive_ShouldAlwaysIncreaseMax(
        float increaseRatio,
        bool increaseCurrentByAmount)
    {
        Shield shield = new Shield();
        shield.Initialize();

        float increaseAmount = shield.Max * increaseRatio;
        float startMax = shield.Max;
        float expectedMax = startMax + increaseAmount;

        shield.IncreaseMaxShieldBy(increaseAmount, increaseCurrentByAmount);

        Assert.AreEqual(expectedMax, shield.Max, TestValues.Tolerance, "Verifies that positive max shield increases always raise Max, regardless of the recharge option.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void IncreaseMaxShieldBy_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float increaseAmount)
    {
        Shield shield = new Shield();
        shield.Initialize();

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => shield.IncreaseMaxShieldBy(increaseAmount),
            "IncreaseMaxShieldBy should reject zero or negative increase values."
        );
    }

    [TestCase(TestValues.SmallRatio)]
    [TestCase(TestValues.QuarterRatio)]
    [TestCase(TestValues.HalfRatio)]
    public void IncreaseMaxShieldBy_WhenIncreaseCurrentByAmountIsFalse_ShouldKeepCurrentUnchanged(float increaseRatio)
    {
        Shield shield = new Shield();
        shield.Initialize();

        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        float increaseAmount = shield.Max * increaseRatio;
        float startCurrent = shield.Current;

        shield.IncreaseMaxShieldBy(increaseAmount, false);

        Assert.AreEqual(startCurrent, shield.Current, TestValues.Tolerance, "Verifies that increasing Max shield does not change Current shield when increasing Current by amount is disabled.");
    }

    [TestCase(TestValues.SmallRatio)]
    [TestCase(TestValues.QuarterRatio)]
    [TestCase(TestValues.HalfRatio)]
    public void IncreaseMaxShieldBy_WhenShieldIsEmptyAndIncreaseCurrentByAmountIsTrue_ShouldIncreaseCurrent(float increaseRatio)
    {
        Shield shield = new Shield();
        shield.Initialize();

        shield.AbsorbDamage(shield.Current);

        float increaseAmount = shield.Max * increaseRatio;
        float startMax = shield.Max;
        float expectedMax = startMax + increaseAmount;

        shield.IncreaseMaxShieldBy(increaseAmount, true);

        Assert.AreEqual(expectedMax, shield.Max, TestValues.Tolerance, "Increasing Max shield should update Max even when shield is empty.");
        Assert.AreEqual(increaseAmount, shield.Current, TestValues.Tolerance, "Verifies that increasing Max shield can restore Current shield from 0 when increasing Current by amount is enabled.");
    }

    [TestCase(TestValues.SmallRatio)]
    [TestCase(TestValues.QuarterRatio)]
    [TestCase(TestValues.HalfRatio)]
    public void IncreaseMaxShieldBy_WhenShieldIsEmptyAndIncreaseCurrentByAmountIsFalse_ShouldKeepCurrentAtZero(float increaseRatio)
    {
        Shield shield = new Shield();
        shield.Initialize();

        shield.AbsorbDamage(shield.Current);

        float increaseAmount = shield.Max * increaseRatio;
        float startMax = shield.Max;
        float expectedMax = startMax + increaseAmount;

        shield.IncreaseMaxShieldBy(increaseAmount, false);

        Assert.AreEqual(expectedMax, shield.Max, TestValues.Tolerance, "Increasing Max shield should still update Max when shield is empty.");
        Assert.AreEqual(TestValues.Empty, shield.Current, TestValues.Tolerance, "Verifies that increasing Max shield does not restore Current shield from 0 when increasing Current by amount is disabled.");
    }
    #endregion

    #region Test Helpers
    [Test]
    public void SetCurrentForTests_WhenValueIsValid_ShouldSetCurrent()
    {
        Shield shield = new Shield();
        shield.Initialize();

        float value = shield.Max * TestValues.HalfRatio;

        bool result = shield.SetCurrentForTests(value);

        Assert.IsTrue(result, "A valid Current value should be accepted.");
        Assert.AreEqual(value, shield.Current, TestValues.Tolerance, "Verifies that a valid test value updates Current shield.");
    }

    [Test]
    public void SetCurrentForTests_WhenValueIsNegative_ShouldReturnFalse()
    {
        Shield shield = new Shield();
        shield.Initialize();

        float startShield = shield.Current;

        bool result = shield.SetCurrentForTests(-shield.Max * TestValues.SmallRatio);

        Assert.IsFalse(result, "Negative Current values should be rejected.");
        Assert.AreEqual(startShield, shield.Current, TestValues.Tolerance, "Negative Current values should not change Current shield.");
    }

    [Test]
    public void SetCurrentForTests_WhenValueExceedsMax_ShouldReturnFalse()
    {
        Shield shield = new Shield();
        shield.Initialize();

        float startShield = shield.Current;
        float value = shield.Max + TestValues.ValueAboveMaxOffset;

        bool result = shield.SetCurrentForTests(value);

        Assert.IsFalse(result, "Current values above Max should be rejected.");
        Assert.AreEqual(startShield, shield.Current, TestValues.Tolerance, "Verifies that Current cannot be set above Max through the test helper.");
    }

    [Test]
    public void SetMaxForTests_WhenValueIsValid_ShouldSetMax()
    {
        Shield shield = new Shield();
        shield.Initialize();

        float newMax = shield.Max * TestValues.DoubleRatio;

        bool result = shield.SetMaxForTests(newMax);

        Assert.IsTrue(result, "A valid Max value should be accepted.");
        Assert.AreEqual(newMax, shield.Max, TestValues.Tolerance, "A valid Max value should update Max shield.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void SetMaxForTests_WhenValueIsNotPositive_ShouldReturnFalse(float value)
    {
        Shield shield = new Shield();
        shield.Initialize();

        float startMax = shield.Max;
        float startCurrent = shield.Current;

        bool result = shield.SetMaxForTests(value);

        Assert.IsFalse(result, "Zero or negative Max values should be rejected.");
        Assert.AreEqual(startMax, shield.Max, TestValues.Tolerance, "Rejected Max values should not change Max shield.");
        Assert.AreEqual(startCurrent, shield.Current, TestValues.Tolerance, "Rejected Max values should not change Current shield.");
    }

    [Test]
    public void SetMaxForTests_WhenNewMaxIsLowerThanCurrent_ShouldClampCurrentToNewMax()
    {
        Shield shield = new Shield();
        shield.Initialize();

        float newMax = shield.Max * TestValues.HalfRatio;

        bool result = shield.SetMaxForTests(newMax);

        Assert.IsTrue(result, "A lower positive Max value should be accepted.");
        Assert.AreEqual(newMax, shield.Max, TestValues.Tolerance, "SetMaxForTests should update Max to the lower valid value.");
        Assert.AreEqual(newMax, shield.Current, TestValues.Tolerance, "Lowering Max below Current should clamp Current to the new Max.");
    }

    [Test]
    public void SetMaxForTests_WhenNewMaxIsHigherThanCurrent_ShouldKeepCurrentUnchanged()
    {
        Shield shield = new Shield();
        shield.Initialize();

        float current = shield.Max * TestValues.HalfRatio;
        bool setCurrentResult = shield.SetCurrentForTests(current);

        Assert.IsTrue(setCurrentResult, "SetCurrentForTests should accept the setup Current value.");

        float newMax = shield.Max * TestValues.DoubleRatio;

        bool result = shield.SetMaxForTests(newMax);

        Assert.IsTrue(result, "A higher positive Max value should be accepted.");
        Assert.AreEqual(newMax, shield.Max, TestValues.Tolerance, "SetMaxForTests should update Max to the higher valid value.");
        Assert.AreEqual(current, shield.Current, TestValues.Tolerance, "Verifies that raising Max does not change Current shield.");
    }

    [TestCase(TestValues.HalfRatio)]
    [TestCase(TestValues.FullRatio)]
    [TestCase(TestValues.DoubleRatio)]
    public void MaxShield_WhenMaxIsChangedForTests_ShouldMatchMax(float maxRatio)
    {
        Shield shield = new Shield();
        shield.Initialize();

        float max = shield.Max * maxRatio;

        bool result = shield.SetMaxForTests(max);

        Assert.IsTrue(result, "SetMaxForTests should accept the test Max value before verifying MaxShield.");
        Assert.AreEqual(shield.Max, shield.MaxShield, TestValues.Tolerance, "Verifies that MaxShield always returns the same value as Max.");
    }

    [TestCase(TestValues.Empty)]
    [TestCase(TestValues.QuarterRatio)]
    [TestCase(TestValues.FullRatio)]
    public void CurrentShield_WhenCurrentIsChangedForTests_ShouldMatchCurrent(float currentRatio)
    {
        Shield shield = new Shield();
        shield.Initialize();

        float current = shield.Max * currentRatio;

        bool result = shield.SetCurrentForTests(current);

        Assert.IsTrue(result, "SetCurrentForTests should accept the test Current value before verifying CurrentShield.");
        Assert.AreEqual(shield.Current, shield.CurrentShield, TestValues.Tolerance, "Verifies that CurrentShield always returns the same value as Current.");
    }
    #endregion
}
