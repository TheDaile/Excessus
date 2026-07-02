using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerMovementSetupTests
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

    #region Required Components
    [Test]
    public void AddComponent_WhenPlayerMovementIsAdded_ShouldAddCharacterController()
    {
        GameObject playerObject = CreateGameObject("Movement Player");

        playerObject.AddComponent<PlayerMovement>();

        Assert.IsNotNull(playerObject.GetComponent<CharacterController>());
    }

    [Test]
    public void AddComponent_WhenPlayerMovementIsAdded_ShouldAddPlayerStats()
    {
        GameObject playerObject = CreateGameObject("Movement Player");

        playerObject.AddComponent<PlayerMovement>();

        Assert.IsNotNull(playerObject.GetComponent<PlayerStats>());
    }
    #endregion

    #region Movement
    [Test]
    public void ProcessMove_WhenForwardInputIsProvided_ShouldMovePlayerForward()
    {
        PlayerMovement movement = CreateMovementPlayer(Vector3.zero);
        Vector3 startPosition = movement.transform.position;

        movement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);

        Assert.Greater(movement.transform.position.z, startPosition.z, "Verifies that forward input moves the player forward in PlayMode.");
    }

    [Test]
    public void SetSprinting_WhenEnabled_ShouldMoveFartherThanWalking()
    {
        PlayerMovement walkingMovement = CreateMovementPlayer(Vector3.left * TestValues.MovementSpawnOffset);
        PlayerMovement sprintingMovement = CreateMovementPlayer(Vector3.right * TestValues.MovementSpawnOffset);

        Vector3 walkingStartPosition = walkingMovement.transform.position;
        Vector3 sprintingStartPosition = sprintingMovement.transform.position;

        sprintingMovement.SetSprinting(true);

        walkingMovement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);
        sprintingMovement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);

        float walkingDistance = walkingMovement.transform.position.z - walkingStartPosition.z;
        float sprintingDistance = sprintingMovement.transform.position.z - sprintingStartPosition.z;

        Assert.Greater(sprintingDistance, walkingDistance, "Verifies that enabling sprint makes PlayerMovement cover more distance than normal movement.");
    }

    [Test]
    public void Sprint_WhenCalled_ShouldUseSprintMovement()
    {
        PlayerMovement walkingMovement = CreateMovementPlayer(Vector3.left * TestValues.MovementSpawnOffset);
        PlayerMovement sprintingMovement = CreateMovementPlayer(Vector3.right * TestValues.MovementSpawnOffset);

        Vector3 walkingStartPosition = walkingMovement.transform.position;
        Vector3 sprintingStartPosition = sprintingMovement.transform.position;

        sprintingMovement.Sprint();

        walkingMovement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);
        sprintingMovement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);

        float walkingDistance = walkingMovement.transform.position.z - walkingStartPosition.z;
        float sprintingDistance = sprintingMovement.transform.position.z - sprintingStartPosition.z;

        Assert.Greater(sprintingDistance, walkingDistance, "Verifies that Sprint uses the current sprint toggle behavior instead of the old direct speed mutation.");
    }

    [Test]
    public void Sprint_WhenCalled_ShouldNotChangeBasePlayerSpeed()
    {
        PlayerMovement movement = CreateMovementPlayer(Vector3.zero);

        float startSpeed = movement.PlayerSpeedForTests;

        movement.Sprint();

        Assert.AreEqual(startSpeed, movement.PlayerSpeedForTests, TestValues.Tolerance, "Verifies that Sprint toggles sprint state without mutating the base playerSpeed value.");
    }

    [Test]
    public void Sprint_WhenCalledTwice_ShouldReturnToWalkingMovement()
    {
        PlayerMovement walkingMovement = CreateMovementPlayer(Vector3.left * TestValues.MovementSpawnOffset);
        PlayerMovement toggledMovement = CreateMovementPlayer(Vector3.right * TestValues.MovementSpawnOffset);

        Vector3 walkingStartPosition = walkingMovement.transform.position;
        Vector3 toggledStartPosition = toggledMovement.transform.position;

        toggledMovement.Sprint();
        toggledMovement.Sprint();

        walkingMovement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);
        toggledMovement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);

        float walkingDistance = walkingMovement.transform.position.z - walkingStartPosition.z;
        float toggledDistance = toggledMovement.transform.position.z - toggledStartPosition.z;

        Assert.AreEqual(walkingDistance, toggledDistance, TestValues.Tolerance, "Verifies that calling Sprint twice returns movement to walking speed.");
    }

    [Test]
    public void SetSprinting_WhenPlayerHasStats_ShouldUseStaminaWhileMoving()
    {
        PlayerMovement movement = CreateMovementPlayerWithStats(Vector3.zero, out PlayerStats playerStats);

        float startStamina = playerStats.CurrentStamina;

        movement.SetSprinting(true);
        movement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);

        Assert.Less(playerStats.CurrentStamina, startStamina, "Verifies that sprint movement spends player stamina.");
    }

    [Test]
    public void SetSprinting_WhenPlayerHasNoStamina_ShouldUseWalkingMovement()
    {
        PlayerMovement walkingMovement = CreateMovementPlayer(Vector3.left * TestValues.MovementSpawnOffset);
        PlayerMovement sprintingMovement = CreateMovementPlayerWithStats(Vector3.right * TestValues.MovementSpawnOffset, out PlayerStats playerStats);

        Vector3 walkingStartPosition = walkingMovement.transform.position;
        Vector3 sprintingStartPosition = sprintingMovement.transform.position;

        Assert.IsTrue(playerStats.UseStamina(playerStats.CurrentStamina), "The test player should be able to spend all available stamina.");
        sprintingMovement.SetSprinting(true);

        walkingMovement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);
        sprintingMovement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);

        float walkingDistance = walkingMovement.transform.position.z - walkingStartPosition.z;
        float sprintingDistance = sprintingMovement.transform.position.z - sprintingStartPosition.z;

        Assert.AreEqual(walkingDistance, sprintingDistance, TestValues.Tolerance, "Verifies that sprinting falls back to walking speed when stamina is empty.");
    }

    [Test]
    public void SetSprinting_WhenPlayerIsNotMoving_ShouldNotUseStamina()
    {
        PlayerMovement movement = CreateMovementPlayerWithStats(Vector3.zero, out PlayerStats playerStats);

        float startStamina = playerStats.CurrentStamina;

        movement.SetSprinting(true);
        movement.ProcessMoveForTests(Vector2.zero, TestValues.OneSecond);

        Assert.AreEqual(startStamina, playerStats.CurrentStamina, TestValues.Tolerance, "Verifies that holding sprint while standing still does not spend stamina.");
    }

    [Test]
    public void SetSprinting_WhenPlayerIsCrouching_ShouldUseWalkingMovementAndNotUseStamina()
    {
        PlayerMovement walkingMovement = CreateMovementPlayer(Vector3.left * TestValues.MovementSpawnOffset);
        PlayerMovement crouchingMovement = CreateMovementPlayerWithStats(Vector3.right * TestValues.MovementSpawnOffset, out PlayerStats playerStats);

        Vector3 walkingStartPosition = walkingMovement.transform.position;
        Vector3 crouchingStartPosition = crouchingMovement.transform.position;
        float startStamina = playerStats.CurrentStamina;

        crouchingMovement.SetCrouching(true);
        crouchingMovement.SetSprinting(true);

        walkingMovement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);
        crouchingMovement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);

        float walkingDistance = walkingMovement.transform.position.z - walkingStartPosition.z;
        float crouchingDistance = crouchingMovement.transform.position.z - crouchingStartPosition.z;

        Assert.AreEqual(walkingDistance, crouchingDistance, TestValues.Tolerance, "Verifies that crouching blocks sprint speed.");
        Assert.AreEqual(startStamina, playerStats.CurrentStamina, TestValues.Tolerance, "Verifies that crouching blocks sprint stamina use.");
    }

    [Test]
    public void SetCrouching_WhenPlayerIsSprinting_ShouldReturnToWalkingMovement()
    {
        PlayerMovement walkingMovement = CreateMovementPlayer(Vector3.left * TestValues.MovementSpawnOffset);
        PlayerMovement sprintingMovement = CreateMovementPlayer(Vector3.right * TestValues.MovementSpawnOffset);

        Vector3 walkingStartPosition = walkingMovement.transform.position;
        Vector3 sprintingStartPosition = sprintingMovement.transform.position;

        sprintingMovement.SetSprinting(true);
        sprintingMovement.SetCrouching(true);

        walkingMovement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);
        sprintingMovement.ProcessMoveForTests(Vector2.up, TestValues.OneSecond);

        float walkingDistance = walkingMovement.transform.position.z - walkingStartPosition.z;
        float sprintingDistance = sprintingMovement.transform.position.z - sprintingStartPosition.z;

        Assert.AreEqual(walkingDistance, sprintingDistance, TestValues.Tolerance, "Verifies that crouching cancels active sprint movement.");
    }
    #endregion

    #region Crouch
    [UnityTest]
    public IEnumerator ToggleCrouch_WhenCalled_ShouldStartReducingControllerHeight()
    {
        PlayerMovement movement = CreateMovementPlayer(Vector3.zero);

        CharacterController controller = movement.GetComponent<CharacterController>();
        float startHeight = controller.height;

        movement.ToggleCrouch();

        yield return PlayModeTestWait.Until(
            () => controller.height < startHeight,
            TestValues.ConditionTimeout
        );

        Assert.Less(controller.height, startHeight, "Verifies that toggling crouch starts reducing CharacterController height.");
    }

    [UnityTest]
    public IEnumerator Crouch_WhenCalled_ShouldUseToggleCrouchBehavior()
    {
        PlayerMovement movement = CreateMovementPlayer(Vector3.zero);

        CharacterController controller = movement.GetComponent<CharacterController>();
        float startHeight = controller.height;

        movement.Crouch();

        yield return PlayModeTestWait.Until(
            () => controller.height < startHeight,
            TestValues.ConditionTimeout
        );

        Assert.Less(controller.height, startHeight, "Verifies that Crouch uses the current toggle crouch behavior.");
    }
    #endregion

    private PlayerMovement CreateMovementPlayer(Vector3 position) => CreateMovementPlayer(position, false, out _);

    private PlayerMovement CreateMovementPlayerWithStats(Vector3 position, out PlayerStats playerStats)
    {
        return CreateMovementPlayer(position, true, out playerStats);
    }

    private PlayerMovement CreateMovementPlayer(Vector3 position, bool addStats, out PlayerStats playerStats)
    {
        GameObject playerObject = CreateGameObject("Movement Player");
        playerObject.SetActive(false);
        playerObject.transform.position += position;

        playerStats = addStats ? playerObject.AddComponent<PlayerStats>() : null;
        PlayerMovement movement = playerObject.AddComponent<PlayerMovement>();

        Assert.IsTrue(movement.SetGravityForTests(TestValues.Empty), "The test gravity value should be accepted.");
        Assert.IsTrue(movement.SetPlayerSpeedForTests(TestValues.TestPlayerSpeed), "The test player speed should be accepted.");
        Assert.IsTrue(movement.SetSprintMultiplierForTests(TestValues.SprintMultiplier), "The test sprint multiplier should be accepted.");

        playerObject.SetActive(true);

        return movement;
    }

    private GameObject CreateGameObject(string name)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.position = Vector3.one * TestValues.IsolatedSceneOffset;
        createdObjects.Add(gameObject);
        return gameObject;
    }
}
