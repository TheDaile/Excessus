using System;

public static class StatValidator
{
    public static void RequirePositive(float value, string valueName, string operationName)
    {
        if (value > 0f)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(
            valueName,
            value,
            $"{operationName} received invalid {valueName}. Value must be greater than 0."
        );
    }

    public static void RequireNotNegative(float value, string valueName, string operationName)
    {
        if (value >= 0f)
        {
            return;
        }

        throw new ArgumentOutOfRangeException(
            valueName,
            value,
            $"{operationName} received invalid {valueName}. Value cannot be negative."
        );
    }
}