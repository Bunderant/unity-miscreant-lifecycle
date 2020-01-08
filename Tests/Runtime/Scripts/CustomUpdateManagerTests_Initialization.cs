using UnityEngine;
using NUnit.Framework;

namespace Miscreant.Lifecycle.RuntimeTests
{
	public class CustomUpdateManagerTests_Initialization
	{
		private CustomUpdateManager _manager;

		[SetUp]
		public void SetUp()
		{
			_manager = ScriptableObject.CreateInstance<CustomUpdateManager>();
		}

		[TearDown]
		public void TearDown()
		{
			Object.Destroy(_manager);
			_manager = null;
		}

		[Test]
		public void SetUpdateGroups_CalledOnSystemWithUnassignedGroupList_DoesNotThrowException()
		{
			Assert.DoesNotThrow(
				() => {
					_manager.SetExecutionGroups(new ManagedExecutionGroup[] {
						ScriptableObject.CreateInstance<ManagedExecutionGroup>()
					});
				},
				"Should be able to set execution groups on a manager that has an unassigned list."
			);
		}

		[Test]
		public void SetUpdateGroups_CalledOnSystemWithEmptyGroupList_DoesNotThrowException()
		{
			_manager.SetExecutionGroups(new ManagedExecutionGroup[0]);

			Assert.DoesNotThrow(
				() =>  {
					_manager.SetExecutionGroups(new ManagedExecutionGroup[] {
						ScriptableObject.CreateInstance<ManagedExecutionGroup>()
					});
				},
				"Should be able to set execution groups on a manager that has an empty list."
			);
		}

		[Test]
		public void SetUpdateGroups_CalledOnSystemWithNonemptyGroupList_ThrowsException()
		{
			_manager.SetExecutionGroups(new ManagedExecutionGroup[] {
				ScriptableObject.CreateInstance<ManagedExecutionGroup>()
			});

			Assert.That(
				() => {
					_manager.SetExecutionGroups(new ManagedExecutionGroup[] {
						ScriptableObject.CreateInstance<ManagedExecutionGroup>()
					});
				},
				Throws.Exception,
				"Should NOT be able to set execution groups on a manager that already has groups assigned."
			);
		}
	}
}

