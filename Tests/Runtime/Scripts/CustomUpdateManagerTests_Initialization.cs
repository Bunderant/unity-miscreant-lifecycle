using UnityEngine;
using NUnit.Framework;

namespace Miscreant.Lifecycle.RuntimeTests
{
	public class CustomUpdateManagerTests_Initialization
	{
		[Test]
		public void SetUpdateGroups_CalledOnSystemWithUnassignedGroupList_DoesNotThrowException()
		{
			var manager = ScriptableObject.CreateInstance<CustomUpdateManager>();

			Assert.DoesNotThrow(
				() => {
					manager.SetUpdateGroups(new CustomUpdatePriority[] {
						ScriptableObject.CreateInstance<CustomUpdatePriority>()
					});
				},
				"Should be able to set priority groups on a manager that has an unassigned list."
			);
		}

		[Test]
		public void SetUpdateGroups_CalledOnSystemWithEmptyGroupList_DoesNotThrowException()
		{
			var manager = ScriptableObject.CreateInstance<CustomUpdateManager>();
			manager.SetUpdateGroups(new CustomUpdatePriority[0]);

			Assert.DoesNotThrow(
				() =>  {
					manager.SetUpdateGroups(new CustomUpdatePriority[] {
						ScriptableObject.CreateInstance<CustomUpdatePriority>()
					});
				},
				"Should be able to set priority groups on a manager that has an empty list."
			);
		}

		[Test]
		public void SetUpdateGroups_CalledOnSystemWithNonemptyGroupList_ThrowsException()
		{
			var manager = ScriptableObject.CreateInstance<CustomUpdateManager>();
			manager.SetUpdateGroups(new CustomUpdatePriority[] {
				ScriptableObject.CreateInstance<CustomUpdatePriority>()
			});

			Assert.That(
				() => {
					manager.SetUpdateGroups(new CustomUpdatePriority[] {
						ScriptableObject.CreateInstance<CustomUpdatePriority>()
					});
				},
				Throws.Exception,
				"Should NOT be able to set priority groups on a manager that already has groups assigned."
			);
		}
	}
}

