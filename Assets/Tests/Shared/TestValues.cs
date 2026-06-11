public static class TestValues
{
    public const float Tolerance = 0.0001f;

    public const float Empty = 0f;
    public const float InvalidZeroAmount = 0f;
    public const float InvalidNegativeAmount = -1f;

    public const float SmallRatio = 0.1f;
    public const float QuarterRatio = 0.25f;
    public const float HalfRatio = 0.5f;
    public const float FullRatio = 1f;
    public const float DoubleRatio = 2f;

    public const float OneSecond = 1f;
    public const float HalfSecond = 0.5f;
    public const float TwoSeconds = 2f;
    public const float ShortDuration = 0.1f;
    public const float TimeBeforeRegenerationDelay = HalfSecond;

    public const float ExtraDamage = 1f;
    public const float ValueAboveMaxOffset = 1f;
    public const float InteractionDistance = 2f;
    public const float IsolatedSceneOffset = 1000f;
    public const float MovementSpawnOffset = 2f;
    public const float TestPlayerSpeed = 1f;
    public const float SprintMultiplier = 2f;
    public const float InsufficientAmountMultiplier = 2f;
    public const float ConditionTimeout = 1f;

    public const int NoCalls = 0;
    public const int OneCall = 1;
    public const int TwoCalls = 2;
    public const int InteractionTestLayer = 30;
    public const int InteractionTestLayerMask = 1 << InteractionTestLayer;
}
