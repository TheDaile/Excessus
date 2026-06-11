using NUnit.Framework;

public class StatsOperationTests
{
    private static StatsOperations CreateOperations(
        out Health health,
        out Shield shield,
        out Stamina stamina,
        out StatsEventsRecorder eventsRecorder)
    {
        health = new Health();
        shield = new Shield();
        stamina = new Stamina();

        health.Initialize();
        shield.Initialize();
        stamina.Initialize();

        eventsRecorder = new StatsEventsRecorder();

        return new StatsOperations(health, shield, stamina, eventsRecorder);
    }

    #region Take Damage

    [Test]
    public void TakeDamage_WhenShieldHasEnoughValue_ShouldReduceShieldAndNotHealth()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float startHealth = health.Current;
        float startShield = shield.Current;
        float damage = shield.Current * TestValues.QuarterRatio;
        float expectedShield = startShield - damage;

        operations.TakeDamage(damage);

        Assert.AreEqual(startHealth, health.Current, TestValues.Tolerance, "Health should not change when shield fully absorbs damage.");
        Assert.AreEqual(expectedShield, shield.Current, TestValues.Tolerance, "Shield should be reduced by absorbed damage.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.ShieldHitCallCount, "OnShieldHit should be called when shield absorbs damage.");
        Assert.AreEqual(damage, eventsRecorder.LastShieldHitAmount, TestValues.Tolerance, "OnShieldHit should receive the absorbed damage amount.");
        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.DeathCallCount, "OnDeath should not be called while health is alive.");
    }

    [Test]
    public void TakeDamage_WhenDamageExceedsShield_ShouldReduceShieldToZeroAndDamageHealth()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float startHealth = health.Current;
        float startShield = shield.Current;
        float remainingDamage = health.Max * TestValues.QuarterRatio;
        float damage = startShield + remainingDamage;
        float expectedHealth = startHealth - remainingDamage;

        operations.TakeDamage(damage);

        Assert.AreEqual(TestValues.Empty, shield.Current, TestValues.Tolerance, "Shield should be depleted when damage exceeds Current shield.");
        Assert.AreEqual(expectedHealth, health.Current, TestValues.Tolerance, "Health should receive damage remaining after shield absorption.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.ShieldHitCallCount, "OnShieldHit should be called once when shield absorbs damage.");
        Assert.AreEqual(startShield, eventsRecorder.LastShieldHitAmount, TestValues.Tolerance, "OnShieldHit should receive only the absorbed shield damage.");
        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.DeathCallCount, "OnDeath should not be called while health remains alive.");
    }

    [Test]
    public void TakeDamage_WhenHealthReachesZero_ShouldCallOnDeath()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float damage = shield.Current + health.Current;

        operations.TakeDamage(damage);

        Assert.AreEqual(TestValues.Empty, health.Current, TestValues.Tolerance, "Health should reach 0 when damage after shield absorption is lethal.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.DeathCallCount, "OnDeath should be called once when health reaches 0.");
    }

    [Test]
    public void TakeDamage_WhenHealthIsAlreadyDead_ShouldNotChangeStateOrCallEventsAgain()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        operations.TakeDamage(shield.Current + health.Current);

        int deathCallsAfterDeath = eventsRecorder.DeathCallCount;
        int shieldHitCallsAfterDeath = eventsRecorder.ShieldHitCallCount;

        operations.TakeDamage(health.Max * TestValues.QuarterRatio);

        Assert.AreEqual(TestValues.Empty, health.Current, TestValues.Tolerance, "Dead health should remain at 0.");
        Assert.AreEqual(deathCallsAfterDeath, eventsRecorder.DeathCallCount, "OnDeath should not be called again when damage is applied to dead health.");
        Assert.AreEqual(shieldHitCallsAfterDeath, eventsRecorder.ShieldHitCallCount, "Shield should not absorb damage after health is already dead.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void TakeDamage_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float damage)
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => operations.TakeDamage(damage),
            "TakeDamage should reject zero or negative damage values."
        );
    }

    #endregion

    #region Healing

    [Test]
    public void Heal_WhenHealthActuallyIncreases_ShouldCallOnHealedWithRealAmount()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        float healAmount = health.Max * TestValues.QuarterRatio;

        operations.Heal(healAmount);

        Assert.AreEqual(TestValues.OneCall, eventsRecorder.HealedCallCount, "OnHealed should be called when Health actually increases.");
        Assert.AreEqual(healAmount, eventsRecorder.LastHealedAmount, TestValues.Tolerance, "OnHealed should receive the real healed amount.");
    }

    [Test]
    public void Heal_WhenHealthIsFull_ShouldNotCallOnHealed()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        operations.Heal(health.Max * TestValues.QuarterRatio);

        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.HealedCallCount, "OnHealed should not be called when Health is already full.");
    }

    [Test]
    public void Heal_WhenAmountExceedsMissingHealth_ShouldCallOnHealedWithClampedAmount()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float damage = health.Max * TestValues.QuarterRatio;
        health.TakeDamage(damage);

        operations.Heal(health.Max);

        Assert.AreEqual(TestValues.OneCall, eventsRecorder.HealedCallCount, "OnHealed should be called when Health increases.");
        Assert.AreEqual(damage, eventsRecorder.LastHealedAmount, TestValues.Tolerance, "OnHealed should receive only the actually restored amount.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void Heal_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float amount)
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => operations.Heal(amount),
            "Heal should reject zero or negative heal values."
        );
    }

    #endregion

    #region Shield

    [Test]
    public void RechargeShield_WhenShieldActuallyIncreases_ShouldCallOnShieldRechargedWithRealAmount()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        float rechargeAmount = shield.Max * TestValues.QuarterRatio;

        operations.RechargeShield(rechargeAmount);

        Assert.AreEqual(TestValues.OneCall, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should be called when Shield actually increases.");
        Assert.AreEqual(rechargeAmount, eventsRecorder.LastShieldRechargedAmount, TestValues.Tolerance, "OnShieldRecharged should receive the real recharged amount.");
    }

    [Test]
    public void RechargeShield_WhenShieldIsFull_ShouldNotCallOnShieldRecharged()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        operations.RechargeShield(shield.Max * TestValues.QuarterRatio);

        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should not be called when Shield is already full.");
    }

    [Test]
    public void RechargeShield_WhenAmountExceedsMissingShield_ShouldCallOnShieldRechargedWithClampedAmount()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float absorbedDamage = shield.Max * TestValues.QuarterRatio;
        shield.AbsorbDamage(absorbedDamage);

        operations.RechargeShield(shield.Max);

        Assert.AreEqual(TestValues.OneCall, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should be called when Shield increases.");
        Assert.AreEqual(absorbedDamage, eventsRecorder.LastShieldRechargedAmount, TestValues.Tolerance, "OnShieldRecharged should receive only the actually restored shield amount.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void RechargeShield_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float amount)
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => operations.RechargeShield(amount),
            "RechargeShield should reject zero or negative recharge values."
        );
    }

    #endregion

    #region Stamina

    [Test]
    public void UseStamina_WhenEnoughStamina_ShouldReturnTrueAndCallOnStaminaUsed()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float amount = stamina.Max * TestValues.QuarterRatio;

        bool result = operations.UseStamina(amount, TestValues.Empty);

        Assert.IsTrue(result, "UseStamina should return true when enough stamina is available.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.StaminaUsedCallCount, "OnStaminaUsed should be called after successful stamina use.");
        Assert.AreEqual(amount, eventsRecorder.LastStaminaUsedAmount, TestValues.Tolerance, "OnStaminaUsed should receive the used stamina amount.");
    }

    [Test]
    public void UseStamina_WhenNotEnoughStamina_ShouldReturnFalseAndNotCallOnStaminaUsed()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        stamina.Use(stamina.Max * TestValues.HalfRatio, TestValues.Empty);

        float amount = stamina.Max;

        bool result = operations.UseStamina(amount, TestValues.Empty);

        Assert.IsFalse(result, "UseStamina should return false when not enough stamina is available.");
        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.StaminaUsedCallCount, "OnStaminaUsed should not be called when stamina use fails.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void UseStamina_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float amount)
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => operations.UseStamina(amount, TestValues.Empty),
            "UseStamina should reject zero or negative stamina values."
        );
    }

    #endregion

    #region Over Time

    [Test]
    public void Tick_WhenStaminaCanRegenerate_ShouldRegenerateStamina()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float regenerationPerSecond = stamina.Max * TestValues.QuarterRatio;
        bool setRegenerationPerSecondResult = stamina.SetRegenerationPerSecondForTests(regenerationPerSecond);
        bool setRegenerationDelayResult = stamina.SetRegenerationDelayForTests(TestValues.Empty);
        bool setCurrentResult = stamina.SetCurrentForTests(stamina.Max * TestValues.HalfRatio);

        Assert.IsTrue(setRegenerationPerSecondResult, "SetRegenerationPerSecondForTests should accept the setup regeneration rate.");
        Assert.IsTrue(setRegenerationDelayResult, "SetRegenerationDelayForTests should accept the setup regeneration delay.");
        Assert.IsTrue(setCurrentResult, "SetCurrentForTests should accept the setup Current stamina value.");

        float startStamina = stamina.Current;
        float expectedStamina = startStamina + regenerationPerSecond * TestValues.OneSecond;

        operations.Tick(TestValues.OneSecond, TestValues.Empty);

        Assert.AreEqual(expectedStamina, stamina.Current, TestValues.Tolerance, "Tick should forward delta time and current time to stamina regeneration.");
    }

    [Test]
    public void Tick_WhenDeltaTimeIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => operations.Tick(TestValues.InvalidNegativeAmount, TestValues.Empty),
            "StatsOperations.Tick should reject negative delta time values."
        );
    }

    [Test]
    public void HealOverTime_WhenDurationIsZero_ShouldHealImmediatelyAndCallOnHealed()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        float startHealth = health.Current;
        float healAmount = health.Max * TestValues.QuarterRatio;
        float expectedHealth = startHealth + healAmount;

        operations.HealOverTime(healAmount, TestValues.Empty);

        Assert.AreEqual(expectedHealth, health.Current, TestValues.Tolerance, "HealOverTime should heal immediately when duration is 0.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.HealedCallCount, "OnHealed should be called for zero-duration HealOverTime.");
        Assert.AreEqual(healAmount, eventsRecorder.LastHealedAmount, TestValues.Tolerance, "OnHealed should receive the immediate HealOverTime amount.");
    }

    [Test]
    public void HealOverTime_WhenDurationIsPositive_ShouldHealThroughTick()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        health.TakeDamage(health.Max * TestValues.HalfRatio);

        float startHealth = health.Current;
        float healAmount = health.Max * TestValues.QuarterRatio;
        float expectedHealStep = healAmount * TestValues.HalfRatio;
        float expectedHealthAfterFirstTick = startHealth + expectedHealStep;
        float expectedHealthAfterCompletion = startHealth + healAmount;

        operations.HealOverTime(healAmount, TestValues.OneSecond);

        Assert.AreEqual(startHealth, health.Current, TestValues.Tolerance, "HealOverTime should not apply positive-duration healing immediately.");
        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.HealedCallCount, "OnHealed should not be called before HealOverTime ticks.");

        operations.Tick(TestValues.HalfSecond, TestValues.Empty);

        Assert.AreEqual(expectedHealthAfterFirstTick, health.Current, TestValues.Tolerance, "HealOverTime should heal proportionally during Tick.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.HealedCallCount, "OnHealed should be called after the first positive HealOverTime step.");
        Assert.AreEqual(expectedHealStep, eventsRecorder.LastHealedAmount, TestValues.Tolerance, "OnHealed should receive the first HealOverTime step amount.");

        operations.Tick(TestValues.HalfSecond, TestValues.Empty);

        Assert.AreEqual(expectedHealthAfterCompletion, health.Current, TestValues.Tolerance, "HealOverTime should reach the full target amount after the duration completes.");
        Assert.AreEqual(TestValues.TwoCalls, eventsRecorder.HealedCallCount, "OnHealed should be called once per positive HealOverTime step.");
    }

    [Test]
    public void HealOverTime_WhenDurationIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => operations.HealOverTime(health.Max * TestValues.QuarterRatio, TestValues.InvalidNegativeAmount),
            "HealOverTime should reject negative durations."
        );
    }

    [Test]
    public void RechargeShieldOverTime_WhenDurationIsZero_ShouldRechargeImmediatelyAndCallOnShieldRecharged()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        float startShield = shield.Current;
        float rechargeAmount = shield.Max * TestValues.QuarterRatio;
        float expectedShield = startShield + rechargeAmount;

        operations.RechargeShieldOverTime(rechargeAmount, TestValues.Empty);

        Assert.AreEqual(expectedShield, shield.Current, TestValues.Tolerance, "RechargeShieldOverTime should recharge immediately when duration is 0.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should be called for zero-duration RechargeShieldOverTime.");
        Assert.AreEqual(rechargeAmount, eventsRecorder.LastShieldRechargedAmount, TestValues.Tolerance, "OnShieldRecharged should receive the immediate RechargeShieldOverTime amount.");
    }

    [Test]
    public void RechargeShieldOverTime_WhenDurationIsPositive_ShouldRechargeThroughTick()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        shield.AbsorbDamage(shield.Max * TestValues.HalfRatio);

        float startShield = shield.Current;
        float rechargeAmount = shield.Max * TestValues.QuarterRatio;
        float expectedRechargeStep = rechargeAmount * TestValues.HalfRatio;
        float expectedShieldAfterFirstTick = startShield + expectedRechargeStep;
        float expectedShieldAfterCompletion = startShield + rechargeAmount;

        operations.RechargeShieldOverTime(rechargeAmount, TestValues.OneSecond);

        Assert.AreEqual(startShield, shield.Current, TestValues.Tolerance, "RechargeShieldOverTime should not apply positive-duration recharge immediately.");
        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should not be called before RechargeShieldOverTime ticks.");

        operations.Tick(TestValues.HalfSecond, TestValues.Empty);

        Assert.AreEqual(expectedShieldAfterFirstTick, shield.Current, TestValues.Tolerance, "RechargeShieldOverTime should recharge proportionally during Tick.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should be called after the first positive RechargeShieldOverTime step.");
        Assert.AreEqual(expectedRechargeStep, eventsRecorder.LastShieldRechargedAmount, TestValues.Tolerance, "OnShieldRecharged should receive the first RechargeShieldOverTime step amount.");

        operations.Tick(TestValues.HalfSecond, TestValues.Empty);

        Assert.AreEqual(expectedShieldAfterCompletion, shield.Current, TestValues.Tolerance, "RechargeShieldOverTime should reach the full target amount after the duration completes.");
        Assert.AreEqual(TestValues.TwoCalls, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should be called once per positive RechargeShieldOverTime step.");
    }

    [Test]
    public void RechargeShieldOverTime_WhenDurationIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => operations.RechargeShieldOverTime(shield.Max * TestValues.QuarterRatio, TestValues.InvalidNegativeAmount),
            "RechargeShieldOverTime should reject negative durations."
        );
    }

    [Test]
    public void IncreaseMaxHealthOverTime_WhenDurationIsZero_ShouldIncreaseMaxAndHealImmediately()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float startMaxHealth = health.Max;
        float startHealth = health.Current;
        float increaseAmount = health.Max * TestValues.QuarterRatio;
        float expectedMaxHealth = startMaxHealth + increaseAmount;
        float expectedHealth = startHealth + increaseAmount;

        operations.IncreaseMaxHealthOverTime(increaseAmount, TestValues.Empty);

        Assert.AreEqual(expectedMaxHealth, health.Max, TestValues.Tolerance, "IncreaseMaxHealthOverTime should increase Max health immediately.");
        Assert.AreEqual(expectedHealth, health.Current, TestValues.Tolerance, "IncreaseMaxHealthOverTime should heal immediately when duration is 0.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.MaxHealthChangedCallCount, "OnMaxHealthChanged should be called when Max health increases over time.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.HealedCallCount, "OnHealed should be called for zero-duration IncreaseMaxHealthOverTime.");
    }

    [Test]
    public void IncreaseMaxHealthOverTime_WhenDurationIsPositive_ShouldIncreaseMaxImmediatelyAndHealThroughTick()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float startMaxHealth = health.Max;
        float startHealth = health.Current;
        float increaseAmount = health.Max * TestValues.QuarterRatio;
        float expectedMaxHealth = startMaxHealth + increaseAmount;
        float expectedHealStep = increaseAmount * TestValues.HalfRatio;
        float expectedHealthAfterFirstTick = startHealth + expectedHealStep;
        float expectedHealthAfterCompletion = startHealth + increaseAmount;

        operations.IncreaseMaxHealthOverTime(increaseAmount, TestValues.OneSecond);

        Assert.AreEqual(expectedMaxHealth, health.Max, TestValues.Tolerance, "IncreaseMaxHealthOverTime should increase Max health immediately.");
        Assert.AreEqual(startHealth, health.Current, TestValues.Tolerance, "IncreaseMaxHealthOverTime should not apply positive-duration healing immediately.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.MaxHealthChangedCallCount, "OnMaxHealthChanged should be called once when over-time Max health increases.");
        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.HealedCallCount, "OnHealed should not be called before IncreaseMaxHealthOverTime ticks.");

        operations.Tick(TestValues.HalfSecond, TestValues.Empty);

        Assert.AreEqual(expectedHealthAfterFirstTick, health.Current, TestValues.Tolerance, "IncreaseMaxHealthOverTime should heal proportionally during Tick.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.HealedCallCount, "OnHealed should be called after the first IncreaseMaxHealthOverTime healing step.");
        Assert.AreEqual(expectedHealStep, eventsRecorder.LastHealedAmount, TestValues.Tolerance, "OnHealed should receive the first IncreaseMaxHealthOverTime healing step amount.");

        operations.Tick(TestValues.HalfSecond, TestValues.Empty);

        Assert.AreEqual(expectedHealthAfterCompletion, health.Current, TestValues.Tolerance, "IncreaseMaxHealthOverTime should finish healing Current health after the duration completes.");
        Assert.AreEqual(TestValues.TwoCalls, eventsRecorder.HealedCallCount, "OnHealed should be called once per positive IncreaseMaxHealthOverTime healing step.");
    }

    [Test]
    public void IncreaseMaxHealthOverTime_WhenDurationIsNegative_ShouldThrowArgumentOutOfRangeException()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => operations.IncreaseMaxHealthOverTime(health.Max * TestValues.QuarterRatio, TestValues.InvalidNegativeAmount),
            "IncreaseMaxHealthOverTime should reject negative durations."
        );
    }

    #endregion

    #region Upgrades

    [Test]
    public void IncreaseMaxHealthBy_WhenIncreaseCurrentByAmountIsTrue_ShouldCallMaxHealthChangedAndHealed()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float startMaxHealth = health.Max;
        float amount = health.Max * TestValues.QuarterRatio;
        float expectedMaxHealth = startMaxHealth + amount;

        operations.IncreaseMaxHealthBy(amount, true);

        Assert.AreEqual(TestValues.OneCall, eventsRecorder.MaxHealthChangedCallCount, "OnMaxHealthChanged should be called when Max health increases.");
        Assert.AreEqual(startMaxHealth, eventsRecorder.LastPreviousMaxHealth, TestValues.Tolerance, "OnMaxHealthChanged should receive the Max health value from before the increase.");
        Assert.AreEqual(expectedMaxHealth, eventsRecorder.LastCurrentMaxHealth, TestValues.Tolerance, "OnMaxHealthChanged should receive the Max health value from after the increase.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.HealedCallCount, "OnHealed should be called when Current health also increases.");
        Assert.AreEqual(amount, eventsRecorder.LastHealedAmount, TestValues.Tolerance, "OnHealed should receive the Current health increase amount.");
    }

    [Test]
    public void IncreaseMaxHealthBy_WhenIncreaseCurrentByAmountIsFalse_ShouldCallMaxHealthChangedAndNotHealed()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float startMaxHealth = health.Max;
        float amount = health.Max * TestValues.QuarterRatio;
        float expectedMaxHealth = startMaxHealth + amount;

        operations.IncreaseMaxHealthBy(amount, false);

        Assert.AreEqual(TestValues.OneCall, eventsRecorder.MaxHealthChangedCallCount, "OnMaxHealthChanged should be called when Max health increases.");
        Assert.AreEqual(startMaxHealth, eventsRecorder.LastPreviousMaxHealth, TestValues.Tolerance, "OnMaxHealthChanged should receive the Max health value from before the increase.");
        Assert.AreEqual(expectedMaxHealth, eventsRecorder.LastCurrentMaxHealth, TestValues.Tolerance, "OnMaxHealthChanged should receive the Max health value from after the increase.");
        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.HealedCallCount, "OnHealed should not be called when Current health does not increase.");
    }

    [Test]
    public void IncreaseMaxShieldBy_WhenIncreaseCurrentByAmountIsTrue_ShouldCallMaxShieldChangedAndShieldRecharged()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float startMaxShield = shield.Max;
        float amount = shield.Max * TestValues.QuarterRatio;
        float expectedMaxShield = startMaxShield + amount;

        operations.IncreaseMaxShieldBy(amount, true);

        Assert.AreEqual(TestValues.OneCall, eventsRecorder.MaxShieldChangedCallCount, "OnMaxShieldChanged should be called when Max shield increases.");
        Assert.AreEqual(startMaxShield, eventsRecorder.LastPreviousMaxShield, TestValues.Tolerance, "OnMaxShieldChanged should receive the Max shield value from before the increase.");
        Assert.AreEqual(expectedMaxShield, eventsRecorder.LastCurrentMaxShield, TestValues.Tolerance, "OnMaxShieldChanged should receive the Max shield value from after the increase.");
        Assert.AreEqual(TestValues.OneCall, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should be called when Current shield also increases.");
        Assert.AreEqual(amount, eventsRecorder.LastShieldRechargedAmount, TestValues.Tolerance, "OnShieldRecharged should receive the Current shield increase amount.");
    }

    [Test]
    public void IncreaseMaxShieldBy_WhenIncreaseCurrentByAmountIsFalse_ShouldCallMaxShieldChangedAndNotShieldRecharged()
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        float startMaxShield = shield.Max;
        float amount = shield.Max * TestValues.QuarterRatio;
        float expectedMaxShield = startMaxShield + amount;

        operations.IncreaseMaxShieldBy(amount, false);

        Assert.AreEqual(TestValues.OneCall, eventsRecorder.MaxShieldChangedCallCount, "OnMaxShieldChanged should be called when Max shield increases.");
        Assert.AreEqual(startMaxShield, eventsRecorder.LastPreviousMaxShield, TestValues.Tolerance, "OnMaxShieldChanged should receive the Max shield value from before the increase.");
        Assert.AreEqual(expectedMaxShield, eventsRecorder.LastCurrentMaxShield, TestValues.Tolerance, "OnMaxShieldChanged should receive the Max shield value from after the increase.");
        Assert.AreEqual(TestValues.NoCalls, eventsRecorder.ShieldRechargedCallCount, "OnShieldRecharged should not be called when Current shield does not increase.");
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void IncreaseMaxHealthBy_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float amount)
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => operations.IncreaseMaxHealthBy(amount),
            "IncreaseMaxHealthBy should reject zero or negative increase values."
        );
    }

    [TestCase(TestValues.InvalidZeroAmount)]
    [TestCase(TestValues.InvalidNegativeAmount)]
    public void IncreaseMaxShieldBy_WhenAmountIsNotPositive_ShouldThrowArgumentOutOfRangeException(float amount)
    {
        StatsOperations operations = CreateOperations(
            out Health health,
            out Shield shield,
            out Stamina stamina,
            out StatsEventsRecorder eventsRecorder
        );

        Assert.Throws<System.ArgumentOutOfRangeException>(
            () => operations.IncreaseMaxShieldBy(amount),
            "IncreaseMaxShieldBy should reject zero or negative increase values."
        );
    }

    #endregion
}
