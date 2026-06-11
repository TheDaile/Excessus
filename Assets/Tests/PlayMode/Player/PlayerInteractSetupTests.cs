using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.TestTools;

public class PlayerInteractSetupTests
{
    private const string PromptMessage = "Open";
    private const string ExpectedPrompt = "Press E to " + PromptMessage;
    private const string PreviousPrompt = "Previous prompt";

    private readonly List<GameObject> createdObjects = new List<GameObject>();
    private Camera playerCamera;

    [UnitySetUp]
    public IEnumerator SetUp()
    {
        PlayModeTestScene.PrepareIsolatedTestScene();
        yield return null;
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
        playerCamera = null;
        yield return null;
        PlayModeTestScene.RestoreSceneState();
    }

    #region Raycast Detection
    [UnityTest]
    public IEnumerator CheckForInteractable_WhenRayHitsInteractable_ShouldShowPrompt()
    {
        PlayerInteract playerInteract = CreatePlayerInteract(out TextMeshProUGUI promptText);
        CreateInteractable(GetPositionInFrontOfCamera(), PromptMessage);

        Physics.SyncTransforms();
        yield return null;

        playerInteract.CheckForInteractable();

        Assert.AreEqual(ExpectedPrompt, promptText.text, "Verifies that PlayerInteract shows the interaction prompt when the raycast hits an Interactable.");
    }

    [UnityTest]
    public IEnumerator CheckForInteractable_WhenRayDoesNotHitInteractable_ShouldClearPrompt()
    {
        PlayerInteract playerInteract = CreatePlayerInteract(out TextMeshProUGUI promptText);

        Physics.SyncTransforms();
        yield return null;

        promptText.text = PreviousPrompt;
        playerInteract.CheckForInteractable();

        Assert.AreEqual(string.Empty, promptText.text, "Verifies that PlayerInteract clears the prompt when the raycast does not hit an Interactable.");
    }

    [UnityTest]
    public IEnumerator CheckForInteractable_WhenRayHitsObjectWithoutInteractable_ShouldClearPrompt()
    {
        PlayerInteract playerInteract = CreatePlayerInteract(out TextMeshProUGUI promptText);
        CreatePlainCollider(GetPositionInFrontOfCamera());

        Physics.SyncTransforms();
        yield return null;

        promptText.text = PreviousPrompt;
        playerInteract.CheckForInteractable();

        Assert.AreEqual(string.Empty, promptText.text, "Verifies that PlayerInteract does not show a prompt when the raycast hits a collider without Interactable.");
    }

    [UnityTest]
    public IEnumerator CheckForInteractable_WhenPreviouslyDetectedTargetIsLost_ShouldClearCurrentInteractable()
    {
        PlayerInteract playerInteract = CreatePlayerInteract(out TextMeshProUGUI promptText);
        InteractableRecorder interactable = CreateInteractable(GetPositionInFrontOfCamera(), PromptMessage);

        Physics.SyncTransforms();
        yield return null;

        playerInteract.CheckForInteractable();

        Assert.AreEqual(ExpectedPrompt, promptText.text, "The setup should detect the Interactable before verifying target loss.");

        interactable.transform.position = GetPositionNextToCamera();

        Physics.SyncTransforms();
        yield return null;

        playerInteract.CheckForInteractable();
        playerInteract.Interact();

        Assert.AreEqual(string.Empty, promptText.text, "Verifies that PlayerInteract clears the prompt after the previously detected target is lost.");
        Assert.AreEqual(TestValues.NoCalls, interactable.InteractCallCount, "Verifies that PlayerInteract clears the current target when the raycast no longer hits it.");
    }
    #endregion

    #region Interaction
    [UnityTest]
    public IEnumerator Interact_WhenCurrentInteractableExists_ShouldCallInteractable()
    {
        PlayerInteract playerInteract = CreatePlayerInteract(out _);
        InteractableRecorder interactable = CreateInteractable(GetPositionInFrontOfCamera(), PromptMessage);

        Physics.SyncTransforms();
        yield return null;

        playerInteract.CheckForInteractable();
        playerInteract.Interact();

        Assert.AreEqual(TestValues.OneCall, interactable.InteractCallCount, "Verifies that Interact calls the currently detected Interactable exactly once.");
    }

    [UnityTest]
    public IEnumerator Interact_WhenCurrentInteractableIsMissing_ShouldNotThrow()
    {
        PlayerInteract playerInteract = CreatePlayerInteract(out TextMeshProUGUI promptText);

        Physics.SyncTransforms();
        yield return null;

        Assert.DoesNotThrow(() => playerInteract.Interact(), "Interact should safely do nothing when no Interactable is currently detected.");
        Assert.AreEqual(string.Empty, promptText.text, "Verifies that interacting without a target does not create HUD prompt text.");
    }
    #endregion

    private PlayerInteract CreatePlayerInteract(out TextMeshProUGUI promptText)
    {
        GameObject playerObject = CreateInactiveGameObject("Player Interact Test Rig");
        playerObject.transform.position = Vector3.one * TestValues.IsolatedSceneOffset;

        PlayerLook playerLook = playerObject.AddComponent<PlayerLook>();
        PlayerInteract playerInteract = playerObject.AddComponent<PlayerInteract>();

        playerCamera = CreateCamera(playerObject.transform);
        PlayerHUD playerHUD = CreateHud(playerObject.transform, out promptText);

        playerLook.playerCamera = playerCamera;

        SetOnlyPrivateFieldOfType(playerInteract, playerHUD);
        SetOnlyPrivateFieldOfType(playerInteract, (LayerMask)TestValues.InteractionTestLayerMask);

        playerObject.SetActive(true);

        return playerInteract;
    }

    private GameObject CreateInactiveGameObject(string name)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.SetActive(false);
        createdObjects.Add(gameObject);

        return gameObject;
    }

    private static Camera CreateCamera(Transform parent)
    {
        GameObject cameraObject = new GameObject("Player Camera");
        cameraObject.SetActive(false);
        cameraObject.transform.SetParent(parent);
        cameraObject.transform.localPosition = Vector3.zero;
        cameraObject.transform.localRotation = Quaternion.identity;

        Camera camera = cameraObject.AddComponent<Camera>();

        cameraObject.SetActive(true);

        return camera;
    }

    private static PlayerHUD CreateHud(Transform parent, out TextMeshProUGUI promptText)
    {
        GameObject canvasObject = new GameObject("HUD Canvas");
        canvasObject.SetActive(false);
        canvasObject.transform.SetParent(parent);

        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;

        PlayerHUD playerHUD = canvasObject.AddComponent<PlayerHUD>();

        GameObject promptObject = new GameObject("Prompt Text");
        promptObject.SetActive(false);
        promptObject.transform.SetParent(canvasObject.transform);

        promptText = promptObject.AddComponent<TextMeshProUGUI>();

        SetOnlyPrivateFieldOfType(playerHUD, promptText);

        promptObject.SetActive(true);
        canvasObject.SetActive(true);

        return playerHUD;
    }

    private Vector3 GetPositionInFrontOfCamera()
    {
        Assert.IsNotNull(playerCamera, "Test rig should contain a player camera.");

        return playerCamera.transform.position + playerCamera.transform.forward * TestValues.InteractionDistance;
    }

    private Vector3 GetPositionNextToCamera()
    {
        Assert.IsNotNull(playerCamera, "Test rig should contain a player camera.");

        return playerCamera.transform.position + playerCamera.transform.right * TestValues.InteractionDistance;
    }

    private GameObject CreatePlainCollider(Vector3 position)
    {
        GameObject colliderObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        colliderObject.name = "Plain Collider";
        colliderObject.layer = TestValues.InteractionTestLayer;
        colliderObject.transform.position = position;
        createdObjects.Add(colliderObject);

        return colliderObject;
    }

    private InteractableRecorder CreateInteractable(Vector3 position, string promptMessage)
    {
        GameObject interactableObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        interactableObject.name = "Interactable";
        interactableObject.layer = TestValues.InteractionTestLayer;
        interactableObject.transform.position = position;
        createdObjects.Add(interactableObject);

        InteractableRecorder interactable = interactableObject.AddComponent<InteractableRecorder>();
        interactable.promptMessage = promptMessage;

        return interactable;
    }

    private static void SetOnlyPrivateFieldOfType<TTarget, TValue>(TTarget target, TValue value)
    {
        FieldInfo[] matchingFields = System.Array.FindAll(
            typeof(TTarget).GetFields(BindingFlags.Instance | BindingFlags.NonPublic),
            field => field.FieldType == typeof(TValue)
        );

        Assert.That(
            matchingFields,
            Has.Length.EqualTo(1),
            $"{typeof(TTarget).Name} should contain exactly one private field of type {typeof(TValue).Name}."
        );

        Assert.DoesNotThrow(
            () => matchingFields[0].SetValue(target, value),
            $"The private {typeof(TValue).Name} field on {typeof(TTarget).Name} should accept the test value."
        );
    }

    private class InteractableRecorder : Interactable
    {
        public int InteractCallCount { get; private set; }

        protected override void Interact()
        {
            InteractCallCount++;
        }
    }
}
