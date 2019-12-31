using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace Miscreant.Lifecycle.RuntimeTests
{
	using ObjectToggleConfig = FakeEnvironment.ObjectToggleConfig;

	public sealed class CustomUpdateManagerTests_MultipleFrames
	{
		private FakeEnvironment _environment;

		[SetUp]
		public void SetUp()
		{
			_environment = new FakeEnvironment(CustomUpdateManagerTests.DEFAULT_GROUP_NAME);
			_environment.StartUpdating();
		}

		[TearDown]
		public void TearDown()
		{
			_environment.Dispose();
			_environment = null;
		}

		[UnityTest]
		public IEnumerator DestroyGameObject_SelfDestructFromManagedUpdate_RemovedFromSystem(
			[ValueSource(typeof(CustomUpdateManagerTests), nameof(CustomUpdateManagerTests.allActiveTogglePermutations))] ObjectToggleConfig config)
		{
			// Arrange
			string groupName = CustomUpdateManagerTests.DEFAULT_GROUP_NAME;
			_environment.InstantiateManagedComponents<TestManagedUpdatesSelfDestruct>(
				out TestManagedUpdatesSelfDestruct[] components,
				groupName,
				ObjectToggleConfig.UpdateActiveAndEnabled
			);

			TestManagedUpdatesSelfDestruct component = components[0];
			component.OnSelfDestruct.AddListener(HandleSelfDestruct);

			// Act
			float expectedLifetime = TestManagedUpdatesSelfDestruct.DEFAULT_COUNTDOWN_DURATION;
			component.StartCountdown(expectedLifetime);

			bool componentIsAlive = true;
			void HandleSelfDestruct()
			{
				componentIsAlive = false;
			}

			float timeout = Time.time + expectedLifetime + 0.3333f;
			while (componentIsAlive && Time.time < timeout)
			{
				yield return new WaitForEndOfFrame();
			}

			// Assert
			_environment.manager.CheckSystemForComponent(
				component,
				out bool updateFound,
				out bool fixedUpdateFound
			);

			Assert.That(
				!updateFound && !fixedUpdateFound,
				"Component did not successfully self-destruct and remove itself from the update manager:\n" +
				$"Update expected/found: {false}/{updateFound}\n" +
				$"FixedUpdate expected/found: {false}/{fixedUpdateFound}"
			);
		}
	}
}

