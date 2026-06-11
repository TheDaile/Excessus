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

    private PlayerMovement CreateMovementPlayer(Vector3 position)
    {
        GameObject playerObject = CreateGameObject("Movement Player");
        playerObject.SetActive(false);
        playerObject.transform.position += position;

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
