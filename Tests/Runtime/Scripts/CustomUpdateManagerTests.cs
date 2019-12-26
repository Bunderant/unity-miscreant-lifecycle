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

		private static void AssertGroupCountForTypeEquals(
			FakeEnvironment env,
			string groupName,
			UpdateType updateType,
			int expectedCount)
		{
			int actualCount = (int)env.GetCountForGroup(groupName, updateType);

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
