using NUnit.Framework;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.TestTools;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	public sealed class UpdateSystemTests
	{
		public const string DEFAULT_GROUP_NAME = "Default";

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

			public void InstantiateManagedComponents<T>(string groupName, params MockObjectToggleConfig[] toggleConfig) where T : CustomUpdateBehaviour
			{
				CustomUpdatePriority group = priorities[groupName];
				Transform containerTransform = _runtimeController.transform;

				foreach (MockObjectToggleConfig config in toggleConfig)
				{
					CustomUpdateBehaviour.Create<T>(
						new CustomUpdateManager.Config(
							manager,
							group,
							config.HasFlag(MockObjectToggleConfig.Update),
							config.HasFlag(MockObjectToggleConfig.FixedUpdate)),
						config.HasFlag(MockObjectToggleConfig.GameObjectActive),
						config.HasFlag(MockObjectToggleConfig.ComponentEnabled),
						containerTransform
					);
				}
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
		public void Instantiate_BasicManagedUpdateActive_AddedToEmptySystem()
		{
			RunSingleObjectTest(MockObjectToggleConfig.UpdateActiveAndEnabled, 1, 0);
		}

		[Test]
		public void Instantiate_BasicManagedFixedUpdateActive_AddedToEmptySystem()
		{
			RunSingleObjectTest(MockObjectToggleConfig.FixedUpdateActiveAndEnabled, 0, 1);
		}

		[Test]
		public void Instantiate_BasicManagedUpdateAndFixedActive_AddedToEmptySystem()
		{
			RunSingleObjectTest(MockObjectToggleConfig.AllActiveAndEnabled, 1, 1);
		}

		private void RunSingleObjectTest(MockObjectToggleConfig toggleConfig, int expectedUpdateCount, int expectedFixedCount)
		{
			string groupName = DEFAULT_GROUP_NAME;
			using (MockEnvironment env = new MockEnvironment(groupName))
			{
				env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
					groupName,
					toggleConfig
				);

				AssertGroupCountForTypeEquals(env, groupName, CustomUpdateManager.UpdateType.Normal, expectedUpdateCount);
				AssertGroupCountForTypeEquals(env, groupName, CustomUpdateManager.UpdateType.Fixed, expectedFixedCount);
			}
		}

		private static void AssertGroupCountForTypeEquals(
			MockEnvironment env,
			string groupName,
			CustomUpdateManager.UpdateType updateType,
			int expectedCount)
		{
			int actualCount = env.manager.GetCountForGroup(env.priorities[groupName], updateType);

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

				AssertGroupCountForTypeEquals(env, groupName, CustomUpdateManager.UpdateType.Normal, 1);

				env.StartUpdating();

				TestManagedUpdatesSelfDestruct.OnSelfDestruct = new UnityEngine.Events.UnityEvent();
				TestManagedUpdatesSelfDestruct.OnSelfDestruct.AddListener(HandleSelfDestruct);

				bool componentStillExists = true;
				void HandleSelfDestruct()
				{
					AssertGroupCountForTypeEquals(env, groupName, CustomUpdateManager.UpdateType.Normal, 0);

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
