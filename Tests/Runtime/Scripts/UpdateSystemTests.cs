using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.TestTools;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	using UpdateType = CustomUpdateManager.UpdateType;

	public sealed class UpdateSystemTests
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

		[System.Flags]
		private enum MockObjectToggleConfig : int
		{
			None = 0,
			GameObjectActive = 1,
			ComponentEnabled = 2,
			Update = 4,
			FixedUpdate = 8,

			AllActiveAndEnabled = GameObjectActive | ComponentEnabled | Update | FixedUpdate,
			UpdateActiveAndEnabled = AllActiveAndEnabled & ~FixedUpdate,
			FixedUpdateActiveAndEnabled = AllActiveAndEnabled & ~Update,
			NoUpdatesActiveAndEnabled = GameObjectActive | ComponentEnabled
		}

		private sealed class MockEnvironment : System.IDisposable
		{
			public readonly CustomUpdateManager manager;
			public readonly ReadOnlyDictionary<string, CustomUpdatePriority> priorities;
			private readonly TestManagedUpdatesSceneController _runtimeController;

			public MockEnvironment(params string[] priorityGroupNames)
			{
				int groupCount = priorityGroupNames.Length;

				if (groupCount == 0)
				{
					throw new System.ArgumentException("Mock update system enviromnent must have at least one priority group.");
				}

				var priorityGroups = new CustomUpdatePriority[groupCount];
				var priorityLookup = new Dictionary<string, CustomUpdatePriority>(groupCount);
				for (int i = 0; i < groupCount; i++)
				{
					var currentGroup = priorityGroups[i] = ScriptableObject.CreateInstance<CustomUpdatePriority>();
					priorityLookup.Add(priorityGroupNames[i], currentGroup);
				}
				priorities = new ReadOnlyDictionary<string, CustomUpdatePriority>(priorityLookup);

				manager = ScriptableObject.CreateInstance<CustomUpdateManager>();
				manager.Initialize(priorityGroups);

				_runtimeController = new GameObject("Runtime Controller").AddComponent<TestManagedUpdatesSceneController>();
				_runtimeController.enabled = false;
			}

			public static void GetExpectedDeltaForComponentToggleChange(
				MockObjectToggleConfig before,
				MockObjectToggleConfig after,
				out int updateDelta,
				out int fixedUpdateDelta)
			{
				updateDelta = 0;
				if (before.HasFlag(MockObjectToggleConfig.UpdateActiveAndEnabled) &&
					!after.HasFlag(MockObjectToggleConfig.UpdateActiveAndEnabled))
				{
					updateDelta = -1;
				}
				else if (!before.HasFlag(MockObjectToggleConfig.UpdateActiveAndEnabled) &&
					after.HasFlag(MockObjectToggleConfig.UpdateActiveAndEnabled))
				{
					updateDelta = 1;
				}

				fixedUpdateDelta = 0;
				if (before.HasFlag(MockObjectToggleConfig.FixedUpdateActiveAndEnabled) &&
					!after.HasFlag(MockObjectToggleConfig.FixedUpdateActiveAndEnabled))
				{
					fixedUpdateDelta = -1;
				}
				else if (!before.HasFlag(MockObjectToggleConfig.FixedUpdateActiveAndEnabled) &&
					after.HasFlag(MockObjectToggleConfig.FixedUpdateActiveAndEnabled))
				{
					fixedUpdateDelta = 1;
				}
			}

			public void InstantiateManagedComponents<T>(string groupName, params MockObjectToggleConfig[] toggleConfig) where T : CustomUpdateBehaviour
			{
				CustomUpdatePriority group = priorities[groupName];
				Transform containerTransform = _runtimeController.transform;

				foreach (MockObjectToggleConfig config in toggleConfig)
				{
					InstantiateManagedUpdateGameObject<T>(group, config, containerTransform);
				}
			}

			public void InstantiateManagedComponents<T>(
				out T[] components,
				string groupName,
				params MockObjectToggleConfig[] toggleConfig) where T : CustomUpdateBehaviour
			{
				CustomUpdatePriority group = priorities[groupName];
				Transform containerTransform = _runtimeController.transform;

				components = new T[toggleConfig.Length];
				for (int i = 0; i < toggleConfig.Length; i++)
				{
					components[i] = InstantiateManagedUpdateGameObject<T>(group, toggleConfig[i], containerTransform);
				}
			}

			public int GetCountForGroup(string groupName, UpdateType updateType)
			{
				return manager.GetCountForGroup(priorities[groupName], updateType);
			}

			private T InstantiateManagedUpdateGameObject<T>(
				CustomUpdatePriority group,
				MockObjectToggleConfig config,
				Transform parent = null) where T : CustomUpdateBehaviour
			{
				return CustomUpdateBehaviour.Create<T>(
					new CustomUpdateManager.Config(
						manager,
						group,
						config.HasFlag(MockObjectToggleConfig.Update),
						config.HasFlag(MockObjectToggleConfig.FixedUpdate)),
					config.HasFlag(MockObjectToggleConfig.GameObjectActive),
					config.HasFlag(MockObjectToggleConfig.ComponentEnabled),
					parent
				);
			}

			public void SetToggleConfigForManagedComponent(CustomUpdateBehaviour component, MockObjectToggleConfig toggleConfig)
			{
				component.gameObject.SetActive(toggleConfig.HasFlag(MockObjectToggleConfig.GameObjectActive));
				component.enabled = toggleConfig.HasFlag(MockObjectToggleConfig.ComponentEnabled);
				component.updateConfig.update = toggleConfig.HasFlag(MockObjectToggleConfig.Update);
				component.updateConfig.fixedUpdate = toggleConfig.HasFlag(MockObjectToggleConfig.FixedUpdate);
			}

			public void StartUpdating()
			{
				_runtimeController.OnMonoBehaviourUpdate = (() =>
				{
					manager.RunUpdate();
				});

				_runtimeController.OnMonoBehaviourFixedUpdate = (() =>
				{
					manager.RunFixedUpdate();
				});

				_runtimeController.enabled = true;
			}

			public void StopUpdating()
			{
				_runtimeController.enabled = false;
				_runtimeController.OnMonoBehaviourUpdate = null;
				_runtimeController.OnMonoBehaviourFixedUpdate = null;
			}

			public void Dispose()
			{
				Object.Destroy(_runtimeController.gameObject);
				Object.Destroy(manager);

				foreach (var kvp in priorities)
				{
					Object.Destroy(kvp.Value);
				}
			}
		}

		[Test]
		public void Instantiate_OneBasicManagedUpdateActive_AddedToEmptySystem()
		{
			using (MockEnvironment env = new MockEnvironment(DEFAULT_GROUP_NAME))
			{
				RunObjectInstantiateCountValidationTest(
					env,
					DEFAULT_GROUP_NAME,
					new ExpectedUpdateCount(1),
					new ExpectedFixedUpdateCount(0),
					MockObjectToggleConfig.UpdateActiveAndEnabled
				);
			}
		}

		[Test]
		public void Instantiate_OneBasicManagedFixedUpdateActive_AddedToEmptySystem()
		{
			using (MockEnvironment env = new MockEnvironment(DEFAULT_GROUP_NAME))
			{
				RunObjectInstantiateCountValidationTest(
					env,
					DEFAULT_GROUP_NAME,
					new ExpectedUpdateCount(0),
					new ExpectedFixedUpdateCount(1),
					MockObjectToggleConfig.FixedUpdateActiveAndEnabled
				);
			}
		}

		[Test]
		public void Instantiate_OneBasicManagedUpdateAndFixedActive_AddedToEmptySystem()
		{
			using (MockEnvironment env = new MockEnvironment(DEFAULT_GROUP_NAME))
			{
				RunObjectInstantiateCountValidationTest(
					env,
					DEFAULT_GROUP_NAME,
					new ExpectedUpdateCount(1),
					new ExpectedFixedUpdateCount(1),
					MockObjectToggleConfig.AllActiveAndEnabled
				);
			}
		}

		[Test]
		public void Instantiate_OneBasicManagedUpdateActive_AddedToPopulatedSystem()
		{
			using (MockEnvironment env = new MockEnvironment(DEFAULT_GROUP_NAME))
			{
				env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
					DEFAULT_GROUP_NAME,
					MockObjectToggleConfig.UpdateActiveAndEnabled);

				RunObjectInstantiateCountValidationTest(
					env,
					DEFAULT_GROUP_NAME,
					new ExpectedUpdateCount(2),
					new ExpectedFixedUpdateCount(0),
					MockObjectToggleConfig.UpdateActiveAndEnabled
				);
			}
		}

		[Test]
		public void Instantiate_OneBasicManagedFixedUpdateActive_AddedToPopulatedSystem()
		{
			using (MockEnvironment env = new MockEnvironment(DEFAULT_GROUP_NAME))
			{
				env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
					DEFAULT_GROUP_NAME,
					MockObjectToggleConfig.FixedUpdateActiveAndEnabled);

				RunObjectInstantiateCountValidationTest(
					env,
					DEFAULT_GROUP_NAME,
					new ExpectedUpdateCount(0),
					new ExpectedFixedUpdateCount(2),
					MockObjectToggleConfig.FixedUpdateActiveAndEnabled
				);
			}
		}

		[Test]
		public void Instantiate_OneBasicManagedUpdateAndFixedActive_AddedToPopulatedSystem()
		{
			using (MockEnvironment env = new MockEnvironment(DEFAULT_GROUP_NAME))
			{
				env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
					DEFAULT_GROUP_NAME,
					MockObjectToggleConfig.AllActiveAndEnabled);

				RunObjectInstantiateCountValidationTest(
					env,
					DEFAULT_GROUP_NAME,
					new ExpectedUpdateCount(2),
					new ExpectedFixedUpdateCount(2),
					MockObjectToggleConfig.AllActiveAndEnabled
				);
			}
		}

		private static void RunObjectInstantiateCountValidationTest(
			MockEnvironment env,
			string groupName,
			ExpectedUpdateCount expectedUpdateCountAfter,
			ExpectedFixedUpdateCount expectedFixedCountAfter,
			params MockObjectToggleConfig[] instantiatedObjectConfigSettings)
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
			var before = MockObjectToggleConfig.GameObjectActive | MockObjectToggleConfig.Update;
			var after = before | MockObjectToggleConfig.ComponentEnabled;

			ValidateComponentToggleStateChange(before, after);
		}

		[Test]
		public void EnableComponent_OneBasicManagedFixedUpdate_AddsToSystem()
		{
			var before = MockObjectToggleConfig.GameObjectActive | MockObjectToggleConfig.FixedUpdate;
			var after = before | MockObjectToggleConfig.ComponentEnabled;

			ValidateComponentToggleStateChange(before, after);
		}

		[Test]
		public void EnableComponent_OneBasicManagedUpdateAndFixed_AddsBothToSystem()
		{
			var before = MockObjectToggleConfig.GameObjectActive | MockObjectToggleConfig.Update | MockObjectToggleConfig.FixedUpdate;
			var after = before | MockObjectToggleConfig.ComponentEnabled;

			ValidateComponentToggleStateChange(before, after);
		}

		/// <summary>
		/// This test should ALWAYS succeed. Creates an empty initial environment and operates on the default priority group. 
		/// Expected counts before/after both instantiation and manipulation are derived from the configuration parameters.
		/// </summary>
		/// <param name="initialConfiguration"></param>
		/// <param name="finalConfiguration"></param>
		private void ValidateComponentToggleStateChange(
			MockObjectToggleConfig initialConfiguration,
			MockObjectToggleConfig finalConfiguration)
		{
			using (MockEnvironment env = new MockEnvironment(DEFAULT_GROUP_NAME))
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
			MockEnvironment env,
			string groupName,
			MockObjectToggleConfig initialConfiguration,
			MockObjectToggleConfig finalConfiguration)
		{
			int expectedInitialUpdateCount = (
				env.GetCountForGroup(groupName, UpdateType.Normal) +
				(initialConfiguration.HasFlag(MockObjectToggleConfig.UpdateActiveAndEnabled) ? 1 : 0)
			);

			int expectedInitialFixedCount = (
				env.GetCountForGroup(groupName, UpdateType.Fixed) +
				(initialConfiguration.HasFlag(MockObjectToggleConfig.FixedUpdateActiveAndEnabled) ? 1 : 0)
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

			env.SetToggleConfigForManagedComponent(components[0], finalConfiguration);

			MockEnvironment.GetExpectedDeltaForComponentToggleChange(
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
			MockEnvironment env,
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
			using (MockEnvironment env = new MockEnvironment(groupName))
			{
				env.InstantiateManagedComponents<TestManagedUpdatesSelfDestruct>(
					groupName,
					MockObjectToggleConfig.UpdateActiveAndEnabled
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
