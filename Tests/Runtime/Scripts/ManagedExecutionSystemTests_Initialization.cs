using UnityEngine;
using NUnit.Framework;

namespace Miscreant.Lifecycle.RuntimeTests
{
	public class ManagedExecutionSystemTests_Initialization
	{
		private ManagedExecutionSystem _system;

		[SetUp]
		public void SetUp()
		{
			_system = ScriptableObject.CreateInstance<ManagedExecutionSystem>();
		}

		[TearDown]
		public void TearDown()
		{
			Object.Destroy(_system);
			_system = null;
		}

		[Test]
		public void SetUpdateGroups_CalledOnSystemWithUnassignedGroupList_DoesNotThrowException()
		{
			Assert.DoesNotThrow(
				() => {
					_system.SetExecutionGroups(new ManagedExecutionGroup[] {
						ScriptableObject.CreateInstance<ManagedExecutionGroup>()
					});
				},
				"Should be able to set execution groups on a system that has an unassigned list."
			);
		}

		[Test]
		public void SetUpdateGroups_CalledOnSystemWithEmptyGroupList_DoesNotThrowException()
		{
			_system.SetExecutionGroups(new ManagedExecutionGroup[0]);

			Assert.DoesNotThrow(
				() =>  {
					_system.SetExecutionGroups(new ManagedExecutionGroup[] {
						ScriptableObject.CreateInstance<ManagedExecutionGroup>()
					});
				},
				"Should be able to set execution groups on a system that has an empty list."
			);
		}

		[Test]
		public void SetUpdateGroups_CalledOnSystemWithNonemptyGroupList_ThrowsException()
		{
			_system.SetExecutionGroups(new ManagedExecutionGroup[] {
				ScriptableObject.CreateInstance<ManagedExecutionGroup>()
			});

			Assert.That(
				() => {
					_system.SetExecutionGroups(new ManagedExecutionGroup[] {
						ScriptableObject.CreateInstance<ManagedExecutionGroup>()
					});
				},
				Throws.Exception,
				"Should NOT be able to set execution groups on a system that already has groups assigned."
			);
		}
	}
}

