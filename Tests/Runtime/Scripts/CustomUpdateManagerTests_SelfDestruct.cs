using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;

namespace Miscreant.Lifecycle.RuntimeTests
{
	using ObjectToggleConfig = FakeEnvironment.ObjectToggleConfig;

	public sealed class CustomUpdateManagerTests_SelfDestruct
	{
		private FakeEnvironment _environment;

		[SetUp]
		public void SetUp()
		{
			_environment = new FakeEnvironment(TestData.DEFAULT_GROUP_NAME);
			_environment.StartUpdating();
		}

		[TearDown]
		public void TearDown()
		{
			_environment.Dispose();
			_environment = null;
		}

		[UnityTest]
		public IEnumerator DestroyGameObject_SelfDestructAllOneByOneFromManagedCallbacks_AllRemovedFromSystem(
			[ValueSource(typeof(TestData), nameof(TestData.allActiveTogglePermutations))] ObjectToggleConfig config)
		{
			// Arrange
			string groupName = TestData.DEFAULT_GROUP_NAME;
			_environment.InstantiateManagedComponents<TestManagedUpdatesSelfDestruct>(
				out TestManagedUpdatesSelfDestruct[] components,
				groupName,
				config,
				config // Test requires at least two components to cover different states of the callback lists
			);

			int originalComponentCount = components.Length;
			int numDestroyed = 0;

			// Store the instance ids so we can check the system for components that were theoretically destroyed. 
			var componentInstanceIds = new int[originalComponentCount];

			for (int i = 0; i < originalComponentCount; i++)
			{
				TestManagedUpdatesSelfDestruct component = components[i];
				component.OnSelfDestruct.AddListener(() => { numDestroyed++; });
				componentInstanceIds[i] = component.GetInstanceID();
			}

			// Act
			float expectedLifetime = 0.2f;
			foreach (var component in components)
			{
				component.StartCountdown(expectedLifetime);
				yield return new WaitForSeconds(expectedLifetime);
			}

			float timeout = Time.time + 0.3333f;
			while (numDestroyed < originalComponentCount)
			{
				yield return new WaitForEndOfFrame();
				if (Time.time >= timeout)
				{
					throw new System.Exception("Self destruct test timed out.");
				}
			}

			// Assert
			bool noUpdatesFound = true;
			bool noFixedUpdatesFound = true;

			foreach (var id in componentInstanceIds)
			{
				_environment.manager.CheckSystemForComponent(
					id,
					out bool updateFound,
					out bool fixedUpdateFound
				);

				noUpdatesFound &= !updateFound;
				noFixedUpdatesFound &= !fixedUpdateFound;
			}

			Assert.That(
				noUpdatesFound && noFixedUpdatesFound,
				"At least one component did not successfully self-destruct and remove itself from the update manager:\n" +
				$"Updates found? {!noUpdatesFound}\n" +
				$"FixedUpdates found? {!noFixedUpdatesFound}"
			);
		}
	}
}

