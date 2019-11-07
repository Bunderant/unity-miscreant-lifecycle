using NUnit.Framework;
using UnityEngine;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine.TestTools;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
	public sealed class UpdateSystemTests
	{
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
			}

			public void InstantiateManagedComponents<T>(string groupName, params MockObjectToggleConfig[] toggleConfig) where T : CustomUpdateBehaviour
			{
				var group = priorities[groupName];
				foreach (var config in toggleConfig)
				{
					CustomUpdateBehaviour.Create<T>(
						new CustomUpdateManager.Config(
							manager,
							group,
							config.HasFlag(MockObjectToggleConfig.Update),
							config.HasFlag(MockObjectToggleConfig.FixedUpdate)),
						config.HasFlag(MockObjectToggleConfig.GameObjectActive),
						config.HasFlag(MockObjectToggleConfig.ComponentEnabled)
					);
				}
			}

			public void Dispose()
			{
				Object.Destroy(manager);

				foreach (var kvp in priorities)
				{
					Object.Destroy(kvp.Value);
				}
			}
		}

		[Test]
		public void Instantiate_BasicManagedUpdateActive_AddedToSystem()
		{
			RunSingleObjectTest(MockObjectToggleConfig.UpdateActiveAndEnabled, 1, 0);
		}

		[Test]
		public void Instantiate_BasicManagedFixedUpdateActive_AddedToSystem()
		{
			RunSingleObjectTest(MockObjectToggleConfig.FixedUpdateActiveAndEnabled, 0, 1);
		}

		[Test]
		public void Instantiate_BasicManagedUpdateAndFixedActive_AddedToSystem()
		{
			RunSingleObjectTest(MockObjectToggleConfig.AllActiveAndEnabled, 1, 1);
		}

		private void RunSingleObjectTest(MockObjectToggleConfig toggleConfig, int expectedUpdateCount, int expectedFixedCount)
		{
			string groupName = "Default";
			using (MockEnvironment env = new MockEnvironment(groupName))
			{
				env.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
					groupName,
					toggleConfig
				);

				Assert.That(
					env.manager.GetCountForGroup(env.priorities[groupName], CustomUpdateManager.UpdateType.Normal) == expectedUpdateCount &&
					env.manager.GetCountForGroup(env.priorities[groupName], CustomUpdateManager.UpdateType.Fixed) == expectedFixedCount,
					$"There should be exactly one {nameof(CustomUpdateBehaviour)} with {toggleConfig} running in the system."
				);
			}
		}
	}
}
