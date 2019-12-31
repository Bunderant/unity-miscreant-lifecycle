namespace Miscreant.Lifecycle.RuntimeTests
{
	public sealed class CustomUpdateManagerTests_InstantiateFromPopulatedSystem : CustomUpdateManagerTests_Instantiate
	{
		protected override void SetUpEnvironment()
		{
			environment = new FakeEnvironment(CustomUpdateManagerTests.DEFAULT_GROUP_NAME);

			environment.InstantiateManagedComponents<TestBasicManagedUpdatesComponent>(
				CustomUpdateManagerTests.DEFAULT_GROUP_NAME,
				CustomUpdateManagerTests.allTogglePermutations
			);
		}
	}
}