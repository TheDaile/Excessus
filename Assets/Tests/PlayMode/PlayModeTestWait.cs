using System;
using System.Collections;
using NUnit.Framework;
using UnityEngine;

public static class PlayModeTestWait
{
    public static IEnumerator Until(Func<bool> condition, float timeout)
    {
        double deadline = Time.realtimeSinceStartupAsDouble + timeout;

        while (!condition())
        {
            if (Time.realtimeSinceStartupAsDouble >= deadline)
            {
                Assert.Fail($"Condition was not met within {timeout:F2} seconds of real time.");
            }

            yield return null;
        }
    }
}
