using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace Miscreant.Utilities.Lifecycle.RuntimeTests
{
    public sealed class UpdateSystemTests
    {
        [Test]
        public void Instantiate_BasicManagedUpdateActive_AddedToSystem()
        {
			var updateManager = ScriptableObject.CreateInstance<CustomUpdateManager>();
			var defaultPriorityGroup = ScriptableObject.CreateInstance<CustomUpdatePriority>();

			updateManager.Initialize(defaultPriorityGroup);

			var component = CustomUpdateBehaviour.Create<TestBasicManagedUpdatesComponent>(
				new CustomUpdateManager.Config(updateManager, defaultPriorityGroup, true, false),
				true,
				true
			);

			Assert.That(
				updateManager.GetCountForGroup(defaultPriorityGroup, CustomUpdateManager.UpdateType.Normal) == 1,
				$"There should be exactly one {nameof(CustomUpdateBehaviour)} with UPDATE running in the system."
			);

			Object.Destroy(updateManager);
			Object.Destroy(defaultPriorityGroup);
        }
    }
}
