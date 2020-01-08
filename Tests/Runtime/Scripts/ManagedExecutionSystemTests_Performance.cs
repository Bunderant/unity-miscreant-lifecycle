using System;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using Unity.PerformanceTesting;

namespace Miscreant.Lifecycle.RuntimeTests
{
	public sealed class ManagedExecutionSystemTests_Performance
	{
		private const int WARMUP_FRAMES = 60;
		private const int MEASURED_FRAMES = 30;

		private static int[] _sizes = new int[] {
			1000,
			10000,
			50000
		};

		private IEnumerator RunPerformanceMeasurement()
		{
			yield return Measure.Frames().WarmupCount(WARMUP_FRAMES).MeasurementCount(MEASURED_FRAMES).Run();
		}

		[UnityTest]
		[Performance]
		public IEnumerator AllUpdates_Unmanaged([ValueSource(nameof(_sizes))] int size)
		{
			if (QualitySettings.vSyncCount != 0)
			{
				throw new ApplicationException("VSync must be disabled to run frame-based performance tests.");
			}

			var container = new GameObject("Container").transform;
			for (int i = 0; i < size; i++)
			{
				var go = new GameObject("Test", typeof(TestBasicUnityUpdatesComponent));
				go.transform.SetParent(container);
			}

			yield return RunPerformanceMeasurement();

			GameObject.DestroyImmediate(container.gameObject);
		}

		[UnityTest]
		[Performance]
		public IEnumerator AllUpdates_Managed([ValueSource(nameof(_sizes))] int size)
		{
			if (QualitySettings.vSyncCount != 0)
			{
				throw new ApplicationException("VSync must be disabled to run frame-based performance tests.");
			}
			
			var config = new FakeEnvironment.ObjectToggleConfig[size];
			for (int i = 0; i < size; i++)
			{
				config[i] = FakeEnvironment.ObjectToggleConfig.AllActiveAndEnabled;
			}

			using (FakeEnvironment env = new FakeEnvironment(TestData.DEFAULT_GROUP_NAME))
			{
				env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
					TestData.DEFAULT_GROUP_NAME,
					config
				);

				env.StartUpdating();

				yield return RunPerformanceMeasurement();
			}
		}
	}
}