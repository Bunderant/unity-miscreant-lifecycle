using NUnit.Framework;
using UnityEngine;
using System.Collections;
using UnityEngine.TestTools;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	using UpdateType = CustomUpdateManager.UpdateType;
	using ObjectToggleConfig = FakeEnvironment.ObjectToggleConfig;

	public sealed class CustomUpdateManagerTests
	{
		public const string DEFAULT_GROUP_NAME = "Default";

		private struct ExpectedUpdateCount
		{
			public int value;
			public ExpectedUpdateCount(int value) => this.value = value;
			public static implicit operator int(ExpectedUpdateCount count) => count.value;
			public override string ToString() => $"{value}";
		}

		private struct ExpectedFixedUpdateCount
		{
			public int value;
			public ExpectedFixedUpdateCount(int value) => this.value = value;
			public static implicit operator int(ExpectedFixedUpdateCount count) => count.value;
			public override string ToString() => $"{value}";
		}

		[Test]
		public void Instantiate_OneBasicManagedUpdateActive_AddedToEmptySystem()
		{
			using (FakeEnvironment env = new FakeEnvironment(DEFAULT_GROUP_NAME))
			{
				RunObjectInstantiateCountValidationTest(
					env,
					DEFAULT_GROUP_NAME,
					new ExpectedUpdateCount(1),
					new ExpectedFixedUpdateCount(0),
					ObjectToggleConfig.UpdateActiveAndEnabled
				);
			}
		}

		[Test]
		public void Instantiate_OneBasicManagedFixedUpdateActive_AddedToEmptySystem()
		{
			using (FakeEnvironment env = new FakeEnvironment(DEFAULT_GROUP_NAME))
			{
				RunObjectInstantiateCountValidationTest(
					env,
					DEFAULT_GROUP_NAME,
					new ExpectedUpdateCount(0),
					new ExpectedFixedUpdateCount(1),
					ObjectToggleConfig.FixedUpdateActiveAndEnabled
				);
			}
		}

		[Test]
		public void Instantiate_OneBasicManagedUpdateAndFixedActive_AddedToEmptySystem()
		{
			using (FakeEnvironment env = new FakeEnvironment(DEFAULT_GROUP_NAME))
			{
				RunObjectInstantiateCountValidationTest(
					env,
					DEFAULT_GROUP_NAME,
					new ExpectedUpdateCount(1),
					new ExpectedFixedUpdateCount(1),
					ObjectToggleConfig.AllActiveAndEnabled
				);
			}
		}

		[Test]
		public void Instantiate_OneBasicManagedUpdateActive_AddedToPopulatedSystem()
		{
			using (FakeEnvironment env = new FakeEnvironment(DEFAULT_GROUP_NAME))
			{
				env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
					DEFAULT_GROUP_NAME,
					ObjectToggleConfig.UpdateActiveAndEnabled);

				RunObjectInstantiateCountValidationTest(
					env,
					DEFAULT_GROUP_NAME,
					new ExpectedUpdateCount(2),
					new ExpectedFixedUpdateCount(0),
					ObjectToggleConfig.UpdateActiveAndEnabled
				);
			}
		}

		[Test]
		public void Instantiate_OneBasicManagedFixedUpdateActive_AddedToPopulatedSystem()
		{
			using (FakeEnvironment env = new FakeEnvironment(DEFAULT_GROUP_NAME))
			{
				env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
					DEFAULT_GROUP_NAME,
					ObjectToggleConfig.FixedUpdateActiveAndEnabled);

				RunObjectInstantiateCountValidationTest(
					env,
					DEFAULT_GROUP_NAME,
					new ExpectedUpdateCount(0),
					new ExpectedFixedUpdateCount(2),
					ObjectToggleConfig.FixedUpdateActiveAndEnabled
				);
			}
		}

		[Test]
		public void Instantiate_OneBasicManagedUpdateAndFixedActive_AddedToPopulatedSystem()
		{
			using (FakeEnvironment env = new FakeEnvironment(DEFAULT_GROUP_NAME))
			{
				env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
					DEFAULT_GROUP_NAME,
					ObjectToggleConfig.AllActiveAndEnabled);

				RunObjectInstantiateCountValidationTest(
					env,
					DEFAULT_GROUP_NAME,
					new ExpectedUpdateCount(2),
					new ExpectedFixedUpdateCount(2),
					ObjectToggleConfig.AllActiveAndEnabled
				);
			}
		}

		private static void RunObjectInstantiateCountValidationTest(
			FakeEnvironment env,
			string groupName,
			ExpectedUpdateCount expectedUpdateCountAfter,
			ExpectedFixedUpdateCount expectedFixedCountAfter,
			params ObjectToggleConfig[] instantiatedObjectConfigSettings)
		{
			env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				groupName,
				instantiatedObjectConfigSettings);

			AssertGroupCountForTypeEquals(env, groupName, UpdateType.Normal, expectedUpdateCountAfter);
			AssertGroupCountForTypeEquals(env, groupName, UpdateType.Fixed, expectedFixedCountAfter);
		}

		[Test]
		public void EnableComponent_OneBasicManagedUpdate_AddsToStstem()
		{
			var before = ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.Update;
			var after = before | ObjectToggleConfig.ComponentEnabled;

			ValidateComponentToggleStateChange(before, after);
		}

		[Test]
		public void EnableComponent_OneBasicManagedFixedUpdate_AddsToSystem()
		{
			var before = ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.FixedUpdate;
			var after = before | ObjectToggleConfig.ComponentEnabled;

			ValidateComponentToggleStateChange(before, after);
		}

		[Test]
		public void EnableComponent_OneBasicManagedUpdateAndFixed_AddsBothToSystem()
		{
			var before = ObjectToggleConfig.GameObjectActive | ObjectToggleConfig.Update | ObjectToggleConfig.FixedUpdate;
			var after = before | ObjectToggleConfig.ComponentEnabled;

			ValidateComponentToggleStateChange(before, after);
		}

		/// <summary>
		/// This test should ALWAYS succeed. Creates an empty initial environment and operates on the default priority group. 
		/// Expected counts before/after both instantiation and manipulation are derived from the configuration parameters.
		/// </summary>
		/// <param name="initialConfiguration"></param>
		/// <param name="finalConfiguration"></param>
		private void ValidateComponentToggleStateChange(
			ObjectToggleConfig initialConfiguration,
			ObjectToggleConfig finalConfiguration)
		{
			using (FakeEnvironment env = new FakeEnvironment(DEFAULT_GROUP_NAME))
			{
				ValidateComponentToggleStateChange(
					env,
					DEFAULT_GROUP_NAME,
					initialConfiguration,
					finalConfiguration);
			}
		}

		/// <summary>
		/// This test should ALWAYS succeed. 
		/// Expected counts before/after both instantiation and manipulation are derived from the configuration parameters.
		/// </summary>
		/// <param name="env">Mock Environment</param>
		/// <param name="groupName">Priority Group Name</param>
		/// <param name="initialConfiguration">Configuration of instantiated component/GameObject.</param>
		/// <param name="finalConfiguration">Final configuration of the component/GameObject.</param>
		private void ValidateComponentToggleStateChange(
			FakeEnvironment env,
			string groupName,
			ObjectToggleConfig initialConfiguration,
			ObjectToggleConfig finalConfiguration)
		{
			int expectedInitialUpdateCount = (
				env.GetCountForGroup(groupName, UpdateType.Normal) +
				(initialConfiguration.HasFlag(ObjectToggleConfig.UpdateActiveAndEnabled) ? 1 : 0)
			);

			int expectedInitialFixedCount = (
				env.GetCountForGroup(groupName, UpdateType.Fixed) +
				(initialConfiguration.HasFlag(ObjectToggleConfig.FixedUpdateActiveAndEnabled) ? 1 : 0)
			);

			env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				out TestBasicManagedUpdatesComponent[] components,
				DEFAULT_GROUP_NAME,
				initialConfiguration
			);

			AssertGroupCountForTypeEquals(env, groupName, UpdateType.Normal, expectedInitialUpdateCount);
			AssertGroupCountForTypeEquals(env, groupName, UpdateType.Fixed, expectedInitialFixedCount);

			// Cache expected counts as the actual counts (since we've asserted that's true) to keep things readable. 
			int initialUpdateCount = expectedInitialUpdateCount;
			int initialFixedCount = expectedInitialFixedCount;

			Debug.Log($"{initialConfiguration} AFTER INSTANTIATE:\t\tUpdate: ({initialUpdateCount})\t\tFixedUpdate: ({initialFixedCount})");

			env.SetToggleConfig(components[0], finalConfiguration);

			FakeEnvironment.GetExpectedDeltaForComponentToggleChange(
				initialConfiguration,
				finalConfiguration,
				out int updateDelta,
				out int fixedUpdateDelata);

			int expectedFinalUpdateCount = initialUpdateCount + updateDelta;
			int expectedFinalFixedCount = initialFixedCount + fixedUpdateDelata;

			AssertGroupCountForTypeEquals(env, groupName, UpdateType.Normal, expectedFinalUpdateCount);
			AssertGroupCountForTypeEquals(env, groupName, UpdateType.Fixed, expectedFinalFixedCount);

			Debug.Log($"{finalConfiguration} AFTER CHANGE:\t\tUpdate: {expectedFinalUpdateCount}\t\tFixedUpdate: {expectedFinalFixedCount}");
		}

		private static void AssertGroupCountForTypeEquals(
			FakeEnvironment env,
			string groupName,
			UpdateType updateType,
			int expectedCount)
		{
			int actualCount = env.GetCountForGroup(groupName, updateType);

			Assert.That(
				actualCount == expectedCount,
				$"Group \'{groupName}\' did not have the expected count for update type \'{updateType}\'.\n" +
				$"Expected count: {expectedCount}\n" +
				$"Actual count: {actualCount}"
			);
		}

		[UnityTest]
		public IEnumerator SelfDestruct_ManagedUpdateOnlyOneInSystem()
		{
			string groupName = DEFAULT_GROUP_NAME;
			using (FakeEnvironment env = new FakeEnvironment(groupName))
			{
				env.InstantiateManagedComponents<TestManagedUpdatesSelfDestruct>(
					groupName,
					ObjectToggleConfig.UpdateActiveAndEnabled
				);

				AssertGroupCountForTypeEquals(env, groupName, UpdateType.Normal, 1);

				env.StartUpdating();

				TestManagedUpdatesSelfDestruct.OnSelfDestruct = new UnityEngine.Events.UnityEvent();
				TestManagedUpdatesSelfDestruct.OnSelfDestruct.AddListener(HandleSelfDestruct);

				bool componentStillExists = true;
				void HandleSelfDestruct()
				{
					AssertGroupCountForTypeEquals(env, groupName, UpdateType.Normal, 0);

					TestManagedUpdatesSelfDestruct.OnSelfDestruct.RemoveListener(HandleSelfDestruct);
					componentStillExists = false;
				}

				// Make sure the component actually self-destructs. 
				float timeout = Time.time + TestManagedUpdatesSelfDestruct.DEFAULT_COUNTDOWN_DURATION * 3;
				while (componentStillExists && Time.time < timeout)
				{
					yield return new WaitForEndOfFrame();
				}

				Assert.That(Time.time < timeout, "Component did not successfully self destruct: Timed out.");
			}
		}
	}
}
