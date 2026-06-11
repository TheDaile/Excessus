using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerStatsTests
{
    private readonly List<GameObject> createdObjects = new List<GameObject>();

    [SetUp]
    public void SetUp()
    {
        PlayModeTestScene.PrepareIsolatedTestScene();
    }

    [UnityTearDown]
    public IEnumerator TearDown()
    {
        foreach (GameObject createdObject in createdObjects)
        {
            if (createdObject != null)
            {
                Object.Destroy(createdObject);
            }
        }

        createdObjects.Clear();
        yield return null;
        PlayModeTestScene.RestoreSceneState();
    }

    #region Initialization
    [Test]
    public void Awake_WhenPlayerStatsStarts_ShouldInitializeAllStats()
    {
        PlayerStats playerStats = CreatePlayerStats();

        Assert.AreEqual(playerStats.MaxHealth, playerStats.CurrentHealth, TestValues.Tolerance, "Verifies that PlayerStats initializes CurrentHealth to MaxHealth in PlayMode.");
        Assert.AreEqual(playerStats.MaxShield, playerStats.CurrentShield, TestValues.Tolerance, "Verifies that PlayerStats initializes CurrentShield to MaxShield in PlayMode.");
        Assert.AreEqual(playerStats.MaxStamina, playerStats.CurrentStamina, TestValues.Tolerance, "Verifies that PlayerStats initializes CurrentStamina to MaxStamina in PlayMode.");
    }
    #endregion

    #region Update Integration
    [UnityTest]
    public IEnumerator HealOverTime_WhenHealthIsDamaged_ShouldRestoreCurrentHealthThroughUpdate()
    {
        PlayerStats playerStats = CreatePlayerStats();

        float healthDamage = playerStats.MaxHealth * TestValues.HalfRatio;
        playerStats.TakeDamage(playerStats.MaxShield + healthDamage);

        float damagedHealth = playerStats.CurrentHealth;
        float healAmount = playerStats.MaxHealth * TestValues.QuarterRatio;
        float duration = TestValues.ShortDuration;
        float expectedHealth = damagedHealth + healAmount;

        playerStats.HealOverTime(healAmount, duration);

        Assert.AreEqual(damagedHealth, playerStats.CurrentHealth, TestValues.Tolerance, "Verifies that HealOverTime does not apply the whole heal instantly.");

        yield return PlayModeTestWait.Until(
            () => Mathf.Abs(playerStats.CurrentHealth - expectedHealth) <= TestValues.Tolerance,
            TestValues.ConditionTimeout
        );

        Assert.AreEqual(expectedHealth, playerStats.CurrentHealth, TestValues.Tolerance, "Verifies that PlayerStats HealOverTime restores CurrentHealth through Update.");
    }

    [UnityTest]
    public IEnumerator RechargeShieldOverTime_WhenShieldIsDamaged_ShouldRestoreCurrentShieldThroughUpdate()
    {
        PlayerStats playerStats = CreatePlayerStats();

        float shieldDamage = playerStats.MaxShield * TestValues.HalfRatio;
        playerStats.TakeDamage(shieldDamage);

        float damagedShield = playerStats.CurrentShield;
        float rechargeAmount = playerStats.MaxShield * TestValues.QuarterRatio;
        float duration = TestValues.ShortDuration;
        float expectedShield = damagedShield + rechargeAmount;

        playerStats.RechargeShieldOverTime(rechargeAmount, duration);

        Assert.AreEqual(damagedShield, playerStats.CurrentShield, TestValues.Tolerance, "Verifies that RechargeShieldOverTime does not apply the whole recharge instantly.");

        yield return PlayModeTestWait.Until(
            () => Mathf.Abs(playerStats.CurrentShield - expectedShield) <= TestValues.Tolerance,
            TestValues.ConditionTimeout
        );

        Assert.AreEqual(expectedShield, playerStats.CurrentShield, TestValues.Tolerance, "Verifies that PlayerStats RechargeShieldOverTime restores CurrentShield through Update.");
    }
    #endregion

    private PlayerStats CreatePlayerStats()
    {
        GameObject playerObject = new GameObject("Player Stats");
        createdObjects.Add(playerObject);

        return playerObject.AddComponent<PlayerStats>();
    }
}
